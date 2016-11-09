using SharpDX;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    [StructLayout(LayoutKind.Sequential)]
    struct VertexNormal {
        public Vector3 Position;
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
    [StructLayout(LayoutKind.Sequential)]
    struct VertexTexture {
        public Vector3 Position;
        public Vector2 TexCoord;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
        };

        public VertexTexture(Vector3 pos, Vector2 texcoord) {
            Position = pos;
            TexCoord = texcoord;
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
    [StructLayout(LayoutKind.Sequential)]
    struct VertexNormalTexture {
        public Vector3 Position;
        public Vector3 Normal;
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
    [StructLayout(LayoutKind.Sequential)]
    struct VertexNormalColor{
        public Vector3 Position;
        public Vector3 Normal;
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

    [StructLayout(LayoutKind.Sequential)]
    struct ModelVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 TexCoord;
        public Color4 Color;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("TANGENT", 0, Format.R32G32B32_Float, 24, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32G32_Float, 36, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 44, 0),
        };

        public ModelVertex(Vector3 pos, Vector3 norm, Vector3 tangent, Vector2 uv, Color color) {
            Position = pos;
            Normal = norm;
            Tangent = tangent;
            TexCoord = uv;
            Color = color;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PlanetVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;
        public Vector3 UVW;
        public Vector2 TempHumid;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 24, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 40, 0),
            new D3D11.InputElement("TEXCOORD", 1, Format.R32G32_Float, 52, 0),
        };

        public PlanetVertex(Vector3 pos, Vector3 norm, Vector3 uvw, Vector2 tempHumid) {
            Position = pos;
            Normal = norm;
            UVW = uvw;
            TempHumid = tempHumid;
            Color = Color4.White;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WaterVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public float Height;

        public static D3D11.InputElement[] InputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new D3D11.InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new D3D11.InputElement("TEXCOORD", 0, Format.R32_Float, 24, 0),
        };

        public WaterVertex(Vector3 pos, Vector3 norm, float height) {
            Position = pos;
            Normal = norm;
            Height = height;
        }
    }
}
