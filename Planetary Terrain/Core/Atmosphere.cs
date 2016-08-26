using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Atmosphere : IDisposable {
        public Vector3[] verticies;
        public short[] indicies;

        public Planet Planet;
        public double Radius;

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;

        [StructLayout(LayoutKind.Explicit)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix world;
            [FieldOffset(64)]
            public Vector3 center;
            [FieldOffset(76)]
            public float radius;
        }
        Constants constants;
        D3D11.Buffer constBuffer;

        public Atmosphere(Planet planet, double radius) {
            Planet = planet;
            Radius = radius;

            constants.radius = (float)Radius;

            Icosphere.GenerateIcosphere(4, out verticies, out indicies);
        }

        public void Draw(Renderer renderer) {
            if (vertexBuffer == null)
                vertexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            if (indexBuffer == null)
                indexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);

            Shaders.AtmosphereShader.Set(renderer);

            Vector3d pos;
            double scale;
            MathTools.AdjustPositionRelative(Planet.Position, renderer.camera, out pos, out scale);
            Matrix world = Matrix.Scaling((float)scale) * Matrix.Translation(pos);

            constants.world = Matrix.Transpose(world);
            constants.center = Planet.Position - renderer.camera.Position;
            constants.radius = (float)(Radius * scale);
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);
            
            renderer.Context.VertexShader.SetConstantBuffers(1, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(1, constBuffer);
            
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0));
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
