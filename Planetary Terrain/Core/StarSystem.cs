using System;
using System.Collections.Generic;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class StarSystem : IDisposable {
        public List<Planet> planets;

        public StarSystem(D3D11.Device device) {
            planets = new List<Planet>();
            
            Planet sun = new Planet("Sol", 696000000, 100, false, true);
            sun.Position = new Vector3d(0, 0, 0);
            sun.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Sun.jpg"), device);
            planets.Add(sun);

            Planet mercury = new Planet("Mercury", 2440000, 10000, false);
            mercury.Position = new Vector3d(0, 0, 57910000000);
            mercury.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mercury.jpg"), device);
            planets.Add(mercury);
            
            Planet venus = new Planet("Venus", 6500000, 40000, false);
            venus.Position = new Vector3d(0, 0, 108200000000);
            venus.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Venus.jpg"), device);
            planets.Add(venus);

            Planet earth = new Planet("Earth", 6371000, 20000, true);
            earth.Position = new Vector3d(0, 0, 149600000000);
            earth.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Earth.jpg"), device);
            planets.Add(earth);

            Planet mars = new Planet("Mars", 3397000, 10000, false);
            mars.Position = new Vector3d(0, 0, 227940000000);
            mars.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(mars);
        }

        public Planet GetNearestPlanet(Vector3d pos) {
            double near = double.MaxValue;
            Planet n = null;
            foreach (Planet p in planets) {
                double d = (p.Position - pos).Length();
                if (d < near) {
                    near = d;
                    n = p;
                }
            }
            return n;
        }

        public void Update(Renderer renderer, D3D11.Device device) {
            foreach (Planet p in planets)
                p.Update(device, renderer.Camera);
        }

        public void Draw(Renderer renderer) {
            foreach (Planet p in planets)
                p.Draw(renderer, planets[0]);

            if (renderer.DrawGUI) {
                renderer.D2DContext.BeginDraw();
                foreach (Planet p in planets)
                    p.DrawHUDIcon(renderer);
                renderer.D2DContext.EndDraw();
            }
        }

        public void Dispose() {
            foreach (Planet p in planets)
                p.Dispose();
        }
    }
}
