using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Testing;

namespace LibraryV3
{
    public sealed class TwoSampleTTestHypothesisTest : ITwoSampleNormalDistributionHypothesisTest
    {
        public TwoSampleHypothesisTestResult TestHypothesis(IEnumerable<double> sample1, IEnumerable<double> sample2, double hypothesizedDifference,
            TwoSampleHypothesis alternateHypothesis,
            double alpha)
        {
            var test = new TwoSampleTTest(
                sample1.ToArray(),
                sample2.ToArray(),
                // TODO: P1 - Is false okay here? Will it get the variances from the inputs? Or should we use true?
                assumeEqualVariances: false,
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