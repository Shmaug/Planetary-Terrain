using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using Assimp;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    static class AssimpHelper {
        public static Vector3 ToVector3(this Vector3D v) {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static Matrix ToMatrix(this Assimp.Matrix4x4 v) {
            return Matrix.Transpose(new Matrix(
                v.A1, v.A2, v.A3, v.A4,
                v.B1, v.B2, v.B3, v.B4,
                v.C1, v.C2, v.C3, v.C4,
                v.D1, v.D2, v.D3, v.D4
                ));
        }
    }
    class Model {
        public Vector3d Position;
        public SharpDX.Quaternion Orientation;
        public Vector3 Scale;

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 144)]
        struct Constants {
            public Matrix World;
            public Matrix WorldInverseTranspose;
            public Vector3 lightDirection;
        }
        Constants constants;
        D3D11.Buffer cbuffer;

        public List<ModelMesh> Meshes;
        string modelPath;

        public Model(string file, D3D11.Device device) {
            Scale = Vector3.One;
            Orientation = SharpDX.Quaternion.Identity;

            AssimpContext ctx = new AssimpContext();
            if (!ctx.IsImportFormatSupported(Path.GetExtension(file)))
                return;

            modelPath = Path.GetDirectoryName(file);

            Scene scene = ctx.ImportFile(file);
            Node node = scene.RootNode;
            Matrix mat = Matrix.Identity;

            Meshes = new List<ModelMesh>();
            AddNode(scene, scene.RootNode, device, mat);
        }

        public void AddNode(Scene scene, Node node, D3D11.Device device, Matrix transform) {
            transform = transform * node.Transform.ToMatrix();

            Matrix invTranspose = Matrix.Transpose(Matrix.Invert(transform));
            if (node.HasMeshes) {
                foreach (int index in node.MeshIndices) {
                    Mesh mesh = scene.Meshes[index];

                    ModelMesh mm = new ModelMesh();
                    Meshes.Add(mm);

                    Material mat = scene.Materials[mesh.MaterialIndex];
                    if (mat != null && mat.GetMaterialTextureCount(TextureType.Diffuse) > 0) {
                        mm.DiffuseTexture = ResourceUtil.LoadTexture(device, modelPath + @"\" + mat.TextureDiffuse.FilePath);
                        mm.DiffuseSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                            AddressU = D3D11.TextureAddressMode.Clamp,
                            AddressV = D3D11.TextureAddressMode.Clamp,
                            AddressW = D3D11.TextureAddressMode.Clamp,
                            Filter = D3D11.Filter.Anisotropic,
                        });
                        mm.DiffuseTextureView = new D3D11.ShaderResourceView(device, mm.DiffuseTexture);
                    }

                    //bool hasTexCoords = mesh.HasTextureCoords(0);
                    //bool hasColors = mesh.HasVertexColors(0);
                    //bool hasNormals = mesh.HasNormals;

                    //int ec = 1;
                    //if (hasTexCoords) ec++;
                    //if (hasColors) ec++;
                    //if (hasNormals) ec++;

                    //mm.InputElements = new D3D11.InputElement[ec];
                    //int e = 0;
                    //mm.InputElements[e++] = new D3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0);
                    //
                    //if (hasNormals) {
                    //    mm.InputElements[e++] = new D3D11.InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, mm.VertexSize, 0);
                    //    mm.VertexSize += Utilities.SizeOf<Vector3>();
                    //}
                    //if (hasColors) {
                    //    mm.InputElements[e++] = new D3D11.InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, mm.VertexSize, 0);
                    //    mm.VertexSize += Utilities.SizeOf<Vector4>();
                    //}
                    //if (hasTexCoords) {
                    //    mm.InputElements[e++] = new D3D11.InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32B32_Float, mm.VertexSize, 0);
                    //    mm.VertexSize += Utilities.SizeOf<Vector3>();
                    //}
                    
                    Vector3D[] verts = mesh.Vertices.ToArray();
                    Vector3D[] texCoords = mesh.TextureCoordinateChannels[0].ToArray();
                    Vector3D[] normals = mesh.Normals.ToArray();
                    //Color4D[] colors = mesh.VertexColorChannels[0].ToArray();
                    
                    switch (mesh.PrimitiveType) {
                        case PrimitiveType.Point:
                            mm.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PointList;
                            break;
                        case PrimitiveType.Line:
                            mm.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;
                            break;
                        case PrimitiveType.Triangle:
                            mm.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                            break;
                        default:
                            break;
                    }

                    VertexNormalTexture[] verticies = new VertexNormalTexture[mesh.VertexCount];
                    for (int i = 0; i < mesh.VertexCount; i++) {
                        verticies[i] = new VertexNormalTexture(
                            (Vector3)Vector3.Transform(new Vector3(verts[i].X, verts[i].Y, verts[i].Z), transform),
                            (Vector3)Vector3.Transform(new Vector3(normals[i].X, normals[i].Y, normals[i].Z), invTranspose),
                            new Vector2(texCoords[i].X, 1f - texCoords[i].Y));
                        // TODO: actually do this right
                    }

                    mm.VertexSize = Utilities.SizeOf<VertexNormalTexture>();

                    mm.VertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);
                    mm.VertexCount = mesh.VertexCount;

                    List<short> indicies = new List<short>();
                    foreach (Face f in mesh.Faces)
                        if (f.HasIndices)
                            for (int i = 2; i < f.Indices.Count; i++) {
                                indicies.Add((short)f.Indices[0]);
                                indicies.Add((short)f.Indices[i - 1]);
                                indicies.Add((short)f.Indices[i]);
                            }
                    mm.IndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies.ToArray());
                    mm.IndexCount = indicies.Count;
                }
            }

            foreach (Node c in node.Children)
                AddNode(scene, c, device, transform);
        }


        public void Draw(Renderer renderer, Vector3d sunPosition, Matrix world) {
            constants.World = world;
            constants.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(world));
            constants.lightDirection = Vector3d.Normalize(Position - sunPosition);

            // create/update constant buffer
            if (cbuffer == null)
                cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, cbuffer);

            Shaders.ModelShader.Set(renderer);

            renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

            foreach (ModelMesh m in Meshes)
                m.Draw(renderer);
        }

        public void Dispose() {
            if (Meshes != null)
                foreach (ModelMesh m in Meshes)
                    m.Dispose();
            cbuffer?.Dispose();
        }
    }
}
