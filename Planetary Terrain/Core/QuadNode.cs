using System;
using SharpDX;
using SharpDX.Direct3D;
using System.Threading;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace Planetary_Terrain {
    static class TriangleCache {
        static readonly int GridSize = QuadNode.GridSize;

        public static short[][] IndexCache;

        static short[] MakeIndicies(int index) {
            bool fanLeft = (index & 1) >= 1;
            bool fanUp = (index & 2) >= 1;
            bool fanRight = (index & 4) >= 1;
            bool fanDown = (index & 8) >= 1;

            List<short> inds = new List<short>();
            int s = GridSize + 1;
            short i0, i1, i2, i3, i4, i5, i6, i7, i8;
            for (int x = 0; x < s - 2; x += 2) {
                for (int z = 0; z < s - 2; z += 2) {
                    i0 = (short)((x + 0) * s + z);
                    i1 = (short)((x + 1) * s + z);
                    i2 = (short)((x + 2) * s + z);

                    i3 = (short)((x + 0) * s + z + 1);
                    i4 = (short)((x + 1) * s + z + 1);
                    i5 = (short)((x + 2) * s + z + 1);

                    i6 = (short)((x + 0) * s + z + 2);
                    i7 = (short)((x + 1) * s + z + 2);
                    i8 = (short)((x + 2) * s + z + 2);

                    if (fanUp && z == s - 3) {
                        if (fanRight && x == s - 3) {
                            #region Fan right/up
                            //    i6 --- i7 --- i8
                            //    |  \        /  |
                            //    |    \    /    |
                            // z+ i3 --- i4     i5
                            //    |  \    | \    |
                            //    |    \  |   \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i8, i4,
                                i8, i2, i4,
                                i6, i4, i3,
                                i3, i4, i1,
                                i3, i1, i0,
                                i4, i2, i1
                            });
                            #endregion
                        } else if (fanLeft && x == 0) {
                            #region Fan left/up
                            //    i6 --- i7 --- i8
                            //    |  \        /  |
                            //    |    \    /    |
                            // z+ i3     i4 --- i5
                            //    |    /  | \    |
                            //    |  /    |   \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i8, i4,
                                i6, i4, i0,
                                i8, i5, i4,
                                i4, i5, i2,
                                i4, i2, i1,
                                i4, i1, i0
                            });
                            #endregion
                        } else {
                            #region Fan up
                            //    i6 --- i7 --- i8
                            //    |  \        /  |
                            //    |    \    /    |
                            // z+ i3 --- i4 --- i5
                            //    |  \    | \    |
                            //    |    \  |   \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i4, i3,
                                i6, i8, i4,
                                i4, i8, i5,

                                i3, i4, i1,
                                i3, i1, i0,
                                i4, i5, i2,
                                i4, i2, i1
                            });
                            #endregion
                        }
                    } else if (fanDown && z == 0) {
                        if (fanRight && x == s - 3) {
                            #region Fan right/down
                            //    i6 --- i7 --- i8
                            //    |  \    |   /  |
                            //    |    \  | /    |
                            // z+ i3 --- i4     i5
                            //    |    /    \    |
                            //    |  /        \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i7, i4,
                                i6, i4, i3,
                                i3, i4, i0,
                                i0, i4, i2,
                                i7, i8 ,i4,
                                i4, i8, i2
                            });
                            #endregion
                        } else if (fanLeft && x == 0) {
                            #region Fan left/down
                            //    i6 --- i7 --- i8
                            //    |  \    | \    |
                            //    |    \  |   \  |
                            // z+ i3     i4 --- i5
                            //    |    /    \    |
                            //    |  /        \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i7, i4,
                                i7, i8, i5,
                                i7, i5, i4,
                                i4, i5, i2,
                                i6, i4, i0,
                                i0, i4, i2
                            });
                            #endregion
                        } else {
                            #region Fan down
                            //    i6 --- i7 --- i8
                            //    |  \    | \    |
                            //    |    \  |   \  |
                            // z+ i3 --- i4 --- i5
                            //    |    /    \    |
                            //    |  /        \  |
                            //    i0 --- i1 --- i2
                            //           x+
                            inds.AddRange(new short[] {
                                i6, i7, i4,
                                i6, i4, i3,
                                i7, i8, i5,
                                i7, i5, i4,

                                i3, i4, i0,
                                i0, i4, i2,
                                i4, i5, i2
                            });
                            #endregion
                        }
                    } else if (fanRight && x == s - 3) {
                        #region Fan right
                        //    i6 --- i7 --- i8
                        //    |  \    |   /  |
                        //    |    \  | /    |
                        // z+ i3 --- i4     i5
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new short[] {
                            i6, i7, i4,
                            i6, i4, i3,
                            i3, i4, i1,
                            i3, i1, i0,

                            i7, i8, i4,
                            i8, i2, i4,
                            i4, i2, i1
                        });
                        #endregion
                    } else if (fanLeft && x == 0) {
                        #region Fan left
                        //    i6 --- i7 --- i8
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        // z+ i3     i4 --- i5
                        //    |    /  | \    |
                        //    |  /    |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new short[] {
                            i6, i7, i4,
                            i6, i4, i0,
                            i7, i8, i5,
                            i7, i5, i4,
                            i4, i5, i2,
                            i4, i2, i1,
                            i4, i1, i0
                        });
                        #endregion
                    } else {
                        #region No fan
                        //    i6 --- i7 --- i8
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        // z+ i3 --- i4 --- i5
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new short[] {
                            i6, i7, i4,
                            i6, i4, i3,
                            i7, i8, i5,
                            i7, i5, i4,
                            i3, i4, i1,
                            i3, i1, i0,
                            i4, i5, i2,
                            i4, i2, i1
                        });
                        #endregion
                    }
                }
            }
            return inds.ToArray();
        }

        static TriangleCache() {
            IndexCache = new short[16][];
            for (int i = 0; i < IndexCache.Length; i++)
                IndexCache[i] = MakeIndicies(i);
        }
    }
    class QuadNode : IDisposable {
        public const int GridSize = 16;
        const double TreeLODThreshold = 30; // vertexspacing < TreeLODThreshold has trees
        const double TreeImposterLODThreshold = 100; // vertexspacing < TreeLODThreshold has trees
        const double waterDetailThreshold = 5000;

        #region Generation/Threading
        const int MaxGenerationCount = 10;

        public static List<QuadNode> GenerateQueue = new List<QuadNode>();
        public static List<QuadNode> Generating = new List<QuadNode>();
        static List<QuadNode> RemoveQueue = new List<QuadNode>();
        public static void Update() {
            if (GenerateQueue.Count > 0) {
                GenerateQueue = GenerateQueue.OrderByDescending(o => o.LoDLevel).ToList(); // prioritize high-lod nodes

                while (Generating.Count < MaxGenerationCount && GenerateQueue.Count > 0) {
                    QuadNode q = GenerateQueue[GenerateQueue.Count - 1];
                    GenerateQueue.Remove(q);
                    if (!q.Disposed)
                        Generating.Add(q);
                    ThreadPool.QueueUserWorkItem((object ctx) => {
                        q.generate();
                        RemoveQueue.Add(q);
                    });
                }
            }
            for (int i = 0; i < RemoveQueue.Count; i++)
                Generating.Remove(RemoveQueue[i]);
            RemoveQueue.Clear();

            // cleanup the Generating queue
            foreach (QuadNode n in Generating)
                if (n.Disposed || !n.generating)
                    RemoveQueue.Add(n);
            for (int i = 0; i < RemoveQueue.Count; i++)
                Generating.Remove(RemoveQueue[i]);
            RemoveQueue.Clear();
        }
        #endregion

        public bool Disposed = false;

        public double Size;
        public double ArcSize;
        public double VertexSpacing; // meters per vertex
        public long NodeID; // Unique ID based off sibling indicies

        public CelestialBody Body;
        public QuadNode Parent;
        public int SiblingIndex;
        public QuadNode[] Children;

        /// <summary>
        /// The position on the cube, before being projected into a sphere
        /// </summary>
        public Vector3d CubePosition;
        /// <summary>
        /// Position on the surface of the planet, along the radius of the planet
        /// </summary>
        public Vector3d SurfacePosition;

        /// <summary>
        /// The position of the mesh of which it is drawn at, relative to the planet
        /// </summary>
        public Vector3d MeshCenter;
        public Matrix3x3 Orientation;

        public Vector3d[] VertexSamples;
        public OrientedBoundingBox OOB;

        PlanetVertex[] verticies;
        VertexNormal[] waterVerticies;
        PlanetVertex[] waterFarVerticies;
        short[] indicies;

        public Matrix[] Trees;

        bool hasWaterVerticies;

        public int IndexCount { get; private set; }
        public int VertexCount { get; private set; }

        public int LoDLevel;

        [StructLayout(LayoutKind.Explicit, Size = 288)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
            [FieldOffset(64)]
            public Matrix WorldInverseTranspose;
            [FieldOffset(128)]
            public Matrix NodeToPlanetMatrix;
            [FieldOffset(192)]
            public Matrix NodeOrientation;
            [FieldOffset(256)]
            public Vector3 Color;
            [FieldOffset(268)]
            public float Scale;
            [FieldOffset(272)]
            public bool drawWaterFar;
        }
        private Constants constants;

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct WaterConstants {
            [FieldOffset(0)]
            public Vector3 Offset;
            [FieldOffset(12)]
            public float FadeDistance;
            [FieldOffset(16)]
            public float Time;
        }
        private WaterConstants waterConstants;

        public D3D11.Buffer vertexBuffer { get; private set; }
        public D3D11.Buffer waterVertexBuffer { get; private set; }
        public D3D11.Buffer waterFarVertexBuffer { get; private set; }
        public D3D11.Buffer indexBuffer { get; private set; }
        public D3D11.Buffer constantBuffer { get; private set; }
        public D3D11.Buffer waterConstantBuffer { get; private set; }
        public D3D11.Buffer TreeBuffer { get; private set; }

        bool indexdirty = false;
        bool vertexDirty = false;
        bool generating = false;
        public bool Ready
        {
            get
            {
                bool childrenReady = true;
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        if (!Children[i].Ready)
                            childrenReady = false;
                }
                return (Children != null && childrenReady) || (vertexDirty || vertexBuffer != null);
            }
        }

        public QuadNode(CelestialBody body, int siblingIndex, double size, int lod, QuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
            NodeID = Convert.ToInt64((parent == null ? "" : parent.NodeID.ToString()) + siblingIndex);
            Size = size;
            Body = body;
            Parent = parent;
            ArcSize = Body.ArcLength(Size);
            LoDLevel = lod;

            VertexSpacing = Size / GridSize;

            CubePosition = cubePos;
            Orientation = rot;

            constants = new Constants();
            constants.World = Matrix.Identity;

            MeshCenter = Vector3d.Normalize(CubePosition);
            SurfacePosition = MeshCenter * Body.Radius;
            MeshCenter *= Body.GetHeight(MeshCenter);

            SetupMesh();

            hasWaterVerticies = Body is Planet && ((Planet)Body).HasOcean;
        }
        #region Setup/Generation
        void SetupMesh() {
            VertexSamples = new Vector3d[9];
            int i = 0;

            double scale = Size / GridSize;

            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

            for (int x = 0; x <= GridSize; x += GridSize / 2) {
                for (int z = 0; z <= GridSize; z += GridSize / 2) {
                    Vector3d p = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                    p *= Body.GetHeight(p);
                    p -= MeshCenter;

                    VertexSamples[i++] = p;
                }
            }
        }
        public void Generate() {
            if (generating) return;
            generating = true;

            if (!GenerateQueue.Contains(this))
                GenerateQueue.Add(this);
        }
        void generate() {
            double scale = Size / GridSize;
            double invScale = 1d / Size;

            int s = GridSize + 1;
            verticies = new PlanetVertex[s * s * 6];
            List<short> inds = new List<short>();

            double oceanLevel = 0;
            if (hasWaterVerticies) {
                oceanLevel = Body.Radius + (Body as Planet).TerrainHeight * (Body as Planet).OceanHeight;
                waterVerticies = new VertexNormal[s * s * 6];
                waterFarVerticies = new PlanetVertex[s * s * 6];
            }

            Vector2 t;
            Vector3d p1d, p2d, p3d, d;
            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);
            double h;
            double rh;
            bool wv = false;

            Vector3[] pts = new Vector3[s * s * 6];

            for (int x = 0; x < s; x++) {
                for (int z = 0; z < s; z++) {
                    if (!generating) // needs to cancel generation
                        break;

                    p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                    p2d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z + 1) - offset), Orientation));
                    p3d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x + 1, 0, z) - offset), Orientation));
                    d = p1d;

                    Body.GetSurfaceInfo(p1d, out t, out h);

                    if (hasWaterVerticies) {
                        waterVerticies[x * s + z] = new VertexNormal((p1d * oceanLevel - MeshCenter) * invScale, p1d);

                        waterFarVerticies[x * s + z] =
                            new PlanetVertex(
                                waterVerticies[x * s + z].Position,
                                p1d,
                                p1d,
                                t,
                                (float)h);
                    }

                    rh = Body.GetHeight(p1d);
                    p1d = p1d * rh - MeshCenter;
                    p2d = p2d * Body.GetHeight(p2d) - MeshCenter;
                    p3d = p3d * Body.GetHeight(p3d) - MeshCenter;

                    verticies[x * s + z] =
                        new PlanetVertex(
                            p1d * invScale,
                            Vector3.Cross(Vector3d.Normalize(p2d - p1d), Vector3d.Normalize(p3d - p1d)),
                            d,
                            t, (float)h);
                    pts[x * s + z] = verticies[x * s + z].Position;

                    //if (x == 0 || z == 0 || x == s - 1 || z == s - 1)
                    //    verticies[x * s + z].Color = Color.Red;

                    if (hasWaterVerticies && rh > oceanLevel)
                        waterFarVerticies[x * s + z] = verticies[x * s + z];
                    else
                        wv = true;
                }

                if (!generating) // cancel generation
                    break;
            }
            if (!generating) { // generation cancelled
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                waterVertexBuffer?.Dispose();
                waterFarVertexBuffer?.Dispose();
                vertexDirty = false;
                verticies = null;
                indicies = null;
                waterVerticies = null;
                waterFarVerticies = null;
                vertexBuffer = null;
                indexBuffer = null;
                waterVertexBuffer = null;
                waterFarVertexBuffer = null;
                return;
            }

            if (!wv) { // no water verticies found
                waterVertexBuffer?.Dispose();
                waterFarVertexBuffer?.Dispose();
                hasWaterVerticies = false;
                waterVerticies = null;
                waterFarVerticies = null;
                waterFarVertexBuffer = null;
                waterVertexBuffer = null;
            }

            OOB = new OrientedBoundingBox(pts);
            GetIndicies(false);

            // trees
            if (VertexSpacing < TreeImposterLODThreshold) {
                bool p = true;
                if (Parent != null && Parent.VertexSpacing < TreeLODThreshold)
                    p = false;

                if (Body is Planet && p) {
                    Planet pl = Body as Planet;
                    if (pl.HasTrees) {
                        // generate trees if the parent node has no trees (otherwise when we render, the parent renders it's trees)
                        Random r = new Random((int)NodeID);
                        int c = (int)(s * s * VertexSpacing * .025); // .025 trees per square meter
                        double x, z;
                        Matrix rot;
                        List<Matrix> trees = new List<Matrix>();
                        for (int i = 0; i < c; i++) {
                            x = r.NextDouble() * s;
                            z = r.NextDouble() * s;
                            p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                            rh = Body.GetHeight(p1d);
                            
                            if (pl.GetTemperature(p1d) < 35 && pl.GetHumidity(p1d) > 0.4 // temp above 0 celsius, humidity above .1
                                && !pl.HasOcean || rh > oceanLevel // above ocean
                                ) {
                                rot = Body.OrientationFromDirection(p1d);
                                trees.Add(
                                    rot * Matrix.RotationAxis(rot.Up, (float)(r.NextDouble() * Math.PI * 2)) *
                                    Matrix.Translation((p1d * rh - MeshCenter))
                                );
                            }
                        }
                        Trees = trees.ToArray();
                    }
                }
            }

            generating = false;
            vertexDirty = true;
        }
        #endregion

        #region Index/Neighbor Calculations
        public void GetIndicies(bool recurse = true) {
            if (recurse && Children != null)
                for (int i = 0; i < Children.Length; i++)
                    Children[i].GetIndicies();

            if (verticies == null)
                return;

            QuadNode l = GetLeft();
            QuadNode r = GetRight();
            QuadNode u = GetUp();
            QuadNode d = GetDown();

            bool fanLeft = l != null && l.LoDLevel < LoDLevel;
            bool fanUp = u != null && u.LoDLevel < LoDLevel;
            bool fanRight = r != null && r.LoDLevel < LoDLevel;
            bool fanDown = d != null && d.LoDLevel < LoDLevel;

            int index = 0;
            if (fanLeft)
                index |= 1;
            if (fanUp)
                index |= 2;
            if (fanRight)
                index |= 4;
            if (fanDown)
                index |= 8;

            indicies = TriangleCache.IndexCache[index];

            indexdirty = true;
        }

        QuadNode GetLeft() {
            if (Parent != null) {
                QuadNode l;
                switch (SiblingIndex) {
                    case 0:
                        l = Parent.GetLeft();
                        if (l != null) {
                            if (l.Children != null) // parent left node is split, return the adjacent child
                                return l.Children[1];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 2:
                        l = Parent.GetLeft();
                        if (l != null) {
                            if (l.Children != null) // parent left node is split, return the adjacent child
                                return l.Children[3];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 1:
                        return Parent.Children[0];
                    case 3:
                        return Parent.Children[2];
                }
            }

            return null;
        }
        QuadNode GetRight() {
            if (Parent != null) {
                QuadNode l;
                switch (SiblingIndex) {
                    case 1:
                        l = Parent.GetRight();
                        if (l != null) {
                            if (l.Children != null) // parent right node is split, return the adjacent child
                                return l.Children[0];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 3:
                        l = Parent.GetRight();
                        if (l != null) {
                            if (l.Children != null) // parent right node is split, return the adjacent child
                                return l.Children[2];
                            else
                                return l; // parent right node isnt split
                        }
                        break;
                    case 0:
                        return Parent.Children[1];
                    case 2:
                        return Parent.Children[3];
                }
            }

            return null;
        }
        QuadNode GetUp() {
            if (Parent != null) {
                QuadNode l;
                switch (SiblingIndex) {
                    case 0:
                        l = Parent.GetUp();
                        if (l != null) {
                            if (l.Children != null) // parent up node is split, return the adjacent child
                                return l.Children[2];
                            else
                                return l; // parent up node isnt split
                        }
                        break;
                    case 1:
                        l = Parent.GetUp();
                        if (l != null) {
                            if (l.Children != null) // parent up node is split, return the adjacent child
                                return l.Children[3];
                            else
                                return l; // parent up node isnt split
                        }
                        break;
                    case 2:
                        return Parent.Children[0];
                    case 3:
                        return Parent.Children[1];
                }
            }

            return null;
        }
        QuadNode GetDown() {
            if (Parent != null) {
                QuadNode l;
                switch (SiblingIndex) {
                    case 2:
                        l = Parent.GetDown();
                        if (l != null) {
                            if (l.Children != null) // parent down node is split, return the adjacent child
                                return l.Children[0];
                            else
                                return l; // parent down node isnt split
                        }
                        break;
                    case 3:
                        l = Parent.GetDown();
                        if (l != null) {
                            if (l.Children != null) // parent down node is split, return the adjacent child
                                return l.Children[1];
                            else
                                return l; // parent down node isnt split
                        }
                        break;
                    case 0:
                        return Parent.Children[2];
                    case 1:
                        return Parent.Children[3];
                }
            }

            return null;
        }

        void UpdateNeighborIndicies() {
            QuadNode r = GetRight();
            QuadNode d = GetDown();
            QuadNode l = GetLeft();
            QuadNode u = GetUp();

            if (r != null && !r.generating)
                r.GetIndicies();
            if (l != null && !l.generating)
                l.GetIndicies();
            if (d != null && !d.generating)
                d.GetIndicies();
            if (u != null && !u.generating)
                u.GetIndicies();
        }
        #endregion

        #region Splitting/LOD
        public void Split(D3D11.Device device) {
            if (Children != null)
                return;

            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Vector3d rght = Vector3.Transform(Vector3.Right, Orientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, Orientation);

            Vector3d p0 = (-rght + fwd);
            Vector3d p1 = (rght + fwd);
            Vector3d p2 = (-rght + -fwd);
            Vector3d p3 = (rght + -fwd);

            Children = new QuadNode[4];
            Children[0] = new QuadNode(Body, 0, s, LoDLevel + 1, this, CubePosition + s * .5 * p0, Orientation);
            Children[1] = new QuadNode(Body, 1, s, LoDLevel + 1, this, CubePosition + s * .5 * p1, Orientation);
            Children[2] = new QuadNode(Body, 2, s, LoDLevel + 1, this, CubePosition + s * .5 * p2, Orientation);
            Children[3] = new QuadNode(Body, 3, s, LoDLevel + 1, this, CubePosition + s * .5 * p3, Orientation);

            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();

            UpdateNeighborIndicies();
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                Children[i]?.Dispose();

            Children = null;

            if (vertexBuffer == null && !generating)
                Generate();
            else
                GetIndicies();

            UpdateNeighborIndicies();
        }
        public void SplitDynamic(Vector3d dir, double height, D3D11.Device device) {
            double dist;
            Vector3d vertex;
            ClosestVertex(dir * height + Body.Position, out vertex, out dist);

            double arcDist = Body.ArcLength(Vector3.Normalize(vertex - Body.Position), dir);

            if (hasWaterVerticies) {
                double wh = Body.Radius + ((Planet)Body).TerrainHeight * ((Planet)Body).OceanHeight;
                Vector3d d2 = Vector3d.Normalize(vertex - Body.Position);
                dist = Math.Min(dist, (d2 * wh - dir * height).Length());
            }

            double x = (arcDist + dist) / 2;

            if (x * x < ArcSize * ArcSize || Size / GridSize > Body.MaxVertexSpacing) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(dir, height, device);
                } else {
                    if ((Size * .5f) / GridSize > Body.MinVertexSpacing)
                        Split(device);
                }
            } else
                UnSplit();
        }
        #endregion


        public void ClosestVertex(Vector3d pos, out Vector3d vert, out double dist) {
            vert = MeshCenter + Body.Position;
            dist = double.MaxValue;

            if (VertexSamples == null) return;

            pos -= MeshCenter + Body.Position;

            for (int i = 0; i < VertexSamples.Length; i++) {
                double d = (pos - VertexSamples[i]).LengthSquared();
                if (d < dist) {
                    dist = d;
                    vert = VertexSamples[i] + MeshCenter + Body.Position;
                }
            }

            dist = Math.Sqrt(dist);
        }
        public bool IsAboveHorizon(Vector3d camera) {
            Vector3d planetToCam = Vector3d.Normalize(camera - Body.Position);
            double horizonAngle = Math.Acos(Body.Radius / (Body.Position - camera).Length());

            for (int i = 0; i < VertexSamples.Length; i++) {
                Vector3d planetToMesh = Vector3d.Normalize(VertexSamples[i] + MeshCenter);

                double meshAngle = Math.Acos(Vector3.Dot(planetToCam, planetToMesh));

                if (horizonAngle > meshAngle)
                    return true;
            }

            return false;
        }

        public void SetData(D3D11.Device device, D3D11.DeviceContext context) {
            if (indexdirty) {
                indexBuffer?.Dispose();
                indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies);
                IndexCount = indicies.Length;
            }

            if (vertexDirty) {
                vertexBuffer?.Dispose();
                vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);
                if (hasWaterVerticies) {
                    waterVertexBuffer?.Dispose();
                    waterVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, waterVerticies);
                    waterFarVertexBuffer?.Dispose();
                    waterFarVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, waterFarVerticies);
                }
                VertexCount = verticies.Length;

                if (Trees != null && Trees.Length > 0){
                    TreeBuffer?.Dispose();
                    TreeBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, Trees, Marshal.SizeOf<Matrix>() * Trees.Length, D3D11.ResourceUsage.Dynamic, D3D11.CpuAccessFlags.Write);
                }
            }

            indexdirty = false;
            vertexDirty = false;
        }

        public enum QuadRenderPass {
            Ground, Water
        }
        public void Draw(Renderer renderer, QuadRenderPass pass, double scale, Vector3d pos) {
            Matrix world = Matrix.Scaling((float)scale) * Matrix.Translation(pos);
            OOB.Transformation = world;
            BoundingBox bbox = OOB.GetBoundingBox();
            if (renderer.Camera.Frustum.Intersects(ref bbox)) {
                double d;
                Vector3d v;
                ClosestVertex(renderer.Camera.Position, out v, out d);
                if (d < Debug.ClosestQuadTreeDistance) {
                    Debug.ClosestQuadTree = this;
                    Debug.ClosestQuadTreeDistance = d;
                    Debug.ClosestQuadTreeScale = scale;
                }

                if (pass == QuadRenderPass.Ground || pass == QuadRenderPass.Water) {
                    constants.World = world;
                    constants.WorldInverseTranspose = Matrix.Identity;
                    constants.NodeOrientation = Body.OrientationFromDirection(Vector3d.Normalize(MeshCenter));
                    constants.Scale = (float)scale;
                    constants.drawWaterFar = pass != QuadRenderPass.Water && hasWaterVerticies && d > waterDetailThreshold;
                    constants.Color = Vector3.One;

                    // constant buffer
                    if (constantBuffer == null)
                        constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
                    renderer.Context.UpdateSubresource(ref constants, constantBuffer);

                    renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
                    renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

                    renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
                }

                switch (pass) {
                    case QuadRenderPass.Water:
                        // when the camera is close, draw waterVertexBuffer
                        if (hasWaterVerticies && d < waterDetailThreshold) { // lod
                            waterConstants.Offset = Vector3.Zero;
                            waterConstants.FadeDistance = 50;
                            waterConstants.Time = (float)renderer.TotalTime;

                            // water constant buffer
                            if (waterConstantBuffer == null)
                                waterConstantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref waterConstants);
                            renderer.Context.UpdateSubresource(ref waterConstants, waterConstantBuffer);

                            renderer.Context.VertexShader.SetConstantBuffer(4, waterConstantBuffer);
                            renderer.Context.PixelShader.SetConstantBuffer(4, waterConstantBuffer);

                            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(waterVertexBuffer, Utilities.SizeOf<VertexNormal>(), 0));
                            renderer.Context.DrawIndexed(indicies.Length, 0, 0);

                            Debug.VerticiesDrawn += VertexCount;
                        }
                        break;
                    case QuadRenderPass.Ground:
                        // when the camera is far away, draw waterFarVertexBuffer and tell the shader to draw the water verticies in blue
                        if (hasWaterVerticies && d > waterDetailThreshold) // water lod
                            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(waterFarVertexBuffer, Utilities.SizeOf<PlanetVertex>(), 0));
                        else {
                            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PlanetVertex>(), 0));

                            constants.drawWaterFar = false;
                            renderer.Context.UpdateSubresource(ref constants, constantBuffer);
                        }
                        renderer.Context.DrawIndexed(indicies.Length, 0, 0);

                        Debug.VerticiesDrawn += VertexCount;
                        break;
                }
            }
        }
        public bool DrawTrees(Renderer renderer) {
            if (VertexSpacing < TreeLODThreshold) {
                if (Trees != null && Trees.Length > 0) {
                    double scale;
                    Vector3d pos;
                    renderer.Camera.GetScaledSpace(MeshCenter + Body.Position, out pos, out scale);

                    renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeBuffer, Marshal.SizeOf<Matrix>(), 0));
                    Models.TreeModel.DrawInstanced(
                        renderer,
                        Vector3d.Normalize(MeshCenter + Body.Position - StarSystem.ActiveSystem.GetStar().Position),
                        Matrix.Translation(pos),
                        Trees.Length);
                }
                return true;
            } else {
                bool imposter = true;
                if (Children != null)
                    for (int i = 0; i < Children.Length; i++)
                        if (Children[i].DrawTrees(renderer))
                            imposter = false;

                if (imposter && VertexSpacing < TreeImposterLODThreshold) {
                    double scale;
                    Vector3d pos;
                    renderer.Camera.GetScaledSpace(MeshCenter + Body.Position, out pos, out scale);

                    renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeBuffer, Marshal.SizeOf<Matrix>(), 0));
                    // TODO: draw tree imposter
                    return true;
                }
            }

            return false;
        }
        public void Draw(Renderer renderer, QuadRenderPass pass, Vector3d planetPos, double planetScale) {
            bool draw = true;

            if (Children != null) {
                draw = false;

                for (int i = 0; i < Children.Length; i++)
                    if (!Children[i].Ready)
                        draw = true;

                if (!draw)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].Draw(renderer, pass, planetPos, planetScale);
            }

            if (draw) {
                if (vertexDirty || indexdirty)
                    SetData(renderer.Device, renderer.Context);

                if (vertexBuffer != null) {
                    if (IsAboveHorizon(renderer.Camera.Position)) {
                        double scale = planetScale;
                        Vector3d pos = planetPos + MeshCenter * planetScale;

                        constants.NodeToPlanetMatrix = Matrix.Scaling((float)(scale * Size)) * Matrix.Translation(pos);

                        renderer.Camera.GetScaledSpace(MeshCenter + Body.Position, out pos, out scale);

                        Draw(renderer, pass, scale * Size, pos);
                    }
                }
            }
        }

        public void Dispose() {
            Disposed = true;

            if (generating) {
                generating = false;
                vertexDirty = false;
                GenerateQueue.Remove(this);
            }

            vertexBuffer?.Dispose();
            constantBuffer?.Dispose();
            indexBuffer?.Dispose();
            waterFarVertexBuffer?.Dispose();
            waterVertexBuffer?.Dispose();
            TreeBuffer?.Dispose();

            if (Children != null)
                for (int i = 0; i < Children.Length; i++)
                    Children[i].Dispose();
        }
    }
    class AtmosphereQuadNode : IDisposable {
        public const int GridSize = 16;

        public double Size;
        public double VertexSpacing; // meters per vertex

        public Atmosphere Atmosphere;
        public AtmosphereQuadNode Parent;
        public int SiblingIndex;
        public AtmosphereQuadNode[] Children;

        /// <summary>
        /// The position on the cube, before being projected into a sphere
        /// </summary>
        public Vector3d CubePosition;

        /// <summary>
        /// The position of the mesh of which it is drawn at, relative to the planet
        /// </summary>
        public Vector3d MeshCenter;
        public Matrix3x3 Orientation;

        public Vector3d[] VertexSamples;

        Vector3[] verticies;
        short[] indicies;

        public int IndexCount { get; private set; }
        public int VertexCount { get; private set; }

        public int LoDLevel;

        [StructLayout(LayoutKind.Explicit, Size = 208)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
            [FieldOffset(64)]
            public Matrix WorldInverseTranspose;
            [FieldOffset(128)]
            public Matrix NodeToPlanetMatrix;
        }
        private Constants constants;

        public D3D11.Buffer vertexBuffer { get; private set; }
        public D3D11.Buffer indexBuffer { get; private set; }
        public D3D11.Buffer constantBuffer { get; private set; }

        bool dirty = false;
        public bool Ready { get { return dirty || vertexBuffer != null; } }

        public AtmosphereQuadNode(Atmosphere atmo, int siblingIndex, double size, int lod, AtmosphereQuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
            Size = size;
            Atmosphere = atmo;
            Parent = parent;
            LoDLevel = lod;

            VertexSpacing = Size / GridSize;

            CubePosition = cubePos;
            Orientation = rot;

            constants = new Constants();
            constants.World = Matrix.Identity;

            MeshCenter = Vector3d.Normalize(CubePosition);
            MeshCenter *= Atmosphere.Radius;

            SetupMesh();
        }
        void SetupMesh() {
            VertexSamples = new Vector3d[9];
            int i = 0;

            double scale = Size / GridSize;

            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

            for (int x = 0; x <= GridSize; x += GridSize / 2) {
                for (int z = 0; z <= GridSize; z += GridSize / 2) {
                    Vector3d p = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                    p *= Atmosphere.Radius;
                    p -= MeshCenter;

                    VertexSamples[i++] = p;
                }
            }
        }

        public void Generate() {
            double scale = Size / GridSize;
            double invScale = 1d / Size;

            int s = GridSize + 1;
            verticies = new Vector3[s * s * 6];
            List<short> inds = new List<short>();

            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

            for (int x = 0; x < s; x++) {
                for (int z = 0; z < s; z++) {
                    verticies[x * s + z] =
                        (Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation)) * Atmosphere.Radius - MeshCenter) * invScale;
                }
            }
            GenerateIndicies();

            dirty = true;
        }

        public void GenerateIndicies() {
            AtmosphereQuadNode l = GetLeft();
            AtmosphereQuadNode r = GetRight();
            AtmosphereQuadNode u = GetUp();
            AtmosphereQuadNode d = GetDown();

            bool fanLeft = l != null && l.LoDLevel < LoDLevel;
            bool fanUp = u != null && u.LoDLevel < LoDLevel;
            bool fanRight = r != null && r.LoDLevel < LoDLevel;
            bool fanDown = d != null && d.LoDLevel < LoDLevel;

            int index = 0;
            if (fanLeft)
                index |= 1;
            if (fanUp)
                index |= 2;
            if (fanRight)
                index |= 4;
            if (fanDown)
                index |= 8;

            indicies = TriangleCache.IndexCache[index];

            dirty = true;
        }

        AtmosphereQuadNode GetLeft() {
            if (Parent != null) {
                AtmosphereQuadNode l;
                switch (SiblingIndex) {
                    case 0:
                        l = Parent.GetLeft();
                        if (l != null) {
                            if (l.Children != null) // parent left node is split, return the adjacent child
                                return l.Children[1];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 2:
                        l = Parent.GetLeft();
                        if (l != null) {
                            if (l.Children != null) // parent left node is split, return the adjacent child
                                return l.Children[3];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 1:
                        return Parent.Children[0];
                    case 3:
                        return Parent.Children[2];
                }
            }

            return null;
        }
        AtmosphereQuadNode GetRight() {
            if (Parent != null) {
                AtmosphereQuadNode l;
                switch (SiblingIndex) {
                    case 1:
                        l = Parent.GetRight();
                        if (l != null) {
                            if (l.Children != null) // parent right node is split, return the adjacent child
                                return l.Children[0];
                            else
                                return l; // parent left node isnt split
                        }
                        break;
                    case 3:
                        l = Parent.GetRight();
                        if (l != null) {
                            if (l.Children != null) // parent right node is split, return the adjacent child
                                return l.Children[2];
                            else
                                return l; // parent right node isnt split
                        }
                        break;
                    case 0:
                        return Parent.Children[1];
                    case 2:
                        return Parent.Children[3];
                }
            }

            return null;
        }
        AtmosphereQuadNode GetUp() {
            if (Parent != null) {
                AtmosphereQuadNode l;
                switch (SiblingIndex) {
                    case 0:
                        l = Parent.GetUp();
                        if (l != null) {
                            if (l.Children != null) // parent up node is split, return the adjacent child
                                return l.Children[2];
                            else
                                return l; // parent up node isnt split
                        }
                        break;
                    case 1:
                        l = Parent.GetUp();
                        if (l != null) {
                            if (l.Children != null) // parent up node is split, return the adjacent child
                                return l.Children[3];
                            else
                                return l; // parent up node isnt split
                        }
                        break;
                    case 2:
                        return Parent.Children[0];
                    case 3:
                        return Parent.Children[1];
                }
            }

            return null;
        }
        AtmosphereQuadNode GetDown() {
            if (Parent != null) {
                AtmosphereQuadNode l;
                switch (SiblingIndex) {
                    case 2:
                        l = Parent.GetDown();
                        if (l != null) {
                            if (l.Children != null) // parent down node is split, return the adjacent child
                                return l.Children[0];
                            else
                                return l; // parent down node isnt split
                        }
                        break;
                    case 3:
                        l = Parent.GetDown();
                        if (l != null) {
                            if (l.Children != null) // parent down node is split, return the adjacent child
                                return l.Children[1];
                            else
                                return l; // parent down node isnt split
                        }
                        break;
                    case 0:
                        return Parent.Children[2];
                    case 1:
                        return Parent.Children[3];
                }
            }

            return null;
        }

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;

            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Vector3d rght = Vector3.Transform(Vector3.Right, Orientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, Orientation);

            Vector3d p0 = (-rght + fwd);
            Vector3d p1 = (rght + fwd);
            Vector3d p2 = (-rght + -fwd);
            Vector3d p3 = (rght + -fwd);

            Children = new AtmosphereQuadNode[4];
            Children[0] = new AtmosphereQuadNode(Atmosphere, 0, s, LoDLevel + 1, this, CubePosition + s * .5 * p0, Orientation);
            Children[1] = new AtmosphereQuadNode(Atmosphere, 1, s, LoDLevel + 1, this, CubePosition + s * .5 * p1, Orientation);
            Children[2] = new AtmosphereQuadNode(Atmosphere, 2, s, LoDLevel + 1, this, CubePosition + s * .5 * p2, Orientation);
            Children[3] = new AtmosphereQuadNode(Atmosphere, 3, s, LoDLevel + 1, this, CubePosition + s * .5 * p3, Orientation);

            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();

            GetRight()?.RefreshIndicies(0);
            GetDown()?.RefreshIndicies(1);
            GetLeft()?.RefreshIndicies(2);
            GetUp()?.RefreshIndicies(3);
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                Children[i]?.Dispose();

            Children = null;

            if (vertexBuffer == null)
                Generate();

            GetRight()?.RefreshIndicies(0);
            GetDown()?.RefreshIndicies(1);
            GetLeft()?.RefreshIndicies(2);
            GetUp()?.RefreshIndicies(3);
        }
        public void SplitDynamic(Vector3d pos, D3D11.Device device) {
            double dist;
            Vector3d vert;
            ClosestVertex(pos, out vert, out dist);

            if (dist < Size || Size / GridSize > Atmosphere.MaxVertexSpacing) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(pos, device);
                } else {
                    if ((Size * .5f) / GridSize > Atmosphere.MinVertexSpacing)
                        Split(device);
                }
            } else
                UnSplit();
        }

        public void RefreshIndicies(int edgeIndex) {
            GenerateIndicies();

            if (Children != null) {
                int[,] ei = new int[,] {
                    { 0, 2 }, // left
                    { 0, 1 }, // up
                    { 1, 3 }, // right
                    { 2, 3 }  // down
                }; // update top 2 children if the top edge is split, etc..
                for (int i = 0; i < 2; i++)
                    Children[ei[edgeIndex, i]].RefreshIndicies(edgeIndex);
            }
        }

        public void ClosestVertex(Vector3d pos, out Vector3d vert, out double dist) {
            vert = MeshCenter + Atmosphere.Planet.Position;
            dist = double.MaxValue;

            if (VertexSamples == null) return;

            pos -= MeshCenter + Atmosphere.Planet.Position;

            for (int i = 0; i < VertexSamples.Length; i++) {
                double d = (pos - VertexSamples[i]).LengthSquared();
                if (d < dist) {
                    dist = d;
                    vert = VertexSamples[i] + MeshCenter + Atmosphere.Planet.Position;
                }
            }

            dist = Math.Sqrt(dist);
        }

        public void SetData(D3D11.Device device, D3D11.DeviceContext context) {
            vertexBuffer?.Dispose();
            vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);

            indexBuffer?.Dispose();
            indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies);

            VertexCount = verticies.Length;
            IndexCount = indicies.Length;

            dirty = false;
        }
        public void Draw(Renderer renderer, double scale, Vector3d pos) {
            double d = (renderer.Camera.Position - (MeshCenter + Atmosphere.Planet.Position)).Length();

            constants.World = Matrix.Scaling((float)scale) * Matrix.Translation(pos);
            constants.WorldInverseTranspose = Matrix.Identity;

            // constant buffer
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0));

            renderer.Context.DrawIndexed(indicies.Length, 0, 0);

            Debug.VerticiesDrawn += VertexCount;
        }
        public void Draw(Renderer renderer, Vector3d planetPos, double planetScale) {
            bool draw = true;

            if (Children != null) {
                draw = false;

                for (int i = 0; i < Children.Length; i++)
                    if (!Children[i].Ready)
                        draw = true;

                if (!draw)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].Draw(renderer, planetPos, planetScale);
            }

            if (draw) {
                if (dirty)
                    SetData(renderer.Device, renderer.Context);

                if (vertexBuffer != null) {
                    double scale = planetScale;
                    Vector3d pos = planetPos + MeshCenter * planetScale;

                    constants.NodeToPlanetMatrix = Matrix.Scaling((float)(scale * Size)) * Matrix.Translation(pos);

                    renderer.Camera.GetScaledSpace(MeshCenter + Atmosphere.Planet.Position, out pos, out scale);

                    scale *= Size;

                    Draw(renderer, scale, pos);
                }
            }
        }

        public void Dispose() {
            vertexBuffer?.Dispose();
            constantBuffer?.Dispose();
            indexBuffer?.Dispose();

            if (Children != null)
                for (int i = 0; i < Children.Length; i++)
                    Children[i].Dispose();
        }
    }
}