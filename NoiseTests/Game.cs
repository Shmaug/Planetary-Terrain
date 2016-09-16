using System;
using SharpDX;
using SharpDX.Windows;
using SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DInput = SharpDX.DirectInput;
using SharpDX.D3DCompiler;
using System.Runtime.InteropServices;
using SharpDX.DXGI;
using Planetary_Terrain;

namespace NoiseTests {
    [StructLayout(LayoutKind.Explicit)]
    struct Vertex {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Color4 Color;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
        };

        public Vertex(Vector3 pos, Color col) {
            Position = pos;
            Color = col;
        }
    }

    class Game : IDisposable {
        public D3D11.VertexShader VertexShader { get; private set; }
        public D3D11.PixelShader PixelShader { get; private set; }
        public ShaderSignature Signature { get; private set; }
        public D3D11.InputLayout InputLayout { get; private set; }
        
        private RenderForm renderForm;

        public Renderer renderer;
        
        DInput.Keyboard keyboard;
        DInput.Mouse mouse;

        DInput.MouseState ms, lastms;
        DInput.KeyboardState ks, lastks;
        Vector2 lastMousePos;
        Vector2 mousePos;

        Vector2 realMousePos;

        D3D11.Buffer vbuffer;
        D3D11.Buffer ibuffer;
        int icount;

        public Game() {
            renderForm = new RenderForm("Noise Tests");
            renderForm.ClientSize = new System.Drawing.Size(512, 512);
            renderForm.MouseMove += (object sender, System.Windows.Forms.MouseEventArgs e) => {
                realMousePos = new Vector2(e.Location.X, e.Location.Y);
            };
            renderForm.AllowUserResizing = false;

            DInput.DirectInput directInput = new DInput.DirectInput();
            keyboard = new DInput.Keyboard(directInput);
            mouse = new DInput.Mouse(directInput);

            keyboard.Acquire();
            mouse.Acquire();

            renderer = new Renderer(this, renderForm);
            
            using (var byteCode = ShaderBytecode.CompileFromFile("shader.hlsl", "vsmain", "vs_5_0", ShaderFlags.Debug)) {
                if (byteCode.Bytecode == null)
                    throw new CompilationException(byteCode.Message);
                Signature = ShaderSignature.GetInputSignature(byteCode);
                VertexShader = new D3D11.VertexShader(renderer.Device, byteCode);
            }
            using (var byteCode = ShaderBytecode.CompileFromFile("shader.hlsl", "psmain", "ps_5_0", ShaderFlags.Debug)) {
                if (byteCode.Bytecode == null)
                    throw new CompilationException(byteCode.Message);
                PixelShader = new D3D11.PixelShader(renderer.Device, byteCode);
            }
            InputLayout = new D3D11.InputLayout(renderer.Device, Signature, Vertex.InputElements);

            renderer.Context.Rasterizer.State = new D3D11.RasterizerState(renderer.Device, new D3D11.RasterizerStateDescription() {
                CullMode = D3D11.CullMode.None,
                FillMode = D3D11.FillMode.Solid,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
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
        // TODO: better handling of noise functions per planet
        // TODO: better camera control
        double height(Vector3d direction) {
            double total = 0;

            double n = Noise.Ridged(direction * 500 + new Vector3(1000), 2, .01f, .3f);
            n = Noise.Map(n, 0, 1);
            n = 1 - n * n * n;

            total += Noise.Map(Noise.Fractal(direction * 100 + new Vector3(1000), 11, .03f, .5f), 0, 1);

            return total;
        }

        void buildMesh() {
            int s = 512;
            Vertex[] verticies = new Vertex[s * s];
            int[] indicies = new int[s * s * 6];
            int i = 0;

            for (int x = 0; x < s; x++)
                for (int y = 0; y < s; y++) {
                    float nx = 2f * x / (s + 1) - 1f, ny = 2f * y / (s + 1) - 1f;

                    Vector3 direction = new Vector3(nx, ny, 0);
                    
                    verticies[x * s + y] =
                        new Vertex(new Vector3(nx, ny, 0),
                        new Color((float)height(direction)));

                    if (x+1 < s && y+1 < s) {
                        indicies[i++] = (x) * s + y;
                        indicies[i++] = (x + 1) * s + y;
                        indicies[i++] = (x) * s + y + 1;

                        indicies[i++] = (x + 1) * s + y;
                        indicies[i++] = (x + 1) * s + y + 1;
                        indicies[i++] = (x) * s + y + 1;
                    }
                }

            vbuffer?.Dispose();
            ibuffer?.Dispose();

            vbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            ibuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);
            icount = indicies.Length;
        }

        bool redraw = false;
        void Update() {
            #region input state update
            ks = keyboard.GetCurrentState();
            ms = mouse.GetCurrentState();
            if (lastks == null) lastks = ks;
            if (lastms == null) lastms = ms;
            mousePos = realMousePos;
            #endregion

            if (ks.IsPressed(DInput.Key.Space) && !lastks.IsPressed(DInput.Key.Space))
                redraw = true;
            
            #region input state update
            lastks = ks;
            lastms = ms;
            lastMousePos = mousePos;
            #endregion
        }

        void Draw() {
            if (redraw) {
                renderer.Clear(Color.Black);

                buildMesh();

                renderer.Context.InputAssembler.InputLayout = InputLayout;
                renderer.Context.VertexShader.Set(VertexShader);
                renderer.Context.PixelShader.Set(PixelShader);
                
                renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vbuffer, Utilities.SizeOf<Vertex>(), 0));
                renderer.Context.InputAssembler.SetIndexBuffer(ibuffer, Format.R32_UInt, 0);

                renderer.Context.DrawIndexed(icount, 0, 0);

                //renderer.Context.Draw(vcount, 0);

                renderer.Present();

                redraw = false;
            }
        }

        public void Dispose() {
            renderer.Dispose();
            
            keyboard.Dispose();
            mouse.Dispose();

            vbuffer?.Dispose();
            ibuffer?.Dispose();
            
            renderForm.Dispose();

            VertexShader.Dispose();
            PixelShader.Dispose();
            Signature.Dispose();
            InputLayout.Dispose();
        }
    }
}
