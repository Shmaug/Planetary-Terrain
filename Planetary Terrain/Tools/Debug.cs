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

            if (path.FillContainsPoint(Input.MousePos, 1)) {
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
        struct Box {
            public Color color;
            public OrientedBoundingBox oob;
        }
        struct FrameSnapshot {
            public float FPS;
            public float FrameTime;
            public int RealFPS;
            public bool mark;
        }
        static D3D11.Buffer vbuffer;
        static D3D11.Buffer cbuffer;

        static List<Line> lines = new List<Line>();
        static List<Box> boxes = new List<Box>();

        public static int TrianglesDrawn;
        public static int TreesDrawn;
        public static int ImposterDrawn;
        public static int FPS;

        static int frameGraphSize = 200;
        static List<FrameSnapshot> frameGraph = new List<FrameSnapshot>();
        static bool frameMarked;

        static List<string> logs = new List<string>();
        static Dictionary<string, string> tracks = new Dictionary<string, string>();
        static Dictionary<string, string> immediateTrack = new Dictionary<string, string>();

        public static bool DrawDebug = true;
        public static bool DrawBoundingBoxes = false;

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

        public static void MarkFrame() {
            frameMarked = true;
        }
        public static void BeginFrame() {
            TrianglesDrawn = 0;
            TreesDrawn = 0;
            ImposterDrawn = 0;
            immediateTrack.Clear();
            frameMarked = false;
            lines.Clear();
            boxes.Clear();
        }
        public static void EndFrame(double frameTime) {
            frameGraph.Add(new FrameSnapshot() { RealFPS = FPS, FrameTime = (float)frameTime, FPS = (float)(1.0 / frameTime), mark = frameMarked });
            if (frameGraph.Count > frameGraphSize)
                frameGraph.RemoveAt(0);
        }
        
        public static void DrawLine(Color color, params Vector3d[] points) {
            lines.Add(new Line() { color = color, points = points });
        }
        public static void DrawBox(Color color, OrientedBoundingBox oob) {
            boxes.Add(new Box() { oob = oob, color = color });
        }
        
        public static void Draw3D(Renderer renderer) {
            if (!DrawDebug) return;
            Matrix m = Matrix.Identity;
            float[] b = new float[32] {
                    m.M11,m.M12,m.M13,m.M14,
                    m.M21,m.M22,m.M23,m.M24,
                    m.M31,m.M32,m.M33,m.M34,
                    m.M41,m.M42,m.M43,m.M44,
                    1,1,1,1,
                    0,0,0,0,0,0,0,0,0,0,0,0
                };
            if (cbuffer == null)
                cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, b);
            else
                renderer.Context.UpdateSubresource(b, cbuffer);

            Shaders.BasicShader.Set(renderer);
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;

            foreach (Line line in lines) {
                VertexColor[] verts = new VertexColor[line.points.Length];
                for (int i = 0; i < line.points.Length; i++)
                    verts[i] = new VertexColor(line.points[i] - renderer.Camera.Position, line.color);

                if (vbuffer == null)
                    vbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verts);
                else
                    renderer.Context.UpdateSubresource(verts, vbuffer);
                
                renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
                renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vbuffer, Utilities.SizeOf<VertexColor>(), 0));

                renderer.Context.Draw(verts.Length, 0);
            }

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            foreach (Box box in boxes) {
                m = Matrix.Scaling(box.oob.Extents) * box.oob.Transformation;
                b = new float[32] {
                    m.M11,m.M12,m.M13,m.M14,
                    m.M21,m.M22,m.M23,m.M24,
                    m.M31,m.M32,m.M33,m.M34,
                    m.M41,m.M42,m.M43,m.M44,
                    box.color.R/255f, box.color.G/255f, box.color.B/255f, box.color.A/255f,
                    0,0,0,0,0,0,0,0,0,0,0,0
                };
                renderer.Context.UpdateSubresource(b, cbuffer);

                renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
                renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.BoundingBoxVertexBuffer, Utilities.SizeOf<VertexColor>(), 0));

                renderer.Context.Draw(24, 0);
            }
        }
        public static void Draw2D(Renderer renderer, Profiler frameProfiler) {
            if (!DrawDebug) return;

            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            float line = 1;
            renderer.D2DContext.DrawText(
                string.Format("{0} triangles, {1} trees/{2} imposters [{3} waiting / {4} generating]",
                TrianglesDrawn.ToString("N0"), TreesDrawn.ToString("N0"), ImposterDrawn.ToString("N0"), QuadNode.GenerateQueue.Count, QuadNode.Generating.Count),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - line * 25, 300, renderer.Viewport.Height - (line - 1) * 25), renderer.Brushes["White"]);
            line++;

            renderer.D2DContext.DrawText(
                string.Format("[{0}, {1}]",
                Planet.min, Planet.max),
                renderer.Consolas14, new RawRectangleF(10, renderer.Viewport.Height - line * 25, 300, renderer.Viewport.Height - (line - 1) * 25), renderer.Brushes["White"]);
            line++;

            #region logs
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.WordWrapping = DWrite.WordWrapping.NoWrap;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            float lh = 10;
            float ly = 10;
            for (int i = 0; i < logs.Count; i++) {
                renderer.D2DContext.DrawText(logs[i].ToString(), renderer.Consolas14,
                    new RawRectangleF(7, ly - 3, 7 + 200, ly - 3 + lh), renderer.Brushes["White"]);

                ly += lh + 5;
            }

            ly = renderer.ResolutionY - 10 - lh;
            foreach (KeyValuePair<string, string> l in tracks) {
                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14, 
                    new RawRectangleF(renderer.ResolutionX * .75f - 200 - 3, ly - 3, renderer.ResolutionX * .75f + 200 + 3, ly + lh + 3),
                    renderer.Brushes["White"]);

                ly -= lh + 5;
            }
            ly = renderer.ResolutionY - 10 - lh;
            foreach (KeyValuePair<string, string> l in immediateTrack) {
                renderer.D2DContext.DrawText(l.Value, renderer.Consolas14,
                    new RawRectangleF(renderer.ResolutionX * .75f - 200 - 3 - 50, ly - 3, renderer.ResolutionX * .75f + 200 + 3 - 50, ly + lh + 3),
                    renderer.Brushes["White"]);

                ly -= lh + 5;
            }
            #endregion
            #region profiler
            int py = frameProfiler.TotalChildren()*Profiler.lineHeight + Profiler.lineHeight;
            renderer.D2DContext.FillRectangle(new RawRectangleF(renderer.ResolutionX - 360, 5, renderer.ResolutionX, 40 + py + 5), renderer.Brushes["TransparentBlack"]);
            frameProfiler.Draw(renderer, new RawRectangleF(renderer.ResolutionX - 350, 10, renderer.ResolutionX - 10, 40));
            float r = renderer.ResolutionX * .075f;
            frameProfiler.DrawCircle(renderer, new Vector2(renderer.ResolutionX - r - 10, renderer.ResolutionY - r - 10), r, r * .05f, 0, Math.PI * 2);
            #endregion

            #region graph
            float minfps = frameGraph[0].FPS;
            float maxfps = frameGraph[0].FPS;
            for (int i = 0; i < frameGraph.Count; i++) {
                minfps = Math.Min(minfps, frameGraph[i].FPS);
                maxfps = Math.Max(maxfps, frameGraph[i].FPS);
            }
            minfps = (int)Math.Floor(minfps / 30) * 30;
            maxfps = (int)Math.Ceiling(maxfps / 30) * 30;
            float gHeight = 400;
            float gWidth = 350;

            float xScale = gWidth / frameGraphSize;
            float ppf = gHeight / (maxfps - minfps); // pixels per frame
            float y0 = renderer.ResolutionY - 100; // bottom of the graph

            RawVector2[] pts = new RawVector2[frameGraph.Count];
            List<RawVector2> marks = new List<RawVector2>();
            for (int i = 0; i < frameGraph.Count; i++) {
                pts[i] = new RawVector2(30 + i * xScale, MathUtil.Clamp(y0 - (frameGraph[i].FPS - minfps) * ppf, y0 - gHeight, y0));
                if (frameGraph[i].mark)
                    marks.Add(pts[i]);
            }
            
            D2D1.PathGeometry fpsline = new D2D1.PathGeometry(renderer.D2DFactory);
            D2D1.GeometrySink fpssink = fpsline.Open();
            fpssink.SetFillMode(D2D1.FillMode.Winding);
            fpssink.BeginFigure(pts[0], D2D1.FigureBegin.Hollow);
            fpssink.AddLines(pts);
            fpssink.EndFigure(D2D1.FigureEnd.Open);
            fpssink.Close();

            renderer.D2DContext.FillRectangle(new RawRectangleF(0, y0 - gHeight - 10, 30 + gWidth + 10, y0+10), renderer.Brushes["TransparentBlack"]);
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Trailing;
            for (int fps = (int)minfps; fps <= maxfps; fps += 30) {
                float y = y0 - (fps - minfps) * ppf;
                // y axis label
                renderer.D2DContext.DrawText(fps.ToString(), renderer.Consolas14,
                    new RawRectangleF(0, y, 25, y), renderer.Brushes["White"]);

                // dash/solid line
                if (fps - minfps > 0)
                    renderer.D2DContext.DrawLine(new RawVector2(28, y), new RawVector2(30+gWidth, y), renderer.Brushes["White"], 1, renderer.DashStyle);
                else
                    renderer.D2DContext.DrawLine(new RawVector2(30, y), new RawVector2(30+gWidth, y), renderer.Brushes["White"], 2);
            }
            renderer.D2DContext.DrawLine(new RawVector2(30, y0 - (maxfps-minfps) * ppf), new RawVector2(30, y0), renderer.Brushes["White"], 2);

            renderer.D2DContext.DrawGeometry(fpsline, renderer.Brushes["CornflowerBlue"]);

            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Center;
            renderer.D2DContext.DrawText(frameGraphSize + " frames", renderer.Consolas14,
                new RawRectangleF(30 + gWidth, y0, 30 + gWidth, y0 + 25), renderer.Brushes["White"]);
            foreach (RawVector2 m in marks) {
                renderer.D2DContext.DrawLine(
                    m,
                    new Vector2(m.X, y0), renderer.Brushes["Yellow"], .25f);
            }
            #endregion
        }

        public static void Dispose() {
            vbuffer?.Dispose();
            cbuffer?.Dispose();
        }
    }
}