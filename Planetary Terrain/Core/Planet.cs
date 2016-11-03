using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SharpDX.Direct3D;

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
        public double SurfaceTemperature; // in Celsuis
        public double TemperatureRange; // in Celsuis

        public bool HasOcean = false;
        public bool HasTrees = false;
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

        D3D11.Buffer TreeBuffer;
        int TreeBufferSize = 0;
        
        public Planet(string name, Vector3d pos, double radius, double mass, double terrainHeight, Atmosphere atmosphere = null) : base(pos, radius, mass) {
            Name = name;
            Radius = radius;
            TerrainHeight = terrainHeight;
            Atmosphere = atmosphere;

            if (atmosphere != null)
                atmosphere.Planet = this;
            
            OceanHeight = .5;
            OceanColor = new Color(45, 100, 245);
        }

        public static double min=1, max=-1;
        double height(Vector3d direction) {
            double total = 0;

            // TODO: Height function

            double rough = Noise.Ridged(direction * 50 + new Vector3d(-5000), 5, .2, .7) * .5 + .5;

            double mntn = Noise.Fractal(direction * 1000 + new Vector3d(2000), 11, .03f, .5f);
            double flat = Noise.SmoothSimplex(direction * 100 + new Vector3d(1000), 2, .01f, .45f) * .5 + .5;

            rough = rough * rough;

            flat *= 1.0 - rough;
            mntn *= rough;
            
            total = mntn + flat;
            
            min = Math.Min(min, total);
            max = Math.Max(max, total);

            return total;
        }
        public override double GetHeight(Vector3d direction) {
            return Radius + height(direction) * TerrainHeight;
        }

        double temperature(Vector3d dir) {
            return Noise.SmoothSimplex(dir * 100, 5, .3f, .8f);
        }
        public double GetTemperature(Vector3d direction) {
            return SurfaceTemperature + TemperatureRange * temperature(direction);
        }
        public double GetHumidity(Vector3d direction) {
            return Noise.SmoothSimplex(direction * 200, 4, .1f, .8f) * .5 + .5; // TODO: better temp/humid function
        }

        public override void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double h) {
            h = height(direction);
            data = new Vector2((float)temperature(direction) * .5f + .5f, (float)GetHumidity(direction));
        }

        public void SetColormap(string file, D3D11.Device device) {
            colorMap?.Dispose();
            colorMapView?.Dispose();

            colorMap = (D3D11.Texture2D)ResourceUtil.LoadFromFile(device, file, out colorMapView);
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

            Star star = StarSystem.ActiveSystem.GetStar();
            if (star != null)
                constants.lightDirection = Vector3d.Normalize(Position - star.Position);
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
            }

            List<QuadNode> nodesToDraw = new List<QuadNode>();
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].GetRenderLevelNodes(renderer, ref nodesToDraw);
            
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
            
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);

            Profiler.Begin(Name + " Ground Draw");
            foreach (QuadNode n in nodesToDraw)
                n.Draw(renderer, QuadNode.QuadRenderPass.Ground, pos, scale);
            Profiler.End();

            if (HasOcean) {
                Profiler.Begin(Name + " Water Draw");
                // set water shader
                Shaders.WaterShader.Set(renderer);

                renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
                renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);
                
                // atmosphere constants
                if (Atmosphere != null) {
                    renderer.Context.VertexShader.SetConstantBuffers(3, Atmosphere.constBuffer);
                    renderer.Context.PixelShader.SetConstantBuffers(3, Atmosphere.constBuffer);
                }

                foreach (QuadNode n in nodesToDraw)
                    n.Draw(renderer, QuadNode.QuadRenderPass.Water, pos, scale);

                Profiler.End();
            }
            if (HasTrees) {
                // tree pass
                /*

                Profiler.Begin("Get Trees");
                List<Matrix> trees = new List<Matrix>();
                List<Matrix> imposters = new List<Matrix>();
                for (int i = 0; i < BaseQuads.Length; i++)
                    BaseQuads[i].GetTrees(renderer, ref trees, ref imposters);
                Profiler.End();

                if (trees.Count > 0) {
                    Debug.Log(trees.Count);

                    Profiler.Begin("Set Trees");
                    if (TreeBufferSize < trees.Count * Matrix.SizeInBytes) {
                        TreeBuffer?.Dispose();
                        TreeBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, trees.ToArray(), trees.Count * Matrix.SizeInBytes, D3D11.ResourceUsage.Dynamic, D3D11.CpuAccessFlags.Write);
                        TreeBufferSize = Matrix.SizeInBytes * trees.Count;
                    } else
                        renderer.Context.UpdateSubresource(trees.ToArray(), TreeBuffer);
                    Profiler.End();

                    // Draw 3d trees
                    Profiler.Begin("Draw Trees");
                    renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeBuffer, Matrix.SizeInBytes, 0));
                    Shaders.InstancedModel.Set(renderer);
                    Resources.TreeModel.DrawInstanced(
                        renderer,
                        constants.lightDirection,
                        Matrix.Identity,
                        trees.Count);
                    Profiler.End();
                }
                /*
                // Draw 2d trees
                if (!Profiler.Resume("Set Imposters")) Profiler.Begin("Set Imposters");
                renderer.Context.MapSubresource(TreeBuffer, 0, D3D11.MapMode.WriteDiscard, D3D11.MapFlags.None, out ds);
                Matrix b;
                float s = 20f;
                foreach (Matrix m in imposters) {
                    b = Matrix.BillboardLH(m.TranslationVector + m.Up * s * .5f, Vector3.Zero, m.Up, renderer.Camera.Rotation.Forward);
                    ds.Write(Matrix.Scaling(s, s, 1f) * b);
                    ds.Write(b.Forward);
                }
                ds.Dispose();
                renderer.Context.UnmapSubresource(TreeBuffer, 0);
                Profiler.End();

                if (!Profiler.Resume("Draw Imposter")) Profiler.Begin("Draw Imposter");
                Shaders.Imposter.Set(renderer);
                renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeBuffer, Matrix.SizeInBytes + Vector3.SizeInBytes, 0));
                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.QuadVertexBuffer, sizeof(float) * 5, 0));
                renderer.Context.InputAssembler.SetIndexBuffer(Resources.QuadIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

                renderer.Context.PixelShader.SetShaderResource(0, Resources.TreeModelImposter);
                
                renderer.Context.DrawIndexedInstanced(6, imposters.Count, 0, 0, 0);
                Profiler.End();
                */
            }

            Profiler.End();
        }

        public override void Dispose() {
            colorMap?.Dispose();
            colorMapView?.Dispose();

            constBuffer?.Dispose();

            TreeBuffer?.Dispose();

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Dispose();
            
            Atmosphere?.Dispose();
        }
    }
}
