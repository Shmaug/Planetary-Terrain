using SharpDX;
using SharpDX.Mathematics.Interop;
using DWrite = SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;

namespace Planetary_Terrain.UI
{
    class TextLabel : UIElement {
        public string Text;
        public DWrite.TextFormat TextFormat;
        public D2D1.Brush TextBrush;
        public DWrite.ParagraphAlignment ParagraphAlignment = DWrite.ParagraphAlignment.Center;
        public DWrite.TextAlignment TextAlignment = DWrite.TextAlignment.Center;

        public TextLabel(UIElement parent, string name, RawRectangleF bounds, string text, DWrite.TextFormat textLayout, D2D1.Brush brush) : base(parent, name, bounds) {
            Text = text;
            TextFormat = textLayout;
            TextBrush = brush;
        }

        public override void Draw(Renderer renderer) {
            TextFormat.ParagraphAlignment = ParagraphAlignment;
            TextFormat.TextAlignment = TextAlignment;

            renderer.D2DContext.DrawText(Text, TextFormat, AbsoluteBounds, TextBrush);

            base.Draw(renderer);
        }

        public override void Dispose() {
            if (TextBrush != null)
                TextBrush.Dispose();
            base.Dispose();
        }
    }
}
