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
    abstract class PhysicsBody {
        public Vector3d Position;
        public Vector3d Velocity;
        public Matrix Rotation = Matrix.Identity;
        public Vector3d AngularVelocity;
        public double Mass;
        public List<Force> Forces = new List<Force>();

        public PhysicsBody(double mass) {
            Mass = mass;
        }

        public virtual void Update(double deltaTime) {
            Position += Velocity * deltaTime;
            Rotation *= Matrix.RotationAxis(Rotation.Right, (float)AngularVelocity.X) * Matrix.RotationAxis(Rotation.Up, (float)AngularVelocity.Y) * Matrix.RotationAxis(Rotation.Backward, (float)AngularVelocity.Z);

            // TODO: drag

            foreach (Force f in Forces) {
                Velocity += (f.ForceVector / Mass) * deltaTime;

                // TODO: calculate and apply torque
            }

            Forces.Clear();
        }
    }
    class Physics {
        public const double LIGHT_SPEED = 299792458;
        public const double G = 6.674e-11;

        public List<PhysicsBody> bodies = new List<PhysicsBody>();

        /// <summary>
        /// Calculates gravity force vector
        /// </summary>
        /// <returns>Force vector (from a to b)</returns>
        public static Vector3d Gravity(double deltaTime, PhysicsBody a, PhysicsBody b) {
            Vector3d dir = a.Position - b.Position;
            double magnitude = G * (a.Mass * b.Mass) / dir.LengthSquared();
            dir.Normalize();

            return dir * magnitude;
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
                b.Update(deltaTime);
            // TODO: gravity
        }
    }
}
