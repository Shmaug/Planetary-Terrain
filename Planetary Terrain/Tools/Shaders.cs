using System;

namespace Planetary_Terrain {
    static class Shaders {
        public const string shaderDirectory = "Shaders\\";

        public static Shader LineShader;
        public static Shader PlanetShader;
        public static Shader StarShader;
        public static Shader AtmosphereShader;

        public static void LoadShaders(SharpDX.Direct3D11.Device device, SharpDX.Direct3D11.DeviceContext context) {
            StarShader = new Shader(
                shaderDirectory + "star",
                device, context, VertexNormalTexture.InputElements);
            PlanetShader = new Shader(
                shaderDirectory + "planet",
                device, context, VertexNormalTexture.InputElements);
            AtmosphereShader = new Shader(
                shaderDirectory + "atmosphere",
                device, context, VertexNormal.InputElements);
            LineShader = new Shader(
                shaderDirectory + "line",
                device, context, VertexColor.InputElements);
        }

        public static void Dispose() {
            LineShader.Dispose();
            PlanetShader.Dispose();
            AtmosphereShader.Dispose();
        }
    }
}
