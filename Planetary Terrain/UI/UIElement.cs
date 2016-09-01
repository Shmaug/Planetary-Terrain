using System;
using System.Collections.Generic;
using SharpDX;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain.UI
{
    struct InputState {
        public Vector2 mousePos, lastMousePos;
        public DInput.KeyboardState ks, lastks;
        public DInput.MouseState ms, lastms;
    }
    abstract class UIElement : IDisposable {
        UIElement _parent;
        public UIElement Parent {
            get {
                return _parent;
            }
            set {
                if (value != this) {
                    if (value == null && _parent != null)
                        _parent.Children.Remove(Name);
                    if (value != null && value != _parent)
                        value.Children.Add(Name, this);
                    _parent = value;
                }
            }
        }
        public Dictionary<string, UIElement> Children = new Dictionary<string, UIElement>();

        public object Tag;

        public string Name;
        public bool Visible = true;
        private RawRectangleF _bounds;
        public RawRectangleF LocalBounds
        {
            get { return _bounds; }
            set { _bounds = value; ComputeBounds(); }
        }
        public RawRectangleF AbsoluteBounds { get; private set; }

        public UIElement this[string n]
        {
            get
            {
                return Children[n];
            }
        }

        internal UIElement(UIElement parent, string name, RawRectangleF bounds) {
            Name = name;
            Parent = parent;
            LocalBounds = bounds;
            ComputeBounds();
        }

        public void Translate(Vector2 delta) {
            Translate(delta.X, delta.Y);
        }
        public void Translate(float x, float y) {
            LocalBounds = new RawRectangleF(LocalBounds.Left + x, LocalBounds.Top + y, LocalBounds.Right + x, LocalBounds.Bottom + y);
        }

        public void ComputeBounds() {
            float left, top;
            left = top = 0;
            if (Parent != null) {
                RawRectangleF b = Parent.AbsoluteBounds;
                left = b.Left;
                top = b.Top;
            }

            AbsoluteBounds = new RawRectangleF(LocalBounds.Left + left, LocalBounds.Top + top, LocalBounds.Right + left, LocalBounds.Bottom + top);

            foreach (KeyValuePair<string, UIElement> kp in Children)
                if (kp.Value != this && kp.Value != Parent)
                    kp.Value.ComputeBounds();
        }
        
        public bool Contains(Vector2 pt) {
            return Contains(pt.X, pt.Y);
        }
        public bool Contains(float x, float y) {
            return AbsoluteBounds.Contains(x, y);
        }
        public bool IntersectsChildren(Vector2 pt) {
            return IntersectsChildren(pt.X, pt.Y);
        }
        public bool IntersectsChildren(float x, float y) {
            if (!Contains(x, y)) return false;
            foreach (KeyValuePair<string, UIElement> kp in Children)
                if (kp.Value.Visible && kp.Value != this && kp.Value != Parent)
                    if (!(kp.Value is TextButton) && (kp.Value.Contains(x, y) || kp.Value.IntersectsChildren(x, y)))
                        return true;
            return false;
        }
        
        public virtual void Update(float time, InputState inputState) {
            foreach (KeyValuePair<string, UIElement> kp in Children)
                if (kp.Value.Visible && kp.Value != this && kp.Value != Parent)
                    kp.Value.Update(time, inputState);
        }
        public virtual void Draw(Renderer renderer) {
            foreach (KeyValuePair<string, UIElement> kp in Children)
                if (kp.Value.Visible && kp.Value != this && kp.Value != Parent)
                    kp.Value.Draw(renderer);
        }

        public virtual void Dispose() {
            foreach (KeyValuePair<string, UIElement> kp in Children)
                if (kp.Value != this && kp.Value != Parent)
                    kp.Value.Dispose();
        }
    }
}
