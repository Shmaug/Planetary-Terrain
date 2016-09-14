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

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 160)]
        struct Constants {
            public Matrix World;
            // ----
            public float InnerRadius;
            public float OuterRadius;
            
            public float CameraHeight;

            public float KrESun;
            // ----
            public float KmESun;
            public float Kr4PI;
            public float Km4PI;
            
            public float g;
            // ----
            public float Scale;
            public float ScaleDepth;
            public float ScaleOverScaleDepth;

            public float InvScaleDepth;
            // ----
            public float fSamples;
            public int nSamples;

            Vector2 spacer0;
            // ----
            public Vector3 planetPos;
            float spacer1;
            // ----
            public Vector3 InvWavelength;
            float spacer2;
        }
        Constants constants;
        D3D11.Buffer constBuffer;

        public Atmosphere(double radius) {
            Radius = radius;

            Icosphere.GenerateIcosphere(6, false, out verticies, out indicies);

            constants = new Constants();
        }

        void SetConstants(Vector3d camPos, Vector3d scaledPos, double scale) {
            constants.nSamples = 10;
            constants.fSamples = 10f;

            constants.InnerRadius = (float)(Planet.Radius * scale);
            constants.OuterRadius = (float)(Radius * scale);

            constants.CameraHeight = (float)scaledPos.Length();

            float kr = .0025f; // rayleigh scattering constant
            float km = .0010f; // mie scattering constant
            float sun = 20f; // sun brightness
            Vector3 wavelength = new Vector3(.65f, .57f, .475f);// new Vector3(6.5e-7f, 5.7e-7f, 4.75e-7f);

            constants.InvWavelength = 1f / (wavelength * wavelength * wavelength * wavelength);

            constants.KrESun = kr * sun;
            constants.KmESun = km * sun;
            constants.Kr4PI = kr * MathUtil.Pi * 4;
            constants.Km4PI = km * MathUtil.Pi * 4;

            constants.g = -.99f; // mie g constant

            constants.Scale = 1f / (constants.OuterRadius - constants.InnerRadius);
            constants.ScaleDepth = .25f; // height at which the average density is found
            constants.ScaleOverScaleDepth = constants.Scale / constants.ScaleDepth;
            constants.InvScaleDepth = 1f / constants.ScaleDepth;

            constants.planetPos = scaledPos;
        }

        public void Draw(Renderer renderer, Vector3d pos, double scale) {
            return;
            if ((renderer.Camera.Position - Planet.Position).Length() < Radius)
                return;

            if (vertexBuffer == null)
                vertexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            if (indexBuffer == null)
                indexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);

            Shaders.AtmosphereShader.Set(renderer);

            constants.World = Matrix.Scaling((float)(scale * Radius)) * Matrix.Translation(pos);
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

            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullFront : renderer.rasterizerStateSolidCullFront;
            #endregion

            renderer.Context.DrawIndexed(indicies.Length, 0, 0);

            #region restore device state
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);
            #endregion
        }

        public void Dispose() {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            constBuffer?.Dispose();
        }
    }
}
