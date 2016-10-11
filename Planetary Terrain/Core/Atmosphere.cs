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
        public double Height;

        public double MaxPressure;

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;

        [StructLayout(LayoutKind.Explicit, Size = 160)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
            
            [FieldOffset(64)]
            public float InnerRadius;
            [FieldOffset(68)]
            public float OuterRadius;

            [FieldOffset(72)]
            public float CameraHeight;

            [FieldOffset(76)]
            public float KrESun;
            [FieldOffset(80)]
            public float KmESun;
            [FieldOffset(84)]
            public float Kr4PI;
            [FieldOffset(88)]
            public float Km4PI;

            [FieldOffset(92)]
            public float g;
            [FieldOffset(96)]
            public float Scale;
            [FieldOffset(100)]
            public float ScaleDepth;
            [FieldOffset(104)]
            public float ScaleOverScaleDepth;

            [FieldOffset(108)]
            public float InvScaleDepth;
            [FieldOffset(112)]
            public float fSamples;
            [FieldOffset(116)]
            public int nSamples;

            [FieldOffset(128)]
            public Vector3 planetPos;

            [FieldOffset(144)]
            public Vector3 InvWavelength;
        }
        Constants constants;
        D3D11.Buffer constBuffer;

        public Atmosphere(double height, double pressure) {
            Height = height;
            MaxPressure = pressure;

            Icosphere.GenerateIcosphere(6, false, out verticies, out indicies);

            constants = new Constants();
        }

        void SetConstants(Vector3d scaledPos, double scale) {
            constants.nSamples = 10;
            constants.fSamples = 10f;

            constants.InnerRadius = (float)(Planet.Radius * scale);
            constants.OuterRadius = (float)((Planet.Radius + Height) * scale);

            constants.CameraHeight = (float)scaledPos.Length();

            float kr = .0025f; // rayleigh scattering constant
            float km = .0010f; // mie scattering constant
            float sun = 15f; // sun brightness
            Vector3 wavelength = new Vector3(.65f, .57f, .475f);
            wavelength = wavelength * wavelength * wavelength * wavelength; // wavelength^4

            constants.InvWavelength = 1f / wavelength;

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
            if (vertexBuffer == null)
                vertexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verticies);
            if (indexBuffer == null)
                indexBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.IndexBuffer, indicies);

            Shaders.AtmosphereShader.Set(renderer);

            constants.World = Matrix.Scaling((float)((Planet.Radius + Height) * scale)) * Matrix.Translation(pos);

            SetConstants(pos, scale);

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

            if ((renderer.Camera.Position - Planet.Position).Length() > Planet.Radius + Height)
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
