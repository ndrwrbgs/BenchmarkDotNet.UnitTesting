using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Testing;

namespace LibraryV3
{
    public sealed class TwoSampleZTestHypothesisTest : ITwoSampleNormalDistributionHypothesisTest
    {
        public TwoSampleHypothesisTestResult TestHypothesis(IEnumerable<double> sample1, IEnumerable<double> sample2, double hypothesizedDifference,
            TwoSampleHypothesis alternateHypothesis, double alpha)
        {
            var test = new TwoSampleZTest(
                sample1.ToArray(),
                sample2.ToArray(),
                hypothesizedDifference: hypothesizedDifference,
                alternate: alternateHypothesis);
            test.Size = alpha;
            return new TwoSampleHypothesisTestResult(
                test.Significant,
                test.GetConfidenceInterval(1 - alpha),
                test.ObservedDifference);
        }
    }
}