using System;
using SharpDX;
using SharpDX.Direct3D;
using System.Threading;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class QuadTree : IDisposable {
        public const int GridSize = 64;

        public double Size;
        public double ArcSize;

        public Planet Planet;
        public QuadTree Parent;
        public QuadTree[] Children;

        /// <summary>
        /// The position on the cube, before being projected into a sphere
        /// </summary>
        public Vector3d Position;

        /// <summary>
        /// The position of the mesh of which it is drawn at, relative to the planet
        /// </summary>
        public Vector3d MeshCenter;

        public Matrix3x3 Orientation;

        int[] vertexSamples;

        VertexNormalTexture[] verticies;
        short[] indicies;

        public int IndexCount { get; private set; }
        public int VertexCount { get; private set; }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 128)]
        struct Constants {
            public Matrix World;
            public Matrix WorldInverseTranspose;
        }
        private Constants shaderConstants;

        public D3D11.Buffer vertexBuffer { get; private set; }
        public D3D11.Buffer indexBuffer { get; private set; }
        public D3D11.Buffer constantBuffer { get; private set; }

        bool dirty = false;

        public QuadTree(Planet planet, double size, QuadTree parent, Vector3d pos, Matrix3x3 rot) {
            Size = size;
            Planet = planet;
            Parent = parent;
            ArcSize = Planet.ArcLength(Size);

            Position = pos;
            Orientation = rot;

            shaderConstants = new Constants();
            shaderConstants.World = Matrix.Identity;
        }

        bool generating = false;
        public void Generate() {
            if (generating) return;
            generating = true;
            
            Vector3d posn = Vector3d.Normalize(Position);
            MeshCenter = posn * Planet.GetHeight(posn);

            float scale = (float)Size / GridSize;
            ThreadPool.QueueUserWorkItem(new WaitCallback((object o) => {
                int s = GridSize + 1;
                verticies = new VertexNormalTexture[s * s * 6];
                indicies = new short[s * s *6];

                Vector3 p1, p2, p3, n;
                Vector3d p1d, p2d, p3d;

                int v = s - 1;
                vertexSamples = new int[] {
                    // x*s + v
                    0 * s + 0,       // 0, 0
                    v * s + 0,       // 1, 0
                    0 * s + v,       // 0, 1
                    v * s + v,       // 1, 1
                    (v/2) * s + 0,   // .5, 0
                    0 * s + v/2,     // 0, .5
                    (v/2) * s + v,   // .5, 1
                    v * s + v/2,     // 1, .5
                    (v/2) * s + v/2, // .5, .5
                };

                int i = 0;
                for (int x = 0; x < s; x++) {
                    for (int z = 0; z < s; z++) {
                        p1 = scale * (new Vector3(x, 0, z)     - new Vector3(GridSize * .5f, 0, GridSize * .5f));
                        p2 = scale * (new Vector3(x, 0, z + 1) - new Vector3(GridSize * .5f, 0, GridSize * .5f));
                        p3 = scale * (new Vector3(x + 1, 0, z) - new Vector3(GridSize * .5f, 0, GridSize * .5f));

                        p1d = Vector3.Transform(p1, Orientation);
                        p2d = Vector3.Transform(p2, Orientation);
                        p3d = Vector3.Transform(p3, Orientation);

                        p1d += Position;
                        p2d += Position;
                        p3d += Position;

                        p1d.Normalize();
                        p2d.Normalize();
                        p3d.Normalize();

                        Vector2 t = Planet.GetTemp(p1d);
                        
                        p1d *= Planet.GetHeight(p1d);
                        p2d *= Planet.GetHeight(p2d);
                        p3d *= Planet.GetHeight(p3d);

                        p1d -= MeshCenter;
                        p2d -= MeshCenter;
                        p3d -= MeshCenter;

                        n = Vector3.Cross(Vector3d.Normalize(p2d - p1d), Vector3d.Normalize(p3d - p1d));

                        verticies[x * s + z] = new VertexNormalTexture(p1d, n, t);

                        if (x + 1 < s && z + 1 < s) {
                            indicies[i++] = (short)((x + 1) * s + z);
                            indicies[i++] = (short)(x * s + z);
                            indicies[i++] = (short)(x * s + z + 1);

                            indicies[i++] = (short)((x + 1) * s + z + 1);
                            indicies[i++] = (short)((x + 1) * s + z);
                            indicies[i++] = (short)(x * s + z + 1);
                        }
                    }
                }
                
                generating = false;
                dirty = true;
            }));
        }
        
        public void SetData(D3D11.Device device, D3D11.DeviceContext context) {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);

            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies);

            VertexCount = verticies.Length;
            IndexCount = indicies.Length;

            dirty = false;
        }

        public Vector3d ClosestVertex(Vector3d pos) {
            if (vertexSamples == null) return MeshCenter + Planet.Position;
            pos -= MeshCenter + Planet.Position;

            int close = 1;
            double dist = double.MaxValue;
            for (int i = 0; i < vertexSamples.Length; i++) {
                double d = (pos - verticies[vertexSamples[i]].Position).LengthSquared();
                if (d < dist) {
                    dist = d;
                    close = vertexSamples[i];
                }
            }

            return (Vector3d)verticies[close].Position + MeshCenter + Planet.Position;
        }

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;
            
            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Vector3d right = Vector3.Transform(Vector3.Right, Orientation);
            Vector3d fwd = Vector3.Transform(Vector3.ForwardLH, Orientation);

            Vector3d p0 = (-right +  fwd);
            Vector3d p1 = ( right +  fwd);
            Vector3d p2 = (-right + -fwd);
            Vector3d p3 = ( right + -fwd);

            Children = new QuadTree[4];
            Children[0] = new QuadTree(Planet, s, this, Position + s * .5 * p0, Orientation);
            Children[1] = new QuadTree(Planet, s, this, Position + s * .5 * p1, Orientation);
            Children[2] = new QuadTree(Planet, s, this, Position + s * .5 * p2, Orientation);
            Children[3] = new QuadTree(Planet, s, this, Position + s * .5 * p3, Orientation);
            
            Children[0].Generate();
            Children[1].Generate();
            Children[2].Generate();
            Children[3].Generate();
        }
        public void UnSplit() {
            if (Children == null) return;

            for (int i = 0; i < Children.Length; i++)
                if (Children[i] != null)
                    Children[i].Dispose();
            
            Children = null;
        }
        public void SplitDynamic(Vector3d pos, D3D11.Device device) {
            double d = (ClosestVertex(pos) - pos).LengthSquared();

            if (d < Size * Size) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(pos, device);
                } else {
                    if (Size * .5f >= Planet.MinChunkSize)
                        Split(device);
                }
            } else
                UnSplit();

        }

        public bool Ready() {
            return dirty || vertexBuffer != null;
        }

        public bool IsAboveHorizon(Vector3d camera) {
            return true;
            Vector3d planetToCam = Vector3d.Normalize(camera - Planet.Position);
            Vector3d planetToMesh = Vector3d.Normalize(ClosestVertex(camera) - Planet.Position);

            double horizonAngle = Math.Acos(Planet.Radius * .99 / (Planet.Position - camera).Length());
            double meshAngle = Math.Acos(Vector3.Dot(planetToCam, planetToMesh));

            return horizonAngle > meshAngle;
        }

        public void Draw(Renderer renderer) {
            if (constantBuffer == null)
                constantBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref shaderConstants);
            renderer.Context.UpdateSubresource(ref shaderConstants, constantBuffer);

            renderer.Context.VertexShader.SetConstantBuffer(1, constantBuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, constantBuffer);

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexNormalTexture>(), 0));
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);

            renderer.Context.DrawIndexed(indicies.Length, 0, 0);
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
                    if (IsAboveHorizon(renderer.Camera.Position)) {
                        
                        Matrix world =
                            Matrix.Scaling((float)planetScale) *
                            Matrix.Translation(MeshCenter * planetScale + planetPos);
                        
                        shaderConstants.World = Matrix.Transpose(world);
                        shaderConstants.WorldInverseTranspose = Matrix.Identity;
                        Draw(renderer);

                        Debug.ChunksDrawn++;
                    }
                }
            }
        }

        public void Dispose() {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            
            if (constantBuffer != null)
                constantBuffer.Dispose();

            if (indexBuffer != null)
                indexBuffer.Dispose();

            if (Children != null)
                for (int i = 0; i < Children.Length; i++)
                    Children[i].Dispose();
        }
    }
}
