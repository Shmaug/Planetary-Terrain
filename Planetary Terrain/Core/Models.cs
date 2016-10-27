using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Models {
        static string modelFolder = "Data/Models/";
        public static Model ShipModel;
        public static Model GunModel;
        public static Model CylinderModel;
        
        public static void Load(D3D11.Device device) {
            ShipModel = new Model(modelFolder + "ship/ship.fbx", device);
            ShipModel.Meshes[0].SetEmissiveTexture(device, modelFolder + "ship/ship_emission.png");
            ShipModel.Meshes[0].SetSpecularTexture(device, modelFolder + "ship/ship_specular.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;

            GunModel = new Model(modelFolder + "gun/gun.fbx", device);
            //GunModel.Meshes[0].SetDiffuseTexture(device, modelFolder + "gun/gun_diffuse.dds");
            //GunModel.Meshes[0].SetNormalTexture(device, modelFolder + "gun/gun_normal.dds");
            //GunModel.Meshes[0].SetSpecularTexture(device, modelFolder + "gun/gun_specular.dds");
            GunModel.SpecularColor = Color.White;
            GunModel.Shininess = 200;
            GunModel.SpecularIntensity = 1;

            CylinderModel = new Model(modelFolder + "cylinder.fbx", device);
            CylinderModel.SpecularColor = Color.White;
            CylinderModel.Shininess = 200;
            CylinderModel.SpecularIntensity = 0;
        }

        public static void Dispose() {
            ShipModel.Dispose();
            GunModel.Dispose();
            CylinderModel.Dispose();
        }
    }
}
