using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain {
    class Ship : PhysicsBody, IDisposable {
        public double Throttle;

        public Ship(D3D11.Device device) : base(100) {
            Drag = 1;
        }

        public override void Update(double deltaTime) {
            AddForce((Vector3d)Rotation.Backward * 460000 * Throttle, Vector3.Zero);

            base.Update(deltaTime);
        }
        
        public override void Draw(Renderer renderer) {
            Vector3d light = new Vector3d();
            Star star = StarSystem.ActiveSystem.GetStar();
            if (star != null)
                light = Vector3d.Normalize(Position - star.Position);

            Matrix world = Rotation * Matrix.Translation(Position - renderer.Camera.Position);

            Shaders.ModelShader.Set(renderer);
            Models.ShipModel.Draw(renderer, light, world);

            //CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            //if (b is Planet && ((Planet)b).Atmosphere != null) {
            //    Atmosphere a = ((Planet)b).Atmosphere;
            //
            //    renderer.DrawAeroFX(world, Velocity, Models.ShipModel.DrawRaw);
            //}
        }
        
        public void Dispose() {
        }
    }
}
