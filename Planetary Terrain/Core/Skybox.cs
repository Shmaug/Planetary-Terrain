using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain.Core {
    class Skybox : IDisposable {
        static Vector3[] CubeVerticies { get
        {
            return new Vector3[] {
                new Vector3(-1f, -1f,  1f),
                new Vector3( 1f, -1f,  1f),
                new Vector3( 1f,  1f,  1f),
                new Vector3(-1f,  1f,  1f),
                new Vector3(-1f, -1f, -1f),
                new Vector3( 1f, -1f, -1f),
                new Vector3( 1f,  1f, -1f),
                new Vector3(-1f,  1f, -1f),
            };
        } }
        static short[] CubeIndicies { get
        {
            return new short[] {	// front
		        0, 1, 2,
                2, 3, 0,
		        // top
		        1, 5, 6,
                6, 2, 1,
		        // back
		        7, 6, 5,
                5, 4, 7,
		        // bottom
		        4, 0, 3,
                3, 7, 4,
		        // left
		        4, 5, 1,
                1, 0, 4,
		        // right
		        3, 2, 6,
                6, 7, 3,
            };
        } }

        D3D11.Buffer vertexBuffer;
        D3D11.Buffer indexBuffer;

        public D3D11.Texture3D Texture;

        public Skybox() {
            // TODO: this
        }

        public void Dispose() {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();

            vertexBuffer = null;
            indexBuffer = null;
        }
    }
}
