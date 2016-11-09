using System;
using SharpDX;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.IO;

namespace Planetary_Terrain {
    class ResourceUtil {
        public static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, string filename) {
            var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                factory,
                filename,
                SharpDX.WIC.DecodeOptions.CacheOnDemand
                );

            var result = new SharpDX.WIC.FormatConverter(factory);

            result.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);

            return result;
        }
        public static D3D11.Texture2D CreateTexture2DFromBitmap(D3D11.Device device, SharpDX.WIC.BitmapSource bitmapSource) {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true)) {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new D3D11.Texture2D(device, new D3D11.Texture2DDescription() {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = D3D11.BindFlags.ShaderResource,
                    Usage = D3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = D3D11.CpuAccessFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = D3D11.ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                }, new DataRectangle(buffer.DataPointer, stride));
            }
        }

        private unsafe static D3D11.Resource LoadDDSFromBuffer(D3D11.Device device, byte[] buffer, out D3D11.ShaderResourceView srv) {
            D3D11.Resource result = null;
            srv = null;
            if (buffer == null)
                throw new ArgumentNullException("buffer");


            int size = buffer.Length;

            // If buffer is allocated on Larget Object Heap, then we are going to pin it instead of making a copy.
            if (size > (85 * 1024)) {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                DDSHelper.CreateDDSTextureFromMemory(device, handle.AddrOfPinnedObject(), size, out result, out srv);
            }

            fixed (void* pbuffer = buffer)
                DDSHelper.CreateDDSTextureFromMemory(device, (IntPtr)pbuffer, size, out result, out srv);

            return result;
        }

        public static D3D11.Resource LoadFromFile(D3D11.Device device, string fileName, out D3D11.ShaderResourceView srv) {
            if (!File.Exists(fileName)) {
                srv = null;
                return null;
            }
            if (Path.GetExtension(fileName).ToLower() == ".dds") {
                var result = LoadDDSFromBuffer(device, SharpDX.IO.NativeFile.ReadAllBytes(fileName), out srv);
                return result;
            } else {
                SharpDX.WIC.ImagingFactory2 fac = new SharpDX.WIC.ImagingFactory2();
                var bs = LoadBitmap(fac, fileName);
                var texture = CreateTexture2DFromBitmap(device, bs);
                srv = new D3D11.ShaderResourceView(device, texture);
                fac.Dispose();
                return texture;
            }
        }
    }
}
