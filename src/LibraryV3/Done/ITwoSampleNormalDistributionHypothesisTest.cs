using System.Collections.Generic;
using Accord.Statistics.Testing;

namespace LibraryV3
{
    public interface ITwoSampleNormalDistributionHypothesisTest /* TODO: : HypothesisTest<NormalDistribution> */
    {
        TwoSampleHypothesisTestResult TestHypothesis(
            IEnumerable<double> sample1,
            IEnumerable<double> sample2,
            double hypothesizedDifference,
            TwoSampleHypothesis alternateHypothesis,
            double alpha);
    }
}