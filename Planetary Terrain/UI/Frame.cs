using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;

namespace Planetary_Terrain.UI
{
    class Frame : UIElement
    {
        public D2D1.Brush Brush;
        public bool Draggable = false;
        bool dragging = false;

        public Frame(UIElement parent, string name, RawRectangleF bounds, D2D1.Brush bg) : base(parent, name, bounds) {
            Brush = bg;
        }

        public override void Update(float time) {
            if (Draggable) {
                if (!Input.lastms.Buttons[0] && Input.ms.Buttons[0])
                    if (Contains(Input.MousePos.X, Input.MousePos.Y) && !IntersectsChildren(Input.ms.X, Input.ms.Y))
                        dragging = true;

                if (!Input.ms.Buttons[0])
                    dragging = false;

                if (dragging && Input.lastms.Buttons[0])
                    Translate(Input.MousePos - Input.LastMousePos);
            }

            base.Update(time);
        }

        public override void Draw(Renderer renderer) {
            if (Brush != null)
                renderer.D2DContext.FillRectangle(AbsoluteBounds, Brush);

            base.Draw(renderer);
        }

        public override void Dispose() {
            Brush?.Dispose();
            base.Dispose();
        }
    }
}
