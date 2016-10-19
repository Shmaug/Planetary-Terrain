using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;

namespace Planetary_Terrain {
    class Skybox : IDisposable {
        static Vector3[] CubeVerticies { get
        {
            return new Vector3[] {
                new Vector3(-1f, -1f,  1f),
                new Vector3( 1f, -1f,  1f),
                new Vector3( 1f,  1f,  1f),
                new Vector3(-1f,  1f,  1f),
                new Vector3(-1f, -1f, -1f),
                new Vector3( 1f, -1f, -1f),
                new Vector3( 1f,  1f, -1f),
                new Vector3(-1f,  1f, -1f),
            };
        } }
        static short[] CubeIndicies { get
        {
            return new short[] {	// front
		        0, 1, 2,
                2, 3, 0,
		        // top
		        1, 5, 6,
                6, 2, 1,
		        // back
		        7, 6, 5,
                5, 4, 7,
		        // bottom
		        4, 0, 3,
                3, 7, 4,
		        // left
		        4, 5, 1,
                1, 0, 4,
		        // right
		        3, 2, 6,
                6, 7, 3,
            };
        } }

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;

        public D3D11.Texture2D Texture;
        public D3D11.SamplerState Sampler;
        public D3D11.ShaderResourceView TextureView;

        public Skybox(string cubemapFile, D3D11.Device device) {
            //Texture = ResourceUtil.LoadCubemap(device, "Data/Textures/Sky.dds");
            //TextureView = new D3D11.ShaderResourceView(device, Texture);
            Sampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });

            vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, CubeVerticies);
            indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, CubeIndicies);
        }
        
        public void Draw(Renderer renderer) {
            Shaders.SkyboxShader.Set(renderer);

            renderer.Context.PixelShader.SetSampler(0, Sampler);
            renderer.Context.PixelShader.SetShaderResource(0, TextureView);

            renderer.Context.Rasterizer.State = renderer.rasterizerStateSolidCullFront;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateNoDepth);

            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0));
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.DrawIndexed(CubeIndicies.Length, 0, 0);

            renderer.Context.Rasterizer.State = renderer.DrawWireframe ? renderer.rasterizerStateWireframeCullBack : renderer.rasterizerStateSolidCullBack;
            renderer.Context.OutputMerger.SetDepthStencilState(renderer.depthStencilStateDefault);
        }

        public void Dispose() {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            TextureView?.Dispose();
            Sampler?.Dispose();
            Texture?.Dispose();

            vertexBuffer = null;
            indexBuffer = null;
            TextureView = null;
            Sampler = null;
            Texture = null;
        }
    }
}
