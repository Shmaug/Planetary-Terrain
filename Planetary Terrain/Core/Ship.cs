using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

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


        string FormatSpeed(double meters) {
            if (meters < Physics.LIGHT_SPEED) {
                string[] u = { "m/s", "km/s", "Mm/s", "Gm/s" };
                int i = 0;
                while (meters / 100 > 1 && i < u.Length - 1) {
                    i++;
                    meters /= 100;
                }
                return meters.ToString("F2") + u[i];
            } else {
                string[] u = { "ls/s", "ld/s", "lm/s", "ly/s" }; // TODO: finish this
                int i = 0;
                while (meters / LIGHT_SPEED > 1 && i < u.Length - 1) {
                    i++;
                    meters /= LIGHT_SPEED;
                }
                return (meters / Physics.LIGHT_SPEED).ToString("F2") +  u[i];
            }
        }
        public void DrawFlightUI(Renderer renderer) {
            // TODO: flight ui

            float xmid = renderer.ResolutionX * .5f;
            renderer.D2DContext.FillRectangle(
                new RawRectangleF(xmid - 150, 0, xmid + 150, 50),
                renderer.Brushes["White"]);
            renderer.D2DContext.DrawRectangle(
                new RawRectangleF(xmid - 150, 0, xmid + 150, 50),
                renderer.Brushes["Black"]);

            renderer.SegoeUI24.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.D2DContext.DrawText(FormatSpeed(Velocity.Length()) + "/s", renderer.SegoeUI24,
                new RawRectangleF(xmid - 130, 0, xmid, 50),
                renderer.Brushes["Black"]);
        }

        public void Dispose() {
            ShipModel.Dispose();
        }
    }
}
