using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using System.Collections.Generic;

namespace Planetary_Terrain {
    class Ship : PhysicsBody, IDisposable {
        public Model ShipModel;

        public double Throttle;

        public Ship(D3D11.Device device, Camera camera) : base(100) {
            ShipModel = new Model("Data/Models/ship.fbx", device);
            ShipModel.Meshes[0].SetEmissiveTexture(device, "Data/Models/ship_emission.png");
            ShipModel.Meshes[0].SetSpecularTexture(device, "Data/Models/ship_specular.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;
        }

        public override void Update(double deltaTime) {
            Forces.Add(new Force((Vector3d)Rotation.Backward * 10000 * Throttle, Vector3.Zero));
            base.Update(deltaTime);

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            if (b != null) {
                Vector3d p = Position - b.Position;
                double l = p.Length();
                p /= l;
                double h = b.GetHeight(p);
                if (l < h) {
                    Position = b.Position + p * h;

                    Vector3d o = Vector3d.Normalize(Position - b.Position);
                    Velocity -= o * Vector3d.Dot(Velocity, o);
                }
            }
        }
        
        public void Draw(Renderer renderer) {
            Vector3d light = new Vector3d();
            Star star = StarSystem.ActiveSystem.GetNearestStar(Position);
            if (star != null)
                light = Vector3d.Normalize(Position - star.Position);

            // TODO: aero FX
            ShipModel.Draw(renderer,
                light,
                Rotation * Matrix.Translation(Position - renderer.Camera.Position));
        }

        public void DrawFlightUI(Renderer renderer) {
            // TODO: flight ui
        }

        public void Dispose() {
            ShipModel.Dispose();
        }
    }
}
