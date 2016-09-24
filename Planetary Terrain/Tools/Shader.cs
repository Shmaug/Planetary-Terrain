//#define COMPILE_AT_RUNTIME

using System;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class HLSLInclude : Include {
        IDisposable shadow;
        public IDisposable Shadow
        {
            get
            {
                return shadow;
            }

            set
            {
                shadow = value;
            }
        }

        public void Close(Stream stream) {
            stream.Close();
        }

        public void Dispose() {
            shadow.Dispose();
        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream) {
            return new FileStream(type == IncludeType.Local ? @"Shaders\Include\" + fileName : fileName, FileMode.Open);
        }
    }
    class Shader : IDisposable {
        public D3D11.VertexShader VertexShader { get; private set; }
        public D3D11.PixelShader PixelShader { get; private set; }
        public ShaderSignature Signature { get; private set; }
        public D3D11.InputLayout InputLayout { get; private set; }
        
        public Shader(string file, D3D11.Device device, D3D11.DeviceContext context, params D3D11.InputElement[] inputElements) {
#if COMPILE_AT_RUNTIME
            HLSLInclude include = new HLSLInclude();
            using (var byteCode = ShaderBytecode.CompileFromFile(file + "_vs.hlsl", "main", "vs_5_0", ShaderFlags.Debug, EffectFlags.None, null, include)) {
                if (byteCode.Bytecode == null)
                    throw new CompilationException(byteCode.Message);
                Signature = ShaderSignature.GetInputSignature(byteCode);
                VertexShader = new D3D11.VertexShader(device, byteCode);
            }
            using (var byteCode = ShaderBytecode.CompileFromFile(file + "_ps.hlsl", "main", "ps_5_0", ShaderFlags.Debug, EffectFlags.None, null, include)) {
                if (byteCode.Bytecode == null)
                    throw new CompilationException(byteCode.Message);
                PixelShader = new D3D11.PixelShader(device, byteCode);
            }
            include.Dispose();
#else
            using (var byteCode = ShaderBytecode.FromFile(file + "_vs.cso")) {
                Signature = ShaderSignature.GetInputSignature(byteCode);
                VertexShader = new D3D11.VertexShader(device, byteCode);
            }
            using (var byteCode = ShaderBytecode.FromFile(file + "_ps.cso")) {
                PixelShader = new D3D11.PixelShader(device, byteCode);
            }
#endif
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
