using System;
using System.Collections.Generic;
using Accord;
using Accord.Statistics.Testing;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;

namespace LibraryV3
{
    internal sealed class BothLatencyValidator : IBenchmarkValidator, ISampleSizeDeterminer
    {
        private readonly double alpha;
        private readonly double minimumDetectableDifferenceDesired;
        private readonly double testStatisticalPower;
        private readonly Percent? byAtLeastPercent;
        private readonly TimeInterval? byAtLeastTimeInterval;
        private readonly TwoSampleHypothesis alternateHypothesis;

        public BothLatencyValidator(
            double alpha, TwoSampleHypothesis alternateHypothesis,
            Percent byAtLeast,
            // TODO: P2 - per the link, appears this may be the number of standard deviations? http://accord-framework.net/docs/html/T_Accord_Statistics_Testing_Power_TwoSampleTTestPowerAnalysis.htm
            double minimumDetectableDifferenceDesired = 0.00001, // Some number larger than 0
            double testStatisticalPower = 0.8 // the test power that we want. 0.8 is a standard - 0.9 is more conservative -- 1 - probability of rejecting the null hypothesis when the null hypothesis is actually false
            )
        {
            this.alpha = alpha;
            this.minimumDetectableDifferenceDesired = minimumDetectableDifferenceDesired;
            this.testStatisticalPower = testStatisticalPower;
            this.byAtLeastPercent = byAtLeast;
            this.alternateHypothesis = alternateHypothesis;
        }

        public BothLatencyValidator(
            double alpha, TwoSampleHypothesis alternateHypothesis,
            // TODO: P2 - per the link, appears this may be the number of standard deviations? http://accord-framework.net/docs/html/T_Accord_Statistics_Testing_Power_TwoSampleTTestPowerAnalysis.htm
            double minimumDetectableDifferenceDesired = 0.00001, // Some number larger than 0
            double testStatisticalPower = 0.8 // the test power that we want. 0.8 is a standard - 0.9 is more conservative -- 1 - probability of rejecting the null hypothesis when the null hypothesis is actually false
            )
            : this (alpha, alternateHypothesis, 0.Percent(), minimumDetectableDifferenceDesired, testStatisticalPower)
        {
        }

        public BothLatencyValidator(
            double alpha, TwoSampleHypothesis alternateHypothesis,
            TimeInterval byAtLeast,
            // TODO: P2 - per the link, appears this may be the number of standard deviations? http://accord-framework.net/docs/html/T_Accord_Statistics_Testing_Power_TwoSampleTTestPowerAnalysis.htm
            double minimumDetectableDifferenceDesired = 0.00001, // Some number larger than 0
            double testStatisticalPower = 0.8 // the test power that we want. 0.8 is a standard - 0.9 is more conservative -- 1 - probability of rejecting the null hypothesis when the null hypothesis is actually false
            )
        {
            this.alpha = alpha;
            this.minimumDetectableDifferenceDesired = minimumDetectableDifferenceDesired;
            this.testStatisticalPower = testStatisticalPower;
            this.byAtLeastTimeInterval = byAtLeast;
            this.alternateHypothesis = alternateHypothesis;
        }

        public IEnumerable<ValidationResult> GetValidationResults(BenchmarkResults results)
        {
            foreach (var result in results.ResultsByCase)
            {
                yield return GetValidationResult(result.Key, result.Value);
            }
        }

        private ValidationResult GetValidationResult(ParameterInstances parameterInstances, BenchmarkResults.BeforeAndAfter resultMeasurement)
        {
            double hypothesizedDifference;

            if (this.byAtLeastTimeInterval != null)
            {
                hypothesizedDifference = byAtLeastTimeInterval.Value.Nanoseconds;
            }
            else if (this.byAtLeastPercent != null)
            {
                var baselineMean = resultMeasurement.Baseline.ResultStatistics.Mean;
                hypothesizedDifference = baselineMean * this.byAtLeastPercent.Value.Multiplier;
            }
            else
            {
                throw new InvalidOperationException("This is why you use a library like OneOf");
            }

            switch (alternateHypothesis)
            {
                case TwoSampleHypothesis.FirstValueIsGreaterThanSecond:
                    // observed: baseline - treatment -- we are saying First<Second so baseline - treatment should be negative
                    hypothesizedDifference *= -1;
                    break;
                case TwoSampleHypothesis.FirstValueIsSmallerThanSecond:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var testResult = new TwoSampleAutoTOrZTestHypothesisTest()
                .TestHypothesis(
                    resultMeasurement,
                    hypothesizedDifference,
                    alternateHypothesis,
                    this.alpha);

            var isMatch = testResult.IsSignificant;
            var confidenceInterval = testResult.ConfidenceInterval;

            var observedDifference = testResult.ObservedDifference;

            var confIntervalInMs = new DoubleRange(confidenceInterval.Min * 1e-6, confidenceInterval.Max * 1e-6);

            var confidenceLevel = 1 - this.alpha;

            string byAtLeastString;
            if (byAtLeastPercent != null)
            {
                byAtLeastString = this.byAtLeastPercent.Value.Multiplier.ToString("P0");
            }else if (byAtLeastTimeInterval != null)
            {
                byAtLeastString = this.byAtLeastTimeInterval.Value.ToString();
            }
            else
            {
                throw new Exception();
            }

            var message =
                $"Support: {(isMatch ? "do support" : "cannot support")}\r\n" +
                $"{this.alternateHypothesis.ToDescriptiveString("baseline duration", "treatment duration")} by {byAtLeastString}\r\n" +
                $"Alpha: {this.alpha}.\r\n" +
                $"HypothesizedDifference: {hypothesizedDifference}.\r\n" +
                $"ObservedDifference: {observedDifference}\r\n" +
                $"ConfidenceInterval: {confIntervalInMs} ms\r\n" +
                $"Baseline {resultMeasurement.Baseline.ResultStatistics.ToSummaryString(confidenceLevel)}" +
                $"Treatment {resultMeasurement.Treatment.ResultStatistics.ToSummaryString(confidenceLevel)}";

            return new ValidationResult(
                parameterInstances,
                this,
                message,
                // TODO: P3 - We are abusing this type here... isViolation != isMatch
                isViolation: isMatch);
        }

        public SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            return new AutoTOrZTestSampleSizeDeterminer(
                    this.alpha,
                    this.minimumDetectableDifferenceDesired,
                    this.testStatisticalPower)
                .GetSampleSizeRequirement(basedOnPreliminaryResults);
        }
    }
}