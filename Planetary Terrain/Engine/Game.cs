using System;
using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;

namespace Planetary_Terrain {
    static class Input {
        public static Vector2 MousePos, LastMousePos;
        public static DInput.KeyboardState ks, lastks;
        public static DInput.MouseState ms, lastms;
        public static bool MouseBlocked;
    }
    class Game : IDisposable {
        #region game management
        private RenderForm renderForm;
        private bool resizePending;

        public Renderer renderer;
        
        private Stopwatch gameTimer;
        private Stopwatch frameTimer;
        #endregion
        #region input
        DInput.Keyboard keyboard;
        DInput.Mouse mouse;
        
        Vector2 realMousePos;
        #endregion

        public UI.Frame ControlPanel;
        Player player;
        Skybox skybox;

        int frameCount = 0;
        double frameTime = 0;

        public Game() {
            renderForm = new RenderForm("D3D11 Planets");
            renderForm.MouseMove += (object sender, System.Windows.Forms.MouseEventArgs e) => {
                realMousePos = new Vector2(e.Location.X, e.Location.Y);
            };
            renderForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            renderForm.AllowUserResizing = true;
            renderForm.ClientSizeChanged += (object sender, EventArgs e) => {
                resizePending = true;
            };

            DInput.DirectInput directInput = new DInput.DirectInput();
            keyboard = new DInput.Keyboard(directInput);
            mouse = new DInput.Mouse(directInput);

            keyboard.Acquire();
            mouse.Acquire();
            
            renderer = new Renderer(this, renderForm);
            
            Shaders.Load(renderer.Device, renderer.Context);
            Resources.Load(renderer.Device);

            Initialize();
        }
        public void Run() {
            gameTimer = Stopwatch.StartNew();
            frameTimer = new Stopwatch();
            
            RenderLoop.Run(renderForm, () => {
                double deltaTime = frameTimer.ElapsedTicks / (double)Stopwatch.Frequency;
                frameTimer.Restart();

                frameTime += deltaTime;
                frameCount++;
                if (frameTime > 1) {
                    frameTime = 0;
                    Debug.FPS = frameCount;
                    frameCount = 0;
                }

                Debug.BeginFrame();
                Profiler p = Profiler.Begin("Frame");

                Profiler.Begin("Update");
                Update(deltaTime);
                Profiler.End();

                Profiler.Begin("Draw");
                Draw();
                Profiler.End();
                
                Profiler.End();
                Debug.EndFrame(p.Stopwatch.Elapsed.TotalSeconds);

                if (renderer.DrawGUI) {
                    renderer.D2DContext.BeginDraw();
                    Debug.Draw2D(renderer, p);
                    renderer.D2DContext.EndDraw();
                }
                renderer.Present();
            });
        }
        
        void Initialize() {
            skybox = new Skybox("Data/Textures/Background", renderer.Device);
            StarSystem.ActiveSystem = new StarSystem(renderer.Device);

            player = new Player();
            player.Camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            player.Vehicle = new Ship(renderer.Device);
            player.Vehicle.CockpitCameraPosition = new Vector3(0, 5.5f, 7.5f);
            player.DisablePhysics = true;
            renderer.Camera = player.Camera;
            StarSystem.ActiveSystem.physics.AddBody(player);
            StarSystem.ActiveSystem.physics.AddBody(player.Vehicle);

            #region build UI
            float h = 35;
            float cpanelWidth = 235;
            RawRectangleF bounds = new RawRectangleF(0, 300, cpanelWidth, 300 + h);
            ControlPanel = new UI.Frame(null, "Panel1", bounds, renderer.CreateBrush(new Color(.5f, .5f, .5f, .5f)));
            ControlPanel.Draggable = true;

            new UI.TextLabel(ControlPanel, "Title", new RawRectangleF(0, 0, 235, h), "NAVIGATOR", renderer.SegoeUI24, renderer.Brushes["White"]);
            float y = h;
            Vector3d d = Vector3d.Normalize(new Vector3d(0, 1, -.45));
            foreach (CelestialBody p in StarSystem.ActiveSystem.bodies) {
                new UI.TextLabel(ControlPanel, p.Name + "label", new RawRectangleF(2, y, cpanelWidth-2, y + h - 2), p.Name, renderer.SegoeUI24, renderer.Brushes["White"]);
                y += 30;
                new UI.TextButton(ControlPanel, p.Name + "SurfaceButton", new RawRectangleF(5, y, (cpanelWidth - 10) * .333f - 1, y + h), "Surface", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    () => {
                        double hh = p.GetHeight(d);
                        if (p is Planet) {
                            Planet planet = p as Planet;
                            if (planet.HasOcean) {
                                hh = Math.Max(hh, planet.Radius + planet.TerrainHeight * planet.OceanHeight);
                            }
                        }
                        player.Teleport(p.Position + d * (hh + 50));
                    });
                new UI.TextButton(ControlPanel, p.Name + "LowOrbitButton", new RawRectangleF((cpanelWidth - 10) * .333f + 1, y, (cpanelWidth - 10) * .666f - 1, y + h), "Low Orbit", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    ()=> {
                        player.Teleport(p.Position + d * (p.SOI + 1000));
                    });
                new UI.TextButton(ControlPanel, p.Name + "HighOrbitButton", new RawRectangleF((cpanelWidth - 10) * .666f + 1, y, cpanelWidth - 2, y + h), "High Orbit", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    () => {
                        player.Teleport(p.Position + d * p.SOI * 3);
                    });
                y += h + 2;
            }
            bounds.Bottom = bounds.Top + y;
            
            ControlPanel.LocalBounds = bounds;
            #endregion

            (ControlPanel["EarthSurfaceButton"] as UI.TextButton).Click();
        }
        
        void Update(double deltaTime) {
            Profiler.Begin("Input Processing");
            #region input state update
            Input.ks = keyboard.GetCurrentState();
            Input.ms = mouse.GetCurrentState();
            if (Input.lastks == null) Input.lastks = Input.ks;
            if (Input.lastms == null) Input.lastms = Input.ms;
            Input.MousePos = realMousePos;
            #endregion
            
            Input.MouseBlocked = ControlPanel.Contains(Input.MousePos.X, Input.MousePos.Y);

            if (Input.ks.IsPressed(DInput.Key.F1) && !Input.lastks.IsPressed(DInput.Key.F1))
                renderer.DrawWireframe = !renderer.DrawWireframe;
            if (Input.ks.IsPressed(DInput.Key.F2) && !Input.lastks.IsPressed(DInput.Key.F2))
                renderer.DrawGUI = !renderer.DrawGUI;
            if (Input.ks.IsPressed(DInput.Key.F3) && !Input.lastks.IsPressed(DInput.Key.F3))
                Debug.DrawDebug = !Debug.DrawDebug;
            if (Input.ks.IsPressed(DInput.Key.F4) && !Input.lastks.IsPressed(DInput.Key.F4))
                Debug.DrawBoundingBoxes = !Debug.DrawBoundingBoxes;

            player.UpdateInput(deltaTime);

            if (player.FirstPerson)
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientRectangle.X + renderForm.ClientSize.Width / 2, renderForm.ClientRectangle.Y + renderForm.ClientSize.Height / 2);
            Profiler.End();
            Profiler.Begin("LOD Update");
            StarSystem.ActiveSystem.UpdateLOD(renderer, renderer.Device, deltaTime);
            Profiler.End();
            Profiler.Begin("Generation Queue Update");
            QuadNode.Update();
            Profiler.End();
            Profiler.Begin("Physics Update");
            StarSystem.ActiveSystem.physics.Update(deltaTime);
            Profiler.End();

            ControlPanel.Update((float)deltaTime);
            
            #region input state update
            Input.lastks = Input.ks;
            Input.lastms = Input.ms;
            Input.LastMousePos = Input.MousePos;
            #endregion
        }
        void Draw() {
            if (resizePending) {
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);
                resizePending = false;
            }
            renderer.TotalTime = gameTimer.Elapsed.TotalSeconds;
            renderer.BeginDrawFrame();
            renderer.Clear(Color.White);

            // 3d
            Profiler.Begin("3d Draw");
            skybox.Draw(renderer);
            StarSystem.ActiveSystem.physics.Draw(renderer);
            Profiler.End();

            Debug.Draw3D(renderer); // act like debug draws don't take a toll on performance
            
            // 2d
            Profiler.Begin("2d Draw");
            if (renderer.DrawGUI) {
                renderer.D2DContext.BeginDraw();
                StarSystem.ActiveSystem.DrawPlanetHudIcons(renderer, player.Velocity.Length());
                player.DrawHUD(renderer);
                ControlPanel.Draw(renderer);
                renderer.D2DContext.EndDraw();
            }
            Profiler.End();
        }

        public void Dispose() {
            // scene stuff
            StarSystem.ActiveSystem.Dispose();
            skybox.Dispose();
            player?.Dispose();
            ControlPanel.Dispose();

            Shaders.Dispose();
            Resources.Dispose();
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
