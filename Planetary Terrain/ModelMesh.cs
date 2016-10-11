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
        
        public D3D11.Texture2D DiffuseTexture;
        public D3D11.ShaderResourceView DiffuseTextureView;
        public D3D11.SamplerState DiffuseSampler;

        public D3D11.Texture2D EmissiveTexture;
        public D3D11.ShaderResourceView EmissiveTextureView;
        public D3D11.SamplerState EmissiveSampler;

        public void SetDiffuseTexture(D3D11.Device device, string filePath) {
            DiffuseTexture?.Dispose();
            DiffuseTextureView?.Dispose();
            DiffuseSampler?.Dispose();

            DiffuseTexture = ResourceUtil.LoadTexture(device, filePath);
            DiffuseSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
            DiffuseTextureView = new D3D11.ShaderResourceView(device, DiffuseTexture);
        }
        public void SetEmissiveTexture(D3D11.Device device, string filePath) {
            EmissiveTexture?.Dispose();
            EmissiveTextureView?.Dispose();
            EmissiveSampler?.Dispose();

            EmissiveTexture = ResourceUtil.LoadTexture(device, filePath);
            EmissiveSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
            EmissiveTextureView = new D3D11.ShaderResourceView(device, EmissiveTexture);
        }

        public void Draw(Renderer renderer) {
            if (DiffuseTextureView != null) {
                renderer.Context.PixelShader.SetShaderResource(0, DiffuseTextureView);
                renderer.Context.PixelShader.SetSampler(0, DiffuseSampler);
            }

            if (EmissiveTextureView != null) {
                renderer.Context.PixelShader.SetShaderResource(1, EmissiveTextureView);
                renderer.Context.PixelShader.SetSampler(1, EmissiveSampler);
            }

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
            DiffuseTexture?.Dispose();

            EmissiveTexture?.Dispose();
            EmissiveTextureView?.Dispose();
            EmissiveSampler?.Dispose();
        }
    }
}
