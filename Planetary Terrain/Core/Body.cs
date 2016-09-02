using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;

namespace Planetary_Terrain {
    abstract class Body : IDisposable {
        /// <summary>
        /// The biggest size chunks are allowed to be
        /// </summary>
        public double MaxChunkSize;
        /// <summary>
        /// The smallest size chunks are allowed to be
        /// </summary>
        public double MinChunkSize = QuadTree.GridSize / 2;

        /// <summary>
        /// The 6 base quadtrees composing the planet
        /// </summary>
        public QuadTree[] BaseChunks;

        public Body OrbitalParent;

        public Vector3d Position;
        public Vector3d Velocity;
        /// <summary>
        /// Sphere of Influence: Radius of which things are considered to be within this body's influence
        /// </summary>
        public double SOI;
        public double Radius;
        public double Mass;
        public string Label;

        public Body(Vector3d pos, double radius, double mass) {
            Position = pos;
            Radius = radius;
            Mass = mass;
            SOI = Radius * 1.05;
        }

        public abstract void Update(D3D11.Device device, Camera camera);
        public abstract void Draw(Renderer renderer, Body mainSun);
        public void DrawHUDIcon(Renderer renderer) {
            Vector2? screenPos = renderer.WorldToScreen(Position);
            if (screenPos.HasValue) {
                renderer.SegoeUI14.TextAlignment = DWrite.TextAlignment.Center;
                renderer.SegoeUI14.WordWrapping = DWrite.WordWrapping.NoWrap;
                renderer.SegoeUI14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

                double dist = (renderer.Camera.Position - Position).Length();

                ulong totalSeconds = (ulong)(dist / (renderer.Camera.Speed * renderer.Camera.SpeedMultiplier));
                ulong totalMinutes = totalSeconds / 60L;
                ulong totalHours = totalMinutes / 60L;
                ulong totalDays = totalHours / 24L;
                ulong totalYears = totalDays / 356L;

                ulong seconds = totalSeconds % 60L;
                ulong minutes = totalMinutes % 60L;
                ulong hours = totalHours % 60L;
                ulong days = totalDays % 24L;
                ulong years = totalYears % 365L;

                string t = Label;
                if (totalSeconds < 60L)
                    t += "\nArrive in " + string.Format("{0} seconds", totalSeconds) + "";
                else if (totalMinutes < 60L)
                    t += "\nArrive in " + string.Format("{0}m:{1}s", minutes, seconds) + "";
                else if (totalHours < 24L)
                    t += "\nArrive in " + string.Format("{0}h:{1}m", hours, minutes) + "";
                else if (totalDays < 365L)
                    t += "\nArrive in " + string.Format("{0}d:{1}h", days, hours) + "";
                else
                    t += "\nArrive in " + string.Format("{0}y:{0}d", years, days) + "";

                DWrite.TextLayout layout = new DWrite.TextLayout(renderer.FontFactory, t, renderer.SegoeUI14, 100, 100);

                float w = layout.DetermineMinWidth();

                RawRectangleF rect = new RawRectangleF(
                    screenPos.Value.X - w, screenPos.Value.Y - 10,
                    screenPos.Value.X + w, screenPos.Value.Y + 10);

                renderer.D2DContext.DrawText(t, renderer.SegoeUI14, rect, renderer.SolidWhiteBrush);
            }
        }

        public abstract double GetHeight(Vector3d direction);
        public abstract Vector2 GetTemp(Vector3d direction);

        public void ApplyGravity(Body other, double deltaTime) {
            Vector3d dir = Position - other.Position;
            double magnitude = Constants.G * (Mass * other.Mass) / dir.LengthSquared();
            dir.Normalize();

            Velocity += dir * magnitude * deltaTime;
        }
        /// <summary>
        /// Returns the point on the surface of the planet, along the line from the planet's position to the given point
        /// </summary>
        public Vector3d GetPointOnSurface(Vector3d p) {
            p -= Position;
            p.Normalize();
            return p * Radius;
        }
        /// <summary>
        /// Convert a chordal distance to arc length
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public double ArcLength(double distance) {
            double angle = 2 * Math.Asin(distance / 2 / Radius);
            return Radius * angle;
        }
        public abstract void Dispose();
    }
}
