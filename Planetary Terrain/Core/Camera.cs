using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        bool frozen = false;
        Vector3d freezepos;
        public bool Frozen { get { return frozen; }
            set
            {
                frozen = value;
                freezepos = Vector3.Zero;
            }
        }

        public enum CameraMode {
            Surface, Orbital
        }

        private CameraMode _fromMode;
        private CameraMode _mode;
        private float transitionTimer;

        private Vector3d _position;
        private Body _planet;
        private Vector3 _rotation;
        private float _fov, _aspect, _near = 1f, _far = 10000000;
        private Quaternion _rotationQuaternion, _bodyQuaternion;
        private Matrix _view, _proj;

        #region make functions
        private void makeProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void makeView() {
            Quaternion q = RotationQuaternion;
            Vector3 fwd = Vector3.Transform(Vector3.ForwardLH, q);
            Vector3 up = Vector3.Transform(Vector3.Up, q);
            _view = Matrix.LookAtLH(freezepos, freezepos + fwd, up);
        }
        private void makeRotation() {
            Matrix3x3 mat = Matrix3x3.RotationX(_rotation.X) * Matrix3x3.RotationY(_rotation.Y);
            Quaternion.RotationMatrix(ref mat, out _rotationQuaternion);
        }
        private Quaternion getBodyQuaternion(CameraMode mode) {
            Quaternion q = Quaternion.Identity;
            Vector3 pUp = Vector3.Up;
            if (_planet != null) {
                switch (mode) {
                    case CameraMode.Surface:
                        pUp = Vector3d.Normalize(_position - _planet.Position);
                        break;
                    case CameraMode.Orbital:
                        pUp = _planet.North;
                        break;
                }
            }

            float ang = (float)Math.Acos(Vector3.Dot(pUp, Vector3.Up));
            if (ang != 0f) {
                Vector3 orthoRay = Vector3.Cross(Vector3.Up, pUp);
                orthoRay.Normalize();
                q = Quaternion.RotationAxis(orthoRay, ang);
            }
            q.Normalize();

            return q;
        }
        #endregion

        public double Speed = 3;
        public double SpeedMultiplier = 1;
        public Vector3d Position { get { return _position; }
            set { _position = value; }
        }

        public CameraMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode != value) {
                    transitionTimer = 1f;
                    _fromMode = _mode;
                }
                _mode = value;
            }
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

        public Quaternion RotationQuaternion { get { return _bodyQuaternion * _rotationQuaternion; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }

        public Body NearestBody { get { return _planet; } set { _planet = value; } }

        public Camera(float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            makeView();
            makeProjection();
        }

        public void Update(float deltaTime) {
            _bodyQuaternion = Quaternion.Identity;
            
            if (transitionTimer > 0) {
                _bodyQuaternion = Quaternion.Lerp(getBodyQuaternion(_fromMode), getBodyQuaternion(_mode), 1f - transitionTimer);
                transitionTimer -= deltaTime;
            } else
                _bodyQuaternion = getBodyQuaternion(_mode);
            
            makeView();
        }

        public void Translate(Vector3 delta) {
            if (frozen)
                freezepos += delta;
            else
                Position += delta;
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
