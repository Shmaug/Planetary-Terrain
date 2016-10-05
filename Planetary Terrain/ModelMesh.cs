using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using Assimp;
using SharpDX.DXGI;

namespace Planetary_Terrain {
    class ModelMesh : IDisposable {
        public D3D11.Buffer VertexBuffer;
        public D3D11.Buffer IndexBuffer;
        public int VertexSize;
        public int VertexCount;
        public int IndexCount;
        public PrimitiveTopology PrimitiveTopology;

        public D3D11.InputElement[] InputElements;
        public D3D11.InputLayout InputLayout;

        public D3D11.Texture2D DiffuseTexture;
        public D3D11.ShaderResourceView DiffuseTextureView;
        public D3D11.SamplerState DiffuseSampler;
        
        public void Draw(Renderer renderer) {
            // color map
            renderer.Context.PixelShader.SetShaderResource(0, DiffuseTextureView);
            renderer.Context.PixelShader.SetSampler(0, DiffuseSampler);
            
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(VertexBuffer, VertexSize, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);

            renderer.Context.DrawIndexed(IndexCount, 0, 0);
        }

        public void Dispose() {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
            DiffuseSampler?.Dispose();
            DiffuseTextureView?.Dispose();
            InputLayout?.Dispose();
            DiffuseTexture?.Dispose();
        }
    }
}
