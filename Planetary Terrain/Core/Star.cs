using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Star : CelestialBody, IDisposable {
        
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.Texture2D colorMap;
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.ShaderResourceView colorMapView;
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.SamplerState colorMapSampler;

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
        struct Constants {
            public float num;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }
        
        public Star(string name, Vector3d pos, double radius, double mass) : base(pos, radius, mass) {
            Name = name;
            Radius = radius;
        }
        
        public override double GetHeight(Vector3d direction) {
            return Radius;
        }
        public override void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double h) {
            data = Vector2.Zero;
            h = 1;
            
            float y = (float)Math.Abs(direction.Y);
            float temp = (float)Noise.SmoothSimplex(direction * 10, 4, .75f, .8f) + y;
            float humid = (float)Noise.SmoothSimplex(direction * 128, 7, .0008f, .8f);

            data = 1 - new Vector2(temp, humid);
        }

        public void SetColormap(string file, D3D11.Device device) {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            colorMap = (D3D11.Texture2D)ResourceUtil.LoadFromFile(device, file, out colorMapView);

            colorMapSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
        }

        public override void UpdateLOD(double deltaTime, D3D11.Device device, Camera camera) {
            Vector3d dir = camera.Position - Position;
            double height = dir.Length();
            dir /= height;
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].SplitDynamic(dir, height, device);
        }

        public override void Draw(Renderer renderer) {
            // Get the entire planet's scale and scaled position
            // This ensures the planet is always within the clipping planes
            Vector3d pos;
            double scale;
            renderer.Camera.GetScaledSpace(Position, out pos, out scale);
            if (scale * Radius < 1)
                return;

            // create/update constant buffer
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            Shaders.StarShader.Set(renderer);
            // set constant buffer
            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            // color map
            renderer.Context.PixelShader.SetShaderResource(0, colorMapView);
            renderer.Context.PixelShader.SetSampler(0, colorMapSampler);
            
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, false, pos, scale);
        }

        public override void Dispose() {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            constBuffer?.Dispose();

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Dispose();
        }
    }
}
