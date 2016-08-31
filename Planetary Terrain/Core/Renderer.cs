using System;
using SharpDX;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Renderer : IDisposable {

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 144)]
        struct Constants {
            public Matrix View;
            public Matrix Projection;
            public Vector3 cameraDirection;
            public float farPlane;
        }
        Constants constants;
        public D3D11.Buffer constantBuffer { get; private set; }

        public D3D11.DepthStencilView depthStencilView { get; private set; }
        public D3D11.RenderTargetView renderTargetView { get; private set; }

        private SwapChain swapChain;
        private D3D11.Device device;
        private D3D11.DeviceContext context;
        
        public D3D11.Device Device { get { return device; } }
        public D3D11.DeviceContext Context { get { return context; } }

        public D3D11.DepthStencilState depthStencilState { get; private set; }
        public D3D11.DepthStencilState depthStencilStateNoDepth { get; private set; }

        public D3D11.RasterizerState rasterizerStateSolid { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframe { get; private set; }
        public D3D11.RasterizerState rasterizerStateSolidNoCull { get; private set; }
        public D3D11.RasterizerState rasterizerStateWireframeNoCull { get; private set; }

        public D3D11.BlendState blendStateOpaque { get; private set; }
        public D3D11.BlendState blendStateTransparent { get; private set; }

        public Camera Camera;

        D3D11.Buffer axisBuffer;
        D3D11.Buffer axisConsts;

        public Renderer(SharpDX.Windows.RenderForm renderForm) {
            int width = renderForm.ClientSize.Width, height = renderForm.ClientSize.Height;

            #region 3d device creation
            SwapChainDescription swapChainDesc = new SwapChainDescription() {
                ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true
            };

            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport, swapChainDesc, out device, out swapChain);
            context = device.ImmediateContext;

            swapChain.GetParent<Factory>().MakeWindowAssociation(renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            #endregion
            
            #region viewport & render target
            context.Rasterizer.SetViewport(0, 0, width, height);

            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) {
                renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
            }
            #endregion

            #region blend states
            D3D11.BlendStateDescription opaqueDesc = new D3D11.BlendStateDescription();
            opaqueDesc.RenderTarget[0].IsBlendEnabled = false;
            opaqueDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
            blendStateOpaque = new D3D11.BlendState(device, opaqueDesc);

            D3D11.BlendStateDescription alphaDesc = new D3D11.BlendStateDescription();
            alphaDesc.RenderTarget[0].IsBlendEnabled = true;
            alphaDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
            alphaDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
            alphaDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.One;
            alphaDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.Zero;
            alphaDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
            alphaDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
            blendStateTransparent = new D3D11.BlendState(device, alphaDesc);
            #endregion

            #region depth buffer & depth stencil states
            D3D11.Texture2DDescription depthDescription = new D3D11.Texture2DDescription() {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };

            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(device, depthDescription))
                depthStencilView = new D3D11.DepthStencilView(device, depthTexture);

            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            
            depthStencilState = new D3D11.DepthStencilState(device, new D3D11.DepthStencilStateDescription() {
                IsDepthEnabled = true,
                IsStencilEnabled = false,
                DepthComparison = D3D11.Comparison.Less,
                DepthWriteMask = D3D11.DepthWriteMask.All
            });

            depthStencilStateNoDepth = new D3D11.DepthStencilState(device, new D3D11.DepthStencilStateDescription() {
                IsDepthEnabled = false,
                IsStencilEnabled = false,
                DepthComparison = D3D11.Comparison.Less,
                DepthWriteMask = D3D11.DepthWriteMask.Zero
            });

            context.OutputMerger.SetDepthStencilState(depthStencilState);
            #endregion

            #region rasterizer states
            rasterizerStateSolid = new D3D11.RasterizerState(device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Solid,
                CullMode = D3D11.CullMode.Back,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateWireframe = new D3D11.RasterizerState(device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Wireframe,
                CullMode = D3D11.CullMode.Back,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateSolidNoCull = new D3D11.RasterizerState(device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Solid,
                CullMode = D3D11.CullMode.None,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            rasterizerStateWireframeNoCull = new D3D11.RasterizerState(device, new D3D11.RasterizerStateDescription() {
                FillMode = D3D11.FillMode.Wireframe,
                CullMode = D3D11.CullMode.None,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = false,
                IsMultisampleEnabled = true
            });
            #endregion

            constants = new Constants();
            constantBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.ConstantBuffer, ref constants);
            
            #region axis lines & line shader
            axisBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, new VertexColor[] {
                new VertexColor(new Vector3(0, 0,  1000), Color.Blue),
                new VertexColor(new Vector3(0, 0, -1000), Color.Blue),

                new VertexColor(new Vector3(-1000, 0, 0), Color.Red),
                new VertexColor(new Vector3( 1000, 0, 0), Color.Red),

                new VertexColor(new Vector3(0, -1000, 0), Color.Green),
                new VertexColor(new Vector3(0,  1000, 0), Color.Green),
            });
            Matrix m = Matrix.Identity;
            axisConsts = D3D11.Buffer.Create(device, D3D11.BindFlags.ConstantBuffer, ref m);
            #endregion
        }

        public void Resize(int width, int height) {
            if (renderTargetView != null)
                renderTargetView.Dispose();
            if (depthStencilView != null)
                depthStencilView.Dispose();

            Camera.AspectRatio = width / (float)height;

            swapChain.ResizeBuffers(swapChain.Description.BufferCount, width, height, Format.Unknown, SwapChainFlags.None);

            // render target
            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) {
                renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
            }
            
            // depth buffer
            D3D11.Texture2DDescription depthDescription = new D3D11.Texture2DDescription() {
                Format = Format.D16_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                OptionFlags = D3D11.ResourceOptionFlags.None
            };
            using (D3D11.Texture2D depthTexture = new D3D11.Texture2D(device, depthDescription))
                depthStencilView = new D3D11.DepthStencilView(device, depthTexture);

            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            context.Rasterizer.SetViewport(0, 0, width, height);
        }

        public void DisableDepth() {
            context.OutputMerger.SetDepthStencilState(depthStencilStateNoDepth);
        }
        public void EnableDepth() {
            context.OutputMerger.SetDepthStencilState(depthStencilState);
        }

        public void PreRender() {
            constants.View = Matrix.Transpose(Camera.View);
            constants.Projection = Matrix.Transpose(Camera.Projection);
            constants.cameraDirection = Camera.View.Forward;
            constants.farPlane = Camera.zFar;

            context.UpdateSubresource(ref constants, constantBuffer);
        }

        public void Clear(Color color, bool depth = true) {
            context.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            context.ClearRenderTargetView(renderTargetView, color);
            if (depth)
                context.ClearDepthStencilView(depthStencilView, D3D11.DepthStencilClearFlags.Depth, 1f, 0);
        }

        public void Present() {
            swapChain.Present(1, PresentFlags.None);
        }

        public void DrawAxis() {
            Matrix mat = Matrix.Transpose(Matrix.Translation(-Camera.Position));
            context.UpdateSubresource(ref mat, axisConsts);

            Shaders.LineShader.Set(this);

            Context.VertexShader.SetConstantBuffer(1, axisConsts);
            Context.PixelShader.SetConstantBuffer(1, axisConsts);

            Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            Context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(axisBuffer, Utilities.SizeOf<VertexColor>(), 0));

            Context.Draw(6, 0);
        }
        
        public void Dispose() {
            blendStateOpaque.Dispose();
            blendStateTransparent.Dispose();

            rasterizerStateSolid.Dispose();
            rasterizerStateWireframe.Dispose();
            rasterizerStateSolidNoCull.Dispose();
            rasterizerStateWireframeNoCull.Dispose();
            depthStencilState.Dispose();
            depthStencilStateNoDepth.Dispose();
            
            constantBuffer.Dispose();
            axisBuffer.Dispose();
            depthStencilView.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            device.Dispose();
            context.Dispose();
        }
    }
}
