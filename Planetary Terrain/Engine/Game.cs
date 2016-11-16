using System;
using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX.Direct3D;

namespace Planetary_Terrain {
    static class Input {
        public static Vector2 MousePos, LastMousePos;
        public static DInput.KeyboardState ks, lastks;
        public static DInput.MouseState ms, lastms;
        public static bool MouseBlocked;
        public static Vector3d MouseRayOrigin;
        public static Vector3d MouseRayDirection;
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

        double TimeWarp = 1;

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
                #region input state update
                if (renderForm.Focused) {
                    Input.ks = keyboard.GetCurrentState();
                    if (Input.lastks == null) Input.lastks = Input.ks;
                }
                Input.ms = mouse.GetCurrentState();
                if (Input.lastms == null) Input.lastms = Input.ms;
                Input.MousePos = realMousePos;
                #endregion
                Update(deltaTime);
                renderer.ScreenToWorld(Input.MousePos, renderer.ActiveCamera, out Input.MouseRayOrigin, out Input.MouseRayDirection);
                #region input state update
                Input.lastks = Input.ks;
                Input.lastms = Input.ms;
                Input.LastMousePos = Input.MousePos;
                #endregion
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

            renderer.MainCamera.FieldOfView = MathUtil.DegreesToRadians(80);
            renderer.MainCamera.AspectRatio = renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width;

            player = new Player();
            player.Camera = renderer.MainCamera;
            player.Vehicle = new Ship(renderer.Device);
            player.Vehicle.CockpitCameraPosition = new Vector3(0, 2.6f, 7.6f);
            player.DisablePhysics = true;
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
            Vector3d d = Vector3d.Normalize(new Vector3d(.25, .85, -.33));
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
                        player.MoveTo(p.Position + d * (hh + 50));
                    });
                new UI.TextButton(ControlPanel, p.Name + "LowOrbitButton", new RawRectangleF((cpanelWidth - 10) * .333f + 1, y, (cpanelWidth - 10) * .666f - 1, y + h), "Low Orbit", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    ()=> {
                        player.MoveTo(p.Position + d * (p.SOI + 1000));
                    });
                new UI.TextButton(ControlPanel, p.Name + "HighOrbitButton", new RawRectangleF((cpanelWidth - 10) * .666f + 1, y, cpanelWidth - 2, y + h), "High Orbit", renderer.SegoeUI14, renderer.Brushes["Black"], renderer.Brushes["LightGray"],
                    () => {
                        player.MoveTo(p.Position + d * p.SOI * 3);
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
            
            Input.MouseBlocked = ControlPanel.Contains(Input.MousePos.X, Input.MousePos.Y);

            if (Input.ks.IsPressed(DInput.Key.F1) && !Input.lastks.IsPressed(DInput.Key.F1))
                renderer.DrawWireframe = !renderer.DrawWireframe;
            if (Input.ks.IsPressed(DInput.Key.F2) && !Input.lastks.IsPressed(DInput.Key.F2))
                renderer.DrawGUI = !renderer.DrawGUI;
            if (Input.ks.IsPressed(DInput.Key.F3) && !Input.lastks.IsPressed(DInput.Key.F3))
                Debug.DrawDebug = !Debug.DrawDebug;
            if (Input.ks.IsPressed(DInput.Key.F4) && !Input.lastks.IsPressed(DInput.Key.F4))
                Debug.DrawBoundingBoxes = !Debug.DrawBoundingBoxes;

            player.HandleInput(deltaTime);

            if (Input.ks.IsPressed(DInput.Key.Period) && !Input.lastks.IsPressed(DInput.Key.Period))
                TimeWarp *= 10;
            else if (Input.ks.IsPressed(DInput.Key.Comma) && !Input.lastks.IsPressed(DInput.Key.Comma))
                TimeWarp /= 10;
            TimeWarp = Math.Min(Math.Max(TimeWarp, 1), 1000);

            if (player.FirstPerson)
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientRectangle.X + renderForm.ClientSize.Width / 2, renderForm.ClientRectangle.Y + renderForm.ClientSize.Height / 2);
            Profiler.End();
            Profiler.Begin("LOD Update");
            StarSystem.ActiveSystem.UpdateLOD(renderer);
            Profiler.End();
            Profiler.Begin("Generation Queue Update");
            QuadNode.Update();
            Profiler.End();
            Profiler.Begin("Physics Update");
            StarSystem.ActiveSystem.physics.Update(deltaTime * TimeWarp);
            Profiler.End();

            ControlPanel.Update((float)deltaTime);
        }
        void Draw() {
            if (resizePending) {
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);
                resizePending = false;
            }
            renderer.TotalTime = gameTimer.Elapsed.TotalSeconds;
            renderer.Clear(Color.White);

            renderer.SetCamera(renderer.MainCamera);
            Star a = StarSystem.ActiveSystem.GetStar();
            Vector3d d = Vector3d.Normalize(renderer.MainCamera.Position - a.Position);
            renderer.ShadowCamera.Position = renderer.MainCamera.Position;
            Matrix v = Matrix.LookAtLH(-d * 100, Vector3.Zero, Vector3.Up);
            Vector3d p = v.Right * renderer.ShadowCamera.OrthographicSize * renderer.ShadowCamera.AspectRatio * .25f;
            renderer.ShadowCamera.View = Matrix.LookAtLH(-d * 100 + p, p, Vector3.Up);

            // 3d
            Profiler.Begin("3d Draw");
            skybox.Draw(renderer);
            StarSystem.ActiveSystem.physics.Draw(renderer);
            Profiler.End();
            
            Debug.Draw3D(renderer); // act like debug draws don't take a toll on performance

            //renderer.SetCamera(renderer.MainCamera);
            //Debug.DrawTexture(
            //    renderer,
            //    new Vector4(0, 0, .25f, .25f * renderer.ResolutionX / renderer.ResolutionY),
            //    renderer.ShadowCamera.renderTargetResource);

            // 2d
            if (renderer.DrawGUI) {
                Profiler.Begin("2d Draw");
                renderer.D2DContext.BeginDraw();

                StarSystem.ActiveSystem.DrawPlanetHudIcons(renderer, player.Velocity.Length());
                DrawHUD(renderer);
                ControlPanel.Draw(renderer);
                
                renderer.D2DContext.EndDraw();
                Profiler.End();
            }
        }

        public void DrawHUD(Renderer renderer) {
            double v = player.Velocity.Length();

            float xmid = renderer.ResolutionX * .5f;
            renderer.D2DContext.FillRectangle(
                new RawRectangleF(xmid - 150, 0, xmid + 150, 50),
                renderer.Brushes["White"]);

            renderer.SegoeUI24.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.SegoeUI14.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;

            renderer.D2DContext.DrawText(Physics.FormatSpeed(v), renderer.SegoeUI24,
                new RawRectangleF(xmid - 130, 0, xmid, 40),
                renderer.Brushes["Black"]);

            CelestialBody cb = StarSystem.ActiveSystem.GetNearestBody(player.Position);
            if (cb != null) {
                Vector3d dir = cb.Position - player.Position;
                double h = dir.Length();
                dir /= h;

                double r = cb.Radius;
                double radarAltitude = h - cb.GetHeight(dir);

                if (cb is Planet) {
                    Planet p = cb as Planet;
                    r = p.Radius + p.OceanHeight * p.TerrainHeight;

                    #region surface info
                    double temp = p.GetTemperature(dir);
                    double humid = p.GetHumidity(dir) * 100;

                    renderer.D2DContext.FillRectangle(
                        new RawRectangleF(xmid + 155, 0, xmid + 260, 80),
                        renderer.Brushes["White"]);

                    renderer.D2DContext.DrawText("Surface: ", renderer.SegoeUI14,
                        new RawRectangleF(xmid + 155, 3, xmid + 240, 10),
                        renderer.Brushes["Black"]);

                    renderer.D2DContext.DrawText(temp.ToString("F1") + "°C", renderer.SegoeUI14,
                        new RawRectangleF(xmid + 165, 15, xmid + 240, 30),
                        renderer.Brushes["Black"]);

                    renderer.D2DContext.DrawText(humid.ToString("F1") + "%", renderer.SegoeUI14,
                        new RawRectangleF(xmid + 165, 30, xmid + 240, 45),
                        renderer.Brushes["Black"]);
                    #endregion
                    #region atmosphere info
                    Atmosphere a = p.Atmosphere;
                    if (a != null && h < a.Radius * 1.5) {
                        double atemp;
                        double pressure;
                        double density;
                        double c;
                        a.MeasureProperties(dir, h, out pressure, out density, out atemp, out c);
                        if (pressure > .1) {
                            renderer.D2DContext.FillRectangle(
                                new RawRectangleF(xmid - 260, 0, xmid - 155, 80),
                                renderer.Brushes["White"]);

                            renderer.D2DContext.DrawText("Atmosphere: ", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 250, 3, xmid - 155, 10),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(atemp.ToString("F1") + "°C", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 15, xmid - 155, 30),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(pressure.ToString("F1") + " kPa", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 30, xmid - 155, 45),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(density.ToString("F1") + " kg/m^3", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 45, xmid - 155, 60),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText("Mach " + (player.Velocity.Length() / c).ToString("F2"), renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 60, xmid - 155, 75),
                                renderer.Brushes["Black"]);
                        }
                    }
                    #endregion
                }

                // altitude
                renderer.D2DContext.DrawText(Physics.FormatDistance(h - r), renderer.SegoeUI24,
                    new RawRectangleF(xmid, 0, xmid + 150, 40),
                    renderer.Brushes["Black"]);
                // planet label
                renderer.D2DContext.DrawText("(" + cb.Name + ")", renderer.SegoeUI14,
                    new RawRectangleF(xmid, 30, xmid + 150, 50),
                    renderer.Brushes["Black"]);
                
                renderer.D2DContext.FillRectangle(
                    new RawRectangleF(0, 0, 100, 40),
                    renderer.Brushes["White"]);
                renderer.D2DContext.DrawText("x" + TimeWarp.ToString("N0"), renderer.SegoeUI24,
                    new RawRectangleF(10, 0, 100, 40),
                    renderer.Brushes["Black"]);
                
                Orbit o = new Orbit(player.Position - cb.Position, player.Velocity - cb.Velocity, cb);

                renderer.SegoeUI14.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Far;
                renderer.SegoeUI14.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
                renderer.D2DContext.DrawText(o.ToString(), renderer.SegoeUI14,
                    new RawRectangleF(renderer.ResolutionX * .5f, renderer.ResolutionY, renderer.ResolutionX * .5f, renderer.ResolutionY),
                    renderer.Brushes["White"]);
                
                // TODO: fix keplerian orbits
                //List<Vector3d> pts = new List<Vector3d>();
                //Vector3d op, ov;
                //for (double t = 0; t < o.T; t += o.T * .05) {
                //    o.ToCartesian(cb, t, out op, out ov);
                //    pts.Add(cb.Position + op);
                //}
                //o.ToCartesian(cb, o.T, out op, out ov);
                //pts.Add(cb.Position + op);
                //Debug.DrawLine(Color.CornflowerBlue, pts.ToArray());
                //
                //foreach (Vector3d p in pts) {
                //    Vector3d pos;
                //    double scale;
                //    renderer.Camera.GetScaledSpace(p + cb.Position, out pos, out scale);
                //    Debug.DrawBox(Color.Green, new OrientedBoundingBox(pos - (Vector3d)Vector3.One * 10000 * scale, pos + (Vector3d)Vector3.One * 10000 * scale));
                //}
            }
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
