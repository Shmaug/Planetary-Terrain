using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private Vector3d _position;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000000;
        private Matrix3x3 _rotationMatrix;
        private Matrix _view, _proj;

        #region make functions
        private void makeProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void makeView() {
            Vector3 fwd = Vector3.Transform(Vector3.ForwardLH, _rotationMatrix);
            Vector3 up = Vector3.Transform(Vector3.Up, _rotationMatrix);
            _view = Matrix.LookAtLH(Vector3.Zero, fwd, up);
        }
        #endregion

        public Vector3d Position {
            get { return _position; }
            set { _position = value; }
        }
        public Vector3 Rotation {
            get { return _rotation; }
            set {
                _rotation = value;
                _rotationMatrix = Matrix3x3.RotationYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
                makeView();
            }
        }

        #region projection parameters
        public float FieldOfView { get { return _fov; }
            set
            {
                _fov = value;
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

        public Matrix3x3 RotationMatrix { get { return _rotationMatrix; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }
        
        public Camera(float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            makeView();
            makeProjection();
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
