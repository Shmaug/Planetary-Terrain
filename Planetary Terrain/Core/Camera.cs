using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        private Vector3d _position;
        private Planet _planet;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000f;
        private Quaternion _rotationQuaternion, _planetQuaternion;
        private Matrix _view, _proj;

        #region make functions
        private void makeProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void makeView() {
            Quaternion q = _planetQuaternion * _rotationQuaternion;
            q.Normalize();
            Vector3 fwd = Vector3.Transform(Vector3.ForwardLH, q);
            Vector3 up = Vector3.Transform(Vector3.Up, q);
            _view = Matrix.LookAtLH(Vector3.Zero, fwd, up);
        }
        private void makePlanetQuaternion(float time) {
            Quaternion q = Quaternion.Identity;
            if (_planet != null) {
                Vector3 pUp = Vector3d.Normalize(_position - _planet.Position);

                float ang = (float)Math.Acos(Vector3.Dot(pUp, Vector3.Up));
                if (ang != 0f) {
                    Vector3 orthoRay = Vector3.Cross(Vector3.Up, pUp);
                    orthoRay.Normalize();
                    q = Quaternion.RotationAxis(orthoRay, ang);
                    q.Normalize();
                }
            }

            _planetQuaternion = Quaternion.Lerp(_planetQuaternion, q, .9f);
            _planetQuaternion.Normalize();

            makeView();
        }
        private void makeRotation() {
            _rotationQuaternion = Quaternion.RotationYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
        }
        #endregion

        public Vector3d Position { get { return _position; }
            set { _position = value; }
        }
        public Vector3 Rotation { get { return _rotation; }
            set {
                _rotation = value;
                _rotation.X = MathUtil.Clamp(_rotation.X, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);

                makeRotation();
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

        public Quaternion RotationQuaternion { get { return _planetQuaternion * _rotationQuaternion; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }

        public Planet AttachedPlanet {
            get { return _planet; }
            set { _planet = value; }
        }

        public Camera(float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            makeView();
            makeProjection();
        }

        public void Update(float deltaTime) {
            makePlanetQuaternion(deltaTime);
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
