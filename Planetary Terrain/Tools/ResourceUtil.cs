using SharpDX;
using SharpDX.DXGI;
using SharpDX.WIC;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class ResourceUtil {
        public static D3D11.Texture2D LoadTexture(D3D11.Device device, string path) {
            ImagingFactory factory = new ImagingFactory2();
            BitmapDecoder decoder = new BitmapDecoder(factory, path, DecodeOptions.CacheOnDemand);
            FormatConverter converter = new FormatConverter(factory);
            converter.Initialize(
                decoder.GetFrame(0),
                PixelFormat.Format32bppPRGBA,
                BitmapDitherType.None,
                null,
                0.0,
                BitmapPaletteType.Custom);

            BitmapSource source = converter as BitmapSource;

            int stride = source.Size.Width * 4;
            using (DataStream buffer = new DataStream(source.Size.Height * stride, true, true)) {
                source.CopyPixels(stride, buffer);
                return new D3D11.Texture2D(device, new D3D11.Texture2DDescription() {
                    Width = source.Size.Width,
                    Height = source.Size.Height,
                    ArraySize = 1,
                    BindFlags = D3D11.BindFlags.ShaderResource,
                    Usage = D3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = D3D11.CpuAccessFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = D3D11.ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0)
                }, new DataRectangle(buffer.DataPointer, stride));
            }
        }
        public static D3D11.Texture2D LoadCubemap(D3D11.Device device, string path) {
            D3D11.Texture2D tex = new D3D11.Texture2D(device, new D3D11.Texture2DDescription() {
                Width = 1024,
                Height = 1024,
                ArraySize = 6,
                BindFlags = D3D11.BindFlags.ShaderResource,
                Usage = D3D11.ResourceUsage.Immutable,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = D3D11.ResourceOptionFlags.TextureCube,
                SampleDescription = new SampleDescription(1, 0)
            });


            return tex;
        }
    }
}
