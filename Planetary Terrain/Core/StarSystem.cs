using System;
using System.Collections.Generic;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class StarSystem : IDisposable {
        public List<Planet> planets;

        public StarSystem(D3D11.Device device) {
            planets = new List<Planet>();
            
            Planet sun = new Planet("Sol", 696000000, 0, null, true);
            sun.Position = new Vector3d(0, 0, 0);
            sun.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Sun.jpg"), device);
            planets.Add(sun);

            Planet mercury = new Planet("Mercury", 2440000, 10000);
            mercury.Position = new Vector3d(0, 0, 57910000000);
            mercury.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mercury.jpg"), device);
            planets.Add(mercury);
            
            Planet venus = new Planet("Venus", 6500000, 40000);
            venus.Position = new Vector3d(0, 0, 108200000000);
            venus.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Venus.jpg"), device);
            planets.Add(venus);

            Planet earth = new Planet("Earth", 6371000, 20000, new Atmosphere(6371000 + 100000));
            earth.Position = new Vector3d(0, 0, 149600000000);
            earth.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Earth.jpg"), device);
            planets.Add(earth);

            Planet mars = new Planet("Mars", 3397000, 10000);
            mars.Position = new Vector3d(0, 0, 227940000000);
            mars.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(mars);

            // Gas giants
            Planet jupiter = new Planet("Jupiter", 71400000, 0, null);
            jupiter.Position = new Vector3d(0, 0, 778330000000);
            jupiter.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(jupiter);

            Planet saturn = new Planet("Saturn", 60330000, 0);
            saturn.Position = new Vector3d(0, 0, 1424600000000);
            saturn.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(saturn);

            Planet uranus = new Planet("Uranus", 25900000, 0);
            uranus.Position = new Vector3d(0, 0, 2873550000000);
            uranus.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(uranus);

            Planet neptune = new Planet("Neptune", 24750000, 0);
            neptune.Position = new Vector3d(0, 0, 4501000000000);
            neptune.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(neptune);

            Planet pluto = new Planet("Pluto", 1650000, 100);
            pluto.Position = new Vector3d(0, 0, 5945900000000);
            pluto.SetColormap(ResourceUtil.LoadTexture(device, "Textures\\Mars.jpg"), device);
            planets.Add(pluto);
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
