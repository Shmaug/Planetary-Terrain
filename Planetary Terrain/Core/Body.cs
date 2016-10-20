using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;

namespace Planetary_Terrain {
    abstract class CelestialBody : PhysicsBody, IDisposable {
        public double MaxVertexSpacing = 2000000; // m/vertex
        public double MinVertexSpacing = 1;       // m/vertex
        
        // TODO: planet rotation

        /// <summary>
        /// The 6 base quadtrees composing the planet
        /// </summary>
        public QuadNode[] BaseQuads;

        public CelestialBody OrbitalParent;
        
        /// <summary>
        /// Sphere of Influence: Radius of which things are considered to be within this body's influence
        /// </summary>
        public double SOI;
        public double Radius;
        public string Label;

        public CelestialBody(Vector3d pos, double radius, double mass) : base(mass) {
            Position = pos;
            Radius = radius;
            SOI = Radius * 1.02;

            MaxVertexSpacing = radius*.5 / QuadNode.GridSize;

            InitializeQuadTree();
        }
        void InitializeQuadTree() {
            double s = 1.41421356237 * Radius;

            BaseQuads = new QuadNode[6];
            BaseQuads[0] = new QuadNode(this, 0, s, 0, null, s * .5f * (Vector3d)Vector3.Up, MathTools.RotationXYZ(0, 0, 0));
            BaseQuads[1] = new QuadNode(this, 1, s, 0, null, s * .5f * (Vector3d)Vector3.Down, MathTools.RotationXYZ(MathUtil.Pi, 0, 0));
            BaseQuads[2] = new QuadNode(this, 2, s, 0, null, s * .5f * (Vector3d)Vector3.Left, MathTools.RotationXYZ(0, 0, MathUtil.PiOverTwo));
            BaseQuads[3] = new QuadNode(this, 3, s, 0, null, s * .5f * (Vector3d)Vector3.Right, MathTools.RotationXYZ(0, 0, -MathUtil.PiOverTwo));
            BaseQuads[4] = new QuadNode(this, 4, s, 0, null, s * .5f * (Vector3d)Vector3.ForwardLH, MathTools.RotationXYZ(MathUtil.PiOverTwo, 0, 0));
            BaseQuads[5] = new QuadNode(this, 5, s, 0, null, s * .5f * (Vector3d)Vector3.BackwardLH, MathTools.RotationXYZ(-MathUtil.PiOverTwo, 0, 0));

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Generate();
        }

        public virtual void Update(double deltaTime, D3D11.Device device, Camera camera) {
            // TODO: keplerian orbits
        }
        public abstract void Draw(Renderer renderer);
        public void DrawHUDIcon(Renderer renderer, double playerSpeed) {
            Vector2? screenPos = renderer.WorldToScreen(Position);
            if (screenPos.HasValue) {
                renderer.SegoeUI14.TextAlignment = DWrite.TextAlignment.Center;
                renderer.SegoeUI14.WordWrapping = DWrite.WordWrapping.NoWrap;
                renderer.SegoeUI14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

                double dist = (renderer.Camera.Position - Position).Length();

                ulong totalSeconds = (ulong)(dist / playerSpeed);
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

                renderer.D2DContext.DrawText(t, renderer.SegoeUI14, rect, renderer.Brushes["White"]);
            }
        }

        public abstract double GetHeight(Vector3d direction);
        public abstract void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double height);

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
        /// <summary>
        /// Convert a chordal distance to arc length
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public double ArcLength(Vector3d dir1, Vector3d dir2) {
            double distance = ((dir1 - dir2) * Radius).Length();
            double angle = 2 * Math.Asin(distance / 2 / Radius);
            return Radius * angle;
        }
        public abstract void Dispose();
    }
}
