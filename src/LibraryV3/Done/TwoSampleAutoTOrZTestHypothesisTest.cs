using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Testing;

namespace LibraryV3
{
    public sealed class TwoSampleAutoTOrZTestHypothesisTest : ITwoSampleNormalDistributionHypothesisTest
    {
        public TwoSampleHypothesisTestResult TestHypothesis(IEnumerable<double> sample1, IEnumerable<double> sample2, double hypothesizedDifference,
            TwoSampleHypothesis alternateHypothesis, double alpha)
        {
            var sample1Collection = sample1 as ICollection<double> ?? sample1.ToArray();
            var sample2Collection = sample2 as ICollection<double> ?? sample2.ToArray();

            ITwoSampleNormalDistributionHypothesisTest test;
            if (sample1Collection.Count < 30
                || sample2Collection.Count < 30)
            {
                test = new TwoSampleTTestHypothesisTest();
            }
            else
            {
                test = new TwoSampleZTestHypothesisTest();
            }

            return test
                .TestHypothesis(
                sample1Collection,
                sample2Collection,
                hypothesizedDifference,
                alternateHypothesis,
                alpha);
        }
    }
}
