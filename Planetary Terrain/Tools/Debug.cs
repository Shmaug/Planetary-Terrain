using System.Collections.Generic;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using D3D11 = SharpDX.Direct3D11;
using DWrite = SharpDX.DirectWrite;
using System.Diagnostics;
using SharpDX;
using System;
using SharpDX.Direct3D;

namespace Planetary_Terrain {
    class Profiler {
        static string[] Colors = new string[] {
                "Red",
                "OrangeRed",
                "White",
                "CornflowerBlue",
                "Magenta",
                "Yellow"
            };
        public static int lineHeight = 14;
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

        public static Profiler Begin(string name = "") {
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
            return ActiveProfiler;
        }
        public static bool Resume(string name) {
            ActiveProfiler?.Stopwatch.Start();
            foreach (Profiler p in ActiveProfiler.Children) {
                if (p.Name == name) {
                    ActiveProfiler = p;
                    p.Stopwatch.Start();
                    return true;
                }
            }
            return false;
        }
        public static void End() {
            ActiveProfiler?.Stopwatch.Stop();
            ActiveProfiler = ActiveProfiler.Parent;
        }

        public void Draw(Renderer renderer, RawRectangleF rect) {
            int y = lineHeight;
            Draw(renderer, rect, 0, (int)rect.Left, ref y);
        }
        public void Draw(Renderer renderer, RawRectangleF rect, int c, int textx, ref int texty) {
            D2D1.Brush brush = renderer.Brushes[Colors[c % Colors.Length]];

            renderer.D2DContext.FillRectangle(rect, brush);
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.D2DContext.DrawText(
                Name + " (" + Stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + "ms)",
                renderer.Consolas14, new RawRectangleF(textx, rect.Bottom + texty, textx + 100, rect.Bottom + texty + lineHeight), brush, D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);
            
            int lineA = texty + 2;
            int lineC = c;

            texty += lineHeight;
            int x = 0;
            foreach (Profiler p in Children) {
                c++;

                int w = (int)((p.Stopwatch.Elapsed.Ticks / (double)Stopwatch.Elapsed.Ticks) * (rect.Right - rect.Left));
                p.Draw(renderer, new RawRectangleF(rect.Left + x, rect.Top, rect.Left + x + w, rect.Bottom), c, textx + 10, ref texty);
                
                x += w;
            }

            if (Children.Count > 0)
                renderer.D2DContext.FillRectangle(new RawRectangleF(textx-3, rect.Bottom + lineA, textx-2, rect.Bottom + texty - 2), renderer.Brushes[Colors[lineC % Colors.Length]]);
        }

        public void DrawCircle(Renderer renderer, Vector2 center, float radius, float step, double a, double b, int col = 0, float txt = 0) {
            D2D1.PathGeometry path = new D2D1.PathGeometry(renderer.D2DFactory);
            D2D1.GeometrySink s = path.Open();
            s.SetFillMode(D2D1.FillMode.Winding);

            s.BeginFigure(center + new Vector2((float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius), D2D1.FigureBegin.Filled);
            for (double i = a; i <= b; i += Math.PI * .05)
                s.AddLine(center + new Vector2((float)Math.Cos(i) * radius, (float)Math.Sin(i) * radius));
            s.AddLine(center + new Vector2((float)Math.Cos(b) * radius, (float)Math.Sin(b) * radius));
            s.AddLine(center);

            s.EndFigure(D2D1.FigureEnd.Closed);
            s.Close();

            if (path.FillContainsPoint(Input.mousePos, 1)) {
                if (txt == 0)
                    txt = radius + 50;
                RawRectangleF r = new RawRectangleF(center.X - 100, center.Y - txt, center.X + 100, center.Y - txt + 12);
                renderer.D2DContext.FillRectangle(r, renderer.Brushes["TransparentBlack"]);
                renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
                renderer.D2DContext.DrawText(
                    Name + " (" + Stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + "ms)",
                    renderer.Consolas14, r, renderer.Brushes[Colors[col % Colors.Length]], D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

                txt += 12;
            }

            renderer.D2DContext.FillGeometry(path, renderer.Brushes[Colors[col % Colors.Length]]);
            s.Dispose();
            path.Dispose();
            
            double t = 0;
            foreach (Profiler p in Children) {
                col++;
                
                double f = (p.Stopwatch.Elapsed.Ticks / (double)Stopwatch.Elapsed.Ticks) * (b - a);
                p.DrawCircle(renderer, center, radius - step, step, a + t, a + t + f, col, txt);
                t += f;
            }
        }

        public int TotalChildren() {
            int i = 1;
            foreach (Profiler p in Children)
                i += p.TotalChildren();
            return i;
        }
    }
    static class Debug {
        struct Line {
            public Color color;
            public Vector3d[] points;
        }
        static D3D11.Buffer linevbuffer;
        static D3D11.Buffer linecbuffer;

        static List<Line> lines = new List<Line>();

        public static int TrianglesDrawn;
        public static int TreesDrawn;
        public static int ImposterDrawn;
        public static int FPS;
        
        static List<string> logs = new List<string>();
        static Dictionary<string, string> tracks = new Dictionary<string, string>();
        static Dictionary<string, string> immediateTrack = new Dictionary<string, string>();

        public static void Log(object l) {
            logs.Add(l.ToString());
            if (logs.Count > 10)
                logs.RemoveAt(0);
        }

        public static void Track(object l, string name) {
            tracks[name] = l?.ToString() ?? "null";
        }
        public static void TrackImmediate(object l, string name) {
            immediateTrack[name] = l?.ToString() ?? "null";
        }

        public static void BeginFrame() {
            TrianglesDrawn = 0;
            TreesDrawn = 0;
            ImposterDrawn = 0;
            immediateTrack.Clear();
            lines.Clear();
        }
        public static void EndFrame() {

        }


        public static void DrawLine(Color color, params Vector3d[] points) {
            lines.Add(new Line() { color = color, points = points });
        }

        public static void Draw3D(Renderer renderer) {
            if (linecbuffer == null) {
                Matrix m = Matrix.Identity;
                linecbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref m);
            } else {
                Matrix mat = Matrix.Identity;
                renderer.Context.UpdateSubresource(ref mat, linecbuffer);
            }

            Shaders.BasicShader.Set(renderer);
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;

            foreach (Line line in lines) {
                VertexColor[] verts = new VertexColor[line.points.Length];
                for (int i = 0; i < line.points.Length; i++)
                    verts[i] = new VertexColor(line.points[i] - renderer.Camera.Position, line.color);

                if (linevbuffer == null)
                    linevbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verts);
                else
                    renderer.Context.UpdateSubresource(verts, linevbuffer);
                
                renderer.Context.VertexShader.SetConstantBuffer(1, linecbuffer);
                renderer.Context.PixelShader.SetConstantBuffer(1, linecbuffer);

                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(linevbuffer, Utilities.SizeOf<VertexColor>(), 0));

                renderer.Context.Draw(line.points.Length, 0);
            }
        }

        public static void Draw2D(Renderer renderer, Profiler frameProfiler) {
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;
            
            renderer.D2DContext.DrawText(
                string.Format("{0} tris, {1} trees/{2} imposters    {3} fps    [{4} waiting / {5} generating]",
                TrianglesDrawn.ToString("N0"), TreesDrawn.ToString("N0"), ImposterDrawn.ToString("N0"), FPS, QuadNode.GenerateQueue.Count, QuadNode.Generating.Count),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 25, 300, renderer.Viewport.Height - 10), renderer.Brushes["White"]);

            renderer.D2DContext.DrawText(
                string.Format("[{0}, {1}]",
                Planet.min, Planet.max),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - 50, 300, renderer.Viewport.Height - 25), renderer.Brushes["White"]);

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
            foreach (KeyValuePair<string, string> l in tracks) {
                float w = 200;
                RawRectangleF rect = new RawRectangleF(renderer.ResolutionX * .75f - w - 3, y - 3, renderer.ResolutionX * .75f + w + 3, y + h + 3);

                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14, rect, renderer.Brushes["White"]);

                y -= h + 5;
            }
            y = renderer.ResolutionY - 10 - h;
            foreach (KeyValuePair<string, string> l in immediateTrack) {
                float w = 200;
                RawRectangleF rect = new RawRectangleF(renderer.ResolutionX * .75f - w - 3 - 50, y - 3, renderer.ResolutionX * .75f + w + 3 - 50, y + h + 3);

                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14, rect, renderer.Brushes["White"]);

                y -= h + 5;
            }
            #endregion

            int py = frameProfiler.TotalChildren()*Profiler.lineHeight + Profiler.lineHeight;
            renderer.D2DContext.FillRectangle(new RawRectangleF(renderer.ResolutionX - 360, 5, renderer.ResolutionX, 40 + py + 5), renderer.Brushes["TransparentBlack"]);

            frameProfiler.Draw(renderer, new RawRectangleF(renderer.ResolutionX - 350, 10, renderer.ResolutionX - 10, 40));

            float r = renderer.ResolutionX * .075f;
            frameProfiler.DrawCircle(renderer, new Vector2(renderer.ResolutionX - r - 10, renderer.ResolutionY - r - 10), r, r * .05f, 0, Math.PI * 2);
        }

        public static void Dispose() {
            linevbuffer?.Dispose();
            linecbuffer?.Dispose();
        }
    }
}