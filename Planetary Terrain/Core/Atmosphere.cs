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

        public Atmosphere(double radius) {
            Radius = radius;

            Icosphere.GenerateIcosphere(6, false, out verticies, out indicies);

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
            if (vertexBuffer == null)
                vertexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            if (indexBuffer == null)
                indexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);

            Shaders.AtmosphereShader.Set(renderer);

            constants.world = Matrix.Scaling((float)(scale * Radius)) * Matrix.Translation(pos);
            //constants.invWVP = Matrix.Invert(constants.world * renderer.Camera.View * renderer.Camera.Projection);

            SetConstants(renderer.Camera.Position, pos, scale);

            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            #region prepare device
            renderer.Context.VertexShader.SetConstantBuffers(1, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(1, constBuffer);

            renderer.Context.VertexShader.SetConstantBuffers(2, Planet.constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2,  Planet.constBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexNormal>(), 0));
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            // alpha blending
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);


            if ((renderer.Camera.Position - Planet.Position).Length() > Radius) {
                renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeNoCull : renderer.rasterizerStateSolidNoCull;
                // no depth buffer if far away (floating point errors too big)s
                renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);
            } else
                renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullFront : renderer.rasterizerStateSolidCullFront;
            #endregion

            renderer.Context.DrawIndexed(indicies.Length, 0, 0);

            #region restore device state
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            #endregion
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
