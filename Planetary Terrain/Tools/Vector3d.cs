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
    }
}
