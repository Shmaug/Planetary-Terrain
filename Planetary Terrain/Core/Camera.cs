using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private bool _frozen;
        private Vector3d _freezepos;
        private Vector3d _position;
        private Planet _planet;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000f;
        private Matrix3x3 _rotationmat;
        private Matrix _view, _proj;

        private void buildProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void buildView() {
            Vector3 fwd = Vector3.Transform(Vector3.ForwardLH, _rotationmat);
            Vector3 up = Vector3.Transform(Vector3.Up, _rotationmat);
            _view = Matrix.LookAtLH(_freezepos, _freezepos + fwd, up);
        }
        private void buildRotation() {
            if (_planet != null) {
                Vector3 vUp = Vector3d.Normalize(Position - _planet.Position);
                Vector3 vFwd = Vector3d.Normalize(Position - _planet.Position); // TODO: this should face north tangent to the planet's surface

                _rotationmat = Matrix3x3.LookAtLH(Vector3.Zero, vFwd, vUp) *
                    Matrix3x3.RotationX(_rotation.X) *
                    Matrix3x3.RotationY(_rotation.Y);
            } else
                _rotationmat = Matrix3x3.RotationZ(_rotation.Z) * Matrix3x3.RotationX(_rotation.X) * Matrix3x3.RotationY(_rotation.Y);
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
                buildRotation();
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

        public Matrix3x3 RotationMatrix { get { return _rotationmat; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }

        public Planet AttachedPlanet { get { return _planet; }
            set {
                _planet = value;
                buildRotation();
                buildView();
            }
        }

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
