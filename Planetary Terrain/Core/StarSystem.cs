using System;
using System.Collections.Generic;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class StarSystem : IDisposable {
        public static StarSystem ActiveSystem;

        public List<Body> bodies;

        public StarSystem(D3D11.Device device) {
            bodies = new List<Body>();
            
            Star sun = new Star("Sol", new Vector3d(), 696000000, 1.989e30);
            sun.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Sun.jpg"), device);
            bodies.Add(sun);

            Planet mercury = new Planet("Mercury", new Vector3d(0, 0, 57910000000), 2440000, 3.285e23, 10000);
            mercury.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mercury.jpg"), device);
            bodies.Add(mercury);
            
            Planet venus = new Planet("Venus",new Vector3d(0, 0, 108200000000), 6500000, 4.867e24, 40000);
            venus.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Venus.jpg"), device);
            bodies.Add(venus);

            Planet earth = new Planet("Earth", new Vector3d(0, 0, 149600000000), 6371000, 5.972e24, 20000, new Atmosphere(40000, 101325), true);
            earth.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Earth.jpg"), device);
            bodies.Add(earth);

            Planet mars = new Planet("Mars", new Vector3d(0, 0, 227940000000), 3397000, 6.39e23, 10000, new Atmosphere(10000, 600));
            mars.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(mars);

            // Gas giants
            Planet jupiter = new Planet("Jupiter", new Vector3d(0, 0, 778330000000), 71400000, 1.898e27, 0, null);
            jupiter.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(jupiter);

            Planet saturn = new Planet("Saturn", new Vector3d(0, 0, 1424600000000), 60330000, 5.683e26, 0);
            saturn.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(saturn);

            Planet uranus = new Planet("Uranus", new Vector3d(0, 0, 2873550000000), 25900000, 8.681e25, 0);
            uranus.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(uranus);

            Planet neptune = new Planet("Neptune", new Vector3d(0, 0, 4501000000000), 24750000, 1.024e26, 0);
            neptune.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(neptune);

            Planet pluto = new Planet("Pluto", new Vector3d(0, 0, 5945900000000), 1650000, 1.309e22, 100);
            pluto.SetColormap(ResourceUtil.LoadTexture(device, "Data/Textures/Mars.jpg"), device);
            bodies.Add(pluto);
        }

        public Body GetNearestBody(Vector3d pos) {
            double near = double.MaxValue;
            Body n = null;
            foreach (Body p in bodies) {
                double d = (p.Position - pos).Length();
                if (d < near) {
                    near = d;
                    n = p;
                }
            }
            return n;
        }

        public Star GetNearestStar(Vector3d pos) {
            double near = double.MaxValue;
            Star n = null;
            foreach (Body p in bodies) {
                if (p is Star) {
                    double d = (p.Position - pos).Length();
                    if (d < near) {
                        near = d;
                        n = p as Star;
                    }
                }
            }
            return n;
        }


        public void Update(Renderer renderer, D3D11.Device device, double deltaTime) {
            foreach (Body b in bodies) {
                b.Update(device, renderer.Camera);

                //foreach (Body b2 in bodies) {
                //    if (b != b2)
                //        b.ApplyGravity(b2, deltaTime);
                //}
            }
        }

        public void Draw(Renderer renderer, double playerSpeed) {
            foreach (Body b in bodies)
                b.Draw(renderer);

            if (renderer.DrawGUI) {
                renderer.D2DContext.BeginDraw();
                foreach (Body b in bodies)
                    b.DrawHUDIcon(renderer, playerSpeed);
                renderer.D2DContext.EndDraw();
            }
        }

        public void Dispose() {
            foreach (Body p in bodies)
                p.Dispose();
        }
    }
}
