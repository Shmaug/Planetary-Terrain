using System;
using SharpDX;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain {
    class Player : PhysicsBody, IDisposable {
        public Ship Vehicle;
        public Vector3 CameraEuler;
        public Camera Camera;
        public bool FirstPerson = false;
        public double camDist = 50;
        public double CameraDistance = 50;
        
        public Player() : base(62) {
            Drag = .2;
        }

        public void MoveTo(Vector3d pos) {
            if (Vehicle != null)
                Vehicle.Position = pos;
            
            Position = pos;
        }

        public void HandleInput(double deltaTime) {
            if (Input.ks.IsPressed(DInput.Key.F) && !Input.lastks.IsPressed(DInput.Key.F)) {
                if (Vehicle != null) {
                    Position = Vehicle.Position + (Vector3d)Vehicle.Rotation.Right * (Vehicle.Hull.SphereRadius + Hull.SphereRadius + 3);
                    Vehicle = null;
                    DisablePhysics = false;
                } else {
                    //TODO: enter vehicle
                    PhysicsBody b;
                    double t;
                    Vector3d n;
                    if (PhysicsSystem.Raycast(Input.MouseRayOrigin, Input.MouseRayDirection, out b, out t, out n)) {
                        if (b is Ship) {
                            Vehicle = b as Ship;
                            DisablePhysics = true;
                        }
                    }
                }
            }

            if (Input.ks.IsPressed(DInput.Key.T) && !Input.lastks.IsPressed(DInput.Key.T)) {
                FirstPerson = !FirstPerson;
                if (FirstPerson)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }

            Vector3d move = Vector3.Zero;
            if (Input.ks.IsPressed(DInput.Key.W))
                move.Z -= -1;
            else if (Input.ks.IsPressed(DInput.Key.S))
                move.Z += -1;
            if (Input.ks.IsPressed(DInput.Key.A))
                move.X -= 1;
            else if (Input.ks.IsPressed(DInput.Key.D))
                move.X += 1;
            if (Input.ks.IsPressed(DInput.Key.Q))
                move.Y += 1;
            if (Input.ks.IsPressed(DInput.Key.E))
                move.Y -= 1;

            CelestialBody cb = StarSystem.ActiveSystem.GetCurrentSOI(Position);
            if (Vehicle != null) {
                // move vehicle
                if (Input.ks.IsPressed(DInput.Key.LeftShift))
                    Vehicle.Throttle += deltaTime * .5;
                else if (Input.ks.IsPressed(DInput.Key.LeftControl))
                    Vehicle.Throttle -= deltaTime * .5;
                Vehicle.Throttle = MathTools.Clamp01(Vehicle.Throttle);

                Vehicle.AngularVelocity = Vector3d.Lerp(Vehicle.AngularVelocity, new Vector3d(move.Z, move.X, move.Y) * Math.PI * .25, deltaTime);
            } else {
                Matrix o = Matrix.Identity;
                if (cb != null) {
                    o = cb.OrientationFromDirection(Vector3d.Normalize(Position - cb.Position));
                    Rotation = o * Matrix.RotationAxis(o.Up, CameraEuler.Y);
                }

                // walk around on whatever we're standing on (based on last frame's collisions)
                move.Y = 0;
                double l = move.LengthSquared();
                if (l > 0) {
                    move /= Math.Sqrt(l);

                    foreach (Contact c in Contacts) {
                        if (Vector3d.Dot(c.ContactPosition - Position, Rotation.Down) > .5) { // object is below us
                            move = Vector3d.Transform(move, (Matrix3x3)Rotation);
                            move *= 2.6;
                            if (Input.ks.IsPressed(DInput.Key.LeftShift))
                                move *= 3;

                            Vector3d delta = move - Velocity;
                            delta -= (Vector3d)o.Up * Vector3d.Dot(delta, c.ContactNormal);
                            if (delta.LengthSquared() > .1)
                                AddForce(delta * 3.0 * Mass, Vector3.Zero);

                            if (Input.ks.IsPressed(DInput.Key.Space))
                                AddForce((c.BodyA == this ? -c.ContactNormal : c.ContactNormal) * Mass * 250, Vector3.Zero);
                            
                            break;
                        }
                    }
                }
            }

            // Mouse look
            camDist = Math.Max(camDist + camDist * -Input.ms.Z / 120, 30);
            CameraDistance = MathUtil.Lerp(CameraDistance, camDist, 10*deltaTime);

            if (FirstPerson)
                CameraEuler += new Vector3(Input.ms.Y, Input.ms.X, 0) * .003f;
            else {
                if (Input.ms.Buttons[0] && !Input.MouseBlocked)
                    CameraEuler += new Vector3(Input.MousePos.Y - Input.LastMousePos.Y, Input.MousePos.X - Input.LastMousePos.X, 0) * .003f;
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
            
            if (Vehicle != null) {
                Camera.Rotation = Rotation * Matrix.RotationAxis(Rotation.Up, CameraEuler.Y);
                Camera.Rotation *= Matrix.RotationAxis(Camera.Rotation.Right, CameraEuler.X);
            } else
                Camera.Rotation = Rotation * Matrix.RotationAxis(Rotation.Right, CameraEuler.X);

            if (FirstPerson) {
                Camera.Position = Position + (Vector3d)Rotation.Up * .75;
                if (Vehicle != null)
                    Camera.Position = Vehicle.Position + (Vector3)Vector3.Transform(Vehicle.CockpitCameraPosition, Vehicle.Rotation);
            } else
                Camera.Position = Position + (Vector3d)Camera.Rotation.Forward * CameraDistance;

            CelestialBody cb = StarSystem.ActiveSystem.GetCurrentSOI(Camera.Position);

            Vector3d dir = Camera.Position - cb.Position;
            double h = dir.Length();
            dir /= h;
            double ch = cb.GetHeight(dir);
            if (h + .2 < ch)
                Camera.Position = cb.Position + dir * ch;

            Vector3d v = Velocity - cb.Velocity;
            if (h < ch + 10000)
                v = Velocity - (cb.VelocityOnPoint(Position - cb.Position) + cb.Velocity);
            Debug.DrawLine(Color.Blue, Position, Position + v); // relative velocity line
            
            foreach (Contact c in Contacts)
                Debug.DrawLine(Color.Red, Position - c.ContactNormal * 10, Position + c.ContactNormal * 10);
            
            // Teleport to right click
            if (!Input.MouseBlocked && !FirstPerson &&
                (Input.ms.Buttons[1] || (!Input.ms.Buttons[1] && Input.lastms.Buttons[1]))) {

                double r = cb.Radius;
                if (cb is Planet) r = cb.Radius + (cb as Planet).TerrainHeight * (cb as Planet).OceanHeight;

                Vector3d m = Input.MouseRayOrigin - cb.Position;
                double b = Vector3d.Dot(m, Input.MouseRayDirection);
                double c = Vector3d.Dot(m, m) - r * r;

                if (!(c > 0 && b > 0)) {
                    double discr = b * b - c;
                    if (discr > 0) {
                        double t = Math.Max(0, -b - Math.Sqrt(discr));
                        Vector3d p = Vector3d.Normalize(Input.MouseRayOrigin + Input.MouseRayDirection * t - cb.Position);
                        double h1 = cb.GetHeight(p);

                        if (Input.ms.Buttons[1]) // holding right click
                            Debug.DrawLine(Color.Red, cb.Position + p * h1, cb.Position + p * (h1+cb.Radius));
                        else { // released right click
                            MoveTo(cb.Position + p * (h1 + Hull.SphereRadius));
                            Velocity = cb.Velocity + cb.VelocityOnPoint(Position - cb.Position);
                            if (Vehicle != null) Vehicle.Velocity = Velocity;
                        }
                    }
                }
            }
        }

        public override void Draw(Renderer renderer) {
            if (FirstPerson && Vehicle == null) {
                Shaders.Model.Set(renderer);
                
                Resources.GunModel.Draw(renderer,
                    Vector3d.Normalize(Position - StarSystem.ActiveSystem.GetStar().Position),
                    Matrix.Translation(new Vector3(.15f, -.1f, .2f)) * Camera.Rotation);
            }
        }
        
        public void Dispose() {

        }
    }
}
