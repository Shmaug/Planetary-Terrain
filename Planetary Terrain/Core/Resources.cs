using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Resources {
        static string modelFolder = "Data/Models/";
        public static Model ShipModel;
        public static Model GunModel;
        public static Model CylinderModel;
        
        public static D3D11.Buffer BoundingBoxVertexBuffer;
        public static D3D11.Buffer QuadVertexBuffer;
        public static D3D11.Buffer QuadIndexBuffer;

        public static Model TreeModel;
        public static D3D11.ShaderResourceView TreeModelImposterDiffuse;
        public static D3D11.ShaderResourceView TreeModelImposterNormals;

        public static D3D11.ShaderResourceView GrassTexture;
        
        public static void Load(D3D11.Device device) {
            QuadVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, new float[] {
                  // POSITION0,   TEXCOORD0
                    -1, -1, 0,      0, 0,
                     1, -1, 0,      1, 0,
                    -1,  1, 0,      0, 1,
                     1,  1, 0,      1, 1,
            });
            QuadIndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, new short[] {
                    0, 1, 2,
                    1, 3, 2,
            });
            BoundingBoxVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer,
                new VertexColor[] {
                    new VertexColor(new Vector3(-1, -1, -1), Color.White),
                    new VertexColor(new Vector3( 1, -1, -1), Color.White),
                    new VertexColor(new Vector3(-1, -1,  1), Color.White),
                    new VertexColor(new Vector3( 1, -1,  1), Color.White),
                    new VertexColor(new Vector3(-1, -1,  1), Color.White),
                    new VertexColor(new Vector3(-1, -1, -1), Color.White),
                    new VertexColor(new Vector3( 1, -1,  1), Color.White),
                    new VertexColor(new Vector3( 1, -1, -1), Color.White),
                    new VertexColor(new Vector3(-1, -1, -1), Color.White),
                    new VertexColor(new Vector3(-1,  1, -1), Color.White),
                    new VertexColor(new Vector3( 1, -1, -1), Color.White),
                    new VertexColor(new Vector3( 1,  1, -1), Color.White),
                    new VertexColor(new Vector3(-1, -1,  1), Color.White),
                    new VertexColor(new Vector3(-1,  1,  1), Color.White),
                    new VertexColor(new Vector3( 1, -1,  1), Color.White),
                    new VertexColor(new Vector3( 1,  1,  1), Color.White),
                    new VertexColor(new Vector3(-1,  1, -1), Color.White),
                    new VertexColor(new Vector3( 1,  1, -1), Color.White),
                    new VertexColor(new Vector3(-1,  1,  1), Color.White),
                    new VertexColor(new Vector3( 1,  1,  1), Color.White),
                    new VertexColor(new Vector3(-1,  1,  1), Color.White),
                    new VertexColor(new Vector3(-1,  1, -1), Color.White),
                    new VertexColor(new Vector3( 1,  1,  1), Color.White),
                    new VertexColor(new Vector3( 1,  1, -1), Color.White),
                });

            ShipModel = new Model(modelFolder + "cruiser/ship.fbx", device, Matrix.Scaling(.05f) * Matrix.RotationY(MathUtil.Pi));
            ShipModel.Meshes[0].SetNormalTexture(device, modelFolder + "cruiser/normal.png");
            ShipModel.Meshes[0].SetEmissiveTexture(device, modelFolder + "cruiser/emissive.png");
            ShipModel.Meshes[0].SetSpecularTexture(device, modelFolder + "cruiser/specular.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;

            GunModel = new Model(modelFolder + "gun/gun.fbx", device, Matrix.Scaling(.02f));
            GunModel.Meshes[0].SetNormalTexture(device, modelFolder + "gun/normal.png");
            GunModel.Meshes[0].SetSpecularTexture(device, modelFolder + "gun/specular.png");
            GunModel.SpecularColor = Color.White;
            GunModel.SpecularIntensity = .1f;

            CylinderModel = new Model(modelFolder + "cylinder.fbx", device);
            CylinderModel.SpecularColor = Color.White;
            CylinderModel.Shininess = 200;
            CylinderModel.SpecularIntensity = 0;
            
            TreeModel = new Model(modelFolder + "tree/tree.fbx", device);
            TreeModel.Meshes[0].SetNormalTexture(device, modelFolder + "tree/leaf_normal.png");
            TreeModel.Meshes[0].SetSpecularTexture(device, modelFolder + "tree/leaf_specular.png");
            TreeModel.SpecularColor = Color.White;
            TreeModel.Shininess = 0;
            TreeModel.SpecularIntensity = 0;
            
            ResourceUtil.LoadFromFile(device, modelFolder + "tree/imposter_diffuse.png", out TreeModelImposterDiffuse);
            ResourceUtil.LoadFromFile(device, modelFolder + "tree/imposter_normal.png", out TreeModelImposterNormals);
            ResourceUtil.LoadFromFile(device, "data/textures/grass.dds", out GrassTexture);
        }

        public static void Dispose() {
            ShipModel.Dispose();
            GunModel.Dispose();
            CylinderModel.Dispose();
            TreeModel.Dispose();
            QuadIndexBuffer.Dispose();
            QuadVertexBuffer.Dispose();
        }
    }
}
