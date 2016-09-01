using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;

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
            renderer.Consolas14.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.Consolas14.WordWrapping = DWrite.WordWrapping.NoWrap;

            renderer.D2DContext.BeginDraw();

            renderer.D2DContext.DrawText(
                ChunksDrawn.ToString("N0") + " chunks (" + (ChunksDrawn * QuadTree.GridSize * QuadTree.GridSize).ToString("N0") + " verticies) " + FPS + "fps",
                renderer.Consolas14, new RawRectangleF(10, 5, 300, 15), renderer.SolidWhiteBrush);

            renderer.D2DContext.DrawText(
                CameraSpeed.ToString("F1") + " m/s (" + (CameraSpeed / Constants.LIGHT_SPEED).ToString("F4") + "c)",
                renderer.Consolas14, new RawRectangleF(10, 20, 300, 30), renderer.SolidWhiteBrush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            renderer.D2DContext.DrawText(
                renderer.Camera.Position.X.ToString("F1") + ", " + renderer.Camera.Position.Y.ToString("F1") + ", " + renderer.Camera.Position.Z.ToString("F1"),
                renderer.Consolas14, new RawRectangleF(10, 35, 300, 45), renderer.SolidWhiteBrush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            renderer.D2DContext.EndDraw();
        }
    }
}
