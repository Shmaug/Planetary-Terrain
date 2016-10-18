using System;

namespace Planetary_Terrain {
    static class Shaders {
        public const string shaderDirectory = "Data/Shaders/";

        public static Shader LineShader;
        public static Shader PlanetShader;
        public static Shader WaterShader;
        public static Shader StarShader;
        public static Shader AtmosphereShader;
        public static Shader ModelShader;
        public static Shader SkyboxShader;

        public static void LoadShaders(SharpDX.Direct3D11.Device device, SharpDX.Direct3D11.DeviceContext context) {
            StarShader = new Shader(
                shaderDirectory + "star",
                device, context, PlanetVertex.InputElements);

            PlanetShader = new Shader(
                shaderDirectory + "planet",
                device, context, PlanetVertex.InputElements);

            WaterShader = new Shader(
                shaderDirectory + "water",
                device, context, VertexNormal.InputElements);

            AtmosphereShader = new Shader(
                shaderDirectory + "atmosphere",
                device, context, VertexNormal.InputElements);

            LineShader = new Shader(
                shaderDirectory + "line",
                device, context, VertexColor.InputElements);

            ModelShader = new Shader(
                shaderDirectory + "model",
                device, context, VertexNormalTexture.InputElements);

            SkyboxShader = new Shader(
                shaderDirectory + "skybox",
                device, context, new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0));
        }

        public static void Dispose() {
            LineShader.Dispose();
            PlanetShader.Dispose();
            WaterShader.Dispose();
            StarShader.Dispose();
            AtmosphereShader.Dispose();
            ModelShader.Dispose();
            SkyboxShader.Dispose();
        }
    }
}
