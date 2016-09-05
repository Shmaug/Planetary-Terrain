﻿using System;
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
        public Action action;
        float hoverTime;

        public TextButton(UIElement parent, string name, RawRectangleF bounds, string text, DWrite.TextFormat textFormat, D2D1.Brush brush1, D2D1.Brush brush2, Action action) : base(parent, name, bounds) {
            Text = text;
            TextFormat = textFormat;
            Brush1 = brush1;
            Brush2 = brush2;
            this.action = action;
        }

        public override void Update(float time, InputState inputState) {
            if (AbsoluteBounds.Contains(inputState.mousePos.X, inputState.mousePos.Y)) {
                hoverTime += time;
            } else
                hoverTime = 0f;

            if (hoverTime > 0 && inputState.lastms.Buttons[0] && !inputState.ms.Buttons[0])
                action();

            base.Update(time, inputState);
        }

        public override void Draw(Renderer renderer) {
            TextFormat.ParagraphAlignment = ParagraphAlignment;
            TextFormat.TextAlignment = TextAlignment;
            
            renderer.D2DContext.FillRectangle(AbsoluteBounds, hoverTime > 0 ? Brush1 : Brush2);
            renderer.D2DContext.DrawText(Text, TextFormat, AbsoluteBounds, hoverTime > 0 ? Brush2 : Brush1);
            
            base.Draw(renderer);
        }

        public override void Dispose() {
            if (Brush1 != null)
                Brush1.Dispose();
            if (Brush2 != null)
                Brush2.Dispose();
            base.Dispose();
        }
    }
}