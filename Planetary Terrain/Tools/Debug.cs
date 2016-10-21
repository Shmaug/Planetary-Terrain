using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using System.Diagnostics;
using SharpDX;

namespace Planetary_Terrain {
    class Profiler {
        static string[] Colors = new string[] {
                "Red",
                "OrangeRed",
                "White",
                "CornflowerBlue",
                "Magenta",
                "Yellow",
                "RosyBrown"
            };
        public static double Total
        {
            get
            {
                double t = 0;
                foreach (KeyValuePair<string, Stopwatch> p in ActiveProfilers)
                    t += p.Value.Elapsed.Ticks;
                return t;
            }
        }
        static Dictionary<string, Stopwatch> ActiveProfilers = new Dictionary<string, Stopwatch>();
        
        
        public string Name;
        List<Profiler> Children;
        Profiler Parent;
        public Profiler(string name) {
            Name = name;
            Children = new List<Profiler>();
        }

        public static void Begin(string name = "") {

        }
        public static void End() {

        }

        public static void Draw(Renderer renderer, RawRectangleF rect, ref int c, int tx, ref int y) {
            double t = Total;

            int x = 0;

            D2D1.Brush brush = renderer.Brushes[Colors[0]];

            int h = 30;

            foreach (KeyValuePair<string, Stopwatch> k in ActiveProfilers) {
                int w = (int)((k.Value.Elapsed.Ticks / t) * (rect.Right - rect.Left));
                renderer.D2DContext.FillRectangle(new RawRectangleF(rect.Left + x, rect.Top, rect.Left + x + w, rect.Bottom), brush);
                renderer.D2DContext.DrawText(
                    k.Key + " (" + k.Value.Elapsed.TotalMilliseconds + "ms)",
                    renderer.Consolas14, new RawRectangleF(tx, y, tx+300, y+h), brush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

                x += w;
                y += h;
                c++;
            }
        }
    }
    static class Debug {
        public static QuadNode ClosestQuadTree;
        public static double ClosestQuadTreeDistance;
        public static double ClosestQuadTreeScale;
        public static int VerticiesDrawn;
        public static int FPS;
        
        public static Profiler UpdateProfiler = new Profiler("Update");
        public static Profiler DrawProfiler = new Profiler("Draw");

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
            VerticiesDrawn = 0;
            ClosestQuadTreeDistance = double.MaxValue;
        }

        public static void EndFrame() {

        }

        public static void Draw(Renderer renderer, Ship ship) {
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;
            
            renderer.D2DContext.DrawText(
                string.Format("{0} verts, {1} fps    [{2} waiting / {3} generating]",
                VerticiesDrawn.ToString("N0"), FPS, QuadNode.GenerateQueue.Count, QuadNode.Generating.Count),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 10, 300, renderer.Viewport.Height - 25), renderer.Brushes["White"]);

            double spd = ship.Velocity.Length();
            renderer.D2DContext.DrawText(
                string.Format("{0} m/s ({1}c)",
                spd.ToString("F1"), (spd / Physics.LIGHT_SPEED).ToString("F4")),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 25, 300, renderer.Viewport.Height - 40), renderer.Brushes["White"], D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            renderer.D2DContext.DrawText(
                string.Format("[{0}, {1}, {2}]      [{3},{4},{5}]",
                renderer.Camera.Position.X.ToString("F1"), renderer.Camera.Position.Y.ToString("F1"), renderer.Camera.Position.Z.ToString("F1"),
                renderer.Camera.Rotation.X.ToString("F1"), renderer.Camera.Rotation.Y.ToString("F1"), renderer.Camera.Rotation.Z.ToString("F1")),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 40, 300, renderer.Viewport.Height - 55), renderer.Brushes["White"], D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            if (ClosestQuadTree != null) {
                Planet p = ClosestQuadTree.Body is Planet ? ClosestQuadTree.Body as Planet : null;
                renderer.D2DContext.DrawText(
                    string.Format("Closest QuadTree: {0} | {1}m/vertex | Scale: {2} | [{3}-{4}]",
                    ClosestQuadTreeDistance.ToString("F2"), ClosestQuadTree.VertexSpacing.ToString("F2"), ClosestQuadTreeScale.ToString("F2"), p?.min.ToString("F1"), p?.max.ToString("F1")),
                    renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 55, 300, renderer.Viewport.Height - 70), renderer.Brushes["White"]);
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

                renderer.D2DContext.DrawText(s, renderer.Consolas14, rect, renderer.Brushes["White"]);

                y += h + 5;
            }

            y = renderer.ResolutionY - 10 - h;
            foreach (KeyValuePair<string, string> l in labels) {
                float w = 200;
                RawRectangleF rect = new RawRectangleF(renderer.ResolutionX * .75f - w - 3, y - 3, renderer.ResolutionX * .75f + w + 3, y + h + 3);

                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14, rect, renderer.Brushes["White"]);

                y -= h + 5;
            }
            #endregion
        }
    }
}