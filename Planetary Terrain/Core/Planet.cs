﻿using System;
using SharpDX;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using D3D11 = SharpDX.Direct3D11;
using System.Runtime.InteropServices;

namespace Planetary_Terrain {
    class Planet : Body, IDisposable {
        /// <summary>
        /// The total possible terrain displacement is Radius +/- TerrainHeight
        /// </summary>
        public double TerrainHeight;
        
        /// <summary>
        /// The planet's atmosphere
        /// </summary>
        public Atmosphere Atmosphere;

        public bool Ocean;
        
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.Texture2D colorMap;
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.ShaderResourceView colorMapView;
        /// <summary>
        /// The map of temperature-humidity to color
        /// </summary>
        D3D11.SamplerState colorMapSampler;

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
        struct Constants {
            public Vector3 lightDirection;
        }
        Constants constants;
        public D3D11.Buffer constBuffer { get; private set; }
        
        public Planet(string name, Vector3d pos, double radius, double mass, double terrainHeight, Atmosphere atmosphere = null, bool ocean = false) : base(pos, radius, mass) {
            Label = name;
            Radius = radius;
            TerrainHeight = terrainHeight;
            Atmosphere = atmosphere;

            if (atmosphere != null)
                atmosphere.Planet = this;

            Ocean = ocean;
        }

        
        double height(Vector3d direction) {
            double total = 0;

            double n = Noise.Ridged(direction * 150 + new Vector3(1000), 2, .01f, .3f);
            n = 1 - n * n * n;

            total += n * Noise.Fractal(direction * 100 + new Vector3(1000), 11, .03f, .5f);

            if (Ocean)
                if (total < 0 || Noise.Ridged(direction * 150 + new Vector3(5000), 2, .01f, .3f) < 0)
                    total = 0;

            return total;
        }

        public override double GetHeight(Vector3d direction) {
            return Radius + height(direction) * TerrainHeight;
        }
        public override Vector2 GetTemp(Vector3d direction) {
            float y = MathUtil.Clamp((float)Math.Abs(direction.Y) - .3f, 0, 1);
            y *= y;
            float temp = (float)(Noise.SmoothSimplex(direction * 5, 5, .3f, .8f)*.5d+.5d) - y;
            float humid = (float)(Noise.SmoothSimplex(direction * 5, 4, .1f, .8f)*.5d+.5d) - .1f;

            if (Ocean) {
                double h = height(direction);
                if (h <= 0)
                    humid = 1;
                else
                    humid += (float)Math.Pow(1.0 - h, 2);
            }

            return new Vector2(temp, humid);
        }

        public void SetColormap(D3D11.Texture2D map, D3D11.Device device) {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            colorMap = map;
            colorMapView = new D3D11.ShaderResourceView(device, colorMap);
            colorMapSampler = new D3D11.SamplerState(device, new D3D11.SamplerStateDescription() {
                AddressU = D3D11.TextureAddressMode.Clamp,
                AddressV = D3D11.TextureAddressMode.Clamp,
                AddressW = D3D11.TextureAddressMode.Clamp,
                Filter = D3D11.Filter.Anisotropic,
            });
        }

        public override void Update(D3D11.Device device, Camera camera) {
            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].SplitDynamic(camera.Position, device);
        }

        public override void Draw(Renderer renderer, Body sun) {
            // Get the entire planet's scale and scaled position
            // This ensures the planet is always within the clipping planes
            Vector3d pos;
            double scale;
            renderer.Camera.GetScaledSpace(Position, out pos, out scale);
            if (scale * Radius < 1)
                return;

            constants.lightDirection = Vector3d.Normalize(Position - sun.Position);

            // create/update constant buffer
            if (constBuffer == null)
                constBuffer = D3D11.Buffer.Create(renderer.Device, D3D11.BindFlags.ConstantBuffer, ref constants);
            renderer.Context.UpdateSubresource(ref constants, constBuffer);

            Shaders.PlanetShader.Set(renderer);
            // set constant buffer
            renderer.Context.VertexShader.SetConstantBuffers(2, constBuffer);
            renderer.Context.PixelShader.SetConstantBuffers(2, constBuffer);

            // color map
            renderer.Context.PixelShader.SetShaderResource(0, colorMapView);
            renderer.Context.PixelShader.SetSampler(0, colorMapSampler);
            
            renderer.Context.OutputMerger.SetBlendState(renderer.blendStateTransparent);

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Draw(renderer, pos, scale);
            
            //Atmosphere?.Draw(renderer, pos, scale);
        }

        public override void Dispose() {
            colorMapSampler?.Dispose();
            colorMap?.Dispose();
            colorMapView?.Dispose();

            constBuffer?.Dispose();

            for (int i = 0; i < BaseQuads.Length; i++)
                BaseQuads[i].Dispose();
            
            Atmosphere?.Dispose();
        }
    }
}
