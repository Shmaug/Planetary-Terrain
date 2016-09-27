using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using SharpDX;

namespace Planetary_Terrain {
    static class Debug {
        public static QuadTree ClosestQuadTree;
        public static double ClosestQuadTreeDistance;
        public static double ClosestQuadTreeScale;
        public static int ChunksDrawn;
        public static int FPS;

        static List<string> logs = new List<string>();
        static Dictionary<string, string> labels = new Dictionary<string, string>();

        public static void Log(object l) {
            logs.Add(l.ToString());
            if (logs.Count > 10)
                logs.RemoveAt(0);
        }

        public static void Track(object l, string name) {
            labels[name] = l?.ToString() ?? "null";
        }

        public static void BeginFrame() {
            ChunksDrawn = 0;
            ClosestQuadTreeDistance = double.MaxValue;
        }

        public static void EndFrame() {

        }

        public static void Draw(Renderer renderer) {
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            renderer.D2DContext.BeginDraw();

            renderer.D2DContext.DrawText(
                ChunksDrawn.ToString("N0") + " chunks (" + (ChunksDrawn * QuadTree.GridSize * QuadTree.GridSize).ToString("N0") + " verticies) " + FPS + "fps",
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 10, 300, renderer.Viewport.Height - 25), renderer.SolidWhiteBrush);

            double spd = renderer.Camera.Speed * renderer.Camera.SpeedMultiplier;
            renderer.D2DContext.DrawText(
                spd.ToString("F1") + " m/s (" + (spd / Constants.LIGHT_SPEED).ToString("F4") + "c)",
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 25, 300, renderer.Viewport.Height - 40), renderer.SolidWhiteBrush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            renderer.D2DContext.DrawText(
                renderer.Camera.Position.X.ToString("F1") + ", " + renderer.Camera.Position.Y.ToString("F1") + ", " + renderer.Camera.Position.Z.ToString("F1"),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 40, 300, renderer.Viewport.Height - 55), renderer.SolidWhiteBrush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            if (ClosestQuadTree != null) {
                renderer.D2DContext.DrawText(
                    "Closest QuadTree: " + ClosestQuadTreeDistance.ToString("F2") + "  " + ClosestQuadTree.VertexSpacing.ToString("F2") + "m/vertex (Scale: " + ClosestQuadTreeScale + ")",
                    renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 55, 300, renderer.Viewport.Height - 70), renderer.SolidWhiteBrush);
            }
            #region logs
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.WordWrapping = DWrite.WordWrapping.NoWrap;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            float h = 10;
            float y = 10;
            for (int i = 0; i < logs.Count; i++) {
                string s = logs[i].ToString();
                float w = 200;
                RawRectangleF rect = new RawRectangleF(7, y - 3, 7 + w, y - 3 + h);

                renderer.D2DContext.FillRectangle(rect, renderer.SolidBlackBrush);
                renderer.D2DContext.DrawText(s, renderer.Consolas14, rect, renderer.SolidWhiteBrush);

                y += h + 5;
            }

            y = 10;
            foreach (KeyValuePair<string, string> l in labels) {
                float w = 200;
                RawRectangleF rect = new RawRectangleF(renderer.ResolutionX * .5f - w - 3, y - 3, renderer.ResolutionX * .5f + w + 3, y + h + 3);

                renderer.D2DContext.FillRectangle(rect, renderer.SolidBlackBrush);
                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14, rect, renderer.SolidWhiteBrush);

                y += h + 5;
            }
            #endregion

            renderer.D2DContext.EndDraw();
        }
    }
}