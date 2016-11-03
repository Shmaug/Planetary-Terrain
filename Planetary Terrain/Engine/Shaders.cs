using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    static class Shaders {
        public const string shaderDirectory = "Data/Shaders/";
        
        public static Shader BasicShader;
        public static Shader TexturedShader;
        public static Shader PlanetShader;
        public static Shader WaterShader;
        public static Shader AtmosphereShader;
        public static Shader StarShader;
        public static Shader ModelShader;
        public static Shader InstancedModel;
        public static Shader Imposter;
        public static Shader AeroFXShader;
        public static Shader BlurShader;

        public static void Load(D3D11.Device device, D3D11.DeviceContext context) {
            // URGENT: Parse shaders
            // URGENT: Write batch to run fxc

            StarShader = new Shader(
                shaderDirectory + "Star",
                device, context, PlanetVertex.InputElements);

            PlanetShader = new Shader(
                shaderDirectory + "Planet",
                device, context, PlanetVertex.InputElements);

            WaterShader = new Shader(
                shaderDirectory + "Water",
                device, context, VertexNormal.InputElements);

            AtmosphereShader = new Shader(
                shaderDirectory + "Atmosphere",
                device, context, VertexNormal.InputElements);

            BasicShader = new Shader(
                shaderDirectory + "Colored",
                device, context, VertexColor.InputElements);

            ModelShader = new Shader(
                shaderDirectory + "Model",
                device, context, VertexNormalTexture.InputElements);

            InstancedModel = new Shader(
                shaderDirectory + "InstancedModel",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, D3D11.InputClassification.PerVertexData, 0),

                new D3D11.InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, D3D11.InputClassification.PerInstanceData, 1)
            );

            TexturedShader = new Shader(
                shaderDirectory + "Textured",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            );

            AeroFXShader = new Shader(
                shaderDirectory + "AeroFX",
                device, context, VertexNormal.InputElements);

            BlurShader = new Shader(
                shaderDirectory + "Blur",
                device, context, VertexTexture.InputElements);

            Imposter = new Shader(
                shaderDirectory + "Imposter",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0),

                new D3D11.InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, D3D11.InputClassification.PerInstanceData, 1));
        }

        public static void Dispose() {
            BasicShader.Dispose();
            PlanetShader.Dispose();
            WaterShader.Dispose();
            StarShader.Dispose();
            AtmosphereShader.Dispose();
            ModelShader.Dispose();
            TexturedShader.Dispose();
        }
    }
}
