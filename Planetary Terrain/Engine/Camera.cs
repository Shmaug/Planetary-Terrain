using System;
using SharpDX;

namespace Planetary_Terrain {
    class Camera {
        public enum CameraMode {
            Ship, Body
        }

        private Vector3d _position;
        private Vector3 _rotation;
        private Vector3 _rotation2;
        private float _fov, _aspect, _near = 1f, _far = 10000000;
        private Matrix _rotationMatrix = Matrix.Identity;
        private Matrix _view, _proj;
        public Ship Ship;
        private double _zoom = 1;

        #region make functions
        private void makeProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
        }
        private void makeView() {
            _view = Matrix.LookAtLH(Vector3.Zero, _rotationMatrix.Backward, _rotationMatrix.Up);
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
                _rotationMatrix = Matrix.RotationYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
                makeView();
            }
        }
        public Vector3 PostRotation
        {
            get { return _rotation2; }
            set
            {
                _rotation2 = value;
                makeView();
            }
        }
        public CameraMode Mode { get; set; } = CameraMode.Body;

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

        public Matrix RotationMatrix { get { return _rotationMatrix; } }
        public Matrix View { get { return _view; } }
        public Matrix Projection { get { return _proj; } }
        public int BodyIndex { get; set; } = 1;
        public double Zoom {
            get { return _zoom; }
            set {
                _zoom = Math.Min(Math.Max(value, .9), 5);
            }
        }

        public Camera(float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            makeView();
            makeProjection();
        }
        
        public void Update() {
            switch (Mode) {
                case CameraMode.Body:
                    _rotationMatrix = Matrix.RotationAxis(Vector3.Right, Rotation.X) * Matrix.RotationAxis(Vector3.Up, Rotation.Y);
                    _position = StarSystem.ActiveSystem.bodies[BodyIndex].Position + (Vector3d)_rotationMatrix.Forward * StarSystem.ActiveSystem.bodies[BodyIndex].Radius * _zoom;
                    break;
                case CameraMode.Ship:
                    _rotationMatrix = Ship.Rotation * (Matrix.RotationAxis(Ship.Rotation.Right, Rotation.X) * Matrix.RotationAxis(Ship.Rotation.Up, Rotation.Y));
                    _position = Ship.Position + (Vector3d)_rotationMatrix.Forward * (25 + (25 * _zoom));
                    break;
            }
            _rotationMatrix *= Matrix.RotationAxis(_rotationMatrix.Right, PostRotation.X) * Matrix.RotationAxis(_rotationMatrix.Up, PostRotation.Y);

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            Vector3d v = _position - b.Position;
            double l = v.Length();
            v /= l;
            double h = b.GetHeight(v);
            if (h + 2 > l)
                _position = b.Position + v * (h + 2);

            makeView();
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
