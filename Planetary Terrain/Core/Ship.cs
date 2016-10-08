using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class PlayerShip {
        public Vector3d Position;
        public Vector3d LinearVelocity;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3d AngularVelocity;
        public double Mass;
        public Model ShipModel;

        public double Throttle;

        public PlayerShip(D3D11.Device device, Camera camera) {
            ShipModel = new Model("Models/ship.fbx", device);
            Mass = 100;
        }

        public void Update(double deltaTime) {
            LinearVelocity += Vector3d.Transform(Vector3.ForwardLH, Rotation) * Throttle * 1000d;

            Position += LinearVelocity * deltaTime;
            Rotation += new Quaternion((AngularVelocity * deltaTime), 0) * Rotation;
            Rotation.Normalize();
        }
        
        public void Draw(Renderer renderer, Vector3d sunPosition) {
            ShipModel.Draw(renderer, sunPosition, Matrix.RotationQuaternion(Rotation) * Matrix.Translation(Position - renderer.Camera.Position));
        }
    }
}
