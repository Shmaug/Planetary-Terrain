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

            if (Vehicle != null) {
                // move vehicle
                if (Input.ks.IsPressed(DInput.Key.LeftShift))
                    Vehicle.Throttle += deltaTime * .5;
                else if (Input.ks.IsPressed(DInput.Key.LeftControl))
                    Vehicle.Throttle -= deltaTime * .5;
                Vehicle.Throttle = MathTools.Clamp01(Vehicle.Throttle);

                Vehicle.AngularVelocity = Vector3d.Lerp(Vehicle.AngularVelocity, new Vector3d(move.Z, move.X, move.Y) * Math.PI * .25, deltaTime);
            } else {
                CelestialBody cb = StarSystem.ActiveSystem.GetNearestBody(Position);
                Matrix o = Matrix.Identity;
                if (cb != null) {
                    o = cb.OrientationFromDirection(Vector3d.Normalize(Position - cb.Position));
                    Rotation = o * Matrix.RotationAxis(o.Up, CameraEuler.Y);
                }

                // walk around on whatever we're standing on (based on last frame's collisions)
                move.Y = 0;
                double l = move.Length();
                if (l > 0)
                    move /= l;
                    
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
                            AddForce(c.ContactNormal * Mass * 250, Vector3.Zero);

                        break;
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

            // Teleport to double right click
            if (!Input.MouseBlocked && !FirstPerson) {
                if (Input.ms.Buttons[1]) {
                    double t = -1;
                    CelestialBody hit = null;
                    foreach (CelestialBody cb in StarSystem.ActiveSystem.bodies) {
                        double r = cb.Radius;
                        if (cb is Planet) r = cb.Radius + (cb as Planet).TerrainHeight * (cb as Planet).OceanHeight;

                        Vector3d m = Input.MouseRayOrigin - cb.Position;
                        double b = Vector3d.Dot(m, Input.MouseRayDirection);
                        double c = Vector3d.Dot(m, m) - r * r;

                        if (c > 0 && b > 0) continue;
                        double discr = b * b - c;
                        if (discr < 0) continue;
                        
                        double f = Math.Max(0, -b - Math.Sqrt(discr));
                        if (f < t || t < 0) {
                            t = f;
                            hit = cb;
                        }
                    }
                    if (hit != null) {
                        Vector3d p = Input.MouseRayOrigin + Input.MouseRayDirection * t;
                        Debug.DrawLine(Color.Red, p, p + Vector3d.Normalize(p - hit.Position) * hit.Radius);
                    }
                } else if (!Input.ms.Buttons[1] && Input.lastms.Buttons[1]) {
                    double t = -1;
                    CelestialBody hit = null;
                    foreach (CelestialBody cb in StarSystem.ActiveSystem.bodies) {
                        double r = cb.Radius;
                        if (cb is Planet) r = cb.Radius + (cb as Planet).TerrainHeight * (cb as Planet).OceanHeight;

                        Vector3d m = Input.MouseRayOrigin - cb.Position;
                        double b = Vector3d.Dot(m, Input.MouseRayDirection);
                        double c = Vector3d.Dot(m, m) - r * r;

                        if (c > 0 && b > 0) continue;
                        double discr = b * b - c;
                        if (discr < 0) continue;

                        // Ray now found to intersect sphere, compute smallest t value of intersection
                        double f = Math.Max(0, -b - Math.Sqrt(discr));
                        if (f < t || t < 0) {
                            t = f;
                            hit = cb;
                        }
                    }
                    if (hit != null) {
                        if (Vehicle != null)
                            Vehicle.Velocity = hit.Velocity;
                        Velocity = hit.Velocity;
                        MoveTo(Input.MouseRayOrigin + Input.MouseRayDirection * t);
                    }
                }
            }
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

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Camera.Position);
            if (b != null) {
                Vector3d dir = Camera.Position - b.Position;
                double h = dir.Length();
                dir /= h;
                double t = b.GetHeight(dir) + .2;
                if (h < t)
                    Camera.Position = b.Position + dir * t;
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
            Vehicle?.Dispose();
            Vehicle = null;
        }
    }
}
