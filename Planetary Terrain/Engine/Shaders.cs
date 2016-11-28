using SharpDX.DXGI;
using System.Collections.Generic;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    static class Shaders {
        public const string shaderDirectory = "Data/Shaders/";
        
        public static Shader Colored;
        public static Shader Textured;
        public static Shader Planet;
        public static Shader Water;
        public static Shader Atmosphere;
        public static Shader Star;
        public static Shader Skybox;
        public static Shader Model;
        public static Shader ModelInstanced;
        public static Shader Imposter;
        public static Shader AeroFX;
        public static Shader Blur;
        public static Shader Depth;

        public static void Load(D3D11.Device device, D3D11.DeviceContext context) {
            Star = new Shader(
                shaderDirectory + "Star",
                device, context, PlanetVertex.InputElements);

            Planet = new Shader(
                shaderDirectory + "Planet",
                device, context, PlanetVertex.InputElements);

            Water = new Shader(
                shaderDirectory + "Water",
                device, context, WaterVertex.InputElements);

            Atmosphere = new Shader(
                shaderDirectory + "Atmosphere",
                device, context, VertexNormal.InputElements);

            Colored = new Shader(
                shaderDirectory + "Colored",
                device, context, VertexColor.InputElements);

            Model = new Shader(
                shaderDirectory + "Model",
                device, context, ModelVertex.InputElements);

            List<D3D11.InputElement> ime = new List<D3D11.InputElement>();
            ime.AddRange(ModelVertex.InputElements);
            ime.Add(new D3D11.InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, D3D11.InputClassification.PerInstanceData, 1));
            ime.Add(new D3D11.InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, D3D11.InputClassification.PerInstanceData, 1));
            ime.Add(new D3D11.InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, D3D11.InputClassification.PerInstanceData, 1));
            ime.Add(new D3D11.InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, D3D11.InputClassification.PerInstanceData, 1));
            ModelInstanced = new Shader(
                shaderDirectory + "InstancedModel",
                device, context,
                ime.ToArray());

            Skybox = new Shader(
                shaderDirectory + "Skybox",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            );

            Textured = new Shader(
                shaderDirectory + "Textured",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            );
            
            AeroFX = new Shader(
                shaderDirectory + "AeroFX",
                device, context, VertexNormal.InputElements);

            Blur = new Shader(
                shaderDirectory + "Blur",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0));

            Imposter = new Shader(
                shaderDirectory + "Imposter",
                device, context,
                new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
                new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0),
                
                new D3D11.InputElement("TEXCOORD", 1, Format.R32G32B32_Float, 0 , 1, D3D11.InputClassification.PerInstanceData, 1),
                new D3D11.InputElement("TEXCOORD", 2, Format.R32G32B32_Float, 12, 1, D3D11.InputClassification.PerInstanceData, 1)
            );

            Depth = new Shader(shaderDirectory + "Depth", device, context);
        }

        public static void Dispose() {
            Colored.Dispose();
            Textured.Dispose();
            Planet.Dispose();
            Water.Dispose();
            Atmosphere.Dispose();
            Star.Dispose();
            Model.Dispose();
            ModelInstanced.Dispose();
            Imposter.Dispose();
            AeroFX.Dispose();
            Blur.Dispose();
            Depth.Dispose();
        }
    }
}
