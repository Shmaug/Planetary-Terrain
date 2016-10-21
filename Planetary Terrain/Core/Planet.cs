using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Planet : CelestialBody, IDisposable {
        /// <summary>
        /// The total possible terrain displacement is Radius +/- TerrainHeight
        /// </summary>
        public double TerrainHeight;
        
        /// <summary>
        /// The planet's atmosphere
        /// </summary>
        public Atmosphere Atmosphere;

        public bool HasOcean;
        public double OceanHeight;
        public Color OceanColor;
        
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

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct Constants {
            [FieldOffset(0)]
            public Vector3 lightDirection;
            [FieldOffset(12)]
            public float oceanLevel;
            [FieldOffset(16)]
            public Vector3 oceanColor;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }

        public Planet(string name, Vector3d pos, double radius, double mass, double terrainHeight, Atmosphere atmosphere = null, bool ocean = false) : base(pos, radius, mass) {
            Name = name;
            Radius = radius;
            TerrainHeight = terrainHeight;
            Atmosphere = atmosphere;

            if (atmosphere != null)
                atmosphere.Planet = this;

            HasOcean = ocean;
            OceanHeight = .5;
            OceanColor = new Color(45, 100, 245);
        }

        public double min=1, max=-1;
        double height(Vector3d direction) {
            double total = 0;

            // TODO: Height function

            double rough = Noise.Simplex(direction * 50 + new Vector3d(-5000));

            double mntn = Noise.Fractal(direction * 1000 + new Vector3(2000), 11, .03f, .5f);
            double flat = Noise.Ridged(direction * 100 + new Vector3(1000), 2, .01f, .45f);

            rough = rough * rough * rough;

            flat *= rough;
            mntn *= 1.0 - rough;
            
            total = mntn + flat;
            
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
                humid += (float)Math.Pow(1.0 - Math.Max(h, 0.0), 2);

            //double mountain = Math.Min(Math.Pow(Noise.Ridged(direction * 1000 + new Vector3(1000), 2, .01f, .45f) + .7, 2), 1);
            //mountain = 1.0 - mountain;

            //temp = 1.0 - Noise.Fractal(direction * 1000 + new Vector3(2000), 11, .03f, .5f);

            //temp *= mountain;

            data = new Vector2((float)temp, (float)humid);
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
            Atmosphere?.UpdateLOD(device, camera);
        }

        public override void Draw(Renderer renderer) {
            Profiler.Begin(Name + " Draw");
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;

            // Get the entire planet's scale and scaled position
            // This ensures the planet is always within the clipping planes
            Vector3d pos;
            double scale;
            renderer.Camera.GetScaledSpace(Position, out pos, out scale);
            if (scale * Radius < 1)
                return;

            Star s = StarSystem.ActiveSystem.GetNearestStar(Position);
            if (s != null)
                constants.lightDirection = Vector3d.Normalize(Position - s.Position);
            else
                constants.lightDirection = new Vector3d();
            constants.oceanLevel = (float)OceanHeight;
            constants.oceanColor = OceanColor.ToVector3();

            // create/update constant buffer
            if (constBuffer == null) constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            if (Atmosphere != null){
                Profiler.Begin(Name + " Atmosphere Draw");
                // draw atmosphere behind planet
                Atmosphere?.Draw(renderer, pos, scale);
                Profiler.End();
                Profiler.Resume(Name + " Draw");
            }

            Shaders.PlanetShader.Set(renderer);

            // atmosphere constants
            if (Atmosphere != null) {
                renderer.Context.VertexShader.SetConstantBuffers(3, Atmosphere.constBuffer);
                renderer.Context.PixelShader.SetConstantBuffers(3, Atmosphere.constBuffer);
            }

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
            
            // atmosphere constants
            if (Atmosphere != null) {
                renderer.Context.VertexShader.SetConstantBuffers(3, Atmosphere.constBuffer);
                renderer.Context.PixelShader.SetConstantBuffers(3, Atmosphere.constBuffer);
            }

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, true, pos, scale);

            Profiler.End();
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
