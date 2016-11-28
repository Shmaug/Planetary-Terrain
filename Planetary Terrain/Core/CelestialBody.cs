using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D3D11 = SharpDX.Direct3D11;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using System.Collections.Generic;

namespace Planetary_Terrain {
    abstract class CelestialBody : PhysicsBody, IDisposable {
        //public double MaxVertexSpacing = 10000000; // m/vertex
        public double MinVertexSpacing = 1;       // m/vertex
        
        /// <summary>
        /// The 6 base quadtrees composing the planet
        /// </summary>
        public QuadNode[] BaseNodes;
        public List<QuadNode> VisibleNodes;

        public CelestialBody OrbitalParent;
        
        /// <summary>
        /// Sphere of Influence: Radius of which things are considered to be within this body's influence
        /// </summary>
        public double SOI;
        public double Radius;
        public double BoundingRadius;
        public string Name;

        public bool WasDrawnLastFrame = false;

        Vector2 hudDir;

        public CelestialBody(Vector3d pos, double radius, double mass) : base(mass) {
            Position = pos;
            Radius = radius;
            SOI = Math.Sqrt(Physics.G * Mass / .1); // SOI is defined as point where acceleration due to gravity is less than .1 m/s^2
            BoundingRadius = radius;

            //MaxVertexSpacing = radius*.5 / QuadNode.GridSize;
            
            InitializeQuadTree();

            Hull.Shape = PhysicsHull.HullShape.Celestial;
        }
        void InitializeQuadTree() {
            double s = 1.41421356237 * Radius;

            VisibleNodes = new List<QuadNode>();
            
            BaseNodes = new QuadNode[6];
            BaseNodes[0] = new QuadNode(this, 0, s, 0, null, s * .5f * (Vector3d)Vector3.Up, MathTools.RotationXYZ(0, 0, 0));
            BaseNodes[1] = new QuadNode(this, 1, s, 0, null, s * .5f * (Vector3d)Vector3.Down, MathTools.RotationXYZ(MathUtil.Pi, 0, 0));
            BaseNodes[2] = new QuadNode(this, 2, s, 0, null, s * .5f * (Vector3d)Vector3.Left, MathTools.RotationXYZ(0, 0, MathUtil.PiOverTwo));
            BaseNodes[3] = new QuadNode(this, 3, s, 0, null, s * .5f * (Vector3d)Vector3.Right, MathTools.RotationXYZ(0, 0, -MathUtil.PiOverTwo));
            BaseNodes[4] = new QuadNode(this, 4, s, 0, null, s * .5f * (Vector3d)Vector3.ForwardLH, MathTools.RotationXYZ(MathUtil.PiOverTwo, 0, 0));
            BaseNodes[5] = new QuadNode(this, 5, s, 0, null, s * .5f * (Vector3d)Vector3.BackwardLH, MathTools.RotationXYZ(-MathUtil.PiOverTwo, 0, 0));

            for (int i = 0; i < BaseNodes.Length; i++)
                BaseNodes[i].Generate();
        }

        public void UpdateVisibleNodes() {
            VisibleNodes.Clear();
            for (int i = 0; i < BaseNodes.Length; i++)
                BaseNodes[i].GetVisible(ref VisibleNodes);
        }
        
        public virtual void UpdateLOD(D3D11.Device device, Camera camera) {
            Vector3d dir = camera.Position - Position;
            double height = dir.Length();
            dir /= height;
            double alt = height - GetHeight(dir);
            for (int i = 0; i < BaseNodes.Length; i++)
                BaseNodes[i].SplitDynamic(dir, height, alt, device);
        }
        public void DrawHUDIcon(Renderer renderer, double playerSpeed, Vector2 hudDir) {
            double h = (Position - renderer.MainCamera.Position).Length();
            if (h > Radius * 2) {
                double dir = Vector3d.Dot(renderer.MainCamera.Position - Position, renderer.MainCamera.Rotation.Forward);
                if (dir > 0) {
                    Vector2 screenPos = (Vector2)renderer.WorldToScreen(Position, renderer.MainCamera);
                    // TODO: UI radius still off

                    float r = 20;
                    r = Math.Max(r, 20);

                    int d = Math.Sign(hudDir.X);

                    renderer.SegoeUI14.TextAlignment = d > 0 ? DWrite.TextAlignment.Leading : DWrite.TextAlignment.Trailing;

                    Vector2 pt2 = screenPos + hudDir * r;
                    Vector2 pt3 = pt2 + hudDir * 60;
                    Vector2 pt4 = pt3 + new Vector2(d * 10, 0);

                    string text = Name + "\nArrive in " + Physics.CalculateTime((renderer.MainCamera.Position - Position).Length(), playerSpeed);

                    RawRectangleF rect = new RawRectangleF(pt4.X + d * 5, pt4.Y - 1, pt4.X + d * 5, pt4.Y - 1);

                    renderer.D2DContext.DrawEllipse(new D2D1.Ellipse(screenPos, r, r), renderer.Brushes["White"]);
                    renderer.D2DContext.DrawLine(pt2, pt3, renderer.Brushes["White"]);
                    renderer.D2DContext.DrawLine(pt3, pt4, renderer.Brushes["White"]);
                    renderer.D2DContext.DrawLine(pt4 + new Vector2(0, -20), pt4 + new Vector2(0, 20), renderer.Brushes["White"]);
                    renderer.D2DContext.DrawLine(pt4 + new Vector2(0, -20), pt4 + new Vector2(d * 5, -20), renderer.Brushes["White"]);
                    renderer.D2DContext.DrawLine(pt4 + new Vector2(0, 20), pt4 + new Vector2(d * 5, 20), renderer.Brushes["White"]);
                    renderer.D2DContext.DrawText(text, renderer.SegoeUI14, rect, renderer.Brushes["White"]);
                }
            }
        }

        public abstract double GetHeight(Vector3d direction, bool transformDirection = true);
        public abstract void GetSurfaceInfo(Vector3d direction, out Vector2 data, out double height);

        public Vector3d GetNormal(Vector3d direction, bool transformDirection = true) {
            Vector3d p1 = direction * GetHeight(direction, transformDirection);
            Matrix m = (transformDirection ? Rotation : Matrix.Identity) * OrientationFromDirection(direction);
            Vector3d p2 = Vector3d.Normalize(p1 + m.Right);
            Vector3d p3 = Vector3d.Normalize(p1 + m.Forward);
            p2 *= GetHeight(p2, transformDirection);
            p3 *= GetHeight(p3, transformDirection);

            return Vector3d.Cross(Vector3d.Normalize(p2 - p1), Vector3d.Normalize(p3 - p1));
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
