using System;
using SharpDX;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Planetary_Terrain {
    class Renderer : IDisposable {
        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 144)]
        struct RendererConstants {
            public Matrix View;
            public Matrix Projection;
            public float C;
            public float FC;
        }
        RendererConstants constants;
        public D3D11.Buffer constantBuffer { get; private set; }

        [StructLayout(LayoutKind.Explicit, Size = 160)]
        struct AeroFXConstants {
            [FieldOffset(0)]
            public Matrix World;
            [FieldOffset(64)]
            public Matrix WorldInverseTranspose;
            [FieldOffset(128)]
            public Vector3 VelocityDirection;
            [FieldOffset(140)]
            public float Size;
            [FieldOffset(144)]
            public float Step;
        };
        AeroFXConstants aeroFXConstants;
        
        public int ResolutionX, ResolutionY;

        #region 2d vars
        public D2D1.Device D2DDevice { get; private set; }
        public D2D1.DeviceContext D2DContext { get; private set; }
        public D2D1.Factory D2DFactory { get; private set; }
        public D2D1.Bitmap D2DTarget { get; private set; }

        public DWrite.Factory FontFactory { get; private set; }
        public DWrite.TextFormat SegoeUI24 { get; private set; }
        public DWrite.TextFormat SegoeUI14 { get; private set; }
        public DWrite.TextFormat Consolas14 { get; private set; }

        public Dictionary<string, D2D1.Brush> Brushes;
        #endregion

        #region 3d vars
        private DXGI.SwapChain swapChain;
        public D3D11.Device Device { get; private set;}
        public D3D11.DeviceContext Context { get; private set; }
        #endregion

        #region states and views
        public Viewport Viewport { get; private set; }

        public D3D11.RenderTargetView renderTargetView { get; private set; }
        public D3D11.DepthStencilView depthStencilView { get; private set; }
        
        public D3D11.DepthStencilState depthStencilStateDefault { get; private set; }
        public D3D11.DepthStencilState depthStencilStateNoDepth { get; private set; }

        public D3D11.RasterizerState rasterizerStateSolidCullBack { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeCullBack { get; private set; }
        public D3D11.RasterizerState rasterizerStateSolidNoCull { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeNoCull { get; private set; }
        public D3D11.RasterizerState rasterizerStateSolidCullFront { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeCullFront { get; private set; }
        
        public D3D11.ShaderResourceView WhiteTextureView { get; private set; }
        public D3D11.ShaderResourceView BlackTextureView { get; private set; }
        public D3D11.SamplerState AnisotropicSampler { get; private set; }

        public D3D11.BlendState blendStateOpaque { get; private set; }
        public D3D11.BlendState blendStateTransparent { get; private set; }
        
        private D3D11.RenderTargetView aeroFXRenderTargetView;
        private D3D11.ShaderResourceView aeroFXShaderResourceView;
        #endregion

        int SampleCount = 8;
        int SampleQuality = 0;

        public bool DrawWireframe = false;
        public bool DrawGUI = true;

        public double TotalTime;

        public Camera Camera;

        D3D11.Buffer aeroFXBuffer;
        D3D11.Buffer screenVBuffer;

        Game game;

        public Renderer(Game game, SharpDX.Windows.RenderForm renderForm) {
            this.game = game;
            int width = renderForm.ClientSize.Width, height = renderForm.ClientSize.Height;
            ResolutionX = width; ResolutionY = height;

            D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport;

#if DEBUG
            creationFlags |= D3D11.DeviceCreationFlags.Debug;
#endif

            #region 3d device & context
            DXGI.SwapChainDescription swapChainDesc = new DXGI.SwapChainDescription() {
                ModeDescription = new DXGI.ModeDescription(width, height, new DXGI.Rational(60, 1), DXGI.Format.R8G8B8A8_UNorm),
                SampleDescription = new DXGI.SampleDescription(SampleCount, SampleQuality),
                Usage = DXGI.Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true,
                SwapEffect = DXGI.SwapEffect.Discard
            };
            D3D11.Device device;
            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, creationFlags, swapChainDesc, out device, out swapChain);
            Device = device;
            Context = Device.ImmediateContext;

            swapChain.GetParent<DXGI.Factory>().MakeWindowAssociation(renderForm.Handle, DXGI.WindowAssociationFlags.IgnoreAll);
            #endregion
            #region 2d device & context
            DXGI.Device dxgiDevice = Device.QueryInterface<D3D11.Device1>().QueryInterface<DXGI.Device2>();
            D2DDevice = new D2D1.Device(dxgiDevice);
            D2DContext = new D2D1.DeviceContext(D2DDevice, D2D1.DeviceContextOptions.None);
            D2DFactory = D2DDevice.Factory;
            #endregion
            #region 2d brushes/fonts
            Brushes = new Dictionary<string, D2D1.Brush>();
            Brushes.Add("Red", new D2D1.SolidColorBrush(D2DContext, Color.Red));
            Brushes.Add("Green", new D2D1.SolidColorBrush(D2DContext, Color.Green));
            Brushes.Add("Blue", new D2D1.SolidColorBrush(D2DContext, Color.Blue));
            Brushes.Add("White", new D2D1.SolidColorBrush(D2DContext, Color.White));
            Brushes.Add("Black", new D2D1.SolidColorBrush(D2DContext, Color.Black));
            Brushes.Add("TransparentWhite", new D2D1.SolidColorBrush(D2DContext, new Color(1, 1, 1, .5f)));
            Brushes.Add("TransparentBlack", new D2D1.SolidColorBrush(D2DContext, new Color(0, 0, 0, .5f)));
            Brushes.Add("LightGray", new D2D1.SolidColorBrush(D2DContext, Color.LightGray));
            Brushes.Add("OrangeRed", new D2D1.SolidColorBrush(D2DContext, Color.OrangeRed));
            Brushes.Add("CornflowerBlue", new D2D1.SolidColorBrush(D2DContext, Color.CornflowerBlue));
            Brushes.Add("Yellow", new D2D1.SolidColorBrush(D2DContext, Color.Yellow));
            Brushes.Add("Magenta", new D2D1.SolidColorBrush(D2DContext, Color.Magenta));
            Brushes.Add("RosyBrown", new D2D1.SolidColorBrush(D2DContext, Color.RosyBrown));

            FontFactory = new DWrite.Factory();
            SegoeUI24 = new DWrite.TextFormat(FontFactory, "Segoe UI", 24f);
            SegoeUI14 = new DWrite.TextFormat(FontFactory, "Segoe UI", 14f);
            Consolas14 = new DWrite.TextFormat(FontFactory, "Consolas", 14f);
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
            #region blank textures
            D3D11.Texture2D wtex = new D3D11.Texture2D(Device, new D3D11.Texture2DDescription() {
                ArraySize = 1,
                Width = 1,
                Height = 1,
                Format = DXGI.Format.R32G32B32A32_Float,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                MipLevels = 0,
                Usage = D3D11.ResourceUsage.Default,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                BindFlags = D3D11.BindFlags.ShaderResource,
                OptionFlags = D3D11.ResourceOptionFlags.None
            });
            Context.UpdateSubresource(new Vector4[] { Vector4.One }, wtex);
            WhiteTextureView = new D3D11.ShaderResourceView(Device, wtex);

            D3D11.Texture2D btex = new D3D11.Texture2D(Device, new D3D11.Texture2DDescription() {
                ArraySize = 1,
                Width = 1,
                Height = 1,
                Format = DXGI.Format.R32G32B32A32_Float,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                MipLevels = 0,
                Usage = D3D11.ResourceUsage.Default,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                BindFlags = D3D11.BindFlags.ShaderResource,
                OptionFlags = D3D11.ResourceOptionFlags.None
            });
            Context.UpdateSubresource(new Vector4[] { new Vector4(0,0,0,1) }, btex);
            BlackTextureView = new D3D11.ShaderResourceView(Device, btex);

            AnisotropicSampler = new D3D11.SamplerState(Device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Wrap,
                AddressV = D3D11.TextureAddressMode.Wrap,
                AddressW = D3D11.TextureAddressMode.Wrap,
                Filter = D3D11.Filter.Anisotropic,
            });
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
            #region screen vertx & constants
            screenVBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.VertexBuffer, new VertexTexture[] {
                new VertexTexture(new Vector3(-1,-1,0), new Vector2(0,0)),
                new VertexTexture(new Vector3( 1,-1,0), new Vector2(1,0)),
                new VertexTexture(new Vector3(-1, 1,0), new Vector2(0,1)),
                new VertexTexture(new Vector3( 1, 1,0), new Vector2(1,1)),
            });

            constants = new RendererConstants();
            constantBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            #endregion
            
            #region depthstencilstates
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

            Camera = new Camera(70, 16 / 9f);
            Resize(ResolutionX, ResolutionY);
        }

        public D2D1.Brush CreateBrush(Color color) {
            return new D2D1.SolidColorBrush(D2DContext, color);
        }

        public void Resize(int width, int height) {
            ResolutionX = width; ResolutionY = height;
            renderTargetView?.Dispose();
            depthStencilView?.Dispose();
            D2DTarget?.Dispose();
            D2DContext?.Dispose();
            aeroFXRenderTargetView?.Dispose();
            aeroFXShaderResourceView?.Dispose();

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
                Format = DXGI.Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new DXGI.SampleDescription(SampleCount, SampleQuality),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(Device, depthDescription))
                depthStencilView = new D3D11.DepthStencilView(Device, depthTexture);
            
            Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            // viewport
            Viewport = new Viewport(0, 0, width, height);
            Context.Rasterizer.SetViewport(Viewport);

            // render targets
            using (D3D11.Texture2D t = new D3D11.Texture2D(Device, new D3D11.Texture2DDescription() {
                Format = DXGI.Format.R8G8B8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = ResolutionX,
                Height = ResolutionY,
                SampleDescription = new DXGI.SampleDescription(SampleCount, SampleQuality),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            }))
                aeroFXRenderTargetView = new D3D11.RenderTargetView(Device, t);
            aeroFXShaderResourceView = new D3D11.ShaderResourceView(Device, aeroFXRenderTargetView.Resource);
        }
        
        public Vector3 WorldToScreen(Vector3d point) {
            point -= Camera.Position;
            point.Normalize();

            return Viewport.Project(point, Camera.Projection, Camera.View, Matrix.Identity);
        }

        public void BeginDrawFrame() {
            constants.View = Camera.View;
            constants.Projection = Camera.Projection;
            constants.C = 1;
            constants.FC = (float)(1.0 / (Math.Log(constants.C * Camera.zFar + 1) / Math.Log(2)));

            Context.UpdateSubresource(ref constants, constantBuffer);
        }
        
        public void Clear(Color color) {
            Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            Context.ClearRenderTargetView(renderTargetView, color);
            Context.ClearDepthStencilView(depthStencilView, D3D11.DepthStencilClearFlags.Depth, 1f, 0);
        }
        
        public void Present() {
            swapChain.Present(1, DXGI.PresentFlags.None);
        }

        public delegate void AeroDrawFunc(Renderer renderer);
        public void DrawAeroFX(Matrix world, Vector3d relativeVelocity, AeroDrawFunc DrawFunc) {
            double l = relativeVelocity.Length();
            if (l > 150) {
                //Context.ClearRenderTargetView(aeroFXRenderTargetView, Color.Transparent);
                //Context.OutputMerger.SetRenderTargets(depthStencilView, aeroFXRenderTargetView);

                Context.Rasterizer.State = DrawWireframe ? rasterizerStateWireframeNoCull : rasterizerStateSolidNoCull;
                Context.OutputMerger.DepthStencilState = depthStencilStateDefault;
                Context.OutputMerger.BlendState = blendStateTransparent;

                Shaders.AeroFXShader.Set(this);

                if (aeroFXBuffer == null)
                    aeroFXBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.ConstantBuffer, ref aeroFXConstants);
                aeroFXConstants.World = world;
                aeroFXConstants.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(world));
                aeroFXConstants.VelocityDirection = relativeVelocity / l;
              
                aeroFXConstants.Size = 7f * (float)Math.Log(.002 * (l + 500));
                float steps = 10f;
                for (float i = 1; i <= steps; i++) {
                    aeroFXConstants.Step = i / steps;
                    Context.UpdateSubresource(ref aeroFXConstants, aeroFXBuffer);
                    Context.VertexShader.SetConstantBuffer(1, aeroFXBuffer);
                    Context.PixelShader.SetConstantBuffer(1, aeroFXBuffer);
                    DrawFunc(this);
                }
                // TODO: aero fx
                // Draw aero FX to the main render target
                //Context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
                
                //Shaders.BlurShader.Set(this);
                //Context.PixelShader.SetShaderResource(0, aeroFXShaderResourceView);
                //Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                //Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(screenVBuffer, Utilities.SizeOf<VertexTexture>(), 0));
                //Context.Draw(4, 0);

                Context.Rasterizer.State = DrawWireframe ? rasterizerStateWireframeCullBack : rasterizerStateSolidCullBack;
            }
        }
        
        public void Dispose() {
            foreach (KeyValuePair<string, D2D1.Brush> p in Brushes)
                p.Value.Dispose();
            
            D2DTarget.Dispose();
            D2DDevice.Dispose();
            D2DContext.Dispose();
            D2DFactory.Dispose();

            BlackTextureView.Dispose();
            WhiteTextureView.Dispose();
            AnisotropicSampler.Dispose();
            
            blendStateOpaque.Dispose();
            blendStateTransparent.Dispose();

            rasterizerStateSolidCullBack.Dispose();
            rasterizerStateWireframeCullBack.Dispose();
            rasterizerStateSolidNoCull.Dispose();
            rasterizerStateWireframeNoCull.Dispose();
            depthStencilStateDefault.Dispose();
            depthStencilStateNoDepth.Dispose();
            
            aeroFXBuffer?.Dispose();
            constantBuffer.Dispose();
            depthStencilView.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            Device.Dispose();
            Context.Dispose();
        }
    }
}
