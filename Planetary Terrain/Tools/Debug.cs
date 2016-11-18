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
        public long ParentTickOffset = 0;

        public Profiler(string name) {
            Name = name;
            Children = new List<Profiler>();
            Stopwatch = new Stopwatch();
        }

        public static Profiler Begin(string name = "") {
#if DEBUG
            if (ActiveProfiler == null) {
                ActiveProfiler = new Profiler(name);
                ActiveProfiler.Stopwatch.Start();
            }else {
                Profiler p = new Profiler(name);
                p.ParentTickOffset = ActiveProfiler.Stopwatch.Elapsed.Ticks;
                p.Parent = ActiveProfiler;
                ActiveProfiler.Children.Add(p);
                ActiveProfiler = p;
                p.Stopwatch.Start();
            }
            return ActiveProfiler;
#else
            return null;
#endif
        }
        public static bool Resume(string name) {
#if DEBUG
            ActiveProfiler?.Stopwatch.Start();
            foreach (Profiler p in ActiveProfiler.Children) {
                if (p.Name == name) {
                    ActiveProfiler = p;
                    p.Stopwatch.Start();
                    return true;
                }
            }
            return false;
#else
            return true;
#endif
        }
        public static void End() {
#if DEBUG
            ActiveProfiler?.Stopwatch.Stop();
            ActiveProfiler = ActiveProfiler.Parent;
#endif
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

                //if (p.ParentTickOffset > 0)
                //    x += (int)((p.ParentTickOffset / (double)Stopwatch.Elapsed.Ticks) * (rect.Right - rect.Left));

                int w = (int)((p.Stopwatch.Elapsed.Ticks / (double)Stopwatch.Elapsed.Ticks) * (rect.Right - rect.Left));
                p.Draw(renderer, new RawRectangleF(rect.Left + x, rect.Top, rect.Left + x + w, rect.Bottom), c, textx + 10, ref texty);
                
                x += w;
            }

            if (Children.Count > 0)
                renderer.D2DContext.FillRectangle(new RawRectangleF(textx-3, rect.Bottom + lineA, textx-2, rect.Bottom + texty - 2), renderer.Brushes[Colors[lineC % Colors.Length]]);
        }

        public void DrawCircle(Renderer renderer, Vector2 center, float radius) {
            D2D1.PathGeometry outline = null;
            DrawCircle(renderer, center, radius, radius * .05f, 0, Math.PI * 2, ref outline, 0, 0);
            if (outline != null) {
                renderer.D2DContext.DrawGeometry(outline, renderer.Brushes["Black"], 2);
                outline.Dispose();
            }
        }
        public void DrawCircle(Renderer renderer, Vector2 center, float radius, float step, double a, double b, ref D2D1.PathGeometry outline, int col, float txt) {
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
            s.Dispose();

            renderer.D2DContext.FillGeometry(path, renderer.Brushes[Colors[col % Colors.Length]]);

            if (path.FillContainsPoint(Input.MousePos, 1)) {
                if (txt == 0)
                    txt = radius + 50;
                RawRectangleF r = new RawRectangleF(center.X - 100, center.Y - txt, center.X + 100, center.Y - txt + 16);
                renderer.D2DContext.FillRectangle(r, renderer.Brushes["TransparentBlack"]);
                renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
                renderer.D2DContext.DrawText(
                    Name + " (" + Stopwatch.Elapsed.TotalMilliseconds.ToString("F1") + "ms)",
                    renderer.Consolas14, r, renderer.Brushes[Colors[col % Colors.Length]], D2D1.DrawTextOptions.None, D2D1.MeasuringMode.GdiNatural);

                txt += 16;

                outline = path;
            } else
                path.Dispose();
            
            double t = 0;
            foreach (Profiler p in Children) {
                col++;

                //if (p.ParentTickOffset > 0)
                //    t += (p.ParentTickOffset / (double)Stopwatch.Elapsed.Ticks) * (b - a);

                double f = (p.Stopwatch.Elapsed.Ticks / (double)Stopwatch.Elapsed.Ticks) * (b - a);
                p.DrawCircle(renderer, center, radius - step, step, a + t, a + t + f, ref outline, col, txt);
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
            public float FrameTimeMS;
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
        public static int NodesDrawn;
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

        public static void BeginFrame() {
#if DEBUG
            TrianglesDrawn = 0;
            TreesDrawn = 0;
            ImposterDrawn = 0;
            NodesDrawn = 0;
            immediateTrack.Clear();
            frameMarked = false;
            lines.Clear();
            boxes.Clear();
#endif
        }
        public static void EndFrame(double frameTime) {
#if DEBUG
            frameGraph.Add(new FrameSnapshot() { RealFPS = FPS, FrameTimeMS = (float)(frameTime * 1000), FPS = (float)(1.0 / frameTime), mark = frameMarked });
            if (frameGraph.Count > frameGraphSize)
                frameGraph.RemoveAt(0);
#endif
        }
        public static void MarkFrame() {
            frameMarked = true;
        }
        
        public static void DrawLine(Color color, params Vector3d[] points) {
#if DEBUG
            lines.Add(new Line() { color = color, points = points });
#endif
        }
        public static void DrawBox(Color color, OrientedBoundingBox oob) {
#if DEBUG
            boxes.Add(new Box() { oob = oob, color = color });
#endif
        }

        public static void Draw3D(Renderer renderer) {
            if (!DrawDebug) return;
            Profiler.Begin("Debug 3d Draw");

            Matrix m = Matrix.Identity;
            float[] b = new float[20] {
                    1,0,0,0, // Matrix
                    0,1,0,0, // Matrix
                    0,0,1,0, // Matrix
                    0,0,0,1, // Matrix
                    1,1,1,1, // Color
                };
            cbuffer?.Dispose();
            cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, b);

            Shaders.Colored.Set(renderer);
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
            renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

            Random r = new Random();
            foreach (Line line in lines) {
                List<VertexColor> verts = new List<VertexColor>();
                for (int i = 0; i < line.points.Length; i++) {
                    double s;
                    Vector3d p;
                    renderer.ActiveCamera.GetScaledSpace(line.points[i], out p, out s);
                    
                    verts.Add(new VertexColor(p, line.color));

                    // tesselate line if its got gaps (to reduce depth error from logarithmic depth buffer)
                    if (i + 1 < line.points.Length) {
                        Vector2 sp1 = (Vector2)renderer.WorldToScreen(line.points[i], renderer.MainCamera);
                        Vector2 sp2 = (Vector2)renderer.WorldToScreen(line.points[i + 1], renderer.MainCamera);
                        
                        double d = (sp1 - sp2).Length();
                        if (d > 10) {
                            d = Math.Min(d, 100);
                            double s2;
                            Vector3d p2;
                            renderer.ActiveCamera.GetScaledSpace(line.points[i + 1], out p2, out s2);
                            for (double t = 0; t < 1; t += 10 / d)
                                verts.Add(new VertexColor(Vector3d.Lerp(p, p2, t), line.color));
                        }
                    }
                }

                vbuffer?.Dispose();
                vbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.VertexBuffer, verts.ToArray());
                
                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vbuffer, Utilities.SizeOf<VertexColor>(), 0));

                renderer.Context.Draw(verts.Count, 0);

            }

            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            foreach (Box box in boxes) {
                m = Matrix.Scaling(box.oob.Extents) * box.oob.Transformation;
                b = new float[20] {
                    m.M11,m.M12,m.M13,m.M14,
                    m.M21,m.M22,m.M23,m.M24,
                    m.M31,m.M32,m.M33,m.M34,
                    m.M41,m.M42,m.M43,m.M44,
                    box.color.R/255f, box.color.G/255f, box.color.B/255f, box.color.A/255f,
                };
                cbuffer?.Dispose();
                cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, b);

                renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
                renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

                renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.BoundingBoxVertexBuffer, Utilities.SizeOf<VertexColor>(), 0));

                renderer.Context.Draw(24, 0);
            }

            Profiler.End();
        }
        public static void Draw2D(Renderer renderer) {
            if (!DrawDebug) return;
            Profiler.Begin("Debug 2d Draw");

            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Leading;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            float line = 1;
            renderer.D2DContext.DrawText(
                string.Format("{0} triangles, {1} nodes, {2} trees/{3} imposters [{4} waiting / {5} generating]",
                TrianglesDrawn.ToString("N0"), NodesDrawn.ToString("N0"), TreesDrawn.ToString("N0"), ImposterDrawn.ToString("N0"), QuadNode.GenerateQueue.Count, QuadNode.Generating.Count),
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

            #region graph
            float min = frameGraph[0].FrameTimeMS;
            float max = frameGraph[0].FrameTimeMS;
            for (int i = 0; i < frameGraph.Count; i++) {
                min = Math.Min(min, frameGraph[i].FrameTimeMS);
                max = Math.Max(max, frameGraph[i].FrameTimeMS);
            }
            float step = Math.Max((int)((max - min) / 10 / .5f) * .5f, .5f);
            min = (int)Math.Floor(min / step) * step;
            max = (int)Math.Ceiling(max / step) * step;

            RawRectangleF grect = new RawRectangleF(renderer.ResolutionX - 360, 10, renderer.ResolutionX - 10, 360);
            float xScale = (grect.Right - grect.Left - 30) / frameGraphSize;
            float ppf = (grect.Bottom - grect.Top - 20) / (max - min); // pixels per ms

            RawVector2[] pts = new RawVector2[frameGraph.Count];
            List<RawVector2> marks = new List<RawVector2>();
            for (int i = 0; i < frameGraph.Count; i++) {
                pts[i] = new RawVector2(grect.Left + 30 + i * xScale, MathUtil.Clamp(grect.Bottom - 20 - (frameGraph[i].FrameTimeMS - min) * ppf, grect.Top, grect.Bottom - 20));
            }
            D2D1.PathGeometry graphline = new D2D1.PathGeometry(renderer.D2DFactory);
            D2D1.GeometrySink graphsink = graphline.Open();
            graphsink.SetFillMode(D2D1.FillMode.Winding);
            graphsink.BeginFigure(pts[0], D2D1.FigureBegin.Hollow);
            graphsink.AddLines(pts);
            graphsink.EndFigure(D2D1.FigureEnd.Open);
            graphsink.Close();

            renderer.D2DContext.FillRectangle(grect, renderer.Brushes["TransparentBlack"]);

            renderer.D2DContext.DrawLine( // y axis
                new RawVector2(grect.Left + 30, grect.Bottom - (max - min) * ppf - 20),
                new RawVector2(grect.Left + 30, grect.Bottom - 20), renderer.Brushes["White"], 2);
                renderer.D2DContext.DrawLine(
                    new RawVector2(grect.Left + 30, grect.Bottom - 20),
                    new RawVector2(grect.Right, grect.Bottom - 20), renderer.Brushes["White"], 2); // x axis

            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Trailing;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Center;
            for (float ms = min; ms <= max; ms += step) {
                float y = grect.Bottom - 20 - (ms - min) * ppf;

                // y axis numbers
                if (ms.ToString().Length <= 3)
                    renderer.D2DContext.DrawText(ms.ToString(), renderer.Consolas14,
                        new RawRectangleF(grect.Left, y, grect.Left + 25, y), renderer.Brushes["White"]);
                
                if (ms > min)
                    renderer.D2DContext.DrawLine(new RawVector2(grect.Left + 28, y), new RawVector2(grect.Right, y), renderer.Brushes["White"], .25f);
            }
            
            renderer.D2DContext.DrawGeometry(graphline, renderer.Brushes["CornflowerBlue"]); // graph line

            // x axis label
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Trailing;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Far;
            renderer.D2DContext.DrawText(frameGraphSize + " frames", renderer.Consolas14,
                new RawRectangleF(grect.Right, grect.Bottom, grect.Right, grect.Bottom), renderer.Brushes["White"]);
            
            // y axis label
            renderer.Consolas14.TextAlignment = DWrite.TextAlignment.Center;
            renderer.Consolas14.ParagraphAlignment = DWrite.ParagraphAlignment.Near;
            renderer.D2DContext.DrawText("Draw+Update Time (ms)", renderer.Consolas14, // y axis label
                new RawRectangleF(grect.Left + 30, grect.Top, grect.Right, grect.Top), renderer.Brushes["White"]);
            #endregion
            
            Profiler.End();
        }

        public static void DrawTexture(Renderer renderer, Vector4 pos, D3D11.ShaderResourceView texture) {
            Shaders.Blur.Set(renderer);
            
            cbuffer?.Dispose();
            cbuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref pos);

            renderer.Context.PixelShader.SetShaderResource(0, texture);

            renderer.Context.VertexShader.SetConstantBuffer(1, cbuffer);
            renderer.Context.PixelShader.SetConstantBuffer(1, cbuffer);

            renderer.Context.Rasterizer.State = renderer.rasterizerStateSolidNoCull;
            renderer.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            renderer.Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(Resources.QuadVertexBuffer, sizeof(float) * 5, 0));
            renderer.Context.InputAssembler.SetIndexBuffer(Resources.QuadIndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            renderer.Context.DrawIndexed(6, 0, 0);
        }

        public static void Dispose() {
            vbuffer?.Dispose();
            cbuffer?.Dispose();
        }
    }
}