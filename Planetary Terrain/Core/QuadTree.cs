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

        public Vector3d Position; // position on cube
        public Vector3d RenderPosition; // center of mesh
        public Matrix Orientation;

        Vector3d[] vertexSamples;

        VertexNormalTexture[] verticies;
        short[] indicies;

        [StructLayout(LayoutKind.Explicit)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
            [FieldOffset(64)]
            public Matrix WorldInverseTranspose;
        }
        private Constants shaderConstants;

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;
        D3D11.Buffer constantBuffer;

        bool dirty = false;

        public QuadTree(Planet planet, double size, QuadTree parent, Vector3d pos, Matrix rot) {
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
            
            Vector3d apos = AbsolutePosition();

            Vector3d aposn = Vector3d.Normalize(apos);
            RenderPosition = aposn * Planet.GetHeight(aposn);

            float scale = (float)Size / GridSize;
            ThreadPool.QueueUserWorkItem(new WaitCallback((object o) => {
                int s = GridSize + 1;
                verticies = new VertexNormalTexture[s * s * 6];
                indicies = new short[s * s *6];

                Vector3 p1, p2, p3, n;
                Vector3d p1d, p2d, p3d;

                vertexSamples = new Vector3d[5];

                int i = 0;
                for (int x = 0; x < s; x++) {
                    for (int z = 0; z < s; z++) {
                        p1 = scale * (new Vector3(x, 0, z)     - new Vector3(GridSize * .5f, 0, GridSize * .5f));
                        p2 = scale * (new Vector3(x, 0, z + 1) - new Vector3(GridSize * .5f, 0, GridSize * .5f));
                        p3 = scale * (new Vector3(x + 1, 0, z) - new Vector3(GridSize * .5f, 0, GridSize * .5f));

                        p1d = Vector3.Transform(p1, Orientation).ToVector3().ToDouble();
                        p2d = Vector3.Transform(p2, Orientation).ToVector3().ToDouble();
                        p3d = Vector3.Transform(p3, Orientation).ToVector3().ToDouble();

                        p1d += apos;
                        p2d += apos;
                        p3d += apos;

                        p1d.Normalize();
                        p2d.Normalize();
                        p3d.Normalize();

                        Vector2 t = Planet.GetTemp(p1d);
                        
                        p1d *= Planet.GetHeight(p1d);
                        p2d *= Planet.GetHeight(p2d);
                        p3d *= Planet.GetHeight(p3d);

                        if (x == s/2 && z == s / 2)
                            vertexSamples[0] = p1d;
                        else if (x == 0 && z == 0)
                            vertexSamples[1] = p1d;
                        else if (x == s-1 && z == 0)
                            vertexSamples[2] = p1d;
                        else if (x == 0 && z == s-1)
                            vertexSamples[3] = p1d;
                        else if (x == s-1 && z == s-1)
                            vertexSamples[4] = p1d;

                        p1d -= RenderPosition;
                        p2d -= RenderPosition;
                        p3d -= RenderPosition;

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

        public Vector3d ClosestVertex(Vector3d pos) {
            Vector3d close = RenderPosition;
            double dist = double.MaxValue;
            for (int i = 0; i < vertexSamples.Length; i++) {
                double d = (pos - vertexSamples[i]).LengthSquared();
                if (d < dist) {
                    dist = d;
                    close = vertexSamples[i];
                }
            }

            return close;
        }

        public Vector3d AbsolutePosition() {
            if (Parent == null)
                return Position + Planet.Position;
            else
                return Position + Parent.AbsolutePosition();
        }
        public Vector3d SurfacePosition() {
            return Planet.GetPointOnSurface(AbsolutePosition());
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
            
            double s = Size * .5;

            //  | 0 | 1 |
            //  | 2 | 3 |
            
            Vector3d p0 = (Orientation.Left  + Orientation.Forward).ToDouble();
            Vector3d p1 = (Orientation.Right + Orientation.Forward).ToDouble();
            Vector3d p2 = (Orientation.Left  + Orientation.Backward).ToDouble();
            Vector3d p3 = (Orientation.Right + Orientation.Backward).ToDouble();

            Children = new QuadTree[4];
            Children[0] = new QuadTree(Planet, s, this, s * .5 * p0, Orientation);
            Children[1] = new QuadTree(Planet, s, this, s * .5 * p1, Orientation);
            Children[2] = new QuadTree(Planet, s, this, s * .5 * p2, Orientation);
            Children[3] = new QuadTree(Planet, s, this, s * .5 * p3, Orientation);
            
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
        public void SplitDynamic(Vector3d dir, double heightDelta, D3D11.Device device) {
            double d = Planet.ArcLength((dir * Planet.Radius - SurfacePosition()).Length());

            if (d < ArcSize && heightDelta < Size) {
                if (Children != null) {
                    for (int i = 0; i < Children.Length; i++)
                        Children[i].SplitDynamic(dir, heightDelta, device);
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

        bool IsAboveHorizon(Vector3d camera) {
            Vector3d planetToCam = Vector3d.Normalize(camera - Planet.Position);
            Vector3d planetToMesh = Vector3d.Normalize(ClosestVertex(camera));

            double horizonAngle = Math.Acos(Planet.Radius * .99 / (Planet.Position - camera).Length());
            double meshAngle = Math.Acos(Vector3.Dot(planetToCam, planetToMesh));

            return horizonAngle > meshAngle;
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
                    if (IsAboveHorizon(renderer.Camera.Position)) {
                        Vector3d pos;
                        double scale;
                        renderer.Camera.AdjustPositionRelative(RenderPosition, out pos, out scale);
                        Matrix world = //Matrix.Translation(RenderPosition - renderer.Camera.Position);
                            Matrix.Scaling((float)scale) * Matrix.Translation(pos);

                        world = Matrix.Transpose(world);
                        shaderConstants.World = world;
                        shaderConstants.WorldInverseTranspose = Matrix.Identity;// Matrix.Transpose(Matrix.Invert(world));
                        // dont have to set worldinversetranspose because the scale-transform matrix never effects the normals

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
