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

        public override void Update(float time, InputState inputState) {
            if (Draggable) {
                if (!inputState.lastms.Buttons[0] && inputState.ms.Buttons[0])
                    if (Contains(inputState.mousePos.X, inputState.mousePos.Y) && !IntersectsChildren(inputState.ms.X, inputState.ms.Y))
                        dragging = true;

                if (!inputState.ms.Buttons[0])
                    dragging = false;

                if (dragging && inputState.lastms.Buttons[0])
                    Translate(inputState.mousePos - inputState.lastMousePos);
            }

            base.Update(time, inputState);
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
