namespace LibraryV3
{
    public sealed class AutoTOrZTestSampleSizeDeterminer : ISampleSizeDeterminer
    {
        private readonly TTestSampleSizeDeterminer t;
        private readonly ZTestSampleSizeDeterminer z;
        public AutoTOrZTestSampleSizeDeterminer(double alpha, double minimumDetectableDifferenceDesired, double testStatisticalPower)
        {
            this.t = new TTestSampleSizeDeterminer(
                alpha,
                minimumDetectableDifferenceDesired,
                testStatisticalPower);
            this.z = new ZTestSampleSizeDeterminer(
                alpha,
                testStatisticalPower);
        }

        public SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            if (basedOnPreliminaryResults.Baseline.ResultStatistics.N < 30 ||
                basedOnPreliminaryResults.Treatment.ResultStatistics.N < 30)
            {
                return this.t.GetSampleSizeRequirement(basedOnPreliminaryResults);
            }
            else
            {
                return this.z.GetSampleSizeRequirement(basedOnPreliminaryResults);
            }
        }
    }
}