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

        public static void GenerateIcosphere(int detail, out Vector3[] verticies, out short[] indicies) {
            List<Vector3> verts = new List<Vector3>();
            List<short> inds = new List<short>();

            verts.AddRange(Verticies);
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
                    
                    verts.Add((verts[i1] + verts[i2]) * .5f); // i4
                    verts.Add((verts[i2] + verts[i3]) * .5f); // i5
                    verts.Add((verts[i1] + verts[i3]) * .5f); // i6
                    
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

            for (int i = 0; i < verticies.Length; i++)
                verticies[i].Normalize();
        }
    }
}
