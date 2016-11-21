using System;
using SharpDX;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;

namespace Planetary_Terrain {
    class Camera {
        private Vector3d _position;
        private float _fov, _fovy, _aspect, _near = 1f, _far = 1000000;
        private double _cosfov2;
        private float _orthoSize;
        private bool _ortho = false;
        private Matrix _rotation = Matrix.Identity, _view, _proj;
        private BoundingFrustum _frustum;
        
        private void makeProjection() {
            if (_ortho)
                _proj = Matrix.OrthoOffCenterLH(
                    -OrthographicSize * AspectRatio * .5f,
                    OrthographicSize * AspectRatio * .5f,
                    -OrthographicSize * .5f,
                    OrthographicSize * .5f,
                    _near, _far);
            else
                _proj = Matrix.PerspectiveFovLH(_fov, _aspect, _near, _far);
            _frustum = new BoundingFrustum(_view * _proj);
        }
        private void makeView() {
            _view = Matrix.LookAtLH(Vector3.Zero, _rotation.Backward, _rotation.Up);
            _frustum = new BoundingFrustum(_view * _proj);
        }

        public Vector3d Position {
            get { return _position; }
            set { _position = value; }
        }
        public Matrix Rotation
        {
            get { return _rotation; }
            set { _rotation = value; makeView(); }
        }

        #region public parameters
        public float FieldOfView { get { return _fov; }
            set
            {
                _fov = value;
                _fovy = (float)(2 * Math.Atan(Math.Tan(_fov * .5) * _aspect));
                _cosfov2 = 1.0 - Math.Cos(_fov * .5);

                makeProjection();
            }
        }
        public float VerticalFieldOfView { get { return _fovy; } }
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
        public float OrthographicSize
        {
            get { return _orthoSize; }
            set
            {
                _orthoSize = value;
                makeProjection();
            }
        }
        public bool Orthographic
        {
            get { return _ortho; }
            set
            {
                _ortho = value;
                makeProjection();
            }
        }

        public Matrix View { get { return _view; } set { _view = value; _frustum = new BoundingFrustum(_view * _proj); } }
        public Matrix Projection { get { return _proj; } }
        public BoundingFrustum Frustum { get { return _frustum; } }
        #endregion

        public D3D11.RenderTargetView renderTargetView;
        public D3D11.DepthStencilView depthStencilView;
        public D3D11.ShaderResourceView renderTargetResource;
        public D3D11.ShaderResourceView depthStencilResource;
        // TODO: Deferred rendering

        public static Camera CreatePerspective(float fieldOfView, float aspectRatio) {
            Camera c = new Camera();
            c._aspect = aspectRatio;
            c.FieldOfView = fieldOfView;

            c.makeView();
            c.makeProjection();
            return c;
        }
        public static Camera CreateOrthographic(float orthoSize, float aspectRatio) {
            Camera c = new Camera();
            c._ortho = true;
            c._aspect = aspectRatio;
            c._orthoSize = orthoSize;

            c.makeView();
            c.makeProjection();
            return c;
        }

        public bool Intersects(OrientedBoundingBox oob) {
            Matrix invoob = Matrix.Invert(oob.Transformation);
            BoundingFrustum frustum = new BoundingFrustum(oob.Transformation * View * Projection);

            BoundingBox bbox = new BoundingBox(-oob.Extents, oob.Extents);
            return frustum.Intersects(ref bbox);
        }
        
        public void GetScaledSpace(Vector3d location, out Vector3d pos, out double scale) {
            double d;
            GetScaledSpace(location, out pos, out scale, out d);
        }
        public void GetScaledSpace(Vector3d location, out Vector3d pos, out double scale, out double distance) {
            scale = 1.0;
            pos = location - Position;

            distance = pos.Length();

            double f = zFar * _cosfov2;
            double p = .5 * f;

            if (distance > p) {
                double s = 1.0 - Math.Exp(-p / (distance - p));
                double dist = p + (f - p) * s;

                scale = dist / distance;
                pos = Vector3d.Normalize(pos) * dist;
            }
        }

        public void CreateResources(D3D11.Device device, int sampleCount, int sampleQuality, int width, int height) {
            FieldOfView = width / (float)height;
            // render target
            D3D11.Texture2DDescription targetTextureDesc = new D3D11.Texture2DDescription() {
                Format = DXGI.Format.R8G8B8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new DXGI.SampleDescription(sampleCount, sampleQuality),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            using (D3D11.Texture2D target = new D3D11.Texture2D(device, targetTextureDesc)) {
                renderTargetResource = new D3D11.ShaderResourceView(device, target);
                renderTargetView = new D3D11.RenderTargetView(device, target);
            }

            // depth buffer
            D3D11.Texture2DDescription depthTextureDesc = new D3D11.Texture2DDescription() {
                Format = DXGI.Format.R32_Typeless,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new DXGI.SampleDescription(sampleCount, sampleQuality),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil | D3D11.BindFlags.ShaderResource,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            D3D11.DepthStencilViewDescription depthViewDesc = new D3D11.DepthStencilViewDescription() {
                Flags = D3D11.DepthStencilViewFlags.None,
                Dimension = D3D11.DepthStencilViewDimension.Texture2D,
                Format = DXGI.Format.D32_Float,
            };
            D3D11.ShaderResourceViewDescription depthResourceDesc = new D3D11.ShaderResourceViewDescription() {
                Format = DXGI.Format.R32_Float,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D
            };
            depthResourceDesc.Texture2D.MipLevels = 1;
            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(device, depthTextureDesc)) {
                depthStencilView = new D3D11.DepthStencilView(device, depthTexture, depthViewDesc);
                depthStencilResource = new D3D11.ShaderResourceView(device, depthTexture, depthResourceDesc);
            }
        }
        public void Dispose() {
            renderTargetView?.Dispose();
            depthStencilView?.Dispose();
            renderTargetResource?.Dispose();
            depthStencilResource?.Dispose();
        }
    }
}
