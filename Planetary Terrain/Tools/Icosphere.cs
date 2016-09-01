using System;
using System.Collections.Generic;
using SharpDX;

namespace Planetary_Terrain {
    class Icosphere {
        const float t = 1.61803398875f;
        static Vector3[] Verticies {
            get {
                return new Vector3[] {
                    new Vector3(-1,  t,  0),
                    new Vector3( 1,  t,  0),
                    new Vector3(-1, -t,  0),
                    new Vector3( 1, -t,  0),

                    new Vector3( 0, -1,  t),
                    new Vector3( 0,  1,  t),
                    new Vector3( 0, -1, -t),
                    new Vector3( 0,  1, -t),

                    new Vector3( t, 0, -1),
                    new Vector3( t, 0,  1),
                    new Vector3(-t, 0, -1),
                    new Vector3(-t, 0,  1) };
                }
            }
        static short[] Indicies {
            get {
                return new short[] {
                    0, 11, 5,
                    0, 5, 1,
                    0, 1, 7,
                    0, 7, 10,
                    0, 10, 11,

                    1, 5, 9,
                    5, 11, 4,
                    11, 10, 2,
                    10, 7, 6,
                    7, 1, 8,

                    3, 9, 4,
                    3, 4, 2,
                    3, 2, 6,
                    3, 6, 8,
                    3, 8, 9,

                    4, 9, 5,
                    2, 4, 11,
                    6, 2, 10,
                    8, 6, 7,
                    9, 8, 1 };
                }
        }
        static short[] ReverseIndicies
        {
            get
            {
                return new short[] {
                    0, 5,  11,
                    0, 1,  5, 
                    0, 7,  1, 
                    0, 10, 7, 
                    0, 11, 10,

                    1,  9, 5, 
                    5,  4, 11,
                    11, 2, 10,
                    10, 6, 7, 
                    7,  8, 1, 

                    3, 4, 9,
                    3, 2, 4,
                    3, 6, 2,
                    3, 8, 6,
                    3, 9, 8,

                    4, 5,  9,
                    2, 11, 4,
                    6, 10, 2,
                    8, 7,  6,
                    9, 1,  8,};
            }
        }

        public static void GenerateIcosphere(int detail, bool reverseTriangleDirection, out VertexNormal[] verticies, out short[] indicies) {
            List<VertexNormal> verts = new List<VertexNormal>();
            List<short> inds = new List<short>();

            for (int i = 0; i < Verticies.Length; i++)
                verts.Add(new VertexNormal(Verticies[i], Vector3.Zero));

            if (reverseTriangleDirection)
                inds.AddRange(ReverseIndicies);
            else
                inds.AddRange(Indicies);

            short i1, i2, i3, i4, i5, i6;
            for (int l = 1; l < detail; l++) {
                List<short> newinds = new List<short>();
                for (int i = 0; i < inds.Count; i += 3) {
                    i1 = inds[i];
                    i2 = inds[i + 1];
                    i3 = inds[i + 2];
                    i4 = (short)(verts.Count);
                    i5 = (short)(verts.Count + 1);
                    i6 = (short)(verts.Count + 2);
                    
                    verts.Add(new VertexNormal((verts[i1].Position + verts[i2].Position) * .5f, Vector3.Zero)); // i4
                    verts.Add(new VertexNormal((verts[i2].Position + verts[i3].Position) * .5f, Vector3.Zero)); // i5
                    verts.Add(new VertexNormal((verts[i1].Position + verts[i3].Position) * .5f, Vector3.Zero)); // i6
                    
                    newinds.AddRange(new short[]{
                        i1, i4, i6,
                        i4, i2, i5,
                        i6, i5, i3,
                        i4, i5, i6
                    });
                }
                inds.Clear();
                inds.AddRange(newinds);
            }

            verticies = verts.ToArray();
            indicies = inds.ToArray();

            for (int i = 0; i < verticies.Length; i++) {
                verticies[i].Position.Normalize();
                verticies[i].Normal = verticies[i].Position;
            }
        }
    }
}
