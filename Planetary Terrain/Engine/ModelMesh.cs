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

        public D3D11.Texture2D EmissiveTexture;
        public D3D11.ShaderResourceView EmissiveTextureView;

        public D3D11.Texture2D SpecularTexture;
        public D3D11.ShaderResourceView SpecularTextureView;

        public D3D11.ShaderResourceView WhiteTextureView;
        public D3D11.ShaderResourceView BlackTextureView;

        public D3D11.SamplerState AnisotropicSampler;

        public void SetDiffuseTexture(D3D11.Device device, string filePath) {
            DiffuseTexture?.Dispose();
            DiffuseTextureView?.Dispose();

            DiffuseTexture = (D3D11.Texture2D)ResourceUtil.LoadFromFile(device, filePath, out DiffuseTextureView);
        }
        public void SetEmissiveTexture(D3D11.Device device, string filePath) {
            EmissiveTexture?.Dispose();
            EmissiveTextureView?.Dispose();

            EmissiveTexture = (D3D11.Texture2D)ResourceUtil.LoadFromFile(device, filePath, out EmissiveTextureView);
        }
        public void SetSpecularTexture(D3D11.Device device, string filePath) {
            SpecularTexture?.Dispose();
            SpecularTextureView?.Dispose();

            SpecularTexture = (D3D11.Texture2D)ResourceUtil.LoadFromFile(device, filePath, out SpecularTextureView);
        }

        public void Draw(Renderer renderer) {
            if (AnisotropicSampler == null) {
                AnisotropicSampler = new D3D11.SamplerState(renderer.Device, new D3D11.SamplerStateDescription() {
                    AddressU = D3D11.TextureAddressMode.Clamp,
                    AddressV = D3D11.TextureAddressMode.Clamp,
                    AddressW = D3D11.TextureAddressMode.Clamp,
                    Filter = D3D11.Filter.Anisotropic,
                });
            }
            if (WhiteTextureView == null) {
                D3D11.Texture2D tex = new D3D11.Texture2D(renderer.Device, new D3D11.Texture2DDescription() {
                    ArraySize = 1,
                    Width = 1,
                    Height = 1,
                    Format = Format.R32G32B32A32_Float,
                    CpuAccessFlags = D3D11.CpuAccessFlags.None,
                    MipLevels = 0,
                    Usage = D3D11.ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = D3D11.BindFlags.ShaderResource,
                    OptionFlags = D3D11.ResourceOptionFlags.None
                });
                Vector4[] data = new Vector4[] { Vector4.One };
                renderer.Context.UpdateSubresource(data, tex);
                
                WhiteTextureView = new D3D11.ShaderResourceView(renderer.Device, tex);
            }
            if (BlackTextureView == null) {
                D3D11.Texture2D tex = new D3D11.Texture2D(renderer.Device, new D3D11.Texture2DDescription() {
                    ArraySize = 1,
                    Width = 1,
                    Height = 1,
                    Format = Format.R32G32B32A32_Float,
                    CpuAccessFlags = D3D11.CpuAccessFlags.None,
                    MipLevels = 0,
                    Usage = D3D11.ResourceUsage.Default,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = D3D11.BindFlags.ShaderResource,
                    OptionFlags = D3D11.ResourceOptionFlags.None
                });
                Vector4[] data = new Vector4[] { Vector4.Zero };
                renderer.Context.UpdateSubresource(data, tex);

                BlackTextureView = new D3D11.ShaderResourceView(renderer.Device, tex);
            }

            renderer.Context.PixelShader.SetSampler(0, AnisotropicSampler);

            if (DiffuseTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(0, DiffuseTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(0, WhiteTextureView);

            if (EmissiveTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(1, EmissiveTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(1, BlackTextureView);

            if (SpecularTextureView != null)
                renderer.Context.PixelShader.SetShaderResource(2, SpecularTextureView);
            else
                renderer.Context.PixelShader.SetShaderResource(2, WhiteTextureView);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(VertexBuffer, VertexSize, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R16_UInt, 0);

            renderer.Context.DrawIndexed(IndexCount, 0, 0);
        }

        public void Dispose() {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();

            BlackTextureView?.Dispose();
            WhiteTextureView?.Dispose();

            AnisotropicSampler?.Dispose();

            DiffuseTextureView?.Dispose();
            DiffuseTexture?.Dispose();

            EmissiveTexture?.Dispose();
            EmissiveTextureView?.Dispose();

            SpecularTexture?.Dispose();
            SpecularTextureView?.Dispose();
        }
    }
}
