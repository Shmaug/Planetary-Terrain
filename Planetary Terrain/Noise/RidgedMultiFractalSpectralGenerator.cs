using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    class RidgedMultiFractalSpectralGenerator : ISpectralGenerator
    {
        // Inspired by LibNoise

        readonly INoiseGenerator _noiseGenerator;

        public RidgedMultiFractalSpectralGenerator(INoiseGenerator noiseGenerator)
        {
            _noiseGenerator = noiseGenerator;
        }

        public double GetNoise(Vector3d location, double initialFrequencyMultiplier, int numberOfOctaves, double lacunarity, double gain)
        {
            var sampleLocation = location * initialFrequencyMultiplier;
            return AccumulateNoise(sampleLocation, numberOfOctaves, lacunarity, gain);
        }

        double AccumulateNoise(Vector3d location, int numberOfOctaves, double lacunarity, double gain)
        {
            double weight = 1;
            double noise = 0;
            double amplitude = 1;

            for (int x = 0; x < numberOfOctaves; x++)
            {
                double signal = _noiseGenerator.GetNoise(location);
                signal = 1 - Math.Abs(signal);
                signal *= signal * weight;

                weight = signal / gain;
                weight = Math.Max(Math.Min(weight, 1), 0);

                noise += (signal * amplitude);

                location *= lacunarity;
                amplitude *= gain;
            }

            return (noise * 1.25) - 1.0; ;
        }
    }
}
