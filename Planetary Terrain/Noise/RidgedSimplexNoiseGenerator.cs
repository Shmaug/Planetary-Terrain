using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    class RidgedSimplexNoiseGenerator : INoiseGenerator
    {
        SimplexNoiseGenerator _sourceGenerator = new SimplexNoiseGenerator();

        public double GetNoise(Vector3d location)
        {
            return 1 - Math.Abs(_sourceGenerator.GetNoise(location));
        }
    }
}
