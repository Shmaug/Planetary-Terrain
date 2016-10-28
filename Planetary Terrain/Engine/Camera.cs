using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private Vector3d _position;
        private float _fov, _fovy, _aspect, _near = 1f, _far = 10000000;
        private Matrix _rotation = Matrix.Identity, _view, _proj;
        private BoundingFrustum _frustum;
        float _orthoSize;

        #region make functions
        private void makeProjection() {
            if (Orthographic)
                _proj = Matrix.OrthoOffCenterLH(-_orthoSize*.5f*_aspect, -_orthoSize * .5f, _orthoSize * .5f * _aspect, _orthoSize * .5f, _near, _far);
            else
                _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);

            _frustum = new BoundingFrustum(_view * _proj);
        }
        private void makeView() {
            _view = Matrix.LookAtLH(Vector3.Zero, _rotation.Backward, _rotation.Up);
            _frustum = new BoundingFrustum(_view * _proj);
        }
        #endregion

        public Vector3d Position {
            get { return _position; }
            set { _position = value; }
        }
        public Matrix Rotation
        {
            get { return _rotation; }
            set { _rotation = value; makeView(); }
        }

        #region projection parameters
        public float FieldOfView { get { return _fov; }
            set
            {
                _fov = value;
                _fovy = (float)(2 * Math.Atan(Math.Tan(_fov * .5) * _aspect));

                makeProjection();
            }
        }
        public float AspectRatio { get { return _aspect; }
            set {
                _aspect = value;
                makeProjection();
            }
        }
        public float zNear { get { return _near; }
            set {
                _near = value;
                makeProjection();
            }
        }
        public float zFar { get { return _far; }
            set {
                _far = value;
                makeProjection();
            }
        }
        #endregion

        public bool Orthographic;
        public float OrthographicSize { get { return _orthoSize; } set { _orthoSize = value; makeProjection(); } }
        
        public Matrix View {
            get { return _view; }
            set {
                _view = value;
                _frustum = new BoundingFrustum(_view * _proj);
            }
        }
        public Matrix Projection { get { return _proj; } }
        public BoundingFrustum Frustum { get { return _frustum; } }
        public float VerticalFieldOfView { get { return _fovy; } }

        public static Camera PerspectiveCamera(float fieldOfView, float aspectRatio) {
            Camera c = new Camera();
            c._aspect = aspectRatio;
            c.FieldOfView = fieldOfView;
            c.Orthographic = false;

            c.makeView();
            c.makeProjection();

            return c;
        }
        public static Camera OrthoCamera(float orthoSize, float aspectRatio) {
            Camera c = new Camera();
            c._aspect = aspectRatio;
            c._orthoSize = orthoSize;
            c.Orthographic = true;

            c.makeView();
            c.makeProjection();

            return c;
        }

        public void GetScaledSpace(Vector3d location, out Vector3d pos, out double scale) {
            scale = 1.0;
            pos = location - Position;

            double x = pos.Length();

            double f = zFar * (1.0 - Math.Cos(_fov * .5));
            double p = .5 * f;
            
            if (x > p) {
                double s = 1.0 - Math.Exp(-p / (x - p));
                double dist = p + (f - p) * s;
                
                scale = dist / x;
                pos = Vector3d.Normalize(pos) * dist;
            }
        }
    }
}
