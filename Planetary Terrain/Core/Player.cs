using System;
using SharpDX;
using DInput = SharpDX.DirectInput;

namespace Planetary_Terrain {
    class Player : PhysicsBody, IDisposable {
        public Ship Vehicle;
        public Vector3 CameraEuler;
        public Camera Camera;
        public bool FirstPerson = false;

        public Player() : base(62) {
            Drag = .2;
        }

        public void Teleport(Vector3d pos) {
            if (Vehicle != null)
                Vehicle.Position = pos;
            
            Position = pos;
        }

        public void UpdateInput(UI.InputState InputState, double deltaTime) {
            if (InputState.ks.IsPressed(DInput.Key.F) && !InputState.lastks.IsPressed(DInput.Key.F)) {
                if (Vehicle != null) {
                    Vehicle = null;
                    DisablePhysics = false;
                } else {
                    //TODO: enter vehicle
                }
            }
            if (InputState.ks.IsPressed(DInput.Key.T) && !InputState.lastks.IsPressed(DInput.Key.T)) {
                FirstPerson = !FirstPerson;
                if (FirstPerson)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }

            Vector3d move = Vector3.Zero;
            if (InputState.ks.IsPressed(DInput.Key.W))
                move.Z -= -1;
            else if (InputState.ks.IsPressed(DInput.Key.S))
                move.Z += -1;
            if (InputState.ks.IsPressed(DInput.Key.A))
                move.X -= 1;
            else if (InputState.ks.IsPressed(DInput.Key.D))
                move.X += 1;
            if (InputState.ks.IsPressed(DInput.Key.Q))
                move.Y += 1;
            if (InputState.ks.IsPressed(DInput.Key.E))
                move.Y -= 1;

            if (Vehicle != null) {
                // move vehicle
                if (InputState.ks.IsPressed(DInput.Key.LeftShift))
                    Vehicle.Throttle += deltaTime * .5;
                else if (InputState.ks.IsPressed(DInput.Key.LeftControl))
                    Vehicle.Throttle -= deltaTime * .5;
                Vehicle.Throttle = MathTools.Clamp01(Vehicle.Throttle);

                Vehicle.AngularVelocity = Vector3d.Lerp(Vehicle.AngularVelocity, new Vector3d(move.Z, move.X, move.Y) * .03, deltaTime);
            } else {
                CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
                Vector3d d = Vector3d.Normalize(Position - b.Position);
                Vector3d planetUp = Vector3.Up;
                if (b != null) {
                    Rotation = b.OrientationFromDirection(d);
                    planetUp = Rotation.Up;
                    Rotation *= Matrix.RotationAxis(Rotation.Up, CameraEuler.Y);
                }

                // walk around
                move.Y = 0;
                double l = move.Length();
                if (l > 0) {
                    move /= l;
                    Vector3d n = b.GetNormal(d);

                    move = Vector3d.Transform(move, (Matrix3x3)Camera.Rotation);

                    Vector3d delta = move - Velocity;
                    delta *= .2;
                    delta -= planetUp * Vector3d.Dot(delta, n);
                    if (delta.LengthSquared() > .1)
                        AddForce(delta * 3.0 * Mass, Vector3.Zero);
                }

            }

            // Mouse look
            if (FirstPerson)
                CameraEuler += new Vector3(InputState.ms.Y, InputState.ms.X, 0) * .003f;
            else {
                if (InputState.ms.Buttons[0])
                    CameraEuler += new Vector3(InputState.mousePos.Y - InputState.lastMousePos.Y, InputState.mousePos.X - InputState.lastMousePos.X, 0) * .003f;
            }
            CameraEuler.X = MathUtil.Clamp(CameraEuler.X, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
        }
        
        public override void PostUpdate() {
            if (Vehicle != null) {
                Position = Vehicle.Position;
                Velocity = Vehicle.Velocity;
                AngularVelocity = Vehicle.AngularVelocity;
                Rotation = Vehicle.Rotation;
            }

            Camera.Rotation = Rotation * Matrix.RotationAxis(Rotation.Up, CameraEuler.Y);
            Camera.Rotation *= Matrix.RotationAxis(Camera.Rotation.Right, CameraEuler.X);
            if (FirstPerson)
                Camera.Position = Position + (Vector3d)Rotation.Up * .75;
            else
                Camera.Position = Position + (Vector3d)Camera.Rotation.Forward * 50;
        }

        public override void Draw(Renderer renderer) {

        }
        public void DrawHUD(Renderer renderer) {
            if (FirstPerson)
                Vehicle?.DrawHUD(renderer);
        }

        public void Dispose() {
            Vehicle?.Dispose();
            Vehicle = null;
        }
    }
}
