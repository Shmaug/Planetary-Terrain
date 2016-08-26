using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace BetterTerrain {
    class Planet : IDisposable {
        public double Radius;
        public double TerrainHeight;
        public double MaxChunkSize;
        public double MinChunkSize = QuadTree.GridSize / 2;

        public QuadTree[] baseChunks;
        public Vector3d Position;

        [StructLayout(LayoutKind.Explicit)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix world;
        }
        Constants constants;
        D3D11.Buffer constBuffer;

        D3D11.Texture2D colorMap;
        D3D11.ShaderResourceView colorMapView;
        D3D11.SamplerState colorMapSampler;
        
        public Planet(D3D11.Device device, double radius, double terrainHeight) {
            Radius = radius;
            TerrainHeight = terrainHeight;
            Position = new Vector3d();

            double s = 1.41421356237 * Radius;

            MaxChunkSize = s;
            
            baseChunks = new QuadTree[6];
            baseChunks[0] = new QuadTree(this, s, null, s*.5f*Vector3.Up.ToDouble(),         MathTools.RotationXYZ(0, 0, 0));
            baseChunks[1] = new QuadTree(this, s, null, s*.5f*Vector3.Down.ToDouble(),       MathTools.RotationXYZ(MathUtil.Pi, 0, 0));
            baseChunks[2] = new QuadTree(this, s, null, s*.5f*Vector3.Left.ToDouble(),       MathTools.RotationXYZ(0, 0, MathUtil.PiOverTwo));
            baseChunks[3] = new QuadTree(this, s, null, s*.5f*Vector3.Right.ToDouble(),      MathTools.RotationXYZ(0, 0, -MathUtil.PiOverTwo));
            baseChunks[4] = new QuadTree(this, s, null, s*.5f*Vector3.ForwardLH.ToDouble(),  MathTools.RotationXYZ(MathUtil.PiOverTwo, 0, 0));
            baseChunks[5] = new QuadTree(this, s, null, s*.5f*Vector3.BackwardLH.ToDouble(), MathTools.RotationXYZ(-MathUtil.PiOverTwo, 0, 0));

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].Generate();
        }
        
        double height(Vector3d direction) {
            double hill = Noise.noise(direction, 100, 8, .003, .8);

            double m = Noise.ridgenoise(direction, Radius, 7, .002f, .7f) * 2;
            double mountain =
                MathTools.Clamp01(m * m * m);

            return hill + mountain;
        }

        public double GetHeight(Vector3d direction) {
            return Radius + height(direction) * TerrainHeight;
        }
        public Vector2 GetTemp(Vector3d direction) {
            double y = Math.Abs(direction.Y);
            float temp = (float)(Noise.noise(direction, 172, 6, .0006f, .9f) * (1 - y * y * y));
            float humid = (float)Noise.noise(direction, 128, 6, .0008f, .9f);

            return new Vector2(temp, humid);
        }
        public Vector3d GetPointOnSurface(Vector3d p) {
            p -= Position;
            p.Normalize();
            return p * Radius;
        }
        public double ArcLength(double distance) {
            double angle = 2 * Math.Asin(distance / 2 / Radius);
            return Radius * angle;
        }

        public void SetColormap(D3D11.Texture2D map, D3D11.Device device) {
            if (colorMapSampler != null)
                colorMapSampler.Dispose();
            if (colorMap != null)
                colorMap.Dispose();
            if (colorMapView != null)
                colorMapView.Dispose();

            colorMap = map;
            colorMapView = new D3D11.ShaderResourceView(device, colorMap);
            colorMapSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
        }

        public void Update(D3D11.Device device, Camera camera) {
            Vector3d dir = camera.Position - Position;
            double h = dir.Length();
            dir.Normalize();

            h -= GetHeight(dir);

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].SplitDynamic(dir, h, device);
        }
        public void Draw(Renderer renderer) {
            constants.world = Matrix.Transpose(Matrix.Identity);
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);
            
            renderer.Context.PixelShader.SetShaderResource(0, colorMapView);
            renderer.Context.PixelShader.SetSampler(0, colorMapSampler);

            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].Draw(renderer, renderer.camera.Position - Position);
        }

        public void Dispose() {
            if (colorMapSampler != null)
                colorMapSampler.Dispose();
            if (colorMap != null)
                colorMap.Dispose();
            if (colorMapView != null)
                colorMapView.Dispose();

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].Dispose();
        }
    }
}
