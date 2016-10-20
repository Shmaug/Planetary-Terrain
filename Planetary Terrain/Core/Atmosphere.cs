using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Atmosphere : IDisposable {
        public double MaxVertexSpacing = 500000; // m/vertex
        public double MinVertexSpacing = 20000;       // m/vertex

        public Planet Planet;
        public double Radius;

        public double SurfacePressure; // kg/m^3
        public double SurfaceTemperature; // celcius

        public Vector3 Wavelengths = new Vector3(.65f, .57f, .475f);

        /// <summary>
        /// The 6 base quadtrees composing the planet
        /// </summary>
        public AtmosphereQuadNode[] BaseQuads;

        [StructLayout(LayoutKind.Explicit, Size = 112)]
        struct Constants {
            [FieldOffset(0)]
            public float InnerRadius;
            [FieldOffset(4)]
            public float OuterRadius;

            [FieldOffset(8)]
            public float CameraHeight;

            [FieldOffset(12)]
            public float KrESun;
            [FieldOffset(16)]
            public float KmESun;
            [FieldOffset(20)]
            public float Kr4PI;
            [FieldOffset(24)]
            public float Km4PI;

            [FieldOffset(28)]
            public float g;
            [FieldOffset(32)]
            public float Scale;
            [FieldOffset(36)]
            public float ScaleDepth;
            [FieldOffset(40)]
            public float ScaleOverScaleDepth;
            [FieldOffset(44)]
            public float InvScaleDepth;

            [FieldOffset(48)]
            public float Exposure;

            [FieldOffset(52)]
            public float fSamples;
            [FieldOffset(56)]
            public int nSamples;

            [FieldOffset(64)]
            public Vector3 planetPos;

            [FieldOffset(80)]
            public Vector3 InvWavelength;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }

        public Atmosphere(double radius) {
            Radius = radius;
            
            constants = new Constants();

            double s = 1.41421356237 * Radius;

            BaseQuads = new AtmosphereQuadNode[6];
            BaseQuads[0] = new AtmosphereQuadNode(this, 0, s, 0, null, s * .5f * (Vector3d)Vector3.Up, MathTools.RotationXYZ(0, 0, 0));
            BaseQuads[1] = new AtmosphereQuadNode(this, 1, s, 0, null, s * .5f * (Vector3d)Vector3.Down, MathTools.RotationXYZ(MathUtil.Pi, 0, 0));
            BaseQuads[2] = new AtmosphereQuadNode(this, 2, s, 0, null, s * .5f * (Vector3d)Vector3.Left, MathTools.RotationXYZ(0, 0, MathUtil.PiOverTwo));
            BaseQuads[3] = new AtmosphereQuadNode(this, 3, s, 0, null, s * .5f * (Vector3d)Vector3.Right, MathTools.RotationXYZ(0, 0, -MathUtil.PiOverTwo));
            BaseQuads[4] = new AtmosphereQuadNode(this, 4, s, 0, null, s * .5f * (Vector3d)Vector3.ForwardLH, MathTools.RotationXYZ(MathUtil.PiOverTwo, 0, 0));
            BaseQuads[5] = new AtmosphereQuadNode(this, 5, s, 0, null, s * .5f * (Vector3d)Vector3.BackwardLH, MathTools.RotationXYZ(-MathUtil.PiOverTwo, 0, 0));

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Generate();
        }
        
        public void GetMeasurements(double height, out double pressure, out double temperature, out double c) {
            double x = Math.Max((height - Planet.Radius) / (Radius - Planet.Radius), 0); // height as [0, infinity)

            pressure = SurfacePressure * Math.Exp(-4.0 * x);
            temperature = SurfaceTemperature * -10 * (x - .0714) * (x - .4714) * (x - .6857) - .23872;

            c = 331.3 + (.6 * temperature);
        }

        public void Update(D3D11.Device device, Camera camera) {
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].SplitDynamic(camera.Position, device);
        }

        void SetConstants(Vector3d scaledPos, double scale) {
            constants.nSamples = 10;
            constants.fSamples = 10f;

            constants.InnerRadius = (float)(Planet.Radius * scale);
            constants.OuterRadius = (float)(Radius * scale);

            constants.CameraHeight = (float)scaledPos.Length();

            float kr = .0025f; // rayleigh scattering constant
            float km = .0010f; // mie scattering constant
            float sun = 15f; // sun brightness
            Vector3 wavelength = Wavelengths;

            constants.InvWavelength = 1f / new Vector3(
                    (float)Math.Pow(wavelength.X, 4),
                    (float)Math.Pow(wavelength.Y, 4),
                    (float)Math.Pow(wavelength.Z, 4)
                );

            constants.KrESun = kr * sun;
            constants.KmESun = km * sun;
            constants.Kr4PI = kr * MathUtil.Pi * 4;
            constants.Km4PI = km * MathUtil.Pi * 4;

            constants.g = -.98f; // mie g constant

            constants.Scale = (float)(1.0 / ((Radius - Planet.Radius) * scale));
            constants.ScaleDepth = .25f; // height at which the average density is found
            constants.InvScaleDepth = 1f / constants.ScaleDepth;
            constants.ScaleOverScaleDepth = constants.Scale * constants.InvScaleDepth;

            constants.planetPos = scaledPos;

            constants.Exposure = 1.2f;
        }
        public void Draw(Renderer renderer, Vector3d pos, double scale) {
            Shaders.AtmosphereShader.Set(renderer);
            
            SetConstants(pos, scale);

            if (constBuffer == null) constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            #region prepare device
            renderer.Context.VertexShader.SetConstantBuffers(3, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(3, constBuffer);

            renderer.Context.VertexShader.SetConstantBuffers(2, Planet.constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2,  Planet.constBuffer);
            
            // alpha blending
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);
            
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);

            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullFront : renderer.rasterizerStateSolidCullFront;
            #endregion

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, pos, scale);

            #region restore device state
            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);
            #endregion
        }

        public void Dispose() {
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Dispose();
            constBuffer?.Dispose();
        }
    }
}
