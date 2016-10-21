using System;
using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;

namespace Planetary_Terrain {
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

        public UI.InputState InputState;
        Vector2 realMousePos;
        bool lockMouse;
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
            
            Shaders.LoadShaders(renderer.Device, renderer.Context);

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
                Debug.EndFrame();

                if (renderer.DrawGUI) {
                    renderer.D2DContext.BeginDraw();
                    Debug.Draw(renderer, p);
                    renderer.D2DContext.EndDraw();
                }
                renderer.Present();
            });
        }
        
        void Initialize() {
            skybox = new Skybox("Data/Textures/EmptySpace.dds", renderer.Device);
            StarSystem.ActiveSystem = new StarSystem(renderer.Device);

            player = new Player();
            player.Camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            player.Vehicle = new Ship(renderer.Device);
            player.DisablePhysics = true;
            renderer.Camera = player.Camera;
            StarSystem.ActiveSystem.physics.AddBody(player);
            StarSystem.ActiveSystem.physics.AddBody(player.Vehicle);

            #region build UI
            float h = 35;
            RawRectangleF bounds = new RawRectangleF(0, 300, 235, 300 + h);
            ControlPanel = new UI.Frame(null, "Panel1", bounds, renderer.CreateBrush(new Color(.5f, .5f, .5f, .5f)));
            ControlPanel.Draggable = true;

            new UI.TextLabel(ControlPanel, "Title", new RawRectangleF(0, 0, 235, h), "NAVIGATOR", renderer.SegoeUI24, renderer.Brushes["White"]);
            float y = h;
            foreach (CelestialBody p in StarSystem.ActiveSystem.bodies) {
                Vector3d d = Vector3d.Normalize(new Vector3d(0, 1, -.45));
                new UI.TextButton(ControlPanel, p.Name + "Button", new RawRectangleF(5, y, 170, y + h-2), p.Name, renderer.SegoeUI24, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    ()=> {
                        player.Teleport(p.Position + d * (p.SOI + 1000));
                    });
                new UI.TextButton(ControlPanel, p.Name + "SfcButton", new RawRectangleF(175, y, 230, y + h - 2), "Surface", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
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
                y += h;
                bounds.Bottom += h;
            }
            
            ControlPanel.LocalBounds = bounds;
            #endregion

            (ControlPanel["EarthSfcButton"] as UI.TextButton).Click();
        }
        
        void Update(double deltaTime) {
            #region input state update
            InputState.ks = keyboard.GetCurrentState();
            InputState.ms = mouse.GetCurrentState();
            if (InputState.lastks == null) InputState.lastks = InputState.ks;
            if (InputState.lastms == null) InputState.lastms = InputState.ms;
            InputState.mousePos = realMousePos;
            #endregion

            if (InputState.ks.IsPressed(DInput.Key.F2) && !InputState.lastks.IsPressed(DInput.Key.F2))
                renderer.DrawGUI = !renderer.DrawGUI;
            
            if (InputState.ks.IsPressed(DInput.Key.F1) && !InputState.lastks.IsPressed(DInput.Key.F1))
                renderer.DrawWireframe = !renderer.DrawWireframe;

            Profiler.Begin("Player Update");
            player.UpdateInput(InputState, deltaTime);
            if (player.FirstPerson)
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientRectangle.X + renderForm.ClientSize.Width / 2, renderForm.ClientRectangle.Y + renderForm.ClientSize.Height / 2);
            Profiler.End();
            Profiler.Begin("StarSystem Update");
            StarSystem.ActiveSystem.UpdateLOD(renderer, renderer.Device, deltaTime);
            Profiler.End();
            Profiler.Begin("QuadNode Update");
            QuadNode.Update();
            Profiler.End();
            Profiler.Begin("Physics Update");
            StarSystem.ActiveSystem.physics.Update(deltaTime);
            Profiler.End();
            Profiler.Begin("ControlPanel Update");
            ControlPanel.Update((float)deltaTime, InputState);
            Profiler.End();
            
            #region input state update
            InputState.lastks = InputState.ks;
            InputState.lastms = InputState.ms;
            InputState.lastMousePos = InputState.mousePos;
            #endregion
        }
        void Draw() {
            if (resizePending)
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);
            renderer.TotalTime = gameTimer.Elapsed.TotalSeconds;

            renderer.BeginDrawFrame();
            renderer.Clear(Color.Black);

            renderer.Camera = player.Camera;
            // 3d
            Profiler.Begin("3d Draw");
            skybox.Draw(renderer);
            StarSystem.ActiveSystem.physics.Draw(renderer);
            Profiler.End();

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
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            renderer.Dispose();
            renderForm.Dispose();
        }
    }
}
