using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    interface ISpectralGenerator
    {
        double GetNoise(Vector3d location, double initialFrequencyMultiplier, int numberOfOctaves,
                        double lacunarity, double gain); 
    }
}
