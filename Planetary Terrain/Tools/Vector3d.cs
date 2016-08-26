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
            double l = Length();
            X /= l;
            Y /= l;
            Z /= l;
        }

        public static Vector3d Normalize(Vector3d a) {
            return a / a.Length();
        }

        public static implicit operator Vector3(Vector3d v) {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
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
            return new Vector3d(a.X / b, a.Y / b, a.Z / b);
        }
        public static Vector3d operator *(double b, Vector3d a) {
            return new Vector3d(a.X * b, a.Y * b, a.Z * b);
        }
        public static Vector3d operator /(double b, Vector3d a) {
            return new Vector3d(a.X / b, a.Y / b, a.Z / b);
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
    }
}
