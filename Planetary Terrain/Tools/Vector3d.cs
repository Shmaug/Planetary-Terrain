using System;
using SharpDX;

namespace Planetary_Terrain {
    struct Vector3d {
        public double X, Y, Z;

        public Vector3d(double a) {
            X = Y = Z = a;
        }
        public Vector3d(double x, double y, double z) {
            X = x; Y = y; Z = z;
        }
        public Vector3d(Vector3 v) {
            X = v.X; Y = v.Y; Z = v.Z;
        }

        public double Length() {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }
        public double LengthSquared() {
            return X * X + Y * Y + Z * Z;
        }
        public void Normalize() {
            double l = 1 / Length();
            X *= l;
            Y *= l;
            Z *= l;
        }

        public static Vector3d Normalize(Vector3d a) {
            return a / a.Length();
        }

        public static double Dot(Vector3d v1, Vector3d v2) {
            return (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);
        }
        public static Vector3d Cross(Vector3d v1, Vector3d v2) {
            Vector3d product = new Vector3d(
                (v1.Y * v2.Z) - (v1.Z * v2.Y),
                (v1.Z * v2.X) - (v1.X * v2.Z),
                (v1.X * v2.Y) - (v1.Y * v2.X));

            return product;
        }

        public static Vector3d Lerp(Vector3d a, Vector3d b, double t) {
            return a + (b - a) * t;
        }

        public static Vector3d Transform(Vector3d vector, Matrix3x3 transform) {
            return new Vector3d(
                vector.X * transform.M11 + vector.Y * transform.M21 + vector.Z * transform.M31,
                vector.X * transform.M12 + vector.Y * transform.M22 + vector.Z * transform.M32,
                vector.X * transform.M13 + vector.Y * transform.M23 + vector.Z * transform.M33);
        }

        public static Vector3d Transform(Vector3d vector, Quaternion rotation) {
            double num = rotation.X + rotation.X;
            double num2 = rotation.Y + rotation.Y;
            double num3 = rotation.Z + rotation.Z;
            double num4 = rotation.W * num;
            double num5 = rotation.W * num2;
            double num6 = rotation.W * num3;
            double num7 = rotation.X * num;
            double num8 = rotation.X * num2;
            double num9 = rotation.X * num3;
            double num10 = rotation.Y * num2;
            double num11 = rotation.Y * num3;
            double num12 = rotation.Z * num3;
            return new Vector3d(
                vector.X * (1f - num10 - num12) + vector.Y * (num8 - num6) + vector.Z * (num9 + num5),
                vector.X * (num8 + num6) + vector.Y * (1f - num7 - num12) + vector.Z * (num11 - num4),
                vector.X * (num9 - num5) + vector.Y * (num11 + num4) + vector.Z * (1f - num7 - num10));
        }

        public static Vector3d operator +(Vector3d a, Vector3d b) {
            return new Vector3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public static Vector3d operator -(Vector3d a, Vector3d b) {
            return new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        public static Vector3d operator *(Vector3d a, double b) {
            return new Vector3d(a.X * b, a.Y * b, a.Z * b);
        }
        public static Vector3d operator /(Vector3d a, double b) {
            double f = 1 / b;
            return new Vector3d(a.X * f, a.Y * f, a.Z  * f);
        }
        public static Vector3d operator *(double b, Vector3d a) {
            return new Vector3d(a.X * b, a.Y * b, a.Z * b);
        }
        public static Vector3d operator /(double b, Vector3d a) {
            double f = 1 / b;
            return new Vector3d(a.X * f, a.Y * f, a.Z * f);
        }

        public static Vector3d operator -(Vector3d a) {
            return new Vector3d(-a.X, -a.Y, -a.Z);
        }

        public static Vector3d operator +(Vector3d a, Vector3 b) {
            return new Vector3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        public static Vector3d operator -(Vector3d a, Vector3 b) {
            return new Vector3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static implicit operator Vector3(Vector3d v) {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }
        public static implicit operator Vector3d(Vector3 v) {
            return new Vector3d(v.X, v.Y, v.Z);
        }

        public override string ToString() {
            return X + ", " + Y + ", " + Z;
        }
    }
}
