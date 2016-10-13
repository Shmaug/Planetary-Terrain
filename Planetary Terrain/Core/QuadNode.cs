using System;
using SharpDX;
using SharpDX.Direct3D;
using System.Threading;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Planetary_Terrain {
    class QuadNode : IDisposable {
        public const int GridSize = 16;
        const double waterDetailThreshold = 2500;

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
        
        public QuadNode(Body body, int siblingIndex, double size, QuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
            Size = size;
            Body = body;
            Parent = parent;
            ArcSize = Body.ArcLength(Size);

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
                        p1d = p1d * rh                  - MeshCenter;
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

        public void GenerateIndicies() {
            List<short> inds = new List<short>();
            int s = GridSize + 1;
            for (int x = 0; x < s; x++) {
                for (int z = 0; z < s; z++) {
                    if (x + 1 < s && z + 1 < s) {
                        // TODO: Quad fanning to handle cracks
                        inds.Add((short)((x + 1) * s + z));
                        inds.Add((short)(x * s + z));
                        inds.Add((short)(x * s + z + 1));

                        inds.Add((short)((x + 1) * s + z + 1));
                        inds.Add((short)((x + 1) * s + z));
                        inds.Add((short)(x * s + z + 1));
                    }
                }
            }
            indicies = inds.ToArray();
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

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;

            //if (generating) {
            //    generating = false;
            //    dirty = false;
            //}
            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Vector3d right = Vector3.Transform(Vector3.Right, Orientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, Orientation);

            Vector3d p0 = (-right + fwd);
            Vector3d p1 = (right + fwd);
            Vector3d p2 = (-right + -fwd);
            Vector3d p3 = (right + -fwd);

            Children = new QuadNode[4];
            Children[0] = new QuadNode(Body, 0, s, this, CubePosition + s * .5 * p0, Orientation);
            Children[1] = new QuadNode(Body, 1, s, this, CubePosition + s * .5 * p1, Orientation);
            Children[2] = new QuadNode(Body, 2, s, this, CubePosition + s * .5 * p2, Orientation);
            Children[3] = new QuadNode(Body, 3, s, this, CubePosition + s * .5 * p3, Orientation);

            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                Children[i]?.Dispose();

            Children = null;

            if (vertexBuffer == null && !generating)
                Generate();
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

        public bool Ready() {
            return dirty || vertexBuffer != null;
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
                    if (!Children[i].Ready())
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
        bool generating = false;

        public AtmosphereQuadNode(Atmosphere atmo, int siblingIndex, double size, AtmosphereQuadNode parent, Vector3d cubePos, Matrix3x3 rot) {
            SiblingIndex = siblingIndex;
            Size = size;
            Parent = parent;
            Atmosphere = atmo;

            VertexSpacing = Size / GridSize;

            CubePosition = cubePos;
            Orientation = rot;

            constants = new Constants();
            constants.World = Matrix.Identity;

            MeshCenter = Vector3d.Normalize(CubePosition);
            MeshCenter *= atmo.Radius;

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
            if (generating) return;
            generating = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object _ctx) => {
                double scale = Size / GridSize;
                double invScale = 1d / Size;

                int s = GridSize + 1;
                verticies = new Vector3[s * s * 6];
                List<short> inds = new List<short>();
                
                Vector3d p1d;
                Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

                for (int x = 0; x < s; x++) {
                    for (int z = 0; z < s; z++) {
                        if (!generating)
                            break;

                        p1d = Vector3d.Normalize(CubePosition + Vector3d.Transform(scale * (new Vector3d(x, 0, z) - offset), Orientation));
                        
                        p1d = p1d * Atmosphere.Radius - MeshCenter;

                        verticies[x * s + z] = p1d * invScale;
                    }

                    if (!generating)
                        break;
                }
                
                if (!generating) { // generation cancelled due to split
                    dirty = false;
                    verticies = null;
                    indicies = null;
                    vertexBuffer?.Dispose();
                    vertexBuffer = null;
                    indexBuffer?.Dispose();
                    indexBuffer = null;
                    return;
                }
                
                GenerateIndicies();
                
                generating = false;
                dirty = true;
            }));
        }

        public void GenerateIndicies() {
            List<short> inds = new List<short>();
            int s = GridSize + 1;
            for (int x = 0; x < s; x++) {
                for (int z = 0; z < s; z++) {
                    if (x + 1 < s && z + 1 < s) {
                        // TODO: Quad fanning to handle cracks
                        inds.Add((short)((x + 1) * s + z));
                        inds.Add((short)(x * s + z));
                        inds.Add((short)(x * s + z + 1));

                        inds.Add((short)((x + 1) * s + z + 1));
                        inds.Add((short)((x + 1) * s + z));
                        inds.Add((short)(x * s + z + 1));
                    }
                }
            }
            indicies = inds.ToArray();
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

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;

            //if (generating) {
            //    generating = false;
            //    dirty = false;
            //}
            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Vector3d right = Vector3.Transform(Vector3.Right, Orientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, Orientation);

            Vector3d p0 = (-right + fwd);
            Vector3d p1 = (right + fwd);
            Vector3d p2 = (-right + -fwd);
            Vector3d p3 = (right + -fwd);

            Children = new AtmosphereQuadNode[4];
            Children[0] = new AtmosphereQuadNode(Atmosphere, 0, s, this, CubePosition + s * .5 * p0, Orientation);
            Children[1] = new AtmosphereQuadNode(Atmosphere, 1, s, this, CubePosition + s * .5 * p1, Orientation);
            Children[2] = new AtmosphereQuadNode(Atmosphere, 2, s, this, CubePosition + s * .5 * p2, Orientation);
            Children[3] = new AtmosphereQuadNode(Atmosphere, 3, s, this, CubePosition + s * .5 * p3, Orientation);

            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                Children[i]?.Dispose();

            Children = null;

            if (vertexBuffer == null && !generating)
                Generate();
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

        public bool Ready() {
            return dirty || vertexBuffer != null;
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
                    if (!Children[i].Ready())
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