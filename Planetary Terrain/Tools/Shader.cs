using System;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class IncludeHLSL : Include {
        IDisposable a;
        public IDisposable Shadow
        {
            get
            {
                return a;
            }

            set
            {
                a = value;
            }
        }

        public void Close(Stream stream) {
            stream.Close();
        }

        public void Dispose() {

        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream) {
            return new FileStream(type == IncludeType.Local ? "Shaders\\" + fileName : fileName, FileMode.Open);
        }
    }

    class Shader : IDisposable {
        public D3D11.VertexShader VertexShader { get; private set; }
        public D3D11.PixelShader PixelShader { get; private set; }
        public ShaderSignature Signature { get; private set; }
        public D3D11.InputLayout InputLayout { get; private set; }

        public Shader(string fileName, D3D11.Device device, D3D11.DeviceContext context, params D3D11.InputElement[] inputElements) {
            Include include = new IncludeHLSL();

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(fileName, "vsmain", "vs_4_0", ShaderFlags.Debug, EffectFlags.None, null, include)) {
                 if (vertexShaderByteCode.Bytecode == null)
                    throw new CompilationException(vertexShaderByteCode.Message);
                Signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                VertexShader = new D3D11.VertexShader(device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(fileName, "psmain", "ps_4_0", ShaderFlags.Debug, EffectFlags.None, null, include)) {
                if (pixelShaderByteCode.Bytecode == null)
                    throw new CompilationException(pixelShaderByteCode.Message);
                PixelShader = new D3D11.PixelShader(device, pixelShaderByteCode);
            }
            
            InputLayout = new D3D11.InputLayout(device, Signature, inputElements);
        }

        public void Set(Renderer renderer) {
            renderer.Context.InputAssembler.InputLayout = InputLayout;
            renderer.Context.VertexShader.Set(VertexShader);
            renderer.Context.PixelShader.Set(PixelShader);

            renderer.Context.VertexShader.SetConstantBuffer(0, renderer.constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(0, renderer.constantBuffer);
        }

        public void Dispose() {
            VertexShader.Dispose();
            PixelShader.Dispose();
            Signature.Dispose();
            InputLayout.Dispose();
        }
    }
}
