using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Resources {
        static string modelFolder = "Data/Models/";
        public static Model ShipModel;
        public static Model GunModel;
        public static Model CylinderModel;

        public static D3D11.Buffer QuadVertexBuffer;
        public static D3D11.Buffer QuadIndexBuffer;

        public static Model TreeModel;
        public static D3D11.ShaderResourceView TreeModelImposter;
        
        public static void Load(D3D11.Device device) {
            QuadVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, new float[] {
                    -1, -1, 0,      0, 0,
                     1, -1, 0,      1, 0,
                    -1,  1, 0,      0, 1,
                     1,  1, 0,      1, 1,
            });
            QuadIndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, new short[] {
                    0, 1, 2,
                    1, 3, 2,
            });

            ShipModel = new Model(modelFolder + "cruiser/ship.fbx", device, Matrix.Scaling(5) * Matrix.RotationY(MathUtil.Pi));
            ShipModel.Meshes[0].SetSpecularTexture(device, modelFolder + "ship/specular.png");
            //ShipModel.Meshes[0].SetEmissiveTexture(device, modelFolder + "ship/emission.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;

            GunModel = new Model(modelFolder + "gun/gun.fbx", device);
            GunModel.Meshes[0].SetNormalTexture(device, modelFolder + "gun/normal.png");
            GunModel.Meshes[0].SetSpecularTexture(device, modelFolder + "gun/specular.png");
            GunModel.SpecularColor = Color.White;
            GunModel.Shininess = 200;
            GunModel.SpecularIntensity = 1;

            CylinderModel = new Model(modelFolder + "cylinder.fbx", device);
            CylinderModel.SpecularColor = Color.White;
            CylinderModel.Shininess = 200;
            CylinderModel.SpecularIntensity = 0;
            
            TreeModel = new Model(modelFolder + "trees/tree0.fbx", device);
            TreeModel.SpecularColor = Color.White;
            TreeModel.Shininess = 0;
            TreeModel.SpecularIntensity = 0;

            ResourceUtil.LoadFromFile(device, modelFolder + "trees/tree0imposter.png", out TreeModelImposter);
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
