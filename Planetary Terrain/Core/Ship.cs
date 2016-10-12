﻿using System;
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
            ShipModel = new Model("Data/Models/ship.fbx", device);
            ShipModel.Meshes[0].SetEmissiveTexture(device, "Data/Models/ship_emission.png");
            ShipModel.Meshes[0].SetSpecularTexture(device, "Data/Models/ship_specular.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;
            Mass = 100;
        }

        public void Update(double deltaTime) {
            LinearVelocity += (Vector3d)Rotation.Backward * Throttle * 500d;

            // extremely fake aerodynamic forces (in space)
            LinearVelocity = (Vector3d)Rotation.Backward * LinearVelocity.Length();
            if (Throttle < .1)
                LinearVelocity *= .8;

            Position += LinearVelocity * deltaTime;
            Rotation *= Matrix.RotationAxis(Rotation.Right, AngularVelocity.X) * Matrix.RotationAxis(Rotation.Up, AngularVelocity.Y) * Matrix.RotationAxis(Rotation.Backward, AngularVelocity.Z);

            Body b = StarSystem.ActiveSystem.GetNearestBody(Position);
            if (b != null) {
                Vector3d p = b.Position - Position;
                double l = p.Length();
                p /= l;
                double h = b.GetHeight(p);
                if (l < h)
                    Position = b.Position + p * h;
            }
        }
        
        public void Draw(Renderer renderer) {
            Vector3d light = new Vector3d();
            Star star = StarSystem.ActiveSystem.GetNearestStar(Position);
            if (star != null)
                light = Vector3d.Normalize(Position - star.Position);
            ShipModel.Draw(renderer,
                light,
                Rotation * Matrix.Translation(Position - renderer.Camera.Position));
        }
    }
}
