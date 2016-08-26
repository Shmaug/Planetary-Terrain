using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private Vector3d _position;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000000f;
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

        public void AdjustPositionRelative(Vector3d position, out Vector3d newPos, out double scale) {
            var locationRelativeToCamera = position - Position;
            var distanceFromCamera = locationRelativeToCamera.Length();
            var unscaledViewSpace = zNear + zFar * 0.25;

            if (distanceFromCamera > unscaledViewSpace) {
                var scaledViewSpace = zFar - unscaledViewSpace;
                double f = 1.0 - Math.Exp((scaledViewSpace - distanceFromCamera) / 10000000000);
                double scaledDistanceFromCamera = 
                    unscaledViewSpace + (scaledViewSpace * f);
                Vector3d dirToCam = Vector3d.Normalize(locationRelativeToCamera);
                Vector3d scaledLocationRelativeToCamera = dirToCam * scaledDistanceFromCamera;

                scale = (scaledDistanceFromCamera / distanceFromCamera);
                newPos = scaledLocationRelativeToCamera;
            } else {
                scale = 1;
                newPos = position - Position;
            }
        }
    }
}
