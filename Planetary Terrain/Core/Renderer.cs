using System;
using SharpDX;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Renderer : IDisposable {
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 144)]
        struct Constants {
            public Matrix View;
            public Matrix Projection;
            public Vector3 cameraDirection;
            public float farPlane;
        }
        Constants constants;
        public D3D11.Buffer constantBuffer { get; private set; }

        public int ResolutionX, ResolutionY;

        #region 2d vars
        public D2D1.Device D2DDevice { get; private set; }
        public D2D1.DeviceContext D2DContext { get; private set; }
        public D2D1.Factory D2DFactory { get; private set; }
        public D2D1.Bitmap D2DTarget { get; private set; }

        public DWrite.Factory FontFactory { get; private set; }
        public D2D1.Brush SolidWhiteBrush { get; private set; }
        public D2D1.Brush SolidGrayBrush { get; private set; }
        public D2D1.Brush SolidBlackBrush { get; private set; }
        public D2D1.Brush SolidGreenBrush { get; private set; }
        public D2D1.Brush SolidRedBrush { get; private set; }
        public D2D1.Brush SolidBlueBrush { get; private set; }
        public DWrite.TextFormat SegoeUI24 { get; private set; }
        public DWrite.TextFormat SegoeUI14 { get; private set; }
        public DWrite.TextFormat Consolas14 { get; private set; }

        #endregion

        #region 3d vars
        private DXGI.SwapChain swapChain;
        public D3D11.Device Device { get; private set;}
        public D3D11.DeviceContext Context { get; private set; }
        #endregion

        #region states and views
        public Viewport Viewport { get; private set; }

        public D3D11.DepthStencilView depthStencilView { get; private set; }
        public D3D11.RenderTargetView renderTargetView { get; private set; }

        public D3D11.DepthStencilState depthStencilStateDefault { get; private set; }
        public D3D11.DepthStencilState depthStencilStateNoDepth { get; private set; }

        public D3D11.RasterizerState rasterizerStateSolidCullBack { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeCullBack { get; private set; }
        public D3D11.RasterizerState rasterizerStateSolidNoCull { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeNoCull { get; private set; }
        public D3D11.RasterizerState rasterizerStateSolidCullFront { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeCullFront { get; private set; }
        

        public D3D11.BlendState blendStateOpaque { get; private set; }
        public D3D11.BlendState blendStateTransparent { get; private set; }
        #endregion

        public bool DrawWireframe = false;
        public bool DrawGUI = true;

        public Camera Camera;

        D3D11.Buffer axisBuffer;
        D3D11.Buffer axisConsts;

        Game game;

        public Renderer(Game game, SharpDX.Windows.RenderForm renderForm) {
            this.game = game;
            int width = renderForm.ClientSize.Width, height = renderForm.ClientSize.Height;
            ResolutionX = width; ResolutionY = height;

            #region 3d device & context
            DXGI.SwapChainDescription swapChainDesc = new DXGI.SwapChainDescription() {
                ModeDescription = new DXGI.ModeDescription(width, height, new DXGI.Rational(60, 1), DXGI.Format.R8G8B8A8_UNorm),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = DXGI.Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true
            };
            D3D11.Device device;
            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport | D3D11.DeviceCreationFlags.Debug, swapChainDesc, out device, out swapChain);
            Device = device;
            Context = Device.ImmediateContext;

            swapChain.GetParent<DXGI.Factory>().MakeWindowAssociation(renderForm.Handle, DXGI.WindowAssociationFlags.IgnoreAll);
            #endregion

            #region 2d device & context
            DXGI.Device dxgiDevice = Device.QueryInterface<D3D11.Device1>().QueryInterface<DXGI.Device2>();
            D2DDevice = new D2D1.Device(dxgiDevice);
            D2DContext = new D2D1.DeviceContext(D2DDevice, D2D1.DeviceContextOptions.None);
            D2DFactory = new D2D1.Factory1(D2D1.FactoryType.SingleThreaded);

            using (DXGI.Surface surface = swapChain.GetBackBuffer<DXGI.Surface>(0))
                D2DTarget = new D2D1.Bitmap1(D2DContext, surface,
                    new D2D1.BitmapProperties1(new D2D1.PixelFormat(DXGI.Format.R8G8B8A8_UNorm, D2D1.AlphaMode.Premultiplied),
                    D2DFactory.DesktopDpi.Width, D2DFactory.DesktopDpi.Height, D2D1.BitmapOptions.CannotDraw | D2D1.BitmapOptions.Target)
                );
            D2DContext.Target = D2DTarget;
            #endregion

            #region 2d resources
            SolidRedBrush = new D2D1.SolidColorBrush(D2DContext, Color.Red);
            SolidGreenBrush = new D2D1.SolidColorBrush(D2DContext, Color.Green);
            SolidBlueBrush = new D2D1.SolidColorBrush(D2DContext, Color.Blue);
            SolidWhiteBrush = new D2D1.SolidColorBrush(D2DContext, Color.White);
            SolidBlackBrush = new D2D1.SolidColorBrush(D2DContext, Color.Black);
            SolidGrayBrush = new D2D1.SolidColorBrush(D2DContext, Color.LightGray);

            FontFactory = new DWrite.Factory();
            SegoeUI24 = new DWrite.TextFormat(FontFactory, "Segoe UI", 24f);
            SegoeUI14 = new DWrite.TextFormat(FontFactory, "Segoe UI", 14f);
            Consolas14 = new DWrite.TextFormat(FontFactory, "Consolas", 14f);
            #endregion

            #region viewport & render target
            Viewport = new Viewport(0, 0, width, height);
            Context.Rasterizer.SetViewport(Viewport);

            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) {
                renderTargetView = new D3D11.RenderTargetView(Device, backBuffer);
            }
            #endregion

            #region blend states
            D3D11.BlendStateDescription opaqueDesc = new D3D11.BlendStateDescription();
            opaqueDesc.RenderTarget[0].IsBlendEnabled = false;
            opaqueDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
            blendStateOpaque = new D3D11.BlendState(Device, opaqueDesc);

            D3D11.BlendStateDescription alphaDesc = new D3D11.BlendStateDescription();
            alphaDesc.RenderTarget[0].IsBlendEnabled = true;
            alphaDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            alphaDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
            alphaDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.One;
            alphaDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.Zero;
            alphaDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
            blendStateTransparent = new D3D11.BlendState(Device, alphaDesc);
            #endregion

            #region depth buffer & depth stencil states
            D3D11.Texture2DDescription depthDescription = new D3D11.Texture2DDescription() {
                Format = DXGI.Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };

            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(Device, depthDescription))
                depthStencilView = new D3D11.DepthStencilView(Device, depthTexture);

            Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            
            depthStencilStateDefault = new D3D11.DepthStencilState(Device, new D3D11.DepthStencilStateDescription() {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthComparison = D3D11.Comparison.Less,
                DepthWriteMask = D3D11.DepthWriteMask.All
            });

            depthStencilStateNoDepth = new D3D11.DepthStencilState(Device, new D3D11.DepthStencilStateDescription() {
                IsDepthEnabled = false,
                IsStencilEnabled = false,
                DepthComparison = D3D11.Comparison.Less,
                DepthWriteMask = D3D11.DepthWriteMask.Zero
            });

            Context.OutputMerger.SetDepthStencilState(depthStencilStateDefault);
            #endregion

            #region rasterizer states
            rasterizerStateSolidCullBack = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Solid,
                CullMode = D3D11.CullMode.Back,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateWireframeCullBack = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Wireframe,
                CullMode = D3D11.CullMode.Back,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateSolidNoCull = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Solid,
                CullMode = D3D11.CullMode.None,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateWireframeNoCull = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Wireframe,
                CullMode = D3D11.CullMode.None,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateSolidCullFront = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Solid,
                CullMode = D3D11.CullMode.Front,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateWireframeCullFront = new D3D11.RasterizerState(Device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Wireframe,
                CullMode = D3D11.CullMode.Front,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            #endregion

            constants = new Constants();
            constantBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            
            #region axis lines & line shader
            axisBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.VertexBuffer, new VertexColor[] {
                new VertexColor(new Vector3(0, 0,  1000), Color.Blue),
                new VertexColor(new Vector3(0, 0, -1000), Color.Blue),

                new VertexColor(new Vector3(-1000, 0, 0), Color.Red),
                new VertexColor(new Vector3( 1000, 0, 0), Color.Red),

                new VertexColor(new Vector3(0, -1000, 0), Color.Green),
                new VertexColor(new Vector3(0,  1000, 0), Color.Green),
            });
            Matrix m = Matrix.Identity;
            axisConsts = D3D11.Buffer.Create(Device, D3D11.BindFlags.ConstantBuffer, ref m);
            #endregion
        }

        public D2D1.Brush CreateBrush(Color color) {
            return new D2D1.SolidColorBrush(D2DContext, color);
        }

        public void Resize(int width, int height) {
            ResolutionX = width; ResolutionY = height;
            renderTargetView.Dispose();
            depthStencilView.Dispose();
            D2DTarget.Dispose();
            D2DContext.Dispose();

            Camera.AspectRatio = width / (float)height;

            swapChain.ResizeBuffers(swapChain.Description.BufferCount, width, height, DXGI.Format.Unknown, DXGI.SwapChainFlags.None);
            
            D2DContext = new D2D1.DeviceContext(D2DDevice, D2D1.DeviceContextOptions.None);
            using (DXGI.Surface surface = swapChain.GetBackBuffer<DXGI.Surface>(0))
                D2DTarget = new D2D1.Bitmap1(D2DContext, surface,
                    new D2D1.BitmapProperties1(new D2D1.PixelFormat(DXGI.Format.R8G8B8A8_UNorm, D2D1.AlphaMode.Premultiplied),
                    D2DFactory.DesktopDpi.Height, D2DFactory.DesktopDpi.Width, D2D1.BitmapOptions.CannotDraw | D2D1.BitmapOptions.Target)
                );
            D2DContext.Target = D2DTarget;

            // render target
            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0))
                renderTargetView = new D3D11.RenderTargetView(Device, backBuffer);
            
            // depth buffer
            D3D11.Texture2DDescription depthDescription = new D3D11.Texture2DDescription() {
                Format = DXGI.Format.D16_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(Device, depthDescription))
                depthStencilView = new D3D11.DepthStencilView(Device, depthTexture);

            Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            Viewport = new Viewport(0, 0, width, height);
            Context.Rasterizer.SetViewport(Viewport);
        }
        
        public Vector2? WorldToScreen(Vector3d point) {
            point -= Camera.Position;
            point.Normalize();

            Vector3 vec = Viewport.Project(point, Camera.Projection, Camera.View, Matrix.Identity);
            if (vec.Z < 0)
                return new Vector2(vec.X, vec.Y);
            return null;
        }

        public void BeginDrawFrame() {
            constants.View = Camera.View;
            constants.Projection = Camera.Projection;
            constants.cameraDirection = Camera.View.Forward;
            constants.farPlane = Camera.zFar;

            Context.UpdateSubresource(ref constants, constantBuffer);
        }
        
        public void Clear(Color color, bool depth = true) {
            Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            Context.ClearRenderTargetView(renderTargetView, color);
            if (depth)
                Context.ClearDepthStencilView(depthStencilView, D3D11.DepthStencilClearFlags.Depth, 1f, 0);
        }
        
        public void EndDrawFrame() {
            swapChain.Present(1, DXGI.PresentFlags.None);
        }

        public void DrawAxis() {
            Matrix mat = Matrix.Translation(-Camera.Position);
            Context.UpdateSubresource(ref mat, axisConsts);

            Shaders.LineShader.Set(this);

            Context.VertexShader.SetConstantBuffer(1, axisConsts);
            Context.PixelShader.SetConstantBuffer(1, axisConsts);

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(axisBuffer, Utilities.SizeOf<VertexColor>(), 0));

            Context.Draw(6, 0);
        }
        
        public void Dispose() {
            SolidWhiteBrush.Dispose();
            SolidBlackBrush.Dispose();
            SolidRedBrush.Dispose();
            SolidGreenBrush.Dispose();
            SolidBlueBrush.Dispose();
            SolidGrayBrush.Dispose();

            D2DTarget.Dispose();
            D2DDevice.Dispose();
            D2DContext.Dispose();
            D2DFactory.Dispose();

            blendStateOpaque.Dispose();
            blendStateTransparent.Dispose();

            rasterizerStateSolidCullBack.Dispose();
            rasterizerStateWireframeCullBack.Dispose();
            rasterizerStateSolidNoCull.Dispose();
            rasterizerStateWireframeNoCull.Dispose();
            depthStencilStateDefault.Dispose();
            depthStencilStateNoDepth.Dispose();
            
            constantBuffer.Dispose();
            axisBuffer.Dispose();
            depthStencilView.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            Device.Dispose();
            Context.Dispose();
        }
    }
}
