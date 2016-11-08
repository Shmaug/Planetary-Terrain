using System;
using SharpDX;
using System.Collections.Generic;

namespace Planetary_Terrain {
    struct Force {
        public Vector3d ForceVector;
        public Vector3d Offset;

        public Force(Vector3d force, Vector3d offset) {
            ForceVector = force;
            Offset = offset;
        }
    }
    struct Orbit {
        public double Eccentricity;
        public double SemimajorAxis;
        public double Inclination;
        public double Longitude;
        public double ArgumentPeriapsis;
        public double ArgumentAnomaly;
        public PhysicsBody OrbitalParent;

        // TODO: keplerian orbits
        public static Orbit FromCartesian(Vector3d position, Vector3d velocity, double mass) {
            return new Orbit() {

            };
        }
        public void ToCartesian(out Vector3d position, out Vector3d velocity) {
            position = Vector3.Zero;
            velocity = Vector3.Zero;
        }
    }
    struct Contact {
        public PhysicsBody BodyA;
        public PhysicsBody BodyB;
        public Vector3d ContactPosition;
        /// <summary>
        /// Relative to BodyA
        /// </summary>
        public Vector3d ContactNormal;
        /// <summary>
        /// Relative to BodyA
        /// </summary>
        public Vector3d ContactVelocity;
    }
    class PhysicsHull {
        public enum HullShape {
            TriangleMesh, Sphere
        }
        public HullShape Shape;

        public double SphereRaduis;

        public Vector3d[] Verticies;
        public int[] Indicies;

        public PhysicsHull(double radius) {
            Shape = HullShape.Sphere;
            SphereRaduis = radius;
        }

        public PhysicsHull(Vector3d[] verticies, int[] indicies) {
            Shape = HullShape.TriangleMesh;
            Verticies = verticies;
            Indicies = indicies;
        }
    }
    abstract class PhysicsBody {
        public Vector3d Position;
        public Vector3d Velocity;
        public Matrix Rotation = Matrix.Identity;
        public Vector3d AngularVelocity;
        public Orbit Orbit;
        public PhysicsHull Hull;
        public OrientedBoundingBox OOB;

        public double Mass = 1;
        public double Drag = 1;
        public double StaticFriction = 1;
        public double DynamicFriction = 2;

        public bool DisablePhysics = false;

        public List<Force> Forces = new List<Force>();
        public Contact[] Contacts = new Contact[0];
        
        public PhysicsBody(double mass) {
            Mass = mass;
            Hull = new PhysicsHull(1);
            OOB = new OrientedBoundingBox(-Vector3.One, Vector3.One);
        }
        public PhysicsBody(double mass, PhysicsHull hull) {
            Mass = mass;
            Hull = hull;
            OOB = new OrientedBoundingBox(-Vector3.One, Vector3.One);
        }

        public Force AddForce(Vector3d force, Vector3d pos) {
            Force f = new Force(force, pos);
            Forces.Add(f);
            return f;
        }
        
        public virtual void Update(double deltaTime) {
            if (DisablePhysics) return;

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);

            // drag
            if (b != null && b != this) {
                Vector3d dir = (Position - b.Position);
                double h = dir.Length();
                dir /= h;
                if (b is Planet) {
                    Atmosphere a = (b as Planet).Atmosphere;
                    if (a != null && h < a.Radius) {
                        double v = Velocity.LengthSquared();
                        if (v > .1) {
                            double temp;
                            double pressure;
                            double density;
                            double c;
                            a.MeasureProperties(dir, h, out pressure, out density, out temp, out c);

                            double drag = .5 * density * v * Drag;
                            AddForce(-Vector3d.Normalize(Velocity) * drag, Vector3.Zero);
                        }
                    }
                }
            }
        }
        
        public void Integrate(double deltaTime) {
            if (DisablePhysics) return;

            Force netForce = new Force();
            foreach (Force f in Forces) {
                netForce.ForceVector += f.ForceVector;
                // TODO: torque
            }
            Forces.Clear();

            Velocity += (netForce.ForceVector / Mass) * deltaTime;
            Position += Velocity * deltaTime;
            Rotation *= Matrix.RotationAxis(Rotation.Right, (float)AngularVelocity.X) * Matrix.RotationAxis(Rotation.Up, (float)AngularVelocity.Y) * Matrix.RotationAxis(Rotation.Backward, (float)AngularVelocity.Z);
            OOB.Transformation = Rotation;

            List<Contact> contacts = new List<Contact>();

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            if (b != null && b != this) {
                // check collision
                if (Hull.Shape == PhysicsHull.HullShape.Sphere) {
                    Vector3d dir = Position - b.Position;
                    double h = dir.Length();
                    dir /= h;
                    double t = b.GetHeight(dir);
                    if (h < t + Hull.SphereRaduis) {
                        Vector3d n = -b.GetNormal(dir);
                        double vdn = Vector3d.Dot(Velocity, n);
                        if (vdn > 0) {
                            Contact contact = new Contact();
                            contact.BodyA = b;
                            contact.BodyB = this;
                            contact.ContactPosition = b.Position + dir * t;
                            contact.ContactNormal = -n;
                            contact.ContactVelocity = -n * vdn; // TODO: impulse collision

                            Position = b.Position + dir * (t + Hull.SphereRaduis);
                            Velocity += contact.ContactVelocity;

                            contacts.Add(contact);

                            // Friction
                            double mu = (DynamicFriction + b.DynamicFriction) * .5;
                            double l = Velocity.LengthSquared();
                            if (l < .25)
                                mu = MathUtil.Lerp((StaticFriction + b.StaticFriction) * .5, mu, l * 4); // static coefficient

                            Force f = AddForce(-Vector3d.Normalize(Velocity) * Mass * mu, contact.ContactPosition - Position);
                            
                            Debug.DrawLine(Color.Red, contact.ContactPosition, contact.ContactPosition + f.ForceVector);
                            Debug.DrawLine(Color.Blue, contact.ContactPosition, contact.ContactPosition + Velocity);
                        }
                    }
                }
            }

            Contacts = contacts.ToArray();
        }

        public virtual void PostUpdate() { }
        public virtual void Draw(Renderer renderer) { }
    }
    class Physics {
        public const double LIGHT_SPEED = 299792458;
        public const double LIGHT_YEAR = 9.4605284e15;
        public const double G = 6.674e-11;

        public static string FormatSpeed(double meters) {
            if (meters < Physics.LIGHT_SPEED) {
                string[] u = { "m/s", "km/s", "Mm/s", "Gm/s" };
                int i = 0;
                while (meters / 1000 > 1 && i < u.Length - 1) {
                    i++;
                    meters /= 1000;
                }
                return meters.ToString("F1") + u[i];
            } else {
                return (meters / Physics.LIGHT_SPEED).ToString("F4") + "c";
            }
        }
        public static string FormatDistance(double meters) {
            if (meters < Physics.LIGHT_YEAR) {
                string[] u = { "m", "km", "Mm", "Gm" };
                int i = 0;
                while (meters / 1000 > 1 && i < u.Length - 1) {
                    i++;
                    meters /= 1000;
                }
                return meters.ToString("F1") + u[i];
            } else {
                return (meters / Physics.LIGHT_YEAR).ToString("F4") + "ly";
            }
        }
        public static string CalculateTime(double dist, double speed) {
            ulong totalSeconds = (ulong)(dist / speed);
            ulong totalMinutes = totalSeconds / 60L;
            ulong totalHours = totalMinutes / 60L;
            ulong totalDays = totalHours / 24L;
            ulong totalYears = totalDays / 356L;

            ulong seconds = totalSeconds % 60L;
            ulong minutes = totalMinutes % 60L;
            ulong hours = totalHours % 60L;
            ulong days = totalDays % 24L;
            ulong years = totalYears % 365L;
            
            if (totalSeconds < 60L)
                return string.Format("{0} seconds", totalSeconds);
            else if (totalMinutes < 60L)
                return string.Format("{0}m:{1}s", minutes, seconds);
            else if (totalHours < 24L)
                return string.Format("{0}h:{1}m", hours, minutes);
            else if (totalDays < 365L)
                return string.Format("{0}d:{1}h", days, hours);
            else
                return string.Format("{0}y:{0}d", years, days);
        }

        public List<PhysicsBody> bodies = new List<PhysicsBody>();

        /// <summary>
        /// Calculates gravity force vector
        /// </summary>
        /// <returns>Force vector (from a to b)</returns>
        public static Vector3d Gravity(double deltaTime, PhysicsBody a, PhysicsBody b) {
            Vector3d dir = b.Position - a.Position;
            double l2 = dir.LengthSquared();
            if (l2 > .1) {
                double magnitude = G * (a.Mass * b.Mass) / l2;
                dir.Normalize();

                return dir * magnitude;
            }
            return Vector3.Zero;
        }

        public void AddBody(PhysicsBody body) {
            if (!bodies.Contains(body))
                bodies.Add(body);
        }
        public void RemoveBody(PhysicsBody body) {
            bodies.Remove(body);
        }

        public void Update(double deltaTime) {
            foreach (PhysicsBody b in bodies)
                if (!b.DisablePhysics)
                    foreach (PhysicsBody cb in StarSystem.ActiveSystem.bodies)
                        if (b != cb && !cb.DisablePhysics)
                            b.AddForce(Gravity(deltaTime, b, cb), Vector3.Zero);

            foreach (PhysicsBody b in bodies)
                b.Update(deltaTime);

            foreach (PhysicsBody b in bodies)
                b.Integrate(deltaTime);

            foreach (PhysicsBody b in bodies)
                b.PostUpdate();
        }

        public void Draw(Renderer renderer) {
            foreach (PhysicsBody b in bodies)
                b.Draw(renderer);
        }
    }
}
