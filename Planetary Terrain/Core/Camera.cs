using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private bool _frozen;
        private Vector3d _freezepos;
        private Vector3d _position;

        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000f;
        private Matrix _rotationmat, _view, _proj;

        private void buildProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void buildView() {
            Matrix rmat = RotationMatrix;
            _view = Matrix.LookAtLH(_freezepos, _freezepos + rmat.Forward, rmat.Up);
        }
        
        public bool Frozen { get { return _frozen; }
            set
            {
                _freezepos = new Vector3d();
                _frozen = value;
            }
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

        public void Translate(Vector3d delta) {
            if (Frozen)
                _freezepos += delta;
            else
                _position += delta;
        }
        
        public void AdjustPositionRelative(Vector3d location, out Vector3d pos, out double scale) {
            scale = 1d;
            pos = location - Position;

            double distance = pos.Length();
            double scaleSpaceStart = zFar * 0.25d;
            
            if (distance > scaleSpaceStart) {
                double totalScaleSpace = zFar - scaleSpaceStart;
                double scaledDistanceFromCamera = scaleSpaceStart + (totalScaleSpace * (1d - Math.Exp((totalScaleSpace - distance) / 1000000000d)));
                pos = Vector3d.Normalize(pos) * scaledDistanceFromCamera;

                scale = scaledDistanceFromCamera / distance;
            }
        }
    }
}
