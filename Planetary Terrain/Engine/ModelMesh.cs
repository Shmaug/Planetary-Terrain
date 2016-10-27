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
        
        public D3D11.ShaderResourceView DiffuseTextureView;
        public D3D11.ShaderResourceView EmissiveTextureView;
        public D3D11.ShaderResourceView SpecularTextureView;
        public D3D11.ShaderResourceView NormalTextureView;
        
        public void SetDiffuseTexture(D3D11.Device device, string filePath) {
            DiffuseTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out DiffuseTextureView).Dispose();
        }
        public void SetEmissiveTexture(D3D11.Device device, string filePath) {
            EmissiveTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out EmissiveTextureView).Dispose();
        }
        public void SetSpecularTexture(D3D11.Device device, string filePath) {
            SpecularTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out SpecularTextureView).Dispose();
        }
        public void SetNormalTexture(D3D11.Device device, string filePath) {
            NormalTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out NormalTextureView).Dispose();
        }
        
        public void SetResources(Renderer renderer) {
            renderer.Context.PixelShader.SetSampler(0, renderer.AnisotropicSampler);

            if (DiffuseTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(0, DiffuseTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(0, renderer.WhiteTextureView);

            if (EmissiveTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(1, EmissiveTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(1, renderer.BlackTextureView);

            if (SpecularTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(2, SpecularTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(2, renderer.WhiteTextureView);

            if (NormalTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(3, NormalTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(3, renderer.BlackTextureView);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(VertexBuffer, VertexSize, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);
        }
        public void Draw(Renderer renderer) {
            SetResources(renderer);
            renderer.Context.DrawIndexed(IndexCount, 0, 0);
        }
        public void DrawInstanced(Renderer renderer, int instanceCount) {
            SetResources(renderer);
            renderer.Context.DrawIndexedInstanced(IndexCount, instanceCount, 0, 0, 0);
        }
        public void DrawNoResources(Renderer renderer) {
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(VertexBuffer, VertexSize, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);

            renderer.Context.DrawIndexed(IndexCount, 0, 0);
        }

        public void Dispose() {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();

            DiffuseTextureView?.Dispose();
            EmissiveTextureView?.Dispose();
            SpecularTextureView?.Dispose();
            NormalTextureView?.Dispose();
        }
    }
}
