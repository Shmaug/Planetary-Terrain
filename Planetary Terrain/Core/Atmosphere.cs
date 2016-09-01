using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Atmosphere : IDisposable {
        public VertexNormal[] verticies;
        public short[] indicies;

        public Planet Planet;
        public double Radius;

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 128)]
        struct Constants {
            public Matrix world;

            public int Samples;

            public float InnerRadius;
            public float OuterRadius;

            public float ScaleH;
            public float ScaleL;

            public float kr;
            public float km;
            public float e;
            public Vector3 cr;
            public float gm;
            
            public Vector3 planetPos;
        }
        Constants constants;
        D3D11.Buffer constBuffer;

        public Atmosphere(Planet planet, double radius) {
            Planet = planet;
            Radius = radius;

            Icosphere.GenerateIcosphere(6, out verticies, out indicies);

            constants = new Constants();
        }

        void SetConstants(Vector3d camPos, Vector3d scaledPos, double scale) {
            constants.Samples = 10;

            constants.InnerRadius = (float)(Planet.Radius * scale);
            constants.OuterRadius = (float)(Radius * scale);

            constants.ScaleH = 4f / (constants.OuterRadius - constants.InnerRadius);
            constants.ScaleL = 1f / (constants.OuterRadius - constants.InnerRadius);

            constants.kr = .166f;
            constants.km = .0025f;
            constants.e = 14.3f;
            constants.cr = new Vector3(.3f, .7f, 1f);
            constants.gm = -.85f;

            constants.planetPos = scaledPos;
        }

        public void Draw(Renderer renderer, Vector3d pos, double scale) {
            if ((renderer.Camera.Position - Planet.Position).Length() < Radius)
                return;

            if (vertexBuffer == null)
                vertexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            if (indexBuffer == null)
                indexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);

            Shaders.AtmosphereShader.Set(renderer);
            
            Matrix world = Matrix.Scaling((float)(scale * Radius)) * Matrix.Translation(pos);

            SetConstants(renderer.Camera.Position, pos, scale);

            constants.world = Matrix.Transpose(world);
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);
            
            renderer.Context.VertexShader.SetConstantBuffers(1, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(1, constBuffer);

            renderer.Context.VertexShader.SetConstantBuffers(2, Planet.constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2,  Planet.constBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexNormal>(), 0));
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);
            renderer.Context.Rasterizer.State =
                renderer.Context.Rasterizer.State.Description.FillMode == D3D11.FillMode.Solid ?
                renderer.rasterizerStateSolidNoCull : renderer.rasterizerStateWireframeNoCull;

            renderer.Context.DrawIndexed(indicies.Length, 0, 0);

            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateOpaque);

            renderer.Context.Rasterizer.State =
                renderer.Context.Rasterizer.State.Description.FillMode == D3D11.FillMode.Solid ?
                renderer.rasterizerStateSolid : renderer.rasterizerStateWireframe;
        }

        public void Dispose() {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();
            if (constBuffer != null)
                constBuffer.Dispose();
        }
    }
}
