using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain {
    class Ship : PhysicsBody, IDisposable {
        public Model ShipModel;

        public double Throttle;

        public Ship(D3D11.Device device) : base(100) {
            ShipModel = new Model("Data/Models/ship/ship.fbx", device);
            ShipModel.Meshes[0].SetEmissiveTexture(device, "Data/Models/ship/ship_emission.png");
            ShipModel.Meshes[0].SetSpecularTexture(device, "Data/Models/ship/ship_specular.png");
            ShipModel.SpecularColor = Color.White;
            ShipModel.Shininess = 200;
            ShipModel.SpecularIntensity = 1;

            Drag = 1;
        }

        public override void Update(double deltaTime) {
            AddForce((Vector3d)Rotation.Backward * 460000 * Throttle, Vector3.Zero);

            base.Update(deltaTime);
        }
        
        public override void Draw(Renderer renderer) {
            Vector3d light = new Vector3d();
            Star star = StarSystem.ActiveSystem.GetNearestStar(Position);
            if (star != null)
                light = Vector3d.Normalize(Position - star.Position);

            // TODO: aero FX
            ShipModel.Draw(renderer,
                light,
                Rotation * Matrix.Translation(Position - renderer.Camera.Position));
        }

        public void DrawHUD(Renderer renderer) {
            double v = Velocity.Length();

            float xmid = renderer.ResolutionX * .5f;
            renderer.D2DContext.FillRectangle(
                new RawRectangleF(xmid - 150, 0, xmid + 150, 50),
                renderer.Brushes["White"]);

            renderer.SegoeUI24.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.SegoeUI14.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;

            renderer.D2DContext.DrawText(Physics.FormatSpeed(v), renderer.SegoeUI24,
                new RawRectangleF(xmid - 130, 0, xmid, 40),
                renderer.Brushes["Black"]);

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            if (b != null) {
                double h = (b.Position - Position).Length();
                renderer.D2DContext.DrawText(Physics.FormatDistance(h - b.Radius), renderer.SegoeUI24,
                    new RawRectangleF(xmid, 0, xmid + 150, 40),
                    renderer.Brushes["Black"]);
                renderer.D2DContext.DrawText("(" + b.Name + ")", renderer.SegoeUI14,
                    new RawRectangleF(xmid, 30, xmid + 150, 50),
                    renderer.Brushes["Black"]);

                if (b is Planet) {
                    Atmosphere a = (b as Planet).Atmosphere;
                    if (a != null && h < a.Radius * 1.5) {
                        double temp;
                        double pressure;
                        double density;
                        double c;
                        a.MeasureProperties(h, out pressure, out density, out temp, out c);

                        renderer.D2DContext.FillRectangle(
                            new RawRectangleF(xmid - 260, 0, xmid - 155, 80),
                            renderer.Brushes["White"]);

                        renderer.D2DContext.DrawText("Atmosphere: ", renderer.SegoeUI14,
                            new RawRectangleF(xmid - 250, 3, xmid - 155, 10),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText(temp.ToString("F1") + "°C", renderer.SegoeUI14,
                            new RawRectangleF(xmid - 240, 15, xmid - 155, 30),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText(pressure.ToString("F1") + " kPa", renderer.SegoeUI14,
                            new RawRectangleF(xmid - 240, 30, xmid - 155, 45),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText(density.ToString("F1") + " kg/m^3", renderer.SegoeUI14,
                            new RawRectangleF(xmid - 240, 45, xmid - 155, 60),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText("Mach " + (Velocity.Length() / c).ToString("F2"), renderer.SegoeUI14,
                            new RawRectangleF(xmid - 240, 60, xmid - 155, 75),
                            renderer.Brushes["Black"]);
                    }
                }
            }
        }

        public void Dispose() {
            ShipModel.Dispose();
        }
    }
}
