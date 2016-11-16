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
        struct CameraConstants {
            public Matrix View;
            public Matrix Projection;
            public float C;
            public float FC;
        }
        CameraConstants constants;
        public D3D11.Buffer constantBuffer { get; private set; }
        
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
        public D2D1.StrokeStyle DashStyle;
        #endregion

        #region 3d vars
        private DXGI.SwapChain swapChain;
        public D3D11.Device Device { get; private set;}
        public D3D11.DeviceContext Context { get; private set; }
        #endregion

        #region states and views
        public Viewport Viewport { get; private set; }
        
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
        #endregion

        int SampleCount = 8;
        int SampleQuality = 0;

        public bool DrawWireframe = false;
        public bool DrawGUI = true;

        public double TotalTime;

        public Camera ActiveCamera;
        public Camera MainCamera;
        public Camera ShadowCamera;
        public List<Camera> Cameras;
        
        Game game;

        public Renderer(Game game, SharpDX.Windows.RenderForm renderForm) {
            this.game = game;
            int width = renderForm.ClientSize.Width, height = renderForm.ClientSize.Height;
            ResolutionX = width; ResolutionY = height;
            
            #region 3d device & context
            D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport;
            #if DEBUG
            creationFlags |= D3D11.DeviceCreationFlags.Debug;
            #endif
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

            DashStyle = new D2D1.StrokeStyle(D2DFactory, new D2D1.StrokeStyleProperties() {
                StartCap = D2D1.CapStyle.Flat,
                DashCap = D2D1.CapStyle.Round,
                EndCap = D2D1.CapStyle.Flat,
                DashStyle = D2D1.DashStyle.Custom,
                DashOffset = 0,
                LineJoin = D2D1.LineJoin.Round,
                MiterLimit = 1
            }, new float[] { 4f, 4f });

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
            
            #region depth stencil states
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
                DepthWriteMask = D3D11.DepthWriteMask.All
            });

            Context.OutputMerger.SetDepthStencilState(depthStencilStateDefault);
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
            #region screen vertex & constants
            constants = new CameraConstants();
            constantBuffer = D3D11.Buffer.Create(Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            #endregion
            //swapChain.GetParent<DXGI.Factory>().MakeWindowAssociation(renderForm.Handle, DXGI.WindowAssociationFlags.);

            Cameras = new List<Camera>();
            MainCamera = Camera.CreatePerspective(MathUtil.DegreesToRadians(70), 16 / 9f);
            ActiveCamera = MainCamera;
            Cameras.Add(MainCamera);
            
            ShadowCamera = Camera.CreateOrthographic(500, 1);
            ShadowCamera.zNear = 0;
            ShadowCamera.zFar = 1000;
            ShadowCamera.CreateResources(Device, 1, 0, 1024, 1024);
            //Cameras.Add(ShadowCamera);
            // TODO: Shadow camera has no depth

            Resize(ResolutionX, ResolutionY);
        }

        public D2D1.Brush CreateBrush(Color color) {
            return new D2D1.SolidColorBrush(D2DContext, color);
        }

        public void Resize(int width, int height) {
            ResolutionX = width; ResolutionY = height;
            MainCamera.renderTargetView?.Dispose();
            MainCamera.depthStencilView?.Dispose();
            D2DTarget?.Dispose();
            D2DContext?.Dispose();

            MainCamera.AspectRatio = width / (float)height;

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
                MainCamera.renderTargetView = new D3D11.RenderTargetView(Device, backBuffer);
            
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
                MainCamera.depthStencilView = new D3D11.DepthStencilView(Device, depthTexture);
            
            // viewport
            Viewport = new Viewport(0, 0, width, height);
            Context.Rasterizer.SetViewport(Viewport);
        }
        
        public void SetCamera(Camera camera) {
            constants.View = camera.View;
            constants.Projection = camera.Projection;
            constants.C = 1f;
            constants.FC = (float)(1.0 / (Math.Log(constants.C * camera.zFar + 1) / Math.Log(2)));

            Context.UpdateSubresource(ref constants, constantBuffer);

            Context.OutputMerger.ResetTargets();
            Context.OutputMerger.SetTargets(camera.depthStencilView, camera.renderTargetView);
        }
        
        public void Clear(Color color) {
            foreach (Camera c in Cameras) {
                Context.ClearRenderTargetView(c.renderTargetView, color);
                Context.ClearDepthStencilView(c.depthStencilView, D3D11.DepthStencilClearFlags.Depth | D3D11.DepthStencilClearFlags.Stencil, 1f, 0);
            }
        }
        
        public void Present() {
            swapChain.Present(1, DXGI.PresentFlags.None);
        }
        
        public Vector3 WorldToScreen(Vector3d point, Camera camera) {
            point -= camera.Position;
            point.Normalize();

            return Viewport.Project(point * (camera.zFar + camera.zNear) * .5, camera.Projection, camera.View, Matrix.Identity);
        }
        public void ScreenToWorld(Vector2 point, Camera camera, out Vector3d origin, out Vector3d direction) {
            origin = camera.Position;
            direction = Vector3d.Normalize(Viewport.Unproject(new Vector3(point, .1f), camera.Projection, camera.View, Matrix.Identity));
        }

        public void Dispose() {
            foreach (KeyValuePair<string, D2D1.Brush> p in Brushes)
                p.Value.Dispose();
            DashStyle.Dispose();
            
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

            foreach (Camera c in Cameras)
                c.Dispose();
            constantBuffer.Dispose();
            
            swapChain.Dispose();
            Device.Dispose();
            Context.Dispose();
        }
    }
}
