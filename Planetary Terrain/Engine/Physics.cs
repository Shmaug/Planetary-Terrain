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
        public double a;
        public double e;
        public double i;
        public double w;
        public double omega;
        public double T;

        public Orbit(Vector3d position, Vector3d velocity, CelestialBody parent) {
            // http://ccar.colorado.edu/asen5070/handouts/cart2kep2002.pdf
            double v = velocity.Length();
            double r = position.Length();

            double u = parent.Mass * Physics.G;

            Vector3d H = Vector3d.Cross(position, velocity);
            double h = H.Length();

            double E = v * v * .5 - u / r;

            a = -u / (2 * E);
            e = Math.Sqrt(1 - h * h / (a * u));
            i = Math.Acos(H.Y / h);
            omega = Math.Atan2(H.X, -H.Z);
            double trueAnomaly = Math.Acos((a * (1 - e * e) - r) / (e * r));
            double eccentricAnomaly = 2 * Math.Atan(Math.Sqrt((1 - e) / (1 + e)) * Math.Tan(trueAnomaly / 2));
            w = Math.Atan2(position.Y / Math.Sin(i), position.X * Math.Cos(omega) + position.Z * Math.Sin(omega)) - trueAnomaly;
            T = 2 * Math.PI * Math.Sqrt(a * a * a / u);
        }

        double KeplerStart3(double M) {
            double t34 = e * e;
            double t35 = e * t34;
            double t33 = Math.Cos(M);
            return M + (-.5 * t35 + e + (t34 + 1.5 * t33 * t35) * t33) * Math.Sin(M);
        }
        double eps3(double M, double x) {
            double t1 = Math.Cos(x);
            double t2 = -1 + e * t1;
            double t3 = Math.Sin(x);
            double t4 = e * t3;
            double t5 = -x + t4 + M;
            double t6 = t5 / (1 / 2 * t5 * t4 / t2 + t2);
            return t5 / ((1 / 2 * t3 - 1 / 6 * t1 * t6) * e * t6 + t2);
        }
        double KeplerSolve(double M, double tolerance = 1e-14) {
            // http://alpheratz.net/dynamics/twobody/KeplerIterations_summary.pdf
            double Mnorm = M % 2 * Math.PI;
            double E0 = KeplerStart3(Mnorm);
            double dE = tolerance + 1;
            int count = 0;
            double E = 0;
            while (dE > tolerance) {
                E = E0 - eps3(Mnorm, E0);
                dE = Math.Abs(E - E0);
                E0 = E;
                count++;
                if (count > 100) break;
            }
            return E;
        }

        public void ToCartesian(CelestialBody parent, double t, out Vector3d position, out Vector3d velocity) {
            // http://ccar.colorado.edu/asen5070/handouts/kep2cart_2002.doc
            position = velocity = Vector3.Zero;

            double u = parent.Mass * Physics.G;

            double MA = Math.Sqrt(u / (a * a * a)) * (t - T);
            double EA = KeplerSolve(MA);

            double v = 2 * Math.Tan(Math.Sqrt((1 + e) / (1 - e)) * Math.Tan(EA * .5));
            double p = a * (1 - e * e);
            double r = p / (1 + e * Math.Cos(v));

            double cosOmega = Math.Cos(omega);
            double sinOmega = Math.Sin(omega);
            double coswv = Math.Cos(w + v);
            double sinwv = Math.Sin(w + v);
            double sini = Math.Sin(i);
            double cosi = Math.Cos(i);

            double term1 = cosOmega * coswv - sinOmega * sinwv * cosi;
            double term2 = sinOmega * coswv - cosOmega * sinwv * cosi;

            position.X = r * term1;
            position.Y = r * sini * sinwv;
            position.Z = r * term2;

            double h = Math.Sqrt(u * a * (1 - e * e));
            double sinv = Math.Sin(v);

            double herpsinv = h * e / (r * p) * sinv;
            double hr = h / r;

            velocity.X = position.X * herpsinv - hr * term1;
            velocity.Y = position.Y * herpsinv - hr * sini * coswv;
            velocity.Z = position.Z * herpsinv - hr * term2;
        }

        public override string ToString() {
            return string.Format(
                "Semimajor Axis: {0}"+
                "\nEccentricity: {1}"+
                "\nInclination: {2}"+
                "\nRight Ascension: {3}"+
                "\nPeriod: {4}", Physics.FormatDistance(a), e, i * 180 / Math.PI, omega, Physics.FormatTime(T));
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
        public double Penetration;
    }
    class PhysicsHull {
        public enum HullShape {
            TriangleMesh, Sphere, Celestial
        }
        public HullShape Shape;

        // Sphere
        public double SphereRadius;

        // TriangleMesh
        public Vector3d[] Verticies;
        public int[] Indicies;
        
        public PhysicsHull(double radius) {
            Shape = HullShape.Sphere;
            SphereRadius = radius;
        }
        public PhysicsHull(Vector3d[] verticies, int[] indicies) {
            Shape = HullShape.TriangleMesh;
            Verticies = verticies;
            Indicies = indicies;
        }
    }
    abstract class PhysicsBody {
        public Physics PhysicsSystem;

        public Vector3d Position;
        public Vector3d Velocity;
        public Matrix Rotation = Matrix.Identity;
        public Vector3d AngularVelocity;
        /// <summary>
        /// Returns linear velocity due to angular velocity
        /// </summary>
        public Vector3d VelocityOnPoint(Vector3d position) {
            double r = position.Length();
            Vector3d dx = AngularVelocity.X * (Vector3d)Rotation.Right;
            Vector3d dy = AngularVelocity.Y * (Vector3d)Rotation.Up;
            Vector3d dz = AngularVelocity.Z * (Vector3d)Rotation.Backward;
            return Vector3d.Cross(dx + dy + dz, position);
        }

        public Orbit Orbit;
        public PhysicsHull Hull;
        public OrientedBoundingBox OOB;

        public double Mass = 1;
        public double Drag = 1;
        public double Restitution = .2;
        public double StaticFriction = .2;
        public double DynamicFriction = .5;

        public bool DisablePhysics = false;
        public bool DisableCollision = false;
        
        public List<Force> Forces = new List<Force>();

        public List<Contact> Contacts = new List<Contact>();
        
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
        
        public virtual void UpdateForces(double deltaTime) {
            if (DisablePhysics) return;

            // gravity
            foreach (PhysicsBody cb in PhysicsSystem.bodies)
                if (cb is CelestialBody && this != cb && !cb.DisablePhysics)
                    AddForce(Physics.Gravity(this, cb, deltaTime), Vector3.Zero);

            // drag
            if (!(this is CelestialBody) && Drag > 0) {
                Atmosphere a = StarSystem.ActiveSystem.GetNearestAtmosphere(Position);
                if (a != null) {
                    Vector3d dir = (Position - a.Planet.Position);
                    double h = dir.Length();
                    dir /= h;
                    if (h < a.Radius) {
                        Vector3d airspeed = (Velocity - a.Planet.Velocity) - a.Planet.VelocityOnPoint(Position - a.Planet.Position);
                        double v = airspeed.LengthSquared();
                        if (v > .1) {
                            double temp;
                            double pressure;
                            double density;
                            double c;
                            a.MeasureProperties(dir, h, out pressure, out density, out temp, out c);

                            double drag = .5 * density * v * Drag;
                            AddForce(Vector3d.Normalize(-airspeed) * drag, Vector3.Zero);
                        }
                    }
                }
            }
        }
        public virtual void PostUpdate() { }
        public virtual void Draw(Renderer renderer) { }
    }
    class Collision {
        public static bool SphereSphereCollision(PhysicsBody a, PhysicsBody b, out Contact contact) {
            // TODO: Elastic sphere collision
            contact = new Contact();
            if ((b.Position - a.Position).LengthSquared() > (a.Hull.SphereRadius + b.Hull.SphereRadius) * (a.Hull.SphereRadius + b.Hull.SphereRadius)) return false;

            contact.BodyA = a;
            contact.BodyB = b;
            contact.ContactPosition = (b.Position * b.Hull.SphereRadius - a.Position * a.Hull.SphereRadius) / (a.Hull.SphereRadius + b.Hull.SphereRadius);
            contact.ContactNormal = Vector3d.Normalize(b.Position - a.Position);
            contact.Penetration = (b.Position - a.Position).Length() - (a.Hull.SphereRadius + b.Hull.SphereRadius);

            a.Velocity = a.Velocity * (a.Mass - b.Mass) + (2 * b.Mass * b.Velocity) / (a.Mass + b.Mass);
            b.Velocity = b.Velocity * (b.Mass - a.Mass) + (2 * a.Mass * a.Velocity) / (a.Mass + b.Mass);

            return true;
        }
        public static bool CelestialSphereCollision(CelestialBody a, PhysicsBody b, out Contact contact) {
            contact = new Contact();
            // TODO: turn celestial bodies into triangle meshes for collision
            // TODO: spheres falling through planet, but not all the way (height calculated wrong?)

            // check collision
            Vector3d dir = b.Position - a.Position;
            double h = dir.Length();
            if (h > a.SOI + b.Hull.SphereRadius) return false;
            dir /= h;
            double f = a.GetHeight(dir);
            if (h < f + b.Hull.SphereRadius) {
                Vector3d n = a.GetNormal(dir);
                double vdn = Vector3d.Dot(b.Velocity - a.Velocity, -n);
                if (vdn > 0) {
                    contact.BodyA = a;
                    contact.BodyB = b;
                    contact.ContactPosition = a.Position + dir * f;
                    contact.ContactNormal = n;
                    contact.Penetration = b.Hull.SphereRadius - (h - f);

                    // Resolve position
                    b.Position = a.Position + dir * (f + b.Hull.SphereRadius);
                    double j = Math.Max(-(1 + Math.Max(b.Restitution, a.Restitution)) * -vdn, 0);
                    b.Velocity += n * j / b.Mass; // Collision impulse

                    // Friction
                    double mu = 1;
                    double l = b.Velocity.LengthSquared();
                    if (l < .25)
                        mu = MathUtil.Lerp((b.StaticFriction + a.StaticFriction) * .5, (b.DynamicFriction + a.DynamicFriction) * .5, l * 4); // static coefficient
                    Vector3d r = contact.ContactPosition - b.Position;

                    return true;
                }
            }

            return false;
        }
    }
    class Physics {
        public const double LIGHT_SPEED = 299792458;
        public const double LIGHT_YEAR = 9.4605284e15;
        public const double G = 6.674e-11;
        public const int ITERATIONS = 4;

        public static string FormatSpeed(double meterspersecond) {
            if (meterspersecond < Physics.LIGHT_SPEED) {
                string[] u = { "m/s", "km/s", "Mm/s", "Gm/s" };
                int i = 0;
                while (meterspersecond / 1000 > 1 && i < u.Length - 1) {
                    i++;
                    meterspersecond /= 1000;
                }
                return meterspersecond.ToString("F1") + u[i];
            } else {
                return (meterspersecond / Physics.LIGHT_SPEED).ToString("F4") + "c";
            }
        }
        public static string FormatDistance(double meters) {
            if (meters < LIGHT_YEAR) {
                string[] u = { "m", "km", "Mm", "Gm" };
                int i = 0;
                while (meters / 1000 > 1 && i < u.Length - 1) {
                    i++;
                    meters /= 1000;
                }
                return meters.ToString("F1") + u[i];
            } else {
                return (meters / LIGHT_YEAR).ToString("F4") + "ly";
            }
        }
        public static string FormatTime(double seconds) {
            double totalMinutes = seconds / 60;
            double totalHours = totalMinutes / 60;
            double totalDays = totalHours / 24;
            double totalYears = totalDays / 356;
            
            if (seconds < 60)
                return string.Format("{0} seconds", seconds.ToString("F2"));
            else if (totalMinutes < 60)
                return string.Format("{0}m:{1}s", (seconds / 60).ToString("N0"), (seconds % 60).ToString("N0"));
            else if (totalHours < 24)
                return string.Format("{0}h:{1}m", (totalMinutes / 60).ToString("N0"), (totalMinutes % 60).ToString("N0"));
            else if (totalDays < 365)
                return string.Format("{0}d:{1}h", (totalHours / 24).ToString("N0"), (totalHours % 24).ToString("N0"));
            else
                return string.Format("{0}y:{1}d", (totalDays / 365).ToString("N0"), (totalDays % 365).ToString("N0"));
        }
        public static string CalculateTime(double dist, double speed) {
            if (speed < .1 && dist > 100000)
                return "Forever";
            return FormatTime(dist / speed);
        }
        
        /// <summary>
        /// Calculates gravity force vector
        /// </summary>
        /// <returns>Force vector (from a to b)</returns>
        public static Vector3d Gravity(PhysicsBody a, PhysicsBody b, double deltaTime) {
            Vector3d dir = b.Position - a.Position;
            double l2 = dir.LengthSquared();
            if (l2 > .1) {
                double magnitude = G * (a.Mass * b.Mass) / l2;
                dir.Normalize();

                return dir * magnitude;
            }
            return Vector3.Zero;
        }

        public List<PhysicsBody> bodies = new List<PhysicsBody>();

        public void AddBody(PhysicsBody body) {
            if (!bodies.Contains(body)) {
                bodies.Add(body);
                body.PhysicsSystem = this;
            }
        }
        public void RemoveBody(PhysicsBody body) {
            bodies.Remove(body);
            body.PhysicsSystem = null;
        }
        

        bool Detect(PhysicsBody a, PhysicsBody b, out Contact contact) {
            contact = new Contact();
            switch (a.Hull.Shape) {
                case PhysicsHull.HullShape.Sphere:
                    switch (b.Hull.Shape) {
                        case PhysicsHull.HullShape.Celestial:
                            return Collision.CelestialSphereCollision(b as CelestialBody, a, out contact);
                        case PhysicsHull.HullShape.Sphere:
                            return Collision.SphereSphereCollision(a, b, out contact);
                    }
                    break;
                case PhysicsHull.HullShape.Celestial:
                    switch (b.Hull.Shape) {
                        case PhysicsHull.HullShape.Sphere:
                            return Collision.CelestialSphereCollision(a as CelestialBody, b, out contact);
                    }
                    break;
            }
            return false;
        }

        void Integrate(double deltaTime) {
            foreach (PhysicsBody b in bodies) {
                if (b.DisablePhysics) continue;

                Vector3d netForce = new Vector3d();
                Vector3d netTorque = new Vector3d();
                foreach (Force f in b.Forces) {
                    netForce += f.ForceVector;
                }

                b.Velocity += (netForce / b.Mass) * deltaTime;
                b.Position += b.Velocity * deltaTime;

                b.Rotation *=
                    Matrix.RotationAxis(b.Rotation.Right, (float)(b.AngularVelocity.X * deltaTime)) *
                    Matrix.RotationAxis(b.Rotation.Up, (float)(b.AngularVelocity.Y * deltaTime)) *
                    Matrix.RotationAxis(b.Rotation.Backward, (float)(b.AngularVelocity.Z * deltaTime));

                b.OOB.Transformation = b.Rotation;

                b.Contacts.Clear();
            }

            List<Contact> contacts = new List<Contact>();
            foreach (PhysicsBody a in bodies) {
                if (a.DisableCollision || a.DisablePhysics) continue;
                foreach (PhysicsBody b in bodies) {
                    if (a == b || b.DisableCollision || b.DisablePhysics) continue;

                    bool f = false;
                    foreach (Contact c in contacts)
                        if ((c.BodyA == a && c.BodyB == b) || (c.BodyA == b && c.BodyB == a)) {
                            f = true;
                            break;
                        }
                    if (f) continue;

                    Contact contact;
                   if (Detect(a, b, out contact)) {
                       b.Contacts.Add(contact);
                       contacts.Add(contact);
                   }
                }
            }
        }

        public void Update(double deltaTime) {
            foreach (PhysicsBody b in bodies) {
                b.Forces.Clear();
                b.UpdateForces(deltaTime);
            }

            //for (int it = 0; it < ITERATIONS; it++)
            Integrate(deltaTime);// / ITERATIONS);

            foreach (PhysicsBody b in bodies)
                b.PostUpdate();
        }

        public void Draw(Renderer renderer) {
            foreach (PhysicsBody b in bodies)
                b.Draw(renderer);
        }

        public bool Raycast(Vector3d origin, Vector3d direction, out PhysicsBody body, out double t, out Vector3d normal, bool DetectStartedInside = false) {
            t = -1;
            normal = Vector3.Zero;
            body = null;

            foreach (PhysicsBody b in bodies) {
                switch (b.Hull.Shape) {
                    case PhysicsHull.HullShape.Sphere:
                        Vector3d d = b.Position - origin;
                        double d2 = Vector3d.Dot(d, d);
                        double r2 = b.Hull.SphereRadius * b.Hull.SphereRadius;
                        if (d2 <= r2 && DetectStartedInside) {
                            t = 0;
                            body = b;
                            return true;
                        }
                        double dot = Vector3d.Dot(d, direction);
                        if (dot < 0f) continue;
                        double num7 = d2 - dot * dot;
                        if (num7 > r2) continue;
                        double f = dot - Math.Sqrt((r2 - num7));
                        if (f < t) {
                            t = f;
                            body = b;
                            normal = Vector3d.Normalize(origin + direction * f - b.Position);
                        }
                        break;
                }
            }

            return t >= 0;
        }

        public void Dispose() {
            foreach (PhysicsBody b in bodies)
                if (b is IDisposable)
                    (b as IDisposable).Dispose();
        }
    }
}
