using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using System;
using System.Collections.Generic;

namespace Planetary_Terrain {
    class Game : IDisposable {
        private RenderForm renderForm;
        private bool resizePending;

        public Renderer renderer;
        public D3D11.Device device { get { return renderer?.Device; } }
        public D3D11.DeviceContext context { get {return renderer?.Context; } }

        private StarSystem starSystem;

        private System.Diagnostics.Stopwatch frameTimer;

        DInput.Keyboard keyboard;
        DInput.Mouse mouse;

        DInput.KeyboardState ks, lastks;
        DInput.MouseState ms, lastms;
        bool lockMouse;

        double cameraSpeed;
        
        public Game() {
            renderForm = new RenderForm("D3D11 Planets");
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

            renderer = new Renderer(renderForm);
            
            frameTimer = new System.Diagnostics.Stopwatch();

            Shaders.LoadShaders(device, context);

            InitializeScene();
        }

        public void Run() {
            RenderLoop.Run(renderForm, () => {
                Update();
                Draw();
            });
        }

        void InitializeScene() {
            starSystem = new StarSystem(device);
            
            Planet startPlanet = starSystem.planets[3];

            renderer.Camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            renderer.Camera.Position = Vector3d.Normalize(new Vector3d(0, 1, -1));
            renderer.Camera.Position *= startPlanet.GetHeight(renderer.Camera.Position) * 1.1;
            renderer.Camera.Position += startPlanet.Position;
            renderer.Camera.Rotation = new Vector3(0, MathUtil.Pi, 0);
        }

        int framec = 0;
        int fps;
        float ftime = 0;
        void Update() {
            float deltaTime = frameTimer.ElapsedMilliseconds / 1000f;
            frameTimer.Restart();

            ftime += deltaTime;
            framec++;
            if (ftime > 1) {
                ftime = 0;
                fps = framec;
                framec = 0;
            }

            ks = keyboard.GetCurrentState();
            ms = mouse.GetCurrentState();

            #region camera control
            if (ks.IsPressed(DInput.Key.Space))
                cameraSpeed += 10000;
            else
                cameraSpeed = 2;
            if (ks.IsPressed(DInput.Key.RightShift))
                cameraSpeed = 299792458; // speed of light

            Vector3 move = Vector3.Zero;
            if (ks.IsPressed(DInput.Key.W))
                move += Vector3.ForwardLH;
            else if (ks.IsPressed(DInput.Key.S))
                move += Vector3.BackwardLH;
            if (ks.IsPressed(DInput.Key.A))
                move += Vector3.Left;
            else if (ks.IsPressed(DInput.Key.D))
                move += Vector3.Right;

            if (!move.IsZero) {
                move.Normalize();
                move = Vector3.Transform(move, renderer.Camera.RotationQuaternion);
                Vector3d moved = move;
                
                moved *= cameraSpeed;

                if (ks.IsPressed(DInput.Key.LeftShift))
                    moved *= 3;
                
                renderer.Camera.Translate(moved * deltaTime);
            }

            if (ks.IsPressed(DInput.Key.LeftControl) && !lastks.IsPressed(DInput.Key.LeftControl)) {
                lockMouse = !lockMouse;

                if (lockMouse)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }

            if (lockMouse) {
                Vector3 delta = new Vector3(ms.X, ms.Y, 0);
                renderer.Camera.Rotation += new Vector3(delta.Y, delta.X, delta.Z) * .003f;
                renderer.Camera.Rotation = new Vector3(MathUtil.Clamp(renderer.Camera.Rotation.X, -MathUtil.PiOverTwo, MathUtil.PiOverTwo), renderer.Camera.Rotation.Y, renderer.Camera.Rotation.Z);
                
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientSize.Width / 2, renderForm.ClientSize.Height / 2);
            }
            #endregion

            if (ks.IsPressed(DInput.Key.P) && !lastks.IsPressed(DInput.Key.P))
                renderer.Camera.Frozen = !renderer.Camera.Frozen;

            Planet p = starSystem.GetNearestPlanet(renderer.Camera.Position);
            if ((renderer.Camera.Position - p.Position).Length() < p.SOI) {
                renderer.Camera.AttachedPlanet = p;

                if (!renderer.Camera.Frozen) {
                    Vector3d c = renderer.Camera.Position - p.Position;
                    double a = c.Length();
                    c.Normalize();
                    double h = p.GetHeight(c);
                    if (h + 2 > a)
                        renderer.Camera.Position = c * (h + 2) + p.Position;
                }
            } else
                renderer.Camera.AttachedPlanet = null;

            starSystem.Update(renderer, device);

            lastks = ks;
            lastms = ms;
        }
        
        void Draw() {
            if (resizePending)
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);

            renderer.PreRender();
            renderer.Clear(Color.Black);

            renderer.Context.Rasterizer.State = ks.IsPressed(DInput.Key.Tab) ? renderer.rasterizerStateWireframe : renderer.rasterizerStateSolid;

            starSystem.Draw(renderer);

            renderer.Present();
        }

        public void Dispose() {
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
