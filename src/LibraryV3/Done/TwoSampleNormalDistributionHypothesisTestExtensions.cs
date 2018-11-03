using System.Linq;
using Accord.Statistics.Testing;
using BenchmarkDotNet.Reports;

namespace LibraryV3
{
    public static class TwoSampleNormalDistributionHypothesisTestExtensions
    {
        public static TwoSampleHypothesisTestResult TestHypothesis(
            this ITwoSampleNormalDistributionHypothesisTest source,
            BenchmarkResults.BeforeAndAfter resultMeasurement,
            double hypothesizedDifference,
            TwoSampleHypothesis alternateHypothesis,
            double alpha)
        {
            return source.TestHypothesis(
                resultMeasurement.Baseline.GetResultRuns().Select(run => MeasurementExtensions.GetAverageNanoseconds(run)),
                resultMeasurement.Treatment.GetResultRuns().Select(run => run.GetAverageNanoseconds()),
                hypothesizedDifference,
                alternateHypothesis,
                alpha);
        }
    }
}