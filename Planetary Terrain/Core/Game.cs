using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using System;

namespace Planetary_Terrain {
    class Game : IDisposable {
        private RenderForm renderForm;
        private bool resizePending;

        public Renderer renderer;
        public D3D11.Device device { get { return renderer?.Device; } }
        public D3D11.DeviceContext context { get {return renderer?.Context; } }
        
        private Planet planet;

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
            planet = new Planet(6371000, 20000);
            planet.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\TerrainColor.jpg"), device);

            renderer.LightDirection = new Vector3(.25f, -1, 1);
            renderer.LightDirection.Normalize();

            renderer.camera = new Camera(MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            renderer.camera.Position = new Vector3d(0, planet.Radius, -planet.Radius);
            renderer.camera.Position.Normalize();
            renderer.camera.Rotation = new Vector3(0, MathUtil.Pi, 0);
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
                move = Vector3.Transform(-move, renderer.camera.RotationMatrix).ToVector3();
                Vector3d moved = move.ToDouble();

                if (ks.IsPressed(DInput.Key.LeftShift))
                    cameraSpeed += 1000;
                else
                    cameraSpeed = 2;
                moved *= cameraSpeed;

                if (ks.IsPressed(DInput.Key.Space))
                    moved *= Math.Min(Math.Max(renderer.camera.Position.Length() - planet.Radius, 2), 20);

                renderer.camera.Position += moved * deltaTime;

                Vector3d c = renderer.camera.Position - planet.Position;
                double a = c.Length();
                c.Normalize();
                double h = planet.GetHeight(c);
                if (h + 2 > a)
                    renderer.camera.Position = c * (h + 2) + planet.Position;
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
                if (ks.IsPressed(DInput.Key.Q))
                    delta.Z = -3;
                else if (ks.IsPressed(DInput.Key.E))
                    delta.Z = 3;
                renderer.camera.Rotation += new Vector3(-delta.Y, delta.X, delta.Z) * .003f;
                renderer.camera.Rotation = new Vector3(MathUtil.Clamp(renderer.camera.Rotation.X, -MathUtil.PiOverTwo, MathUtil.PiOverTwo), renderer.camera.Rotation.Y, renderer.camera.Rotation.Z);
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(renderForm.ClientSize.Width / 2, renderForm.ClientSize.Height / 2);
            }
            #endregion

            planet.Update(device, renderer.camera);

            lastks = ks;
            lastms = ms;
        }
        
        void Draw() {
            if (resizePending) {
                renderer.Resize(renderForm.ClientSize.Width, renderForm.ClientSize.Height);
            }

            renderer.PreRender();
            renderer.Clear(Color.Gray);

            renderer.Context.Rasterizer.State = ks.IsPressed(DInput.Key.Tab) ? renderer.rasterizerStateWireframe : renderer.rasterizerStateSolid;
            
            planet.Draw(renderer);
            
            renderer.Present();
        }

        public void Dispose() {
            // scene stuff
            planet.Dispose();
            renderer.Dispose();
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            Shaders.DisposeShaders();

            renderForm.Dispose();
        }
    }
}
