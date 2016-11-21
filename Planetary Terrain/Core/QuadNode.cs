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
    class QuadNode {
        public const int GridSize = 16;
        const double WaterDetailDistance = 1000;
        static double TreeLODLevel { get { return Properties.Settings.Default.TreeLODLevel; } }
        static double TreeImposterDistance { get { return Properties.Settings.Default.TreeImposterDistance; } }
        static double TreeDistance { get { return Properties.Settings.Default.TreeDistance; } }

        #region Generation/Threading
        const int MaxGenerationCount = 10;

        public static List<QuadNode> GenerateQueue = new List<QuadNode>();
        public static List<QuadNode> Generating = new List<QuadNode>();
        static List<QuadNode> RemoveQueue = new List<QuadNode>();
        public static void Update() {
            if (GenerateQueue.Count > 0) {
                GenerateQueue = GenerateQueue.OrderByDescending(o => o.LODlevel).ToList(); // prioritize high-lod nodes

                while (Generating.Count < MaxGenerationCount && GenerateQueue.Count > 0) {
                    QuadNode q = GenerateQueue[GenerateQueue.Count - 1]; // Prioritize recently added nodes
                    GenerateQueue.Remove(q);
                    if (!q.Disposed)
                        Generating.Add(q);
                    ThreadPool.QueueUserWorkItem((object ctx) => {
                        q.generate();
                        RemoveQueue.Add(q);
                    });
                }
            }

            // cleanup the Generating queue
            foreach (QuadNode n in Generating)
                if (n.Disposed || !n.generating)
                    RemoveQueue.Add(n);
            for (int i = 0; i < RemoveQueue.Count; i++) {
                RemoveQueue[i].Body.UpdateVisibleNodes();
                Generating.Remove(RemoveQueue[i]);
            }
            RemoveQueue.Clear();
        }
        #endregion

        public bool Disposed = false;

        public double Size;
        public double ArcSize;
        public double VertexSpacing; // meters per vertex
        public int NodeID; // Unique ID based off sibling indicies

        public CelestialBody Body;
        public QuadNode Parent;
        public int SiblingIndex;
        public QuadNode[] Children;

        /// <summary>
        /// The position on the cube, before being projected into a sphere
        /// </summary>
        public Vector3d CubePosition;

        Vector3d planetMeshPosition;
        Vector3d planetWaterMeshPosition;
        /// <summary>
        /// The position of the mesh of which it is drawn at, relative to the planet
        /// </summary>
        public Vector3d MeshPosition
        {
            get
            {
                return Vector3d.Transform(planetMeshPosition, (Matrix3x3)Body.Rotation) + Body.Position;
            }
        }
        /// <summary>
        /// The position of the water mesh of which it is drawn at, relative to the planet
        /// </summary>
        public Vector3d WaterMeshPosition
        {
            get
            {
                return Vector3d.Transform(planetWaterMeshPosition, (Matrix3x3)Body.Rotation) + Body.Position;
            }
        }
        /// <summary>
        /// Oreintation on the face of the cube (so that up = out on the cube)
        /// </summary>
        public Matrix3x3 CubeOrientation;
        /// <summary>
        /// Orientation on the face of the planet (so that up = out on the planet)
        /// </summary>
        public Matrix NodeOrientation;

        public Vector3d[] VertexSamples;
        public OrientedBoundingBox OOB;
        public OrientedBoundingBox WaterOOB;

        public Matrix[] Trees;
        
        public int LODlevel;
        
        PlanetVertex[] verticies;
        short[] indicies;
        short[] farIndicies;
        int indexCount;
        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;
        D3D11.Buffer farIndexBuffer; // triangles below water are removed
        D3D11.Buffer constantBuffer;
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
            public Vector3 LightDirection;
            [FieldOffset(272)]
            public Vector3 Color;
            [FieldOffset(284)]
            public float NodeScale;
        }
        Constants constants;

        WaterVertex[] waterVerticies;
        short[] waterIndicies;
        int waterIndexCount;
        bool hasWaterVerticies;
        bool hasVerticiesAboveWater;
        D3D11.Buffer waterVertexBuffer;
        D3D11.Buffer waterIndexBuffer;
        D3D11.Buffer waterConstantBuffer;
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        struct WaterConstants {
            [FieldOffset(0)]
            public Vector3 Offset;
            [FieldOffset(12)]
            public float FadeDistance;
            [FieldOffset(16)]
            public float Time;
        }
        WaterConstants waterConstants;
        double oceanLevel;
        
        D3D11.Buffer TreeBuffer;
        int TreeBufferSize = 0;
        D3D11.Buffer TreeImposterBuffer;
        D3D11.Buffer TreeImposerConstantBuffer;

        bool indexdirty = false;
        bool vertexDirty = false;
        bool generating = false;
        public bool Ready
        {
            get
            {
                return !Disposed && (vertexDirty || vertexBuffer != null);
            }
        }
        
        static int GenerateNodeID(string number) {
            // create base-10 number from base-4 number
            int result = 0;
            int multiplier = 1;
            for (int i = number.Length - 1; i >= 0; i--) {
                result += number[i] * multiplier;
                multiplier *= 4;
            }

            return result;
        }
        public QuadNode(CelestialBody body, int siblingIndex, double size, int lod, QuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
            NodeID = parent == null ? 0 : GenerateNodeID(siblingIndex + parent.NodeID.ToString());
            Size = size;
            Body = body;
            Parent = parent;
            ArcSize = Body.ArcLength(Size);
            LODlevel = lod;

            VertexSpacing = Size / GridSize;

            CubePosition = cubePos;
            CubeOrientation = rot;

            constants = new Constants();
            constants.Color = Vector3.One;
            constants.World = Matrix.Identity;

            hasWaterVerticies = Body is Planet && ((Planet)Body).HasOcean;

            planetMeshPosition = Vector3d.Normalize(CubePosition);
            NodeOrientation = Body.OrientationFromDirection(planetMeshPosition);
            if (hasWaterVerticies) planetWaterMeshPosition = planetMeshPosition * (Body.Radius + (Body as Planet).OceanHeight * (Body as Planet).TerrainHeight);
            planetMeshPosition *= Body.GetHeight(planetMeshPosition, false);

            SetupMesh();
        }
        #region Setup/Generation
        void SetupMesh() {
            VertexSamples = new Vector3d[9];
            int i = 0;

            double scale = Size / GridSize;

            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

            for (int x = 0; x <= GridSize; x += GridSize / 2) {
                for (int z = 0; z <= GridSize; z += GridSize / 2) {
                    Vector3d p = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), CubeOrientation));
                    p *= Body.GetHeight(p, false);
                    p -= planetMeshPosition;

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

            if (hasWaterVerticies) {
                oceanLevel = Body.Radius + (Body as Planet).TerrainHeight * (Body as Planet).OceanHeight;
                waterVerticies = new WaterVertex[s * s * 6];
            }

            Vector2 t;
            Vector3d p1d, p2d, p3d, d;
            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);
            double h, rh;
            double lat, lon;
            bool hasWaterGeometry = false;

            double texScale = Body.Radius * .5 / (Math.PI * 2);

            Matrix invoob = Matrix.Invert(NodeOrientation);
            List<Vector3> oobpts = new List<Vector3>();
            List<Vector3> wateroobpts = new List<Vector3>();

            #region vertex generation
            for (int x = 0; x < s; x++) {
                for (int z = 0; z < s; z++) {
                    if (!generating) // needs to cancel generation
                        break;

                    p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), CubeOrientation));
                    p2d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z + 1) - offset), CubeOrientation));
                    p3d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x + 1, 0, z) - offset), CubeOrientation));
                    d = p1d;
                    lat = Math.Asin(d.Y);
                    lon = Math.Atan2(d.Z, d.X);

                    Body.GetSurfaceInfo(p1d, out t, out h);
                    rh = Body.GetHeight(p1d, false);

                    if (hasWaterVerticies) {
                        waterVerticies[x * s + z] = new WaterVertex((p1d * oceanLevel - planetWaterMeshPosition) * invScale, p1d, (float)(oceanLevel - rh));
                        wateroobpts.Add((Vector3)Vector3.Transform(waterVerticies[x * s + z].Position, invoob));
                        if (rh < oceanLevel)
                            hasWaterGeometry = true;
                        else
                            hasVerticiesAboveWater = true;
                    }

                    p1d = p1d * rh - planetMeshPosition;
                    p2d = p2d * Body.GetHeight(p2d, false) - planetMeshPosition;
                    p3d = p3d * Body.GetHeight(p3d, false) - planetMeshPosition;

                    verticies[x * s + z] =
                        new PlanetVertex(
                            p1d * invScale,
                            Vector3.Cross(Vector3d.Normalize(p2d - p1d), Vector3d.Normalize(p3d - p1d)),
                            t);

                    oobpts.Add((Vector3)Vector3.Transform(verticies[x * s + z].Position, invoob));
                }

                if (!generating) // cancel generation
                    break;
            }
            if (!generating) { // generation cancelled
                vertexDirty = false;
                verticies = null;
                indicies = null;
                waterVerticies = null;
                return;
            }
            #endregion

            OOB = new OrientedBoundingBox(oobpts.ToArray());
            if (!hasWaterGeometry) {
                hasWaterVerticies = false;
                waterVerticies = null;
            } else
                WaterOOB = new OrientedBoundingBox(wateroobpts.ToArray());

            GetIndicies(false);
            
            #region tree generation
            // trees
            if (VertexSpacing < TreeLODLevel) {
                if (Body is Planet) {
                    Planet pl = Body as Planet;
                    if (pl.HasTrees) {
                        // grab trees from parent that has them
                        if (Parent != null && Parent.Trees != null) {
                            List<Matrix> trees = new List<Matrix>();
                            for (int i = 0; i < Parent.Trees.Length; i++) {
                                Vector3 pos = ((Vector3d)Parent.Trees[i].TranslationVector + Parent.planetMeshPosition) - planetMeshPosition;
                                Matrix m = Parent.Trees[i];
                                m.TranslationVector = pos;
                                OOB.Transformation = Matrix.Scaling((float)Size) * NodeOrientation;
                                if (OOB.Contains(pos) != ContainmentType.Disjoint)
                                    trees.Add(m);
                            }
                            Trees = trees.ToArray();
                        } else {
                            // generate trees if the parent node has no trees
                            Random r = new Random(NodeID);
                            int c = (int)(s * VertexSpacing * s * VertexSpacing * .001); // .001 trees per square meter
                            double x, z;
                            Matrix rot;
                            List<Matrix> trees = new List<Matrix>();
                            for (int i = 0; i < c; i++) {
                                x = r.NextDouble();
                                z = r.NextDouble();
                                p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z)*s - offset), CubeOrientation));
                                rh = Body.GetHeight(p1d, false);

                                if (rh > oceanLevel &&  // above ocean
                                    pl.GetTemperature(p1d) < 35
                                    && pl.GetHumidity(p1d) > 0.4) {
                                    rot = Body.OrientationFromDirection(p1d);
                                    float ry = (float)(r.NextDouble() * Math.PI * 2);
                                    trees.Add(
                                        rot * Matrix.RotationAxis(rot.Up, ry) *
                                        Matrix.Translation((p1d * rh - planetMeshPosition))
                                    );
                                }
                            }
                            Trees = trees.ToArray();
                        }
                    }
                }
            }
            #endregion

            generating = false;
            vertexDirty = true;
        }
        #endregion
        
        #region Index/Neighbor Calculations
        public void GetIndicies(bool recurse = true) {
            if (recurse && Children != null) {
                Children[0].GetIndicies();
                Children[1].GetIndicies();
                Children[2].GetIndicies();
                Children[3].GetIndicies();
            }

            if (verticies == null)
                return;

            QuadNode l = GetLeft();
            QuadNode r = GetRight();
            QuadNode u = GetUp();
            QuadNode d = GetDown();


            bool fanLeft  = l != null && l.LODlevel < LODlevel;
            bool fanUp    = u != null && u.LODlevel < LODlevel;
            bool fanRight = r != null && r.LODlevel < LODlevel;
            bool fanDown  = d != null && d.LODlevel < LODlevel;

            int index = 0;
            if (fanLeft)  index |= 1;
            if (fanUp)    index |= 2;
            if (fanRight) index |= 4;
            if (fanDown)  index |= 8;

            indicies = TriangleCache.IndexCache[index];
            
            indexdirty = true;
        }

        QuadNode GetLeft() {
            if (Parent != null && Parent.Children != null) {
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
            if (Parent != null && Parent.Children != null) {
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
            if (Parent != null && Parent.Children != null) {
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
            if (Parent != null && Parent.Children != null) {
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

            Vector3d rght = Vector3.Transform(Vector3.Right, CubeOrientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, CubeOrientation);

            Vector3d p0 = (-rght + fwd);
            Vector3d p1 = (rght + fwd);
            Vector3d p2 = (-rght + -fwd);
            Vector3d p3 = (rght + -fwd);

            Children = new QuadNode[4];
            Children[0] = new QuadNode(Body, 0, s, LODlevel + 1, this, CubePosition + s * .5 * p0, CubeOrientation);
            Children[1] = new QuadNode(Body, 1, s, LODlevel + 1, this, CubePosition + s * .5 * p1, CubeOrientation);
            Children[2] = new QuadNode(Body, 2, s, LODlevel + 1, this, CubePosition + s * .5 * p2, CubeOrientation);
            Children[3] = new QuadNode(Body, 3, s, LODlevel + 1, this, CubePosition + s * .5 * p3, CubeOrientation);

            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();

            UpdateNeighborIndicies();
        }
        public void UnSplit() {
            if (Children == null) return;

            Children[0]?.Dispose();
            Children[1]?.Dispose();
            Children[2]?.Dispose();
            Children[3]?.Dispose();
            Children = null;

            if (vertexBuffer == null && !generating)
                Generate();
            else
                GetIndicies();

            UpdateNeighborIndicies();
            
            if (!Ready && !generating)
                Generate();

            Body.UpdateVisibleNodes();
        }
        public void SplitDynamic(Vector3d dir, double height, double altitude, D3D11.Device device) {
            double dist;
            Vector3d vertex;
            ClosestVertex(dir * height + Body.Position, out vertex, out dist);

            double arcDist = Body.ArcLength(Vector3.Normalize(vertex - Body.Position), dir);

            if (hasWaterVerticies) {
                double wh = Body.Radius + ((Planet)Body).TerrainHeight * ((Planet)Body).OceanHeight;
                Vector3d d2 = Vector3d.Normalize(vertex - Body.Position);
                dist = Math.Min(dist, (d2 * wh - dir * height).Length());
            }

            double x = (arcDist + dist) * .5;
            // TODO: Better split function, that allows for higher vertex spacing from exponentially farther distances, and decreases cracks when LOD transitions are large
            if (x * x < ArcSize * ArcSize || Size / GridSize > Body.MaxVertexSpacing) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(dir, height, altitude, device);
                } else {
                    if ((Size * .5f) / GridSize > Body.MinVertexSpacing)
                        Split(device);
                }
            } else
                UnSplit();
        }
        #endregion

        public void ClosestVertex(Vector3d pos, out Vector3d vert, out double dist) {
            vert = MeshPosition;
            dist = double.MaxValue;

            if (VertexSamples == null) return;

            pos -= MeshPosition;

            for (int i = 0; i < VertexSamples.Length; i++) {
                double d = (pos - VertexSamples[i]).LengthSquared();
                if (d < dist) {
                    dist = d;
                    vert = VertexSamples[i] + MeshPosition;
                }
            }

            dist = Math.Sqrt(dist);
        }
        public void FarthestVertex(Vector3d pos, out Vector3d vert, out double dist) {
            vert = MeshPosition;
            dist = 0;

            if (VertexSamples == null) return;

            pos -= MeshPosition;

            for (int i = 0; i < VertexSamples.Length; i++) {
                double d = (pos - VertexSamples[i]).LengthSquared();
                if (d > dist) {
                    dist = d;
                    vert = VertexSamples[i] + MeshPosition;
                }
            }

            dist = Math.Sqrt(dist);
        }
        public bool IsAboveHorizon(Vector3d camera) {
            Vector3d planetToCam = Vector3d.Normalize(camera - Body.Position);
            double horizonAngle = Math.Acos(Body.Radius / (Body.Position - camera).Length());

            for (int i = 0; i < VertexSamples.Length; i++) {
                Vector3d planetToMesh = Vector3d.Normalize(VertexSamples[i] + MeshPosition - Body.Position);

                double meshAngle = Math.Acos(Vector3.Dot(planetToCam, planetToMesh));

                if (horizonAngle > meshAngle)
                    return true;
            }

            return false;
        }

        public void SetData(D3D11.Device device, D3D11.DeviceContext context) {
            if (indexdirty && vertexDirty)
                Debug.MarkFrame();

            if (indexdirty) {
                indexBuffer?.Dispose();
                indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies);
                indexCount = indicies.Length;

                if (hasWaterVerticies) {
                    // water indicies
                    if (waterVerticies != null) {
                        List<short> wi = new List<short>();
                        List<short> wf = new List<short>();
                        for (int i = 0; i < indicies.Length; i += 3) {
                            // add the triangles that are at least partially above the ground
                            if (waterVerticies[indicies[i]].Height >= 0
                                || waterVerticies[indicies[i + 1]].Height >= 0
                                || waterVerticies[indicies[i + 2]].Height >= 0) {
                                wi.Add(indicies[i]);
                                wi.Add(indicies[i + 1]);
                                wi.Add(indicies[i + 2]);
                            }
                            if (waterVerticies[indicies[i]].Height < 0
                                 || waterVerticies[indicies[i + 1]].Height < 0
                                 || waterVerticies[indicies[i + 2]].Height < 0) {
                                wf.Add(indicies[i]);
                                wf.Add(indicies[i + 1]);
                                wf.Add(indicies[i + 2]);
                            }
                        }
                        waterIndicies = wi.ToArray();
                        farIndicies = wf.ToArray();
                    }

                    if (farIndicies.Length > 0) {
                        farIndexBuffer?.Dispose();
                        farIndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, farIndicies);
                    }
                    if (waterIndicies.Length > 0) {
                        waterIndexBuffer?.Dispose();
                        waterIndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, waterIndicies);
                    }

                    waterIndexCount = waterIndicies.Length;
                }
                indexdirty = false;
            }

            if (vertexDirty) {
                vertexBuffer?.Dispose();
                vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);

                if (hasWaterVerticies) {
                    waterVertexBuffer?.Dispose();
                    waterVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, waterVerticies);
                }
                vertexDirty = false;
            }
        }

        #region Rendering
        public void GetVisible(ref List<QuadNode> list) {
            bool draw = true;

            if (Children != null) {
                draw = false;

                for (int i = 0; i < Children.Length; i++)
                    if (!Children[i].Ready)
                        draw = true;

                if (!draw)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].GetVisible(ref list);
            }

            if (draw)
                list.Add(this);
        }

        Vector3d scaledPos;
        double scaledScale;
        public void PrepareDraw(Renderer renderer) {
            if (vertexDirty || indexdirty)
                SetData(renderer.Device, renderer.Context);
            renderer.ActiveCamera.GetScaledSpace(MeshPosition, out scaledPos, out scaledScale);
            scaledScale *= Size;
        }
        public void Draw(Renderer renderer, Vector3d planetPos, double planetScale, double camHeight) {
            if (!Profiler.Resume("Node Data Set")) Profiler.Begin("Node Data Set");
            // early exits
            if (hasWaterVerticies && !hasVerticiesAboveWater && camHeight > oceanLevel + WaterDetailDistance) { Profiler.End(); return; }
            if (vertexBuffer == null || indexBuffer == null) { Profiler.End(); return; }
            if (!IsAboveHorizon(renderer.ActiveCamera.Position)) { Profiler.End(); return; }
            
            OOB.Transformation = Matrix.Scaling((float)scaledScale) * Body.Rotation * NodeOrientation * Matrix.Translation(scaledPos);
            if (!renderer.ActiveCamera.Intersects(OOB)) { Profiler.End(); return; }
            constants.World = Matrix.Scaling((float)scaledScale) * Body.Rotation * Matrix.Translation(scaledPos);
            constants.NodeToPlanetMatrix = Matrix.Scaling((float)(planetScale * Size)) * Body.Rotation * Matrix.Translation(planetPos + (MeshPosition - Body.Position) * planetScale);
            constants.WorldInverseTranspose = Matrix.Invert(Matrix.Transpose(constants.World));
            constants.NodeOrientation = Body.Rotation * NodeOrientation;
            constants.NodeScale = (float)scaledScale;
            constants.LightDirection = Vector3d.Normalize(MeshPosition - StarSystem.ActiveSystem.GetStar().Position);
                
            // constant buffer
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            else
                renderer.Context.UpdateSubresource(ref constants, constantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            Profiler.End();

            if (!Profiler.Resume("Node Draw")) Profiler.Begin("Node Draw");
            if (Debug.DrawBoundingBoxes) {
                Ray r = new Ray(Vector3.Zero, Input.MouseRayDirection);
                if (OOB.Intersects(ref r))
                    Debug.DrawBox(Color.White, OOB);
                else
                    Debug.DrawBox(Color.Red, OOB);
            }

            // only draw the triangles above the water if we're far away
            if (hasWaterVerticies && hasVerticiesAboveWater && camHeight > oceanLevel + WaterDetailDistance)
                renderer.Context.InputAssembler.SetIndexBuffer(farIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            else
                renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PlanetVertex>(), 0));

            foreach (Camera c in renderer.Cameras) {
                renderer.SetCamera(c);
                renderer.Context.DrawIndexed(indexCount, 0, 0);
            }
            Debug.TrianglesDrawn += indexCount / 3;
            Debug.NodesDrawn++;
            Profiler.End();
        }
        public void DrawWater(Renderer renderer, Vector3d planetPos, double planetScale, double camHeight) {
            if (!Profiler.Resume("Node Data Set")) Profiler.Begin("Node Data Set");
            // early exits
            if (!hasWaterVerticies) { Profiler.End(); return; }
            if (waterVertexBuffer == null || waterIndexBuffer == null) { Profiler.End(); return; }
            if (!IsAboveHorizon(renderer.ActiveCamera.Position)) { Profiler.End(); return; }
            
            // get scaled space
            double wscale;
            Vector3d wpos;
            renderer.ActiveCamera.GetScaledSpace(WaterMeshPosition, out wpos, out wscale);
            wscale *= Size;
            WaterOOB.Transformation = Matrix.Scaling((float)wscale) * Body.Rotation * NodeOrientation * Matrix.Translation(wpos);
            if (!renderer.ActiveCamera.Intersects(WaterOOB)) { Profiler.End(); return; }

            constants.World = Matrix.Scaling((float)wscale) * Body.Rotation * Matrix.Translation(wpos);
            constants.NodeToPlanetMatrix = Matrix.Scaling((float)(planetScale * Size)) * Body.Rotation * Matrix.Translation(planetPos + (WaterMeshPosition - Body.Position) * planetScale);
            constants.WorldInverseTranspose = Matrix.Invert(Matrix.Transpose(constants.World));
            constants.NodeOrientation = Body.Rotation * NodeOrientation;
            constants.NodeScale = (float)scaledScale;
            constants.LightDirection = Vector3d.Normalize(WaterMeshPosition - StarSystem.ActiveSystem.GetStar().Position);

            // constant buffer
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            else
                renderer.Context.UpdateSubresource(ref constants, constantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Profiler.End();

            if (!Profiler.Resume("Node Draw")) Profiler.Begin("Node Draw");
            if (Debug.DrawBoundingBoxes) Debug.DrawBox(Color.Blue, WaterOOB);
            waterConstants.Offset = Vector3.Zero;
            waterConstants.FadeDistance = (float)WaterDetailDistance;
            waterConstants.Time = (float)renderer.TotalTime;

            // water constant buffer
            if (waterConstantBuffer == null)
                waterConstantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref waterConstants);
            else
                renderer.Context.UpdateSubresource(ref waterConstants, waterConstantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(4, waterConstantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(4, waterConstantBuffer);

            renderer.Context.InputAssembler.SetIndexBuffer(waterIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(waterVertexBuffer, Utilities.SizeOf<WaterVertex>(), 0));

            foreach (Camera c in renderer.Cameras) {
                renderer.SetCamera(c);
                renderer.Context.DrawIndexed(waterIndexCount, 0, 0);
            }
            Debug.TrianglesDrawn += waterIndexCount / 3;
            Profiler.End();
        }

        public bool GetTreeNodes(Renderer renderer, ref List<QuadNode> trees, ref List<QuadNode> imposters) {
            bool tree = Trees != null;
            if (Children != null)
                for (int i = 0; i < Children.Length; i++)
                    if (Children[i].GetTreeNodes(renderer, ref trees, ref imposters))
                        tree = false;

            if (tree) {
                if (VertexSpacing < TreeLODLevel) {
                    if (Trees != null && Trees.Length > 0) {
                        double dist;
                        Vector3d v;
                        ClosestVertex(renderer.ActiveCamera.Position, out v, out dist);
                        if (dist < TreeDistance) {
                            if (dist < TreeImposterDistance)
                                trees.Add(this);
                            else
                                imposters.Add(this);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public void DrawTrees(Renderer renderer) {
            if (TreeBuffer == null || TreeBufferSize < Trees.Length) {
                float[] data = new float[Trees.Length * 16];
                for (int i = 0; i < Trees.Length; i++) {
                    data[i * 16 + 0] = Trees[i].M11;
                    data[i * 16 + 1] = Trees[i].M12;
                    data[i * 16 + 2] = Trees[i].M13;
                    data[i * 16 + 3] = Trees[i].M14;

                    data[i * 16 + 4] = Trees[i].M21;
                    data[i * 16 + 5] = Trees[i].M22;
                    data[i * 16 + 6] = Trees[i].M23;
                    data[i * 16 + 7] = Trees[i].M24;

                    data[i * 16 + 8] = Trees[i].M31;
                    data[i * 16 + 9] = Trees[i].M32;
                    data[i * 16 + 10] = Trees[i].M33;
                    data[i * 16 + 11] = Trees[i].M34;

                    data[i * 16 + 12] = Trees[i].M41;
                    data[i * 16 + 13] = Trees[i].M42;
                    data[i * 16 + 14] = Trees[i].M43;
                    data[i * 16 + 15] = Trees[i].M44;
                }
                TreeBufferSize = Trees.Length;
                TreeBuffer?.Dispose();
                TreeBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, data);
            }
            
            renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeBuffer, Matrix.SizeInBytes, 0));
            Resources.TreeModel.DrawInstanced(
                renderer,
                constants.LightDirection,
                Body.Rotation * Matrix.Translation(scaledPos),
                Trees.Length);

            Debug.TreesDrawn += Trees.Length;
        }
        public void DrawImposters(Renderer renderer) {
            Vector3d pos;
            double scale;
            renderer.ActiveCamera.GetScaledSpace(MeshPosition, out pos, out scale);
            Vector3 posf = pos;

            if (TreeImposterBuffer == null) {
                float[] data = new float[Trees.Length * 6];
                for (int i = 0; i < Trees.Length; i++) {
                    data[i * 6 + 0] = Trees[i].TranslationVector.X;
                    data[i * 6 + 1] = Trees[i].TranslationVector.Y;
                    data[i * 6 + 2] = Trees[i].TranslationVector.Z;
                    data[i * 6 + 3] = Trees[i].Up.X;
                    data[i * 6 + 4] = Trees[i].Up.Y;
                    data[i * 6 + 5] = Trees[i].Up.Z;
                }
                TreeImposterBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, data);
            }
            
            renderer.Context.InputAssembler.SetVertexBuffers(1, new D3D11.VertexBufferBinding(TreeImposterBuffer, sizeof(float) * 6, 0));

            float[] cbuffer = new float[24] {
                Body.Rotation.M11, Body.Rotation.M12, Body.Rotation.M13, Body.Rotation.M14,
                Body.Rotation.M21, Body.Rotation.M22, Body.Rotation.M23, Body.Rotation.M24,
                Body.Rotation.M31, Body.Rotation.M32, Body.Rotation.M33, Body.Rotation.M34,
                Body.Rotation.M41, Body.Rotation.M42, Body.Rotation.M43, Body.Rotation.M44,
                posf.X, posf.Y, posf.Z, 0,
                constants.LightDirection.X, constants.LightDirection.Y, constants.LightDirection.Z, 0
            };
            if (TreeImposerConstantBuffer == null)
                TreeImposerConstantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, cbuffer, 96);
            else
                renderer.Context.UpdateSubresource(cbuffer, TreeImposerConstantBuffer);
            renderer.Context.VertexShader.SetConstantBuffer(1, TreeImposerConstantBuffer);

            foreach (Camera c in renderer.Cameras) {
                renderer.SetCamera(c);
                renderer.Context.DrawIndexedInstanced(6, Trees.Length, 0, 0, 0);
            }
            Debug.TrianglesDrawn += Trees.Length * 2;
            Debug.ImposterDrawn += Trees.Length;
        }
        #endregion

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
            waterVertexBuffer?.Dispose();

            TreeBuffer?.Dispose();
            TreeImposterBuffer?.Dispose();
            TreeImposerConstantBuffer?.Dispose();

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
            [FieldOffset(192)]
            public Vector3 LightDirection;
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
            double d = (renderer.ActiveCamera.Position - (MeshCenter + Atmosphere.Planet.Position)).Length();

            constants.World = Matrix.Scaling((float)scale) * Matrix.Translation(pos);
            constants.WorldInverseTranspose = Matrix.Identity;
            constants.LightDirection = Vector3d.Normalize(Atmosphere.Planet.Position + MeshCenter - StarSystem.ActiveSystem.GetStar().Position);

            // constant buffer
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            else
                renderer.Context.UpdateSubresource(ref constants, constantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0));

            foreach (Camera c in renderer.Cameras) {
                if (c == renderer.ShadowCamera) continue;
                renderer.SetCamera(c);
                renderer.Context.DrawIndexed(indicies.Length, 0, 0);
            }

            Debug.TrianglesDrawn += IndexCount / 3;
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

                    renderer.ActiveCamera.GetScaledSpace(MeshCenter + Atmosphere.Planet.Position, out pos, out scale);

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