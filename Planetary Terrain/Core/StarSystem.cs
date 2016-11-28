using System;
using System.Collections.Generic;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class StarSystem : IDisposable {
        public static StarSystem ActiveSystem;

        public List<CelestialBody> bodies;
        public Physics physics;

        public StarSystem(D3D11.Device device) {
            bodies = new List<CelestialBody>();
            physics = new Physics();
            
            Star sun = new Star("Sol", new Vector3d(), 696000000, 1.989e30);
            sun.SetColormap("Data/Textures/Sun.dds", device);
            bodies.Add(sun);

            //Planet mercury = new Planet("Mercury", new Vector3d(0, 0, 57910000000), 2440000, 3.285e23, 10000);
            //mercury.SetColormap("Data/Textures/Mercury.dds", device);
            //bodies.Add(mercury);

            //Planet venus = new Planet("Venus",new Vector3d(0, 0, 108200000000), 6500000, 4.867e24, 40000);
            //venus.SetColormap("Data/Textures/Venus.dds", device);
            //bodies.Add(venus);

            Planet earth = new Planet(
                "Earth",
                new Vector3d(0, 0, 149600000000),
                6371000,
                5.972e24,
                20000,
                new Atmosphere(6371000 + 80000) {
                    SurfacePressure = 6, //kPa
                    SurfaceDensity = 1.2, // kg/m^3
                }) {
                    HasOcean = true,
                    HasTrees = true,
                    SurfaceTemperature = 17,
                    TemperatureRange = 35
                };
            earth.Velocity = new Vector3d(30000, 0, 0);
            earth.SetColormap("Data/Textures/Earth.dds", device);
            earth.Rotation = Matrix.RotationX(MathUtil.DegreesToRadians(23.5f));
            earth.AngularVelocity.Y = 2 * Math.PI / (24 * 60 * 60);
            bodies.Add(earth);

            Planet moon = new Planet(
                "Moon",
                earth.Position + new Vector3d(0, 0, 362570000),
                1737000,
                7.34767309e22,
                20000) {
                HasOcean = false,
                HasTrees = false,
                SurfaceTemperature = 0,
                TemperatureRange = 15
            };
            moon.Velocity = earth.Velocity + new Vector3d(1022, 0, 0);
            moon.SetColormap("Data/Textures/moon.dds", device);
            moon.AngularVelocity.Y = 2 * Math.PI / (27 * 24 * 60 * 60);
            bodies.Add(moon);

            //Planet mars = new Planet("Mars", new Vector3d(0, 0, 227940000000), 3397000, 6.39e23, 10000,
            //    new Atmosphere(3397000 + 10000) {
            //        SurfacePressure = 100, //kPa
            //        SurfaceDensity = 1.2, // kg/m^3
            //    }) {
            //        SurfaceTemperature = -55,
            //        TemperatureRange = 65
            //    };
            //mars.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(mars);

            //// Gas giants
            //Planet jupiter = new Planet("Jupiter", new Vector3d(0, 0, 778330000000), 71400000, 1.898e27, 0, null);
            //jupiter.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(jupiter);

            //Planet saturn = new Planet("Saturn", new Vector3d(0, 0, 1424600000000), 60330000, 5.683e26, 0);
            //saturn.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(saturn);

            //Planet uranus = new Planet("Uranus", new Vector3d(0, 0, 2873550000000), 25900000, 8.681e25, 0);
            //uranus.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(uranus);

            //Planet neptune = new Planet("Neptune", new Vector3d(0, 0, 4501000000000), 24750000, 1.024e26, 0);
            //neptune.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(neptune);

            //Planet pluto = new Planet("Pluto", new Vector3d(0, 0, 5945900000000), 1650000, 1.309e22, 100);
            //pluto.SetColormap("Data/Textures/Mars.dds", device);
            //bodies.Add(pluto);

            foreach (CelestialBody b in bodies)
                physics.AddBody(b);
        }

        public Atmosphere GetCurrentAtmosphere(Vector3d pos) {
            CelestialBody b = GetCurrentSOI(pos);
            if (b is Planet)
                return (b as Planet).Atmosphere;
            return null;
        }
        public CelestialBody GetCurrentSOI(Vector3d pos) {
            double near = double.MaxValue;
            CelestialBody n = bodies[0];
            foreach (CelestialBody b in bodies) {
                double d = (b.Position - pos).LengthSquared();
                if (d < near && d < b.SOI*b.SOI) {
                    near = d;
                    n = b;
                }
            }
            return n;
        }

        public Star GetStar() {
            foreach (CelestialBody p in bodies)
                if (p is Star)
                    return p as Star;
            return null;
        }
        
        public void UpdateLOD(Renderer renderer) {
            foreach (CelestialBody b in bodies)
                b.UpdateLOD(renderer.Device, renderer.ActiveCamera);
        }

        public void DrawPlanetHudIcons(Renderer renderer, double playerSpeed) {
            renderer.SegoeUI14.TextAlignment      = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.SegoeUI14.WordWrapping       = SharpDX.DirectWrite.WordWrapping.NoWrap;
            renderer.SegoeUI14.ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center;

            float t = -MathUtil.PiOverFour;
            foreach (CelestialBody b in bodies) {
                b.DrawHUDIcon(renderer, playerSpeed, new Vector2((float)Math.Cos(t), (float)Math.Sin(t)));
                t += MathUtil.Pi * .2f;
            }
        }

        public void Dispose() {
            physics.Dispose();
        }
    }
}
