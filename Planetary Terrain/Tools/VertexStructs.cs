using SharpDX;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    [StructLayout(LayoutKind.Explicit)]
    struct VertexNormal {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0)
        };

        public VertexNormal(Vector3 pos, Vector3 norm) {
            Position = pos;
            Normal = norm;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct VertexNormalTexture {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Vector2 TexCoord;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
        };

        public VertexNormalTexture(Vector3 pos, Vector3 norm, Vector2 uv) {
            Position = pos;
            Normal = norm;
            TexCoord = uv;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct VertexColor {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Color4 Color;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
        };

        public VertexColor(Vector3 pos, Color col) {
            Position = pos;
            Color = col;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct VertexNormalColor{
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Color4 Color;
        
        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 24, 0)
        };

        public VertexNormalColor(Vector3 pos, Vector3 norm, Color col) {
            Position = pos;
            Normal = norm;
            Color = col;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PlanetVertex {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public Vector3 Normal;
        [FieldOffset(24)]
        public Vector2 TexCoord;
        [FieldOffset(32)]
        public Vector3 Out;
        [FieldOffset(44)]
        public float Height;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0),
            new D3D11.InputElement("TEXCOORD", 1, Format.R32G32B32_Float, 32, 0),
            new D3D11.InputElement("TEXCOORD", 2, Format.R32_Float, 44, 0),
        };

        public PlanetVertex(Vector3 pos, Vector3 norm, Vector3 dir, Vector2 texCoord, float height) {
            Position = pos;
            Normal = norm;
            TexCoord = texCoord;
            Height = height;
            Out = dir;
        }
    }
}
