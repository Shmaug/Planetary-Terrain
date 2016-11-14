using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using SharpDX.Direct3D;

namespace Planetary_Terrain {
    class Ship : PhysicsBody, IDisposable {
        public Vector3 CockpitCameraPosition;
        public double Throttle;

        public Ship(D3D11.Device device) : base(100) {
            Drag = 1;
            Hull.SphereRadius = 7;
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
            Resources.ShipModel.EmissiveIntensity = (float)Throttle;
            Resources.ShipModel.Draw(renderer, light, world);
        }
        
        public void Dispose() {

        }
    }
}
