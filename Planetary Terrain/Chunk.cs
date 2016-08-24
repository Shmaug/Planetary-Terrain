using System;
using SharpDX;
using SharpDX.Direct3D;
using System.Threading;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace BetterTerrain {
    class Chunk : IDisposable {
        public const int GridSize = 32;
        public const float MinSize = .25f;

        public float Size;

        public Planet Planet;
        public Chunk Parent;
        public Chunk[] Children;

        VertexNormalTexture[] verticies;
        short[] indicies;

        [StructLayout(LayoutKind.Explicit)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
        }
        private Constants shaderConstants;

        public Matrix WorldMatrix;

        Vector3 centroid;

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;
        D3D11.Buffer constantBuffer;

        bool dirty = false;

        public Chunk(Planet planet, float size, Chunk parent, Matrix world) {
            Size = size;
            Planet = planet;
            Parent = parent;

            shaderConstants = new Constants();
            WorldMatrix = world;
        }

        public Vector3 ToPlanetSpace(Vector3 p) {
            return Vector3.Transform(p, WorldMatrix).ToVector3();
        }
        public Vector3 ToWorldSpace(Vector3 p) {
            return Planet.ToWorldSpace(Vector3.Transform(p, WorldMatrix).ToVector3());
        }
        public Vector3 PlanetToChunkSpace(Vector3 p) {
            return Vector3.Transform(p, Matrix.Invert(WorldMatrix)).ToVector3();
        }

        bool generating = false;
        public void Generate() {
            if (generating) return;
            generating = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback((object o) => {
                float scale = Size / GridSize;
                int s = GridSize + 1;
                verticies = new VertexNormalTexture[s * s * 6];
                indicies = new short[s * s *6];

                centroid = Vector3.Zero;

                int i = 0;
                for (int x = 0; x < s; x++) {
                    for (int z = 0; z < s; z++) {
                        #region vertex normals
                        Vector3 p1 = scale * (new Vector3(x, 0, z) -     new Vector3(GridSize, 0, GridSize) * .5f);
                        Vector3 p2 = scale * (new Vector3(x, 0, z + 1) - new Vector3(GridSize, 0, GridSize) * .5f);
                        Vector3 p3 = scale * (new Vector3(x + 1, 0, z) - new Vector3(GridSize, 0, GridSize) * .5f);

                        p1 = ToPlanetSpace(p1);
                        p2 = ToPlanetSpace(p2);
                        p3 = ToPlanetSpace(p3);

                        p1.Normalize(); p2.Normalize(); p3.Normalize();
                        
                        Vector2 temp = Planet.GetTemp(p1);

                        p1 *= Planet.GetHeight(p1);
                        p2 *= Planet.GetHeight(p2);
                        p3 *= Planet.GetHeight(p3);
                        
                        p1 = PlanetToChunkSpace(p1);
                        p2 = PlanetToChunkSpace(p2);
                        p3 = PlanetToChunkSpace(p3);

                        Vector3 n = Vector3.Normalize(Vector3.Cross(p2 - p1, p3 - p1));

                        verticies[x + z * s] = new VertexNormalTexture(p1, n, temp);
                        centroid += p1;

                        if (x + 1 < s && z + 1 < s) {
                            indicies[i++] = (short)((x+1) + (z) * s);
                            indicies[i++] = (short)((x) + (z) * s);
                            indicies[i++] = (short)((x) + (z+1) * s);
                            
                            indicies[i++] = (short)((x+1) + (z+1) * s);
                            indicies[i++] = (short)((x+1) + (z) * s);
                            indicies[i++] = (short)((x) + (z+1) * s);
                        }
                        #endregion
                    }
                }

                centroid /= verticies.Length;

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

            dirty = false;
        }

        public void Split(D3D11.Device device) {
            if (Children != null)
                return;
            
            float s = Size * .5f;
            float f = Size * .25f;

            //  | 0 | 1 |
            //  | 2 | 3 |

            Children = new Chunk[4];
            Children[0] = new Chunk(Planet, s, this, Matrix.Translation(-f, 0,  f) * WorldMatrix);
            Children[1] = new Chunk(Planet, s, this, Matrix.Translation( f, 0,  f) * WorldMatrix);
            Children[2] = new Chunk(Planet, s, this, Matrix.Translation(-f, 0, -f) * WorldMatrix);
            Children[3] = new Chunk(Planet, s, this, Matrix.Translation( f, 0, -f) * WorldMatrix);
            
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
        public void SplitDynamic(Vector3 camPos, D3D11.Device device) {
            if (Parent == null) Split(device); // shitty way of making sure top-level chunks stay split

            float d = (camPos - ToWorldSpace(centroid)).Length();

            bool shouldSplit = d < Size * 3f;

            if (Children == null) {
                if (shouldSplit && Size * .5f >= MinSize)
                    Split(device); // split if close and no children
            } else {
                if (shouldSplit)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(camPos, device); // split children if close and children != null
                else
                    UnSplit(); // unsplit if far and children != null
            }
        }

        public bool Ready() {
            return dirty || vertexBuffer != null;
        }
        
        public void Draw(Renderer renderer) {
            bool draw = true;

            if (Children != null) {
                draw = false;

                for (int i = 0; i < Children.Length; i++)
                    if (!Children[i].Ready())
                        draw = true;

                if (!draw)
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].Draw(renderer);
            }

            if (draw) {
                if (dirty)
                    SetData(renderer.Device, renderer.Context);

                if (vertexBuffer != null) {
                    shaderConstants.World = Matrix.Transpose(WorldMatrix);
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
            }
        }

        public void Dispose() {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            
            if (constantBuffer != null)
                constantBuffer.Dispose();

            if (Children != null)
                for (int i = 0; i < Children.Length; i++)
                    Children[i].Dispose();
        }
    }
}
