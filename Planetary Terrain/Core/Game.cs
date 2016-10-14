using System;
using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain {
    class Game : IDisposable {
        private RenderForm renderForm;
        private bool resizePending;

        public Renderer renderer;

        private System.Diagnostics.Stopwatch frameTimer;

        DInput.Keyboard keyboard;
        DInput.Mouse mouse;

        public UI.InputState InputState;
        Vector2 realMousePos;
        bool lockMouse;
        
        public UI.Frame NavigatorWindow;

        PlayerShip ship;
        
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
            
            frameTimer = new System.Diagnostics.Stopwatch();

            Shaders.LoadShaders(renderer.Device, renderer.Context);

            InitializeScene();
        }
        public void Run() {
            RenderLoop.Run(renderForm, () => {
                Update();
                Draw();
            });
        }

        public void Exit() {
            renderForm.Close();
            Dispose();
        }

        void InitializeScene() {
            StarSystem.ActiveSystem = new StarSystem(renderer.Device);
            
            renderer.Camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            ship = new PlayerShip(renderer.Device, renderer.Camera);

            float h = 35;
            NavigatorWindow = new UI.Frame(null, "Panel1", new RawRectangleF(0, 300, 200, 300 + (StarSystem.ActiveSystem.bodies.Count+1) * h), renderer.CreateBrush(new Color(.5f, .5f, .5f, .5f)));
            NavigatorWindow.Draggable = true;

            new UI.TextLabel(NavigatorWindow, "Title", new RawRectangleF(0, 0, 200, h), "NAVIGATOR", renderer.SegoeUI24, renderer.SolidWhiteBrush);
            float y = h;
            foreach (Body p in StarSystem.ActiveSystem.bodies) {
                new UI.TextButton(NavigatorWindow, p.Label + "Button", new RawRectangleF(5, y, 195, y + h-2), p.Label, renderer.SegoeUI24, renderer.SolidBlackBrush, renderer.SolidGrayBrush,
                    ()=> {
                        Vector3d d = new Vector3d(0, 0, -1);
                        ship.Position = p.Position + d * (p.SOI + 1000);
                    });
                y += h;
            }
            (NavigatorWindow["EarthButton"] as UI.TextButton).Click();

            renderer.Camera.Ship = ship;
            renderer.Camera.Mode = Camera.CameraMode.Ship;
        }

        double zoomd = 0;
        int framec = 0;
        double ftime = 0;
        void Update() {
            #region timing
            double deltaTime = frameTimer.ElapsedMilliseconds / 1000d;
            frameTimer.Restart();

            ftime += deltaTime;
            framec++;
            if (ftime > 1) {
                ftime = 0;
                Debug.FPS = framec;
                framec = 0;
            }
            #endregion

            #region input state update
            InputState.ks = keyboard.GetCurrentState();
            InputState.ms = mouse.GetCurrentState();
            if (InputState.lastks == null) InputState.lastks = InputState.ks;
            if (InputState.lastms == null) InputState.lastms = InputState.ms;
            InputState.mousePos = realMousePos;
            #endregion

            #region ship/camera control
            Vector3d r = Vector3.Zero;
            if (InputState.ks.IsPressed(DInput.Key.W))
                r.X -= -1;
            else if (InputState.ks.IsPressed(DInput.Key.S))
                r.X += -1;
            if (InputState.ks.IsPressed(DInput.Key.A))
                r.Y -= 1;
            else if (InputState.ks.IsPressed(DInput.Key.D))
                r.Y += 1;
            if (InputState.ks.IsPressed(DInput.Key.Q))
                r.Z += 1;
            if (InputState.ks.IsPressed(DInput.Key.E))
                r.Z -= 1;

            r *= .01;
            ship.AngularVelocity = Vector3.Lerp(ship.AngularVelocity, r, (float)deltaTime);

            if (InputState.ks.IsPressed(DInput.Key.LeftShift))
                ship.Throttle += deltaTime * .25;
            else if (InputState.ks.IsPressed(DInput.Key.LeftControl))
                ship.Throttle -= deltaTime * .25;
            ship.Throttle = MathTools.Clamp01(ship.Throttle);

            // Camera view switch
            if (InputState.ks.IsPressed(DInput.Key.V) && !InputState.lastks.IsPressed(DInput.Key.V)) {
                if (renderer.Camera.Mode == Camera.CameraMode.Body) {
                    renderer.Camera.Mode = Camera.CameraMode.Ship;
                    renderer.Camera.Ship = ship;
                } else
                    renderer.Camera.Mode = Camera.CameraMode.Body;
            }

            // Mouse lock
            if (InputState.ks.IsPressed(DInput.Key.F1) && !InputState.lastks.IsPressed(DInput.Key.F1)) {
                lockMouse = !lockMouse;
            
                if (lockMouse)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }
            
            // Mouse look
            if (lockMouse || InputState.ms.Buttons[1] || InputState.ms.Buttons[2]) {
                Vector2 delta;
                if (lockMouse) {
                    delta = new Vector2(InputState.ms.X, InputState.ms.Y);
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientRectangle.X + renderForm.ClientSize.Width / 2, renderForm.ClientRectangle.Y + renderForm.ClientSize.Height / 2);
                } else
                    delta = InputState.mousePos - InputState.lastMousePos;

                if (InputState.ms.Buttons[2])
                    renderer.Camera.PostRotation += new Vector3(delta.Y, delta.X, 0) * .003f;
                else
                    renderer.Camera.Rotation += new Vector3(delta.Y, delta.X, 0) * .003f;
            }
            // zoom
            zoomd = -InputState.ms.Z / 120;
            renderer.Camera.Zoom += zoomd * deltaTime;
            zoomd *= .8;
            #endregion

            if (InputState.ks.IsPressed(DInput.Key.F2) && !InputState.lastks.IsPressed(DInput.Key.F2))
                renderer.DrawGUI = !renderer.DrawGUI;

            if (InputState.ks.IsPressed(DInput.Key.Tab) && !InputState.lastks.IsPressed(DInput.Key.Tab))
                renderer.Camera.BodyIndex = (renderer.Camera.BodyIndex + 1) % StarSystem.ActiveSystem.bodies.Count;

            if (InputState.ks.IsPressed(DInput.Key.F) && !InputState.lastks.IsPressed(DInput.Key.F))
                renderer.DrawWireframe = !renderer.DrawWireframe;

            ship.Update(deltaTime);
            StarSystem.ActiveSystem.Update(renderer, renderer.Device, deltaTime);
            NavigatorWindow.Update((float)deltaTime, InputState);
            
            renderer.Camera.Update();

            #region input state update
            InputState.lastks = InputState.ks;
            InputState.lastms = InputState.ms;
            InputState.lastMousePos = InputState.mousePos;
            #endregion
        }

        void Draw() {
            if (resizePending)
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);

            Debug.BeginFrame();

            renderer.BeginDrawFrame();
            renderer.Clear(Color.Black);

            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;

            StarSystem.ActiveSystem.Draw(renderer, ship.LinearVelocity.Length());

            ship.Draw(renderer);
            
            if (renderer.DrawGUI) {
                renderer.D2DContext.BeginDraw();
                NavigatorWindow.Draw(renderer);
                renderer.D2DContext.EndDraw();
            }

            Debug.EndFrame();

            if (renderer.DrawGUI)
                Debug.Draw(renderer, ship);
            
            renderer.EndDrawFrame();
        }

        public void Dispose() {
            NavigatorWindow.Dispose();

            // scene stuff
            StarSystem.ActiveSystem.Dispose();

            renderer.Dispose();
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            Shaders.Dispose();

            renderForm.Dispose();
        }
    }
}
