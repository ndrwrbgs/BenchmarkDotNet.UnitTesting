using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Accord.Statistics.Testing.Power;

namespace LibraryV3
{
    public sealed class TTestSampleSizeDeterminer : ISampleSizeDeterminer
    {
        private readonly double alpha;
        private readonly double minimumDetectableDifferenceDesired;
        private readonly double testStatisticalPower;

        public TTestSampleSizeDeterminer(double alpha, double minimumDetectableDifferenceDesired, double testStatisticalPower)
        {
            this.alpha = alpha;
            this.minimumDetectableDifferenceDesired = minimumDetectableDifferenceDesired;
            this.testStatisticalPower = testStatisticalPower;
        }

        public SamplesRequirement GetSampleSizeRequirement(
            BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            if (basedOnPreliminaryResults.Baseline.ResultStatistics.N >= 30 &&
                basedOnPreliminaryResults.Treatment.ResultStatistics.N >= 30)
            {
                Trace.WriteLine("In this scenario, you should use Z-test");
            }

            {
// Variances are determined by the preliminary results
                var size = TwoSampleTTestPowerAnalysis.GetSampleSize(
                    variance1: basedOnPreliminaryResults.Baseline.ResultStatistics.Variance,
                    variance2: basedOnPreliminaryResults.Treatment.ResultStatistics.Variance,
                    alpha: this.alpha,
                    delta: this.minimumDetectableDifferenceDesired,
                    power: this.testStatisticalPower
                );

                var n1 = (int) Math.Ceiling(size.Samples1);
                var n2 = (int) Math.Ceiling(size.Samples2);

                return new SamplesRequirement(
                    n1,
                    n2);
            }
        }
    }
}
