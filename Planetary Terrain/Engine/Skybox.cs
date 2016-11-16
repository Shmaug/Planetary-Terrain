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

            Shaders.TexturedShader.Set(renderer);
            renderer.Context.Rasterizer.State = renderer.rasterizerStateSolidNoCull;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);
            
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.QuadVertexBuffer, Utilities.SizeOf<float>() * 5, 0));
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(Resources.QuadIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            for (int i = 0; i < 6; i++) {
                renderer.Context.PixelShader.SetShaderResource(1, TextureViews[i]);
                renderer.Context.UpdateSubresource(ref sides[i], constBuffer);
                renderer.Context.VertexShader.SetConstantBuffer(1, constBuffer);
                renderer.Context.DrawIndexed(6, 0, 0);
            }

            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);
        }

        public void Dispose() {
            constBuffer?.Dispose();
            for (int i = 0; i < TextureViews.Length; i++)
                TextureViews[i]?.Dispose();
            
            constBuffer = null;
        }
    }
}
