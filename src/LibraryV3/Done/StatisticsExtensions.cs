using BenchmarkDotNet.Mathematics;

namespace LibraryV3
{
    internal static class StatisticsExtensions
    {
        public static string ToSummaryString(this Statistics statistics, double confidenceInterval)
        {
            ConfidenceLevel ci = ConfidenceLevelExtensions.GetNearest(confidenceInterval);
            return $"mean: {statistics.Mean:F2} {statistics.GetConfidenceInterval(ci, statistics.N).ToStr()}";
        }
    }
}