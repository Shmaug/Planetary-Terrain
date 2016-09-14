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
        /// The total possible terrain displacement, additional to Radius
        /// </summary>
        public double TerrainHeight;
        
        /// <summary>
        /// The planet's atmosphere
        /// </summary>
        public Atmosphere Atmosphere;
        
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
            public Vector3 lightDirection;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }
        
        public Planet(string name, Vector3d pos, double radius, double mass, double terrainHeight, Atmosphere atmosphere = null) : base(pos, radius, mass) {
            Label = name;
            Radius = radius;
            TerrainHeight = terrainHeight;
            Atmosphere = atmosphere;

            if (atmosphere != null)
                atmosphere.Planet = this;

            initialize();
        }

        void initialize() {
            double s = 1.41421356237 * Radius;

            MaxChunkSize = s;

            BaseChunks = new QuadTree[6];
            BaseChunks[0] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.Up, MathTools.RotationXYZ(0, 0, 0));
            BaseChunks[1] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.Down, MathTools.RotationXYZ(MathUtil.Pi, 0, 0));
            BaseChunks[2] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.Left, MathTools.RotationXYZ(0, 0, MathUtil.PiOverTwo));
            BaseChunks[3] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.Right, MathTools.RotationXYZ(0, 0, -MathUtil.PiOverTwo));
            BaseChunks[4] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.ForwardLH, MathTools.RotationXYZ(MathUtil.PiOverTwo, 0, 0));
            BaseChunks[5] = new QuadTree(this, s, null, s * .5f * (Vector3d)Vector3.BackwardLH, MathTools.RotationXYZ(-MathUtil.PiOverTwo, 0, 0));

            for (int i = 0; i < BaseChunks.Length; i++)
                BaseChunks[i].Generate();
        }
        
        double height(Vector3d direction) {
            Vector3d p = direction;

            //double hill = hillNoise.GetNoise(p);
            double mountain = Noise.noise(direction, -1, 1, .002d, .7d) * Noise.ridgenoise(direction, 150, 5, .3f, .8f);

            return mountain;
        }

        public override double GetHeight(Vector3d direction) {
            return Radius + height(direction) * TerrainHeight;
        }
        public override Vector2 GetTemp(Vector3d direction) {
            float y = MathUtil.Clamp((float)Math.Abs(direction.Y - .1f), 0, 1);
            y = y * y * y;
            float temp = (float)Noise.noise(direction, 10, 5, .3f, .8f) - y;
            float humid = (float)Noise.noise(direction, 10, 4, .1f, .8f);

            return new Vector2(temp, humid);
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
            for (int i = 0; i < BaseChunks.Length; i++)
                BaseChunks[i].SplitDynamic(camera.Position, device);
        }

        public override void Draw(Renderer renderer, Body sun) {
            // Get the entire planet's scale and scaled position
            // This ensures the planet is always within the clipping planes
            Vector3d pos;
            double scale;
            renderer.Camera.AdjustPositionRelative(Position, out pos, out scale);
            if (scale * Radius < 1)
                return;

            constants.lightDirection = Vector3d.Normalize(Position - sun.Position);

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

            for (int i = 0; i < BaseChunks.Length; i++)
                BaseChunks[i].Draw(renderer, pos, scale);
            
            Atmosphere?.Draw(renderer, pos, scale);
        }

        public override void Dispose() {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            constBuffer?.Dispose();

            for (int i = 0; i < BaseChunks.Length; i++)
                BaseChunks[i].Dispose();
            
            Atmosphere?.Dispose();
        }
    }
}
