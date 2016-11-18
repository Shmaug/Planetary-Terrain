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
        public float Shininess;
        public float ShininessIntensity;

        public D3D11.ShaderResourceView DiffuseTextureView;
        public D3D11.ShaderResourceView EmissiveTextureView;
        public D3D11.ShaderResourceView SpecularTextureView;
        public D3D11.ShaderResourceView NormalTextureView;
        
        public void SetDiffuseTexture(D3D11.Device device, string filePath) {
            DiffuseTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out DiffuseTextureView);
        }
        public void SetEmissiveTexture(D3D11.Device device, string filePath) {
            EmissiveTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out EmissiveTextureView);
        }
        public void SetSpecularTexture(D3D11.Device device, string filePath) {
            SpecularTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out SpecularTextureView);
        }
        public void SetNormalTexture(D3D11.Device device, string filePath) {
            NormalTextureView?.Dispose();
            ResourceUtil.LoadFromFile(device, filePath, out NormalTextureView);
        }
        
        public void SetResources(Renderer renderer) {
            renderer.Context.PixelShader.SetShaderResource(1, DiffuseTextureView ?? renderer.WhiteTextureView);
            renderer.Context.PixelShader.SetShaderResource(2, EmissiveTextureView ?? renderer.BlackTextureView);
            renderer.Context.PixelShader.SetShaderResource(3, SpecularTextureView ?? renderer.WhiteTextureView);
            renderer.Context.PixelShader.SetShaderResource(4, NormalTextureView ?? renderer.BlackTextureView);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(VertexBuffer, VertexSize, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);
        }
        public void Draw(Renderer renderer) {
            SetResources(renderer);
            foreach (Camera c in renderer.Cameras) {
                renderer.SetCamera(c);
                renderer.Context.DrawIndexed(IndexCount, 0, 0);
            }
            Debug.TrianglesDrawn += IndexCount / 3;
        }
        public void DrawInstanced(Renderer renderer, int instanceCount) {
            SetResources(renderer);
            foreach (Camera c in renderer.Cameras) {
                renderer.SetCamera(c);
                renderer.Context.DrawIndexedInstanced(IndexCount, instanceCount, 0, 0, 0);
            }
            Debug.TrianglesDrawn += (IndexCount / 3) * instanceCount;
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
