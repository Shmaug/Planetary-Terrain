using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;

namespace Planetary_Terrain {
    class Skybox : IDisposable {
        D3D11.Buffer constBuffer;
        
        public D3D11.ShaderResourceView[] TextureViews;
        public Matrix[] sides;
        
        public Skybox(string dir, D3D11.Device device) {
            TextureViews = new D3D11.ShaderResourceView[6];
            ResourceUtil.LoadFromFile(device, dir + "/NegativeX.png", out TextureViews[0]);
            ResourceUtil.LoadFromFile(device, dir + "/PositiveX.png", out TextureViews[1]);
            ResourceUtil.LoadFromFile(device, dir + "/NegativeY.png", out TextureViews[2]);
            ResourceUtil.LoadFromFile(device, dir + "/PositiveY.png", out TextureViews[3]);
            ResourceUtil.LoadFromFile(device, dir + "/NegativeZ.png", out TextureViews[4]);
            ResourceUtil.LoadFromFile(device, dir + "/PositiveZ.png", out TextureViews[5]);
            sides = new Matrix[6] {
                Matrix.RotationY(-MathUtil.PiOverTwo) * Matrix.Translation(-1,  0,  0),
                Matrix.RotationY( MathUtil.PiOverTwo) * Matrix.Translation( 1,  0,  0),
                Matrix.RotationX( MathUtil.PiOverTwo) * Matrix.Translation( 0, -1,  0),
                Matrix.RotationX( MathUtil.PiOverTwo) * Matrix.Translation( 0,  1,  0),
                Matrix.RotationY(0)                   * Matrix.Translation( 0,  0, -1),
                Matrix.RotationY(MathUtil.Pi)         * Matrix.Translation( 0,  0,  1)
            };
            
            Matrix m = Matrix.Identity;
            constBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.ConstantBuffer, ref m);
        }

        public void Draw(Renderer renderer) {
            if (renderer.DrawWireframe) return;
            Profiler.Begin("Skybox Draw");

            Shaders.Skybox.Set(renderer);

            Atmosphere a = StarSystem.ActiveSystem.GetCurrentAtmosphere(renderer.ActiveCamera.Position);
            if (a != null && a.Planet.WasDrawnLastFrame) {
                a.SetConstantBuffer(renderer);
            } else
                renderer.Context.PixelShader.SetConstantBuffer(3, null);

            renderer.Context.Rasterizer.State = renderer.rasterizerStateSolidNoCull;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);
            
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.QuadVertexBuffer, Utilities.SizeOf<float>() * 5, 0));
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(Resources.QuadIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            Vector3 lightDir = Vector3d.Normalize(renderer.ActiveCamera.Position - StarSystem.ActiveSystem.GetStar().Position);
            for (int i = 0; i < 6; i++) {
                Matrix m = sides[i];
                float[] buffer = new float[]{
                    m.M11,m.M12,m.M13,m.M14,
                    m.M21,m.M22,m.M23,m.M24,
                    m.M31,m.M32,m.M33,m.M34,
                    m.M41,m.M42,m.M43,m.M44,
                    lightDir.X, lightDir.Y, lightDir.Z,0
                };
                renderer.Context.PixelShader.SetShaderResource(1, TextureViews[i]);
                renderer.Context.UpdateSubresource(buffer, constBuffer);
                renderer.Context.VertexShader.SetConstantBuffer(1, constBuffer);
                renderer.Context.DrawIndexed(6, 0, 0);
            }

            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);

            Profiler.End();
        }

        public void Dispose() {
            constBuffer?.Dispose();
            for (int i = 0; i < TextureViews.Length; i++)
                TextureViews[i]?.Dispose();
            
            constBuffer = null;
        }
    }
}
