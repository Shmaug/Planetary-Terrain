using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;

namespace Planetary_Terrain {
    static class Debug {
        public static int ChunksDrawn;
        public static int FPS;
        public static double CameraSpeed;

        public static void BeginFrame() {
            ChunksDrawn = 0;
        }

        public static void EndFrame() {

        }

        public static void DrawStats(Renderer renderer) {
            renderer.D2DContext.BeginDraw();
            renderer.D2DContext.DrawText(
                ChunksDrawn.ToString("N0") + " chunks (" + (ChunksDrawn * QuadTree.GridSize * QuadTree.GridSize).ToString("N0") + " verticies) " + FPS + "fps",
                renderer.SegoeUI14, new RawRectangleF(10, 5, 300, 15), renderer.SolidWhiteBrush);

            renderer.D2DContext.DrawText(
                CameraSpeed.ToString("F1") + " m/s (" + (CameraSpeed / Constants.LIGHT_SPEED).ToString("F4") + "c)",
                renderer.SegoeUI14, new RawRectangleF(10, 20, 300, 30), renderer.SolidWhiteBrush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);
            renderer.D2DContext.EndDraw();
        }
    }
}
