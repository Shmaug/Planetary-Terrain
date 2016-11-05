using System;
using System.Linq;
using SharpDX;
using SharpDX.Mathematics.Interop;
using DWrite = SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;

namespace Planetary_Terrain.UI
{
    class TextButton : UIElement
    {
        public string Text;
        public DWrite.TextFormat TextFormat;
        public DWrite.ParagraphAlignment ParagraphAlignment = DWrite.ParagraphAlignment.Center;
        public DWrite.TextAlignment TextAlignment = DWrite.TextAlignment.Center;
        public D2D1.Brush Brush1;
        public D2D1.Brush Brush2;
        public Action Click;
        float hoverTime;

        public TextButton(UIElement parent, string name, RawRectangleF bounds, string text, DWrite.TextFormat textFormat, D2D1.Brush brush1, D2D1.Brush brush2, Action action) : base(parent, name, bounds) {
            Text = text;
            TextFormat = textFormat;
            Brush1 = brush1;
            Brush2 = brush2;
            Click = action;
        }

        public override void Update(float time) {
            if (AbsoluteBounds.Contains(Input.mousePos.X, Input.mousePos.Y)) {
                hoverTime += time;
            } else
                hoverTime = 0f;

            if (hoverTime > 0 && Input.lastms.Buttons[0] && !Input.ms.Buttons[0])
                Click();

            base.Update(time);
        }

        public override void Draw(Renderer renderer) {
            TextFormat.ParagraphAlignment = ParagraphAlignment;
            TextFormat.TextAlignment = TextAlignment;
            
            renderer.D2DContext.FillRectangle(AbsoluteBounds, hoverTime > 0 ? Brush1 : Brush2);
            renderer.D2DContext.DrawText(Text, TextFormat, AbsoluteBounds, hoverTime > 0 ? Brush2 : Brush1);
            
            base.Draw(renderer);
        }

        public override void Dispose() {
            Brush1?.Dispose();
            Brush2?.Dispose();
            base.Dispose();
        }
    }
}
