using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace BetterTerrain {
    class Planet : IDisposable {
        public float Radius;

        public Chunk[] baseChunks;

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

        public Matrix WorldMatrix;

        Chunk mkchunk(Vector3 dir, Vector3 rot, float s) {
            return new Chunk(this, s, null, MathTools.RotationXYZ(rot) * Matrix.Translation(dir * s * .5f));
        }
        public Planet(D3D11.Device device, float radius) {
            Radius = radius;

            WorldMatrix = Matrix.Identity;

            float s = 1.41421356237f * Radius;
            
            baseChunks = new Chunk[6];
            baseChunks[0] = mkchunk(Vector3.Up,         new Vector3(0, 0, 0), s);
            baseChunks[1] = mkchunk(Vector3.Down,       new Vector3(MathUtil.Pi, 0, 0), s);
            baseChunks[2] = mkchunk(Vector3.Left,       new Vector3(0, 0, MathUtil.PiOverTwo), s);
            baseChunks[3] = mkchunk(Vector3.Right,      new Vector3(0, 0, -MathUtil.PiOverTwo), s);
            baseChunks[4] = mkchunk(Vector3.ForwardLH,  new Vector3(MathUtil.PiOverTwo, 0, 0), s);
            baseChunks[5] = mkchunk(Vector3.BackwardLH, new Vector3(-MathUtil.PiOverTwo, 0, 0), s);

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].Generate();
        }

        public Vector3 ToWorldSpace(Vector3 pos) {
            return Vector3.Transform(pos, WorldMatrix).ToVector3();
        }
        public Vector3 WorldToPlanetSpace(Vector3 pos) {
            return Vector3.Transform(pos, Matrix.Invert(WorldMatrix)).ToVector3();
        }

        float height(Vector3 direction) {
            float hill = Noise.noise(direction, 100, 8, .003f, .8f);

            float m = Noise.noise(direction, Radius, 7, .002f, .7f) * 2;
            float mountain =
                MathUtil.Clamp(m*m*m, 0, 1) * 
                Noise.ridgenoise(direction, 500, 11, .03f, .5f) * 2;

            return hill + mountain;
        }

        public float GetHeight(Vector3 direction) {
            float h = Radius * .01f;
            return Radius + height(direction) * h;
        }
        public Vector2 GetTemp(Vector3 direction) {
            float y = Math.Abs(direction.Y);
            float temp = Noise.noise(direction, 172, 6, .0006f, .9f) - (y * y * y * y * y);
            float humid = Noise.noise(direction, 128, 6, .0008f, .9f);

            return new Vector2(temp, humid);
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
            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].SplitDynamic(camera.Position, device);
        }

        public void Draw(Renderer renderer) {
            constants.world = Matrix.Transpose(WorldMatrix);
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);
            
            renderer.Context.PixelShader.SetShaderResource(0, colorMapView);
            renderer.Context.PixelShader.SetSampler(0, colorMapSampler);

            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            for (int i = 0; i < baseChunks.Length; i++)
                baseChunks[i].Draw(renderer);
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
