using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;

namespace LibraryV3
{
    internal static class ConfidenceLevelExtensions
    {
        public static double ToDouble(this ConfidenceLevel confidenceLevel)
        {
            switch (confidenceLevel)
            {
                case ConfidenceLevel.L50:
                    return 50;
                case ConfidenceLevel.L70:
                    return 70;
                case ConfidenceLevel.L75:
                    return 75;
                case ConfidenceLevel.L80:
                    return 80;
                case ConfidenceLevel.L85:
                    return 85;
                case ConfidenceLevel.L90:
                    return 90;
                case ConfidenceLevel.L92:
                    return 92;
                case ConfidenceLevel.L95:
                    return 95;
                case ConfidenceLevel.L96:
                    return 96;
                case ConfidenceLevel.L97:
                    return 97;
                case ConfidenceLevel.L98:
                    return 98;
                case ConfidenceLevel.L99:
                    return 99;
                case ConfidenceLevel.L999:
                    return 99.9;
                default:
                    throw new ArgumentOutOfRangeException(nameof(confidenceLevel), confidenceLevel, null);
            }
        }

        public static ConfidenceLevel GetNearest(double confidenceInterval)
        {
            var allValues = Enum.GetValues(typeof(ConfidenceLevel))
                .Cast<ConfidenceLevel>()
                .ToArray();

            return allValues
                // Smallest difference
                .OrderBy(
                    value =>
                    {
                        var asDouble = value.ToDouble();
                        var difference = Math.Abs(confidenceInterval - asDouble);
                        return difference;
                    })
                .First();
        }
    }
}