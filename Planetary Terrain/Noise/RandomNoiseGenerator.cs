using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planetary_Terrain
{
    class RandomNoiseGenerator : INoiseGenerator
    {
        const int _poolSize = 65536;
        uint[] _numberPool;
        Random _random;

        public RandomNoiseGenerator()
        {
            _numberPool = new uint[_poolSize]; 
            _random = new Random(0);

            FillPool();
        }

        void FillPool()
        {
            for (int x = 0; x < _poolSize; x++)
            {
                _numberPool[x] = (uint)_random.Next(_poolSize);
            }
        }

        public double GetNoise(Vector3d location)
        {
            // Taken from Interactive Visualization paper

            uint xHash = (uint)location.X.GetHashCode();
            uint yHash = (uint)location.Y.GetHashCode();
            uint zHash = (uint)location.Z.GetHashCode();

            uint result = GetFromPool(0);
            while (xHash > 0 || yHash > 0 || zHash > 0)
            {
                result += GetFromPool(xHash + GetFromPool(yHash + GetFromPool(zHash)));

                xHash >>= 16;
                yHash >>= 16;
                zHash >>= 16;
            }

            return (result % _poolSize) / (double)(_poolSize / 2) - 1.0;
        }

        uint GetFromPool(uint index)
        {
            return _numberPool[index % _poolSize];
        }
    }
}


