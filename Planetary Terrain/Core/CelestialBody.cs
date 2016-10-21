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
        public string Name;

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

        public abstract void UpdateLOD(double deltaTime, D3D11.Device device, Camera camera);
        public void DrawHUDIcon(Renderer renderer, double playerSpeed) {
            Vector2? screenPos = renderer.WorldToScreen(Position);
            if (screenPos.HasValue) {
                renderer.SegoeUI14.TextAlignment = DWrite.TextAlignment.Center;
                renderer.SegoeUI14.WordWrapping = DWrite.WordWrapping.NoWrap;
                renderer.SegoeUI14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

                string text = Name + "\nArrive in " + Physics.CalculateTime((renderer.Camera.Position - Position).Length(), playerSpeed);

                DWrite.TextLayout layout = new DWrite.TextLayout(renderer.FontFactory, text, renderer.SegoeUI14, 100, 100);

                float w = layout.DetermineMinWidth();

                RawRectangleF rect = new RawRectangleF(
                    screenPos.Value.X - w, screenPos.Value.Y - 10,
                    screenPos.Value.X + w, screenPos.Value.Y + 10);

                renderer.D2DContext.DrawText(text, renderer.SegoeUI14, rect, renderer.Brushes["White"]);
            }
        }

        public abstract double GetHeight(Vector3d direction);
        public abstract void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double height);

        public Vector3d GetNormal(Vector3d direction) {
            Vector3d p1 = new Vector3d(0, GetHeight(direction), 0);
            Vector3d p2 = new Vector3d(0.1, GetHeight(direction + new Vector3d(0.001, 0, 0)), 0);
            Vector3d p3 = new Vector3d(0, GetHeight(direction + new Vector3d(0, 0, 0.001)), 0.1);
            return Vector3d.Cross(Vector3d.Normalize(p3 - p1), Vector3d.Normalize(p2 - p1));
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
        /// Returns a matrix describing a rotation such that up is relative to the planet's surface
        /// </summary>
        public Matrix OrientationFromDirection(Vector3d direction) {
            Vector3 pUp = direction;

            float ang = (float)Math.Acos(Vector3.Dot(pUp, Vector3.Up));
            if (ang != 0f)
                return Matrix.RotationAxis(Vector3.Normalize(Vector3.Cross(Vector3.Up, pUp)), ang);

            return Matrix.Identity;
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
