using System;
using SharpDX;

namespace Planetary_Terrain {
    static class MathTools {
        public static Vector3 Multiply(this Vector3 a, Vector3 b) {
            return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }
        public static Vector3 Add(this Vector3 a, float b) {
            return new Vector3(a.X + b, a.Y + b, a.Z + b);
        }
        public static Vector2 Multiply(this Vector2 a, Vector2 b) {
            return new Vector2(a.X * b.X, a.Y * b.Y);
        }
        public static Vector2 Add(this Vector2 a, float b) {
            return new Vector2(a.X + b, a.Y + b);
        }
        
        public static Vector3 ToVector3(this Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3 FromVec2(Vector2 xy, float z) {
            return new Vector3(xy.X, xy.Y, z);
        }
        public static Vector3 FromVec2(float x, Vector2 yz) {
            return new Vector3(x, yz.X, yz.Y);
        }

        public static Matrix3x3 RotationXYZ(Vector3 r) {
            return Matrix3x3.RotationX(r.X) * Matrix3x3.RotationY(r.Y) * Matrix3x3.RotationZ(r.Z);
        }
        public static Matrix3x3 RotationXYZ(float x, float y, float z) {
            return Matrix3x3.RotationX(x) * Matrix3x3.RotationY(y) * Matrix3x3.RotationZ(z);
        }

        public static double Clamp01(double a) {
            return Math.Max(Math.Min(a, 1), 0);
        }

        public static Vector3 ToEuler(this Quaternion q) {
            float sqy = q.Y * q.Y, sqz = q.Z * q.Z, sqw = q.W * q.W;
            return new Vector3(
                (float)Math.Asin(2f * (q.X * q.Z - q.W * q.Y)),                           // Pitch 
                (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz + sqw)), // Yaw 
                (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz))  // Roll
                );
        }
    }
}
