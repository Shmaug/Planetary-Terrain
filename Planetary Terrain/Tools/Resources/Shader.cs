using System;
using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Shader : IDisposable {
        D3D11.Effect Effect;
        
        public Shader(string file, D3D11.Device device, D3D11.DeviceContext context, params D3D11.InputElement[] inputElements) {
            using (var byteCode = ShaderBytecode.FromFile(file)) {
                Effect = new D3D11.Effect(device, byteCode);
            }
        }
        
        public void Dispose() {
            Effect.Dispose();
        }
    }
}
