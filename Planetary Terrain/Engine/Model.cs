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
        public static Matrix4x4 ToMatrix(this Matrix m) {
            m = Matrix.Transpose(m);
            return new Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
                );
        }
        public static Color ToColor(this Color4D c) {
            return new Color(c.R, c.B, c.G, c.A);
        }
    }
    class Model {
        [StructLayout(LayoutKind.Explicit, Size = 176)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix World;
            [FieldOffset(64)]
            public Matrix WorldInverseTranspose;
            
            [FieldOffset(128)]
            public Vector3 lightDirection;
            
            [FieldOffset(144)]
            public Vector3 SpecularColor;

            [FieldOffset(156)]
            public float Shininess;
            [FieldOffset(160)]
            public float ShininessIntensity;
            [FieldOffset(164)]
            public float EmissiveIntensity;
            [FieldOffset(168)]
            public bool UseNormalTexture;
        }
        Constants constants;
        D3D11.Buffer cbuffer;

        public float Shininess
        {
            get { return constants.Shininess; }
            set { constants.Shininess = value; }
        }
        public float EmissiveIntensity
        {
            get { return constants.EmissiveIntensity; }
            set { constants.EmissiveIntensity = value; }
        }
        public float SpecularIntensity
        {
            get { return constants.ShininessIntensity; }
            set { constants.ShininessIntensity = value; }
        }
        public Color SpecularColor
        {
            get { return new Color(constants.SpecularColor); }
            set { constants.SpecularColor = value.ToVector3(); }
        }
        
        public List<ModelMesh> Meshes;
        string modelPath;

        public Model(string file, D3D11.Device device) : this(file, device, Matrix.Identity) {
        }
        public Model(string file, D3D11.Device device, Matrix transform) {
            AssimpContext ctx = new AssimpContext();
            if (!ctx.IsImportFormatSupported(Path.GetExtension(file)))
                return;

            modelPath = Path.GetDirectoryName(file);

            Scene scene = ctx.ImportFile(file);
            Node node = scene.RootNode;

            Meshes = new List<ModelMesh>();
            AddNode(scene, scene.RootNode, device, Matrix.Identity, transform);
            
            constants = new Constants();
            SpecularColor = Color.White;
            Shininess = 200;
            SpecularIntensity = 1;
            EmissiveIntensity = 1;
        }

        public void AddNode(Scene scene, Node node, D3D11.Device device, Matrix transform, Matrix fTransform) {
            transform *= node.Transform.ToMatrix();
            Matrix t = transform * fTransform;
            Matrix invTranspose = Matrix.Transpose(Matrix.Invert(t));

            if (node.HasMeshes) {
                foreach (int index in node.MeshIndices) {
                    Mesh mesh = scene.Meshes[index];

                    ModelMesh mm = new ModelMesh();
                    Meshes.Add(mm);

                    Material mat = scene.Materials[mesh.MaterialIndex];
                    if (mat != null) {
                        if (mat.GetMaterialTextureCount(TextureType.Diffuse) > 0)
                            mm.SetDiffuseTexture(device, modelPath + "/" + mat.TextureDiffuse.FilePath);
                        if (mat.GetMaterialTextureCount(TextureType.Emissive) > 0) 
                            mm.SetEmissiveTexture(device, modelPath + "/" + mat.TextureEmissive.FilePath);
                        if (mat.GetMaterialTextureCount(TextureType.Specular) > 0)
                            mm.SetSpecularTexture(device, modelPath + "/" + mat.TextureSpecular.FilePath);
                        if (mat.GetMaterialTextureCount(TextureType.Normals) > 0)
                            mm.SetNormalTexture(device, modelPath + "/" + mat.TextureNormal.FilePath);
                    }

                    List<short> indicies = new List<short>();
                    foreach (Face f in mesh.Faces)
                        if (f.HasIndices)
                            for (int i = 2; i < f.Indices.Count; i++) {
                                indicies.Add((short)f.Indices[0]);
                                indicies.Add((short)f.Indices[i - 1]);
                                indicies.Add((short)f.Indices[i]);
                            }

                    Vector3D[] verts = mesh.Vertices.ToArray();
                    Vector3D[] normals = mesh.Normals.ToArray();
                    Vector3D[] tangents = new Vector3D[verts.Length];
                    Vector3D[] texCoords = new Vector3D[verts.Length];
                    if (mesh.HasTextureCoords(0))
                        texCoords = mesh.TextureCoordinateChannels[0].ToArray();
                    if (mesh.HasTangentBasis)
                        tangents = mesh.Tangents.ToArray();
                    else {
                        tangents = new Vector3D[verts.Length];
                        if (mesh.PrimitiveType == PrimitiveType.Triangle || mesh.PrimitiveType == PrimitiveType.Polygon) {
                            for (int i = 0; i < indicies.Count; i += 3) {
                                int i0 = indicies[i];
                                int i1 = indicies[i + 1];
                                int i2 = indicies[i + 2];
                                Vector3D edge1 = verts[i1] - verts[i0];
                                Vector3D edge2 = verts[i2] - verts[i0];

                                float du1 = texCoords[i1].X - texCoords[i0].X;
                                float dv1 = texCoords[i1].Y - texCoords[i0].Y;
                                float du2 = texCoords[i2].X - texCoords[i0].X;
                                float dv2 = texCoords[i2].Y - texCoords[i0].Y;

                                float f = 1f / (du1 * dv2 - du2 * dv1);

                                Vector3D tangent = new Vector3D(
                                    f * (dv2 * edge1.X - dv1 * edge2.X),
                                    f * (dv2 * edge1.Y - dv1 * edge2.Y),
                                    f * (dv2 * edge1.Z - dv1 * edge2.Z)
                                );
                                tangents[i0] += tangent;
                                tangents[i1] += tangent;
                                tangents[i2] += tangent;
                            }
                            for (int i = 0; i < tangents.Length; i ++)
                                tangents[i].Normalize();
                        }
                    }

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

                    ModelVertex[] verticies = new ModelVertex[mesh.VertexCount];
                    bool colors = mesh.HasVertexColors(0);
                    Color col = Color.White;
                    mm.Shininess = scene.Materials[mesh.MaterialIndex].Shininess;
                    mm.ShininessIntensity = scene.Materials[mesh.MaterialIndex].ShininessStrength;
                    for (int i = 0; i < mesh.VertexCount; i++) {
                        verticies[i] = new ModelVertex(
                            (Vector3)Vector3.Transform(new Vector3(verts[i].X, verts[i].Y, verts[i].Z), t),
                            Vector3.Normalize((Vector3)Vector3.Transform(new Vector3(normals[i].X, normals[i].Y, normals[i].Z), invTranspose)),
                            Vector3.Normalize((Vector3)Vector3.Transform(new Vector3(tangents[i].X, tangents[i].Y, tangents[i].Z), invTranspose)),
                            new Vector2(texCoords[i].X, 1f - texCoords[i].Y),
                            (colors ? mesh.VertexColorChannels[0][i].ToColor() : Color.White) * col);
                    }

                    mm.VertexSize = Utilities.SizeOf<ModelVertex>();

                    mm.VertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, verticies);
                    mm.VertexCount = mesh.VertexCount;

                    mm.IndexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, indicies.ToArray());
                    mm.IndexCount = indicies.Count;
                }
            }

            foreach (Node c in node.Children)
                AddNode(scene, c, device, transform, fTransform);
        }

        public void SetResources(Renderer renderer, Vector3d lightDirection, Matrix world) {
            constants.World = world;
            constants.WorldInverseTranspose = Matrix.Invert(Matrix.Transpose(world));
            constants.lightDirection = lightDirection;

            // create/update constant buffer
            if (cbuffer == null)
                cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            else
                renderer.Context.UpdateSubresource(ref constants, cbuffer);
            
            renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);
        }

        public void Draw(Renderer renderer) {
            foreach (ModelMesh m in Meshes) {
                constants.Shininess = m.Shininess;
                constants.ShininessIntensity = m.ShininessIntensity;
                constants.UseNormalTexture = m.NormalTextureView != null;
                renderer.Context.UpdateSubresource(ref constants, cbuffer);
                m.Draw(renderer);
            }
        }

        public void Draw(Renderer renderer, Vector3d lightDirection, Matrix world) {
            SetResources(renderer, lightDirection, world);
            Draw(renderer);
        }
        
        public void DrawInstanced(Renderer renderer, Vector3d lightDirection, Matrix world, int instanceCount) {
            SetResources(renderer, lightDirection, world);

            foreach (ModelMesh m in Meshes)
                m.DrawInstanced(renderer, instanceCount);
        }

        public void Dispose() {
            if (Meshes != null)
                foreach (ModelMesh m in Meshes)
                    m.Dispose();
            cbuffer?.Dispose();
        }
    }
}
