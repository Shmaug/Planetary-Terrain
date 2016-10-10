using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class PlayerShip {
        public Vector3d Position;
        public Vector3d LinearVelocity;
        public Matrix Rotation = Matrix.Identity;
        public Vector3 AngularVelocity;
        public double Mass;
        public Model ShipModel;

        public double Throttle;

        public PlayerShip(D3D11.Device device, Camera camera) {
            ShipModel = new Model("Models/ship.fbx", device);
            Mass = 100;
        }

        public void Update(double deltaTime) {
            LinearVelocity += (Vector3d)Rotation.Backward * Throttle * 1000d;

            Position += LinearVelocity * deltaTime;
            Rotation *= Matrix.RotationAxis(Rotation.Right, AngularVelocity.X) * Matrix.RotationAxis(Rotation.Up, AngularVelocity.Y) * Matrix.RotationAxis(Rotation.Forward, AngularVelocity.Z);


        }
        
        public void Draw(Renderer renderer, Vector3d sunPosition) {
            ShipModel.Draw(renderer, Vector3d.Normalize(Position - sunPosition), Rotation * Matrix.Translation(Position - renderer.Camera.Position));
        }
    }
}
