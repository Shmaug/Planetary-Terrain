using System;

namespace Planetary_Terrain {
    static class Shaders {
        public const string shaderDirectory = "Shaders";

        public static Shader LineShader;
        public static Shader TerrainShader;
        public static Shader AtmosphereShader;

        public static void LoadShaders(SharpDX.Direct3D11.Device device, SharpDX.Direct3D11.DeviceContext context) {
            TerrainShader = new Shader(shaderDirectory + "\\terrain.hlsl", device, context, VertexNormalTexture.InputElements);
            AtmosphereShader = new Shader(shaderDirectory + "\\atmosphere.hlsl", device, context, new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0));
            LineShader = new Shader(shaderDirectory + "\\line.hlsl", device, context, VertexColor.InputElements);
        }

        public static void Dispose() {
            LineShader.Dispose();
            TerrainShader.Dispose();
            AtmosphereShader.Dispose();
        }
    }
}
