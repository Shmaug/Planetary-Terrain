using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    interface INoiseGenerator
    {
        double GetNoise(Vector3d location);
    }
}
