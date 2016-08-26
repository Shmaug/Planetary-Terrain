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

        public static Vector3d ToDouble(this Vector3 v) {
            return new Vector3d(v.X, v.Y, v.Z);
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

        public static Matrix RotationXYZ(Vector3 r) {
            return Matrix.RotationX(r.X) * Matrix.RotationY(r.Y) * Matrix.RotationZ(r.Z);
        }
        public static Matrix RotationXYZ(float x, float y, float z) {
            return Matrix.RotationX(x) * Matrix.RotationY(y) * Matrix.RotationZ(z);
        }

        public static double Clamp01(double a) {
            return Math.Max(Math.Min(a, 1), 0);
        }
        
        public static void AdjustPositionRelative(Vector3d position, Camera camera, out Vector3d newPos, out double scale) {
            var locationRelativeToCamera = position - camera.Position;
            var distanceFromCamera = locationRelativeToCamera.Length();
            var unscaledViewSpace = camera.zFar * 0.25;

            if (distanceFromCamera > unscaledViewSpace) {
                var scaledViewSpace = camera.zFar - unscaledViewSpace;
                double scaledDistanceFromCamera = unscaledViewSpace + (scaledViewSpace * (1.0 - Math.Exp((scaledViewSpace - distanceFromCamera) / 1000000000)));
                Vector3d scaledLocationRelativeToCamera = Vector3d.Normalize(locationRelativeToCamera) * scaledDistanceFromCamera;
            
                scale = (scaledDistanceFromCamera / distanceFromCamera);
                newPos = scaledLocationRelativeToCamera;
            } else {
                scale = 1;
                newPos = position;
            }
        }
    }
}
