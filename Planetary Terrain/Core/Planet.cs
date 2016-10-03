using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Planet : Body, IDisposable {
        /// <summary>
        /// The total possible terrain displacement is Radius +/- TerrainHeight
        /// </summary>
        public double TerrainHeight;
        
        /// <summary>
        /// The planet's atmosphere
        /// </summary>
        public Atmosphere Atmosphere;

        public bool HasOcean;
        public Color OceanColor;
        public double OceanScaleHeight;
        
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

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
        struct Constants {
            public Vector3 lightDirection;
            public float oceanScaleHeight;
            public Vector3 oceanColor;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }
        
        public Planet(string name, Vector3d pos, double radius, double mass, double terrainHeight, Atmosphere atmosphere = null, bool ocean = false) : base(pos, radius, mass) {
            Label = name;
            Radius = radius;
            TerrainHeight = terrainHeight;
            Atmosphere = atmosphere;

            if (atmosphere != null)
                atmosphere.Planet = this;

            HasOcean = ocean;
            OceanScaleHeight = .5;
            OceanColor = Color.MediumBlue;
        }

        public double min, max;
        double height(Vector3d direction) {
            double total = 0;

            double r2 = Math.Min(Math.Pow(Noise.Ridged(direction * 1000 + new Vector3(1000), 2, .01f, .45f) + .7, 2), 1);
            r2 = 1.0 - r2;

            double rough = 1.0 - Noise.Fractal(direction * 1000 + new Vector3(2000), 11, .03f, .5f);

            rough *= r2;

            double smooth = Noise.Fractal(direction * 200 + new Vector3d(-5000), 4, .02f, .3f);
            smooth *= 1 - rough;

            total = smooth + rough;
            
            min = Math.Min(min, total);
            max = Math.Max(max, total);

            return total;
        }

        public override double GetHeight(Vector3d direction) {
            return Radius + height(direction) * TerrainHeight;
        }
        public override void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double h) {
            data = Vector2.Zero;
            h = height(direction);

            double temp, humid;
            temp = humid = 0;

            float p = MathUtil.Clamp((float)Math.Abs(direction.Y) - .3f, 0, 1);
            p *= p;
            float y = (float)(height(direction) * .1);

            temp = Noise.SmoothSimplex(direction * 2, 5, .3f, .8f)*.5d+.5d - p;
            //humid = Noise.SmoothSimplex(direction * 5, 4, .1f, .8f)*.5d+.5d - .1f;

            if (HasOcean)
                humid += (float)Math.Pow(1.0 - (h - OceanScaleHeight), 2);

            //double mountain = Math.Min(Math.Pow(Noise.Ridged(direction * 1000 + new Vector3(1000), 2, .01f, .45f) + .7, 2), 1);
            //mountain = 1.0 - mountain;

            //temp = 1.0 - Noise.Fractal(direction * 1000 + new Vector3(2000), 11, .03f, .5f);

            //temp *= mountain;

            data = new Vector2((float)temp, (float)humid);
        }

        public void SetColormap(D3D11.Texture2D map, D3D11.Device device) {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            colorMap = map;
            colorMapView = new D3D11.ShaderResourceView(device, colorMap);
            colorMapSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
        }

        public override void Update(D3D11.Device device, Camera camera) {
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].SplitDynamic(camera.Position, device);
        }

        public override void Draw(Renderer renderer, Body sun) {
            // Get the entire planet's scale and scaled position
            // This ensures the planet is always within the clipping planes
            Vector3d pos;
            double scale;
            renderer.Camera.GetScaledSpace(Position, out pos, out scale);
            if (scale * Radius < 1)
                return;

            constants.lightDirection = Vector3d.Normalize(Position - sun.Position);
            constants.oceanScaleHeight = (float)OceanScaleHeight;
            constants.oceanColor = OceanColor.ToVector3();

            // create/update constant buffer
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            Shaders.PlanetShader.Set(renderer);
            // set constant buffer
            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            // color map
            renderer.Context.PixelShader.SetShaderResource(0, colorMapView);
            renderer.Context.PixelShader.SetSampler(0, colorMapSampler);
            
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, false, pos, scale);

            // set water shader
            Shaders.WaterShader.Set(renderer);
            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, true, pos, scale);

            //Atmosphere?.Draw(renderer, pos, scale);
        }

        public override void Dispose() {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            constBuffer?.Dispose();

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Dispose();
            
            Atmosphere?.Dispose();
        }
    }
}
