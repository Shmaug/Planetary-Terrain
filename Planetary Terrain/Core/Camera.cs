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
        private Quaternion _rotationQuaternion;
        private Quaternion _planetQuaternion;
        private Matrix _view, _proj;

        private void buildProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void buildView() {
            Quaternion q = _planetQuaternion * _rotationQuaternion;
            q.Normalize();
            Vector3 fwd = Vector3.Transform(Vector3.ForwardLH, q);
            Vector3 up = Vector3.Transform(Vector3.Up, q);
            _view = Matrix.LookAtLH(_freezepos, _freezepos + fwd, up);
        }
        private void buildPlanetQuaternion() {
            if (_planet != null) {
                Vector3 pUp = Vector3d.Normalize(Position - _planet.Position);

                float ang = (float)Math.Acos(Vector3.Dot(pUp, Vector3.Up));
                if (ang != 0f) {
                    Vector3 orthoRay = Vector3.Cross(Vector3.Up, pUp);
                    orthoRay.Normalize();
                    _planetQuaternion = Quaternion.RotationAxis(orthoRay, ang);
                    _planetQuaternion.Normalize();
                }
            } else
                _planetQuaternion = Quaternion.Identity;
        }
        private void buildRotation() {
            _rotationQuaternion = Quaternion.RotationYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
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
                buildPlanetQuaternion();
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

        public Quaternion RotationQuaternion { get { return _planetQuaternion * _rotationQuaternion; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }

        public Planet AttachedPlanet { get { return _planet; }
            set {
                if (_planet != value) { // switched view spaces
                    if (value != null) {
                        _planet = value;
                        buildPlanetQuaternion();

                        Quaternion q = _planetQuaternion * Quaternion.Invert(_rotationQuaternion);
                        _rotation = q.ToEuler();
                    }else{
                        buildPlanetQuaternion();

                        Quaternion q = _planetQuaternion * _rotationQuaternion;
                        q.Normalize();
                        _rotation = q.ToEuler();

                        _planet = value;
                    }
                    buildRotation();
                    buildView();
                }
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

            double max = zFar * (1 - Math.Cos(_fov * .5));

            double distance = pos.Length();
            double scaleSpaceStart = max * 0.25d;
            
            if (distance > scaleSpaceStart) {
                double totalScaleSpace = max - scaleSpaceStart;
                double scaledDistanceFromCamera = scaleSpaceStart + (totalScaleSpace * (1d - Math.Exp((totalScaleSpace - distance) / 1000000000d)));
                pos = Vector3d.Normalize(pos) * scaledDistanceFromCamera;

                scale = scaledDistanceFromCamera / distance;
            }
        }
    }
}
