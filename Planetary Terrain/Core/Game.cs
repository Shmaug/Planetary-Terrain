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

        private StarSystem starSystem;

        private System.Diagnostics.Stopwatch frameTimer;

        DInput.Keyboard keyboard;
        DInput.Mouse mouse;

        public UI.InputState InputState;
        Vector2 realMousePos;
        bool lockMouse;
        
        public UI.Frame NavigatorWindow;
        
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
            starSystem = new StarSystem(renderer.Device);
            
            renderer.Camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            {
                Body p = starSystem.bodies[3];
                Vector3d d = new Vector3d(0, 1, -1);
                d.Normalize();
                renderer.Camera.Position = p.Position + d * (p.SOI + 1000);
                renderer.Camera.Rotation = new Vector3(0, 0, 0);
            }
            float h = 35;
            NavigatorWindow = new UI.Frame(null, "Panel1", new RawRectangleF(0, 300, 200, 300 + (starSystem.bodies.Count+1) * h), renderer.CreateBrush(new Color(.5f, .5f, .5f, .5f)));
            NavigatorWindow.Draggable = true;

            new UI.TextLabel(NavigatorWindow, "Title", new RawRectangleF(0, 0, 200, h), "NAVIGATOR", renderer.SegoeUI24, renderer.SolidWhiteBrush);
            float y = h;
            foreach (Body p in starSystem.bodies) {
                new UI.TextButton(NavigatorWindow, p.Label + "Button", new RawRectangleF(0, y, 200, y + h), p.Label, renderer.SegoeUI24, renderer.SolidBlackBrush, renderer.SolidGrayBrush,
                    ()=> {
                        Vector3d d = new Vector3d(0, 1, -1);
                        d.Normalize();
                        renderer.Camera.Position = p.Position + d * (p.SOI+1000);
                        renderer.Camera.Rotation = new Vector3(0, 0, 0);
                    });
                y += h;
            }
        }

        int framec = 0;
        double ftime = 0;
        double accelTime = 0;
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

            #region camera control
            Vector3 move = Vector3.Zero;
            if (InputState.ks.IsPressed(DInput.Key.W))
                move += Vector3.ForwardLH;
            else if (InputState.ks.IsPressed(DInput.Key.S))
                move += Vector3.BackwardLH;
            if (InputState.ks.IsPressed(DInput.Key.A))
                move += Vector3.Left;
            else if (InputState.ks.IsPressed(DInput.Key.D))
                move += Vector3.Right;

            bool accelerating = !move.IsZero;

            if (InputState.ks.IsPressed(DInput.Key.LeftShift)) {
                Body near = renderer.Camera.NearestBody;
                if (near != null) {
                    double dist = (renderer.Camera.Position - near.Position).Length();
                    if (dist < near.SOI) {
                        double alt = dist - near.Radius; // altitude from sea level
                        renderer.Camera.Speed = (.5 + alt / (near.SOI - near.Radius)) * 600;
                        accelerating = true;
                    }
                }
            }

            if (InputState.ks.IsPressed(DInput.Key.Space)) {
                if (!move.IsZero) {
                    accelTime += deltaTime;
                    renderer.Camera.Speed += (accelTime < 10 ? Math.Pow(.1 * accelTime, 4) : accelTime) * Constants.LIGHT_SPEED;
                }
                accelerating = true;
            }
            if (!accelerating) {
                renderer.Camera.Speed = 1;
                accelTime = 0;
            }

            if (!move.IsZero) {
                move.Normalize();
                move = Vector3.Transform(move, renderer.Camera.RotationQuaternion);
                Vector3d moved = move;
                
                moved *= renderer.Camera.Speed * renderer.Camera.SpeedMultiplier;
                
                renderer.Camera.Translate(moved * deltaTime);
            }

            // Mouse lock
            if (InputState.ks.IsPressed(DInput.Key.LeftControl) && !InputState.lastks.IsPressed(DInput.Key.LeftControl)) {
                lockMouse = !lockMouse;

                if (lockMouse)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }

            // Mouse look
            if (lockMouse) {
                Vector3 delta = new Vector3(InputState.ms.Y, InputState.ms.X, 0) * .003f;
                renderer.Camera.Rotation += delta;
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientRectangle.X + renderForm.ClientSize.Width / 2, renderForm.ClientRectangle.Y + renderForm.ClientSize.Height / 2);
            }

            #region camera collision & planet attach
            Body b = starSystem.GetNearestBody(renderer.Camera.Position);
            renderer.Camera.NearestBody = b;
             if ((renderer.Camera.Position - b.Position).Length() < b.SOI) {
                renderer.Camera.Mode = Camera.CameraMode.Surface;
                
                Vector3d c = renderer.Camera.Position - b.Position;
                double a = c.Length();
                c.Normalize();
                double h = b.GetHeight(c);
                if (h + 2 > a)
                    renderer.Camera.Position = c * (h + 2) + b.Position;
            } else
                renderer.Camera.Mode = Camera.CameraMode.Orbital;
            #endregion
            #endregion

            #region misc keyboard 
            if (InputState.ks.IsPressed(DInput.Key.Tab) && !InputState.lastks.IsPressed(DInput.Key.Tab))
                renderer.DrawGUI = !renderer.DrawGUI;

            if (InputState.ks.IsPressed(DInput.Key.R) && !InputState.lastks.IsPressed(DInput.Key.R))
                renderer.Camera.Frozen = !renderer.Camera.Frozen;
            #endregion

            renderer.Camera.Update((float)deltaTime);
            starSystem.Update(renderer, renderer.Device, deltaTime);

            NavigatorWindow.Update((float)deltaTime, InputState);

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

            renderer.DrawWireframe = InputState.ks.IsPressed(DInput.Key.F);
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            
            starSystem.Draw(renderer);

            if (renderer.Camera.Frozen)
                renderer.DrawAxis();

            if (renderer.DrawGUI) {
                renderer.D2DContext.BeginDraw();
                NavigatorWindow.Draw(renderer);
                renderer.D2DContext.EndDraw();
            }

            Debug.EndFrame();

            if (renderer.DrawGUI)
                Debug.Draw(renderer);
            
            renderer.EndDrawFrame();
        }

        public void Dispose() {
            NavigatorWindow.Dispose();

            // scene stuff
            starSystem.Dispose();

            renderer.Dispose();
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            Shaders.Dispose();

            renderForm.Dispose();
        }
    }
}
