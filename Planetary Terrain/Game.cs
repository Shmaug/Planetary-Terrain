using SharpDX;
using SharpDX.Windows;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using System;

namespace BetterTerrain {
    class Game : IDisposable {
        private RenderForm renderForm;
        private bool resizePending;

        public Renderer renderer;
        public D3D11.Device device { get { return renderer?.Device; } }
        public D3D11.DeviceContext context { get {return renderer?.Context; } }
        
        public Shader terrainShader;
        
        private Planet planet;

        private System.Diagnostics.Stopwatch frameTimer;

        DInput.Keyboard keyboard;
        DInput.Mouse mouse;

        DInput.KeyboardState ks, lastks;
        DInput.MouseState ms, lastms;
        bool lockMouse;
        
        public Game() {
            renderForm = new RenderForm("D3D11 Game");
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
            
            terrainShader = new Shader("Shaders\\terrain.hlsl", device, context, VertexNormalTexture.InputElements);

            frameTimer = new System.Diagnostics.Stopwatch();

            InitializeScene();
        }

        public void Run() {
            RenderLoop.Run(renderForm, () => {
                Update();
                Draw();
            });
        }

        void InitializeScene() {
            planet = new Planet(device, 1000);
            planet.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\TerrainColor.jpg"), device);

            renderer.camera = new Camera(device, MathUtil.DegreesToRadians(70), renderForm.ClientSize.Width / (float)renderForm.ClientSize.Width);
            Vector3 v = new Vector3(1.2f, 1f, -1f);
            v.Normalize();
            renderer.camera.Position = v * (planet.GetHeight(v) + 100);
            renderer.camera.Rotation = new Vector3(-.5f, MathUtil.Pi * .75f, 0);
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

                if (ks.IsPressed(DInput.Key.LeftShift))
                    move *= 100f;
                if (ks.IsPressed(DInput.Key.Space))
                    move *= 50f;

                renderer.camera.Position += move * deltaTime;
            }

            if (ks.IsPressed(DInput.Key.Escape) && !lastks.IsPressed(DInput.Key.Escape)) {
                lockMouse = !lockMouse;

                if (lockMouse)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();

            }

            if (lockMouse) {
                Vector3 delta = new Vector3(ms.X, ms.Y, 0);
                if (ks.IsPressed(DInput.Key.Q))
                    delta.Z = 3;
                else if (ks.IsPressed(DInput.Key.E))
                    delta.Z -= 3;
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
            renderer.Clear(Color.Black);

            renderer.Context.Rasterizer.State = ks.IsPressed(DInput.Key.Tab) ? renderer.rasterizerStateWireframe : renderer.rasterizerStateSolid;
            
            terrainShader.Set(renderer);
            planet.Draw(renderer);

            renderer.DrawAxis();
            
            renderer.Present();
        }

        public void Dispose() {
            // scene stuff
            planet.Dispose();
            renderer.Dispose();
            
            // other stuff
            keyboard.Dispose();
            mouse.Dispose();

            terrainShader.Dispose();

            renderForm.Dispose();
        }
    }
}
