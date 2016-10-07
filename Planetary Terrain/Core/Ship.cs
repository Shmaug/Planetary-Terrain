using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class PlayerShip {
        public Vector3d Position;
        public Vector3d LinearVelocity;
        public Vector3d Rotation;
        public Vector3d AngularVelocity;
        public double Mass;
        public Model ShipModel;

        public double Throttle;

        public Matrix RotationMatrix { get; private set; }

        public PlayerShip(D3D11.Device device, Camera camera) {
            ShipModel = new Model("Models/ship.fbx", device);
            Mass = 100;
        }

        public void Update(double deltaTime) {
            LinearVelocity += (Vector3d)RotationMatrix.Backward * (Throttle * 1000d);

            Position += LinearVelocity * deltaTime;
            Rotation += AngularVelocity * deltaTime;

            // TODO: fuckin rotations yo
            RotationMatrix = Matrix.RotationYawPitchRoll((float)Rotation.Y, (float)Rotation.X, (float)Rotation.Z);
        }
        
        public void Draw(Renderer renderer, Vector3d sunPosition) {
            ShipModel.Draw(renderer, sunPosition, RotationMatrix * Matrix.Translation(Position - renderer.Camera.Position));
        }
    }
}
