using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    class FractalBrownianMotionSpectralGenerator : ISpectralGenerator
    {
        readonly INoiseGenerator _noiseGenerator;

        public FractalBrownianMotionSpectralGenerator(INoiseGenerator noiseGenerator)
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
            double noiseSum = 0;
            double amplitude = 1;
            double amplitudeSum = 0;

            Vector3d sampleLocation = location;

            for (int x = 0; x < numberOfOctaves; x++)
            {
                noiseSum += amplitude * _noiseGenerator.GetNoise(sampleLocation);
                amplitudeSum += amplitude;

                amplitude *= gain;
                sampleLocation *= lacunarity;
            }

            noiseSum /= amplitudeSum;

            return noiseSum * 1.35;
        }
    }
}


