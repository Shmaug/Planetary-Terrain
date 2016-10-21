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
        public static Profiler ActiveProfiler;

        public Stopwatch Stopwatch;
        public string Name;
        public List<Profiler> Children;
        public Profiler Parent;
        public Profiler(string name) {
            Name = name;
            Children = new List<Profiler>();
            Stopwatch = new Stopwatch();
        }

        public static void Begin(string name = "") {
            if (ActiveProfiler == null) {
                ActiveProfiler = new Profiler(name);
                ActiveProfiler.Stopwatch.Start();
            }else {
                Profiler p = new Profiler(name);
                p.Parent = ActiveProfiler;
                ActiveProfiler.Children.Add(p);
                ActiveProfiler = p;
                p.Stopwatch.Start();
            }
        }
        public static void End() {
            ActiveProfiler?.Stopwatch.Stop();
            ActiveProfiler = ActiveProfiler.Parent;
        }

        public void Draw(Renderer renderer, RawRectangleF rect) {
            Draw(renderer, rect, 0, (int)rect.Left, 16);
        }

        public void Draw(Renderer renderer, RawRectangleF rect, int c, int textx, int texty) {
            D2D1.Brush brush = renderer.Brushes[Colors[c % Colors.Length]];

            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.D2DContext.FillRectangle(rect, brush);
            renderer.D2DContext.DrawText(
                Name + " (" + Stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + "ms) " + Children.Count,
                renderer.Consolas14, new RawRectangleF(textx, rect.Bottom + texty, textx + 300, rect.Bottom + texty + 16), brush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

            textx += 5;
            texty += 16;

            int x = 0;
            foreach (Profiler p in Children) {
                c++;

                int w = (int)((p.Stopwatch.Elapsed.Ticks / (double)Stopwatch.Elapsed.Ticks) * (rect.Right - rect.Left));
                p.Draw(renderer, new RawRectangleF(rect.Left + x, rect.Top, rect.Left + x + w, rect.Bottom), c, textx, texty);

                texty += 16;
                x += w;
            }
        }
    }
    static class Debug {
        public static QuadNode ClosestQuadTree;
        public static double ClosestQuadTreeDistance;
        public static double ClosestQuadTreeScale;
        public static int VerticiesDrawn;
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
            VerticiesDrawn = 0;
            ClosestQuadTreeDistance = double.MaxValue;
        }

        public static void EndFrame() {

        }

        public static void Draw(Renderer renderer) {
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;
            
            renderer.D2DContext.DrawText(
                string.Format("{0} verts, {1} fps    [{2} waiting / {3} generating]",
                VerticiesDrawn.ToString("N0"), FPS, QuadNode.GenerateQueue.Count, QuadNode.Generating.Count),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 25, 300, renderer.Viewport.Height - 10), renderer.Brushes["White"]);
            
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

            Profiler pr = Profiler.ActiveProfiler;
            while (pr.Parent != null)
                pr = pr.Parent;
            pr.Draw(renderer, new RawRectangleF(renderer.ResolutionX - 500, 10, renderer.ResolutionX - 100, 40));
        }
    }
}