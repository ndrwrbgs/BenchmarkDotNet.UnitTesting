using System;
using System.Diagnostics;
using System.Linq;
using Accord.Statistics.Testing;
using Accord.Statistics.Testing.Power;
using BenchmarkDotNet.Reports;

namespace LibraryV3
{
    public sealed class ZTestSampleSizeDeterminer : ISampleSizeDeterminer
    {
        private readonly double alpha;
        private readonly double testStatisticalPower;

        public ZTestSampleSizeDeterminer(double alpha, double testStatisticalPower)
        {
            this.alpha = alpha;
            this.testStatisticalPower = testStatisticalPower;
        }

        public SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {

            if (basedOnPreliminaryResults.Baseline.ResultStatistics.N < 30 ||
                basedOnPreliminaryResults.Treatment.ResultStatistics.N < 30)
            {
                throw new InvalidOperationException(
                    "Too few samples for Z test - please use T test");
            }

            var test = new TwoSampleZTest(
                basedOnPreliminaryResults.Baseline.GetAverageNanosecondsForResultRuns(),
                basedOnPreliminaryResults.Treatment.GetAverageNanosecondsForResultRuns(),
                // TODO: P1 - Doing the tests separately like this and doing one tailed is not correct
                // but achieving the call syntax we want with the semantics statistics needs is hard :(
                // The specific problem is that the desired significance might not be achieved based on how this is done
                alternate: TwoSampleHypothesis.ValuesAreDifferent);

            Func<BaseTwoSamplePowerAnalysis, int> getSampleSizeForSample1 = analysis => (int)Math.Min(int.MaxValue, Math.Ceiling(analysis.Samples1));

            // WORK AROUND FOR BUG IN ACCORD
            {
                // This was a weirdness in the Accord library - looks like a bug. We are going to work around it but validate it here in case it changes in the future.
                var originalAnalysis = test.Analysis.Clone() as TwoSampleZTestPowerAnalysis;
                var newAnalysis = test.Analysis as TwoSampleZTestPowerAnalysis;
                newAnalysis.Power = 0.80;
                newAnalysis.ComputeSamples();

                var smallerPower = originalAnalysis.Power < newAnalysis.Power ? originalAnalysis : newAnalysis;
                var largerPower = smallerPower == newAnalysis ? originalAnalysis : newAnalysis;

                if (largerPower.Samples1 < smallerPower.Samples1)
                {
                    // Not expected, but is the bug we are working around
                    if (largerPower.TotalSamples > smallerPower.Samples1)
                    {
                        // Bug validated, our work around is okay
                        getSampleSizeForSample1 = analysis => (int)Math.Min(int.MaxValue, Math.Ceiling(analysis.TotalSamples));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Larger power resulted in smaller sample size needed? Impossible.");
                    }
                }
                else
                {
                    getSampleSizeForSample1 = analysis => (int)Math.Min(int.MaxValue, Math.Ceiling(analysis.TotalSamples));

                    var version = FileVersionInfo.GetVersionInfo(typeof(BaseTwoSamplePowerAnalysis).Assembly.Location);
                    if (version.FileMajorPart == 3 && version.FileMinorPart <= 8)
                    {
                        // Known version
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"It's possible you just need a lot more samples, but it's also possible our work around for a bug in Accord is no longer needed. Gotta check this! {smallerPower.Samples1} {largerPower.Samples1}");
                    }
                }
            }
            // WORK AROUND FOR BUG IN ACCORD

            // The difference standard deviation
            var standardDeviation = test.StandardError * Math.Sqrt(basedOnPreliminaryResults.Baseline.ResultStatistics.N);

            var size4 = TwoSampleZTestPowerAnalysis.GetSampleSize(
                // TODO: Does this delta need to be minimumDetectableDifferenceDesired, or do we use the observed difference?
                delta: test.ObservedDifference,
                power: this.testStatisticalPower,
                alpha: this.alpha,
                // TODO: P1 - Does the direction here matter?
                hypothesis: TwoSampleHypothesis.ValuesAreDifferent,
                standardDeviation: standardDeviation);

            var n1 = getSampleSizeForSample1(size4);

            return new SamplesRequirement(
                (int)Math.Min(int.MaxValue, n1),
                (int)Math.Min(int.MaxValue, n1));
        }
    }
}