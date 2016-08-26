using System;
using SharpDX;

namespace BetterTerrain {
    class Camera {
        private Vector3d _position;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = .01f, _far = 1000f;
        private Matrix _rotationmat, _view, _proj;

        private void buildProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void buildView() {
            Matrix rmat = RotationMatrix;
            _view = Matrix.LookAtLH(Vector3.Zero, rmat.Forward, rmat.Up);
        }
        
        public Vector3d Position { get { return _position; }
            set {
                _position = value;
                buildView();
            }
        }
        public Vector3 Rotation { get { return _rotation; }
            set {
                _rotation = value;
                _rotationmat = Matrix.RotationZ(_rotation.Z) * Matrix.RotationX(_rotation.X) * Matrix.RotationY(_rotation.Y);
                buildView();
            }
        }

        public float FieldOfView { get { return _fov; }
            set
            {
                _fov = value;
                buildProjection();
            }
        }
        public float AspectRatio { get { return _aspect; }
            set {
                _aspect = value;
                buildProjection();
            }
        }
        public float zNear { get { return _near; }
            set {
                _near = value;
                buildProjection();
            }
        }
        public float zFar { get { return _far; }
            set {
                _far = value;
                buildProjection();
            }
        }

        public Matrix RotationMatrix { get { return _rotationmat; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }

        public Camera(float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            buildView();
            buildProjection();
        }
    }
}
