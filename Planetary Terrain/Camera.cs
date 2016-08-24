using System;
using SharpDX;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace BetterTerrain {
    class Camera : IDisposable {
        [StructLayout(LayoutKind.Explicit)]
        struct Constants {
            [FieldOffset(0)]
            public Matrix View;

            [FieldOffset(64)]
            public Matrix Projection;

            [FieldOffset(128)]
            public Vector3 cameraPosition;

            [FieldOffset(144)]
            public Vector3 cameraDirection;

            [FieldOffset(160)]
            public Vector3 lightDirection;

            [FieldOffset(172)]
            public float spacer;
        }

        private Vector3 _position, _rotation;
        private float _fov, _aspect, _near = 1f, _far = 2000f;
        private Matrix _rotationmat, _view, _proj;
        private BoundingFrustum _frustum;

        Constants constants;
        public D3D11.Buffer ConstantBuffer { get; private set; }

        private void buildProjection() {
            _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
            constants.Projection = Matrix.Transpose(_proj);
            _frustum = new BoundingFrustum(_view * _proj);
        }
        private void buildView() {
            Matrix rmat = RotationMatrix;
            _view = Matrix.LookAtLH(Position, Position + rmat.Forward, rmat.Up);
            constants.View = Matrix.Transpose(_view);
            _frustum = new BoundingFrustum(_view * _proj);

            constants.cameraPosition = _position;
            constants.cameraDirection = _rotationmat.Forward;
        }
        
        public Vector3 Position { get { return _position; }
            set {
                _position = value;
                buildView();
            }
        }
        public Vector3 Rotation { get { return _rotation; }
            set {
                _rotation = value;
                _rotationmat = MathTools.RotationXYZ(_rotation);
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
        public BoundingFrustum Frustum { get { return _frustum; } }

        public Camera(D3D11.Device device, float fieldOfView, float aspectRatio) {
            _fov = fieldOfView;
            _aspect = aspectRatio;

            constants = new Constants();
            constants.lightDirection = new Vector3(0, 0, 1);
            constants.lightDirection.Normalize();
            ConstantBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.ConstantBuffer, ref constants);

            buildView();
            buildProjection();
        }

        public void UpdateSubresource(D3D11.DeviceContext context) {
            context.UpdateSubresource(ref constants, ConstantBuffer);
        }

        public void Dispose() {
            if (ConstantBuffer != null)
                ConstantBuffer.Dispose();
        }
    }
}
