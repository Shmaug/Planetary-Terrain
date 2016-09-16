using System;
using SharpDX;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;

namespace NoiseTests {
    class Renderer : IDisposable {
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
        
        public D3D11.RenderTargetView renderTargetView { get; private set; }
        
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

            #region render target
            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) {
                renderTargetView = new D3D11.RenderTargetView(Device, backBuffer);
            }
            #endregion

            Context.Rasterizer.SetViewport(0, 0, 512, 512);
        }

        public D2D1.Brush CreateBrush(Color color) {
            return new D2D1.SolidColorBrush(D2DContext, color);
        }
        
        public void Clear(Color color) {
            Context.OutputMerger.SetTargets(renderTargetView);

            Context.ClearRenderTargetView(renderTargetView, color);
        }
        
        public void Present() {
            swapChain.Present(1, DXGI.PresentFlags.None);
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

            renderTargetView.Dispose();
            swapChain.Dispose();
            Device.Dispose();
            Context.Dispose();
        }
    }
}
