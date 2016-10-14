using System;
using SharpDX;
using SharpDX.Direct3D;
using System.Threading;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Planetary_Terrain {
    static class TriangleCache {
        static readonly int GridSize = QuadNode.GridSize;

        public static short[][] IndexCache;

        static short[] MakeIndicies(int index) {
            bool fanLeft  = (index & 1) >= 1;
            bool fanUp    = (index & 2) >= 1;
            bool fanRight = (index & 4) >= 1;
            bool fanDown  = (index & 8) >= 1;

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
        const double waterDetailThreshold = 5000;

        public double Size;
        public double ArcSize;
        public double VertexSpacing; // meters per vertex

        public Body Body;
        public QuadNode Parent;
        public int SiblingIndex;
        public QuadNode[] Children;

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

        PlanetVertex[] verticies;
        VertexNormal[] waterVerticies;
        PlanetVertex[] waterFarVerticies;
        short[] indicies;

        bool hasWaterVerticies;

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
            public bool drawWaterFar;
        }
        private Constants constants;

        public D3D11.Buffer vertexBuffer { get; private set; }
        public D3D11.Buffer waterVertexBuffer { get; private set; }
        public D3D11.Buffer waterFarVertexBuffer { get; private set; }
        public D3D11.Buffer indexBuffer { get; private set; }
        public D3D11.Buffer constantBuffer { get; private set; }

        bool dirty = false;
        bool generating = false;
        public bool Ready { get { return dirty || vertexBuffer != null; } }

        public QuadNode(Body body, int siblingIndex, double size, int lod, QuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
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
            MeshCenter *= Body.GetHeight(MeshCenter);

            SetupMesh();

            hasWaterVerticies = Body is Planet && ((Planet)Body).HasOcean;
        }
        void SetupMesh() {
            VertexSamples = new Vector3d[9];
            int i = 0;

            double scale = Size / GridSize;
            
            Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

            for (int x = 0; x <= GridSize; x+=GridSize/2) {
                for (int z = 0; z <= GridSize; z+=GridSize/2) {
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

            ThreadPool.QueueUserWorkItem(new WaitCallback((object _ctx) => {
                double scale = Size / GridSize;
                double invScale = 1d / Size;

                int s = GridSize + 1;
                verticies = new PlanetVertex[s * s * 6];
                List<short> inds = new List<short>();

                double oceanLevel = 0;
                if (hasWaterVerticies) {
                    oceanLevel = Body.Radius + ((Planet)Body).TerrainHeight * ((Planet)Body).OceanScaleHeight;
                    waterVerticies = new VertexNormal[s * s * 6];
                    waterFarVerticies = new PlanetVertex[s * s * 6];
                }

                Vector2 t;
                Vector3d p1d, p2d, p3d;
                Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);
                double h;
                double rh;
                bool wv = false;

                for (int x = 0; x < s; x++) {
                    for (int z = 0; z < s; z++) {
                        if (!generating)
                            break;

                        p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                        p2d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z + 1) - offset), Orientation));
                        p3d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x + 1, 0, z) - offset), Orientation));

                        Body.GetSurfaceInfo(p1d, out t, out h);

                        if (hasWaterVerticies) {
                            waterVerticies[x * s + z] = new VertexNormal((p1d * oceanLevel - MeshCenter) * invScale, p1d);

                            waterFarVerticies[x * s + z] =
                                new PlanetVertex(
                                    waterVerticies[x * s + z].Position,
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
                                t, (float)h);
                        
                        if (hasWaterVerticies && rh > oceanLevel)
                            waterFarVerticies[x * s + z] = verticies[x * s + z];
                        else
                            wv = true;
                    }

                    if (!generating)
                        break;
                }
                if (!generating) { // generation cancelled due to split
                    dirty = false;
                    verticies = null;
                    indicies = null;
                    waterVerticies = null;
                    waterFarVerticies = null;
                    vertexBuffer?.Dispose();
                    vertexBuffer = null;
                    indexBuffer?.Dispose();
                    indexBuffer = null;
                    waterVertexBuffer?.Dispose();
                    waterVertexBuffer = null;
                    waterFarVertexBuffer?.Dispose();
                    waterFarVertexBuffer = null;
                    return;
                }

                if (!wv) { // no water verticies found
                    hasWaterVerticies = false;
                    waterVerticies = null;
                    waterFarVerticies = null;
                    waterFarVertexBuffer?.Dispose();
                    waterFarVertexBuffer = null;
                }

                GenerateIndicies();

                generating = false;
                dirty = true;
            }));
        }
        int j = 0;
        public void GenerateIndicies() {
            Vector4 edgeColor = new Vector4[] {
                new Vector4(0, 1, 1, 1),
                new Vector4(1, 0, 1, 1),
            }[j % 2];
            Vector4 noEdgeColor = new Vector4[] {
                new Vector4(1, 0, 0, 1),
                new Vector4(0, 0, 0, 1),
            }[j % 2];
            j++;

            QuadNode l = GetLeft();
            QuadNode r = GetRight();
            QuadNode u = GetUp();
            QuadNode d = GetDown();

            bool fanLeft  = l != null && l.LoDLevel < LoDLevel;
            bool fanUp    = u != null && u.LoDLevel < LoDLevel;
            bool fanRight = r != null && r.LoDLevel < LoDLevel;
            bool fanDown  = d != null && d.LoDLevel < LoDLevel;

            int s = GridSize + 1;
            if (l == null) {
                int x = 0;
                for (int z = 0; z < s; z++)
                    verticies[x * s + z].Color = noEdgeColor;
            }else {
                int x = 0;
                for (int z = 0; z < s; z++)
                    verticies[x * s + z].Color = edgeColor;
            }
            if (u == null) {
                int z = s-1;
                for (int x = 0; x < s; x++)
                    verticies[x * s + z].Color = noEdgeColor;
            } else {
                int z = s-1;
                for (int x = 0; x < s; x++)
                    verticies[x * s + z].Color = edgeColor;
            }
            if (r == null) {
                int x = s-1;
                for (int z = 0; z < s; z++)
                    verticies[x * s + z].Color = noEdgeColor;
            } else {
                int x = s-1;
                for (int z = 0; z < s; z++)
                    verticies[x * s + z].Color = edgeColor;
            }
            if (d == null) {
                int z = 0;
                for (int x = 0; x < s; x++)
                    verticies[x * s + z].Color = noEdgeColor;
            } else {
                int z = 0;
                for (int x = 0; x < s; x++)
                    verticies[x * s + z].Color = edgeColor;
            }

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

        void UpdateNeighbors() {
            QuadNode r = GetRight();
            QuadNode d = GetDown();
            QuadNode l = GetLeft();
            QuadNode u = GetUp();

            if (r != null && r.verticies != null && !r.generating)
                r.GenerateIndicies();
            if (l != null && l.verticies != null && !l.generating)
                l.GenerateIndicies();
            if (d != null && d.verticies != null && !d.generating)
                d.GenerateIndicies();
            if (u != null && u.verticies != null && !u.generating)
                u.GenerateIndicies();
        }

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;

            // TODO: stop generating if we get split
            // BUT then the children won't draw
            //if (generating) {
            //    generating = false;
            //    dirty = false;
            //}
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

            UpdateNeighbors();
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                Children[i]?.Dispose();

            Children = null;

            if (vertexBuffer == null && !generating)
                Generate();

            GenerateIndicies();

            UpdateNeighbors();
        }
        public void SplitDynamic(Vector3d pos, D3D11.Device device) {
            double dist;
            Vector3d vert;
            ClosestVertex(pos, out vert, out dist);

            if (dist < Size || Size / GridSize > Body.MaxVertexSpacing) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(pos, device);
                } else {
                    if ((Size * .5f) / GridSize > Body.MinVertexSpacing)
                        Split(device);
                }
            } else
                UnSplit();
        }
        
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
            vertexBuffer?.Dispose();
            vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);

            if (hasWaterVerticies) {
                waterVertexBuffer?.Dispose();
                waterVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, waterVerticies);
                waterFarVertexBuffer?.Dispose();
                waterFarVertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, waterFarVerticies);
            }

            indexBuffer?.Dispose();
            indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies);

            VertexCount = verticies.Length;
            IndexCount = indicies.Length;

            dirty = false;
        }
        public void Draw(Renderer renderer, bool waterPass, double scale, Vector3d pos) {
            double d = (renderer.Camera.Position - (MeshCenter + Body.Position)).Length();

            constants.World = Matrix.Scaling((float)scale) * Matrix.Translation(pos);
            constants.WorldInverseTranspose = Matrix.Identity;
            constants.drawWaterFar = !waterPass && hasWaterVerticies && d > waterDetailThreshold;

            // constant buffer
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constantBuffer);
            
            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);
            
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            if (waterPass) {
                // when teh camera is close, draw waterVertexBuffer
                if (hasWaterVerticies && d < waterDetailThreshold) { // lod
                    renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(waterVertexBuffer, Utilities.SizeOf<VertexNormal>(), 0));
                    renderer.Context.DrawIndexed(indicies.Length, 0, 0);

                    Debug.VerticiesDrawn += VertexCount;
                }
            } else {
                // when the camera is far away, draw waterFarVertexBuffer and tell the shader to draw the water verticies in blue
                if (hasWaterVerticies && d > waterDetailThreshold) // water lod
                    renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(waterFarVertexBuffer, Utilities.SizeOf<PlanetVertex>(), 0));
                else
                    renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PlanetVertex>(), 0));

                renderer.Context.DrawIndexed(indicies.Length, 0, 0);
                
                Debug.VerticiesDrawn += VertexCount;
            }

            if (d < Debug.ClosestQuadTreeDistance) {
                Debug.ClosestQuadTree = this;
                Debug.ClosestQuadTreeDistance = d;
                Debug.ClosestQuadTreeScale = scale;
            }
        }
        public void Draw(Renderer renderer, bool waterPass, Vector3d planetPos, double planetScale) {
            bool draw = true;

            if (Children != null) {
                draw = false;

                for (int i = 0; i < Children.Length; i++)
                    if (!Children[i].Ready)
                        draw = true;

                if (!draw)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].Draw(renderer, waterPass, planetPos, planetScale);
            }

            if (draw) {
                if (dirty)
                    SetData(renderer.Device, renderer.Context);

                if (vertexBuffer != null) {
                    if (IsAboveHorizon(renderer.Camera.Position)) {
                        double scale = planetScale;
                        Vector3d pos = planetPos + MeshCenter * planetScale;

                        constants.NodeToPlanetMatrix = Matrix.Scaling((float)(scale * Size)) * Matrix.Translation(pos);

                        renderer.Camera.GetScaledSpace(MeshCenter + Body.Position, out pos, out scale);

                        scale *= Size;

                        Draw(renderer, waterPass, scale, pos);
                    }
                }
            }
        }

        public void Dispose() {
            if (generating) {
                generating = false;
                dirty = false;
            }

            vertexBuffer?.Dispose();
            constantBuffer?.Dispose();
            indexBuffer?.Dispose();
            waterFarVertexBuffer?.Dispose();
            waterVertexBuffer?.Dispose();

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

            // TODO: stop generating if we get split
            // BUT then the children won't draw
            //if (generating) {
            //    generating = false;
            //    dirty = false;
            //}
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