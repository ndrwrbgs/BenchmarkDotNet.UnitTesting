using System;

namespace LibraryV3
{
    internal static class RandEx
    {
        public static double Next(this Random rand, double lower, double upper)
        {
            return lower + (upper - lower) * rand.NextDouble();
        }
    }
}