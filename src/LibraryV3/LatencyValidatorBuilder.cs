using System;

namespace LibraryV3
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Accord;
    using Accord.Statistics.Testing;
    using Accord.Statistics.Testing.Power;
    using BenchmarkDotNet.Parameters;

    public struct Percent
    {
        public double Value { get; }

        public double Multiplier => this.Value / 100.0;

        public Percent(double value)
        {
            this.Value = value;
        }
    }

    public static class PercentEx
    {
        public static Percent Percent(this int input)
        {
            return new Percent(input);
        }
    }

    public sealed class LatencyValidatorBuilder
    {
        private readonly IReadOnlyList<BuilderStep> steps;

        private LatencyValidatorBuilder(IReadOnlyList<BuilderStep> steps)
        {
            this.steps = steps;
        }

        public LatencyValidatorBuilder()
        {
            this.steps = new BuilderStep[0];
        }

        public LatencyValidatorBuilder IfTreatmentFasterThanBaseline(double withConfidenceLevel, LatencyValidatorBehavior then)
        {
            return new LatencyValidatorBuilder(
                this.steps
                    .Append(new BuilderStep(BuilderStepType.IfFasterThan, withConfidenceLevel, then))
                    .ToList());
        }

        public LatencyValidatorBuilder IfTreatmentSlowerThanBaseline(
            Percent byAtLeast,
            double withConfidenceLevel,
            LatencyValidatorBehavior then)
        {
            return new LatencyValidatorBuilder(
                this.steps
                    .Append(new BuilderStep(BuilderStepType.IfSlowerThan, withConfidenceLevel, then, byAtLeast))
                    .ToList());
        }

        /// <summary>
        /// Returns validator as it must be the terminal method
        /// aka: Build()
        /// </summary>
        /// <param name="fallbackBehavior"></param>
        /// <returns></returns>
        public IBenchmarkValidator Otherwise(LatencyValidatorBehavior fallbackBehavior)
        {
            var finalSteps = RemoveRedundantSteps(this.steps, fallbackBehavior);

            var totalNumberOfTests = finalSteps.Count;

            var validatorsAndBehaviors = finalSteps
                .Select(step =>
                {
                    var alpha = 1 - step.ConfidenceLevel;
                    var modifiedAlpha = alpha / totalNumberOfTests;

                    switch (step.StepType)
                    {
                        case BuilderStepType.IfFasterThan:
                            return new
                            {
                                validator = (IBenchmarkValidator)
                                    new IfFasterThanValidator(
                                        // TODO: P3 - Pass in things like the detectable difference and power?
                                        modifiedAlpha),
                                ifViolation = step.Behavior
                            };
                        case BuilderStepType.IfSlowerThan:
                            return new
                            {
                                validator = (IBenchmarkValidator)
                                    new IfSlowerThanValidator(
                                        // TODO: P3 - Pass in things like the detectable difference and power?
                                        modifiedAlpha,
                                        byAtLeast: step.ByAtLeast),
                                ifViolation = step.Behavior
                            };
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                })
                .ToArray();

            // TODO: Too much logic here
            return new DelegateBenchmarkValidator(
                basedOnPreliminaryResults =>
                {
                    var samplesRequirements = validatorsAndBehaviors
                        .Select(valWithBehavior => valWithBehavior.validator)
                        .Select(validator => validator.GetSampleSizeRequirement(basedOnPreliminaryResults))
                        .ToArray();

                    int samplesForBaseline = samplesRequirements
                        .Max(size => size.SamplesForBaseline);
                    int samplesForTreatment = samplesRequirements
                        .Max(size => size.SamplesForTreatment);

                    return new SamplesRequirement(
                        samplesForBaseline,
                        samplesForTreatment);
                },
                results =>
                {
                    var toReturn = new List<ValidationResult>();

                    foreach (var resultAndCase in results.ResultsByCase)
                    {
                        var singleItemBenchmarkResult = new BenchmarkResults(
                            new Dictionary<ParameterInstances, BenchmarkResults.BeforeAndAfter>
                            {
                                [resultAndCase.Key] = resultAndCase.Value,
                            });

                        
                        StringBuilder sb = new StringBuilder();

                        ValidationResult validationResult = null;
                        foreach (var valWithBehavior in validatorsAndBehaviors)
                        {
                            var result = valWithBehavior.validator.GetValidationResults(singleItemBenchmarkResult).Single();

                            sb.AppendLine($"Condition {result.Validator} was {result.IsViolation} match - {result.Message}");

                            if (result.IsViolation)
                            {
                                validationResult = new ValidationResult(
                                    resultAndCase.Key,
                                    result.Validator,
                                    sb.ToString(),
                                    // TODO: P1 - This doesn't handle inconclusive - there's no way for us to do that?
                                    isViolation: valWithBehavior.ifViolation == LatencyValidatorBehavior.Fail);
                                // Stop running validators for this collection
                                break;
                            }
                        }

                        sb.AppendLine($"No condition was satisfied, so using the fallback {fallbackBehavior}");
                        if (validationResult == null)
                        {
                            // use the fallback
                            validationResult = new ValidationResult(
                                resultAndCase.Key,
                                null,// Need the 'validator' that created this
                                sb.ToString(),
                                isViolation: fallbackBehavior == LatencyValidatorBehavior.Fail);
                        }

                        toReturn.Add(validationResult);
                    }

                    return toReturn;
                });
        }

        private static IReadOnlyList<BuilderStep> RemoveRedundantSteps(
            IReadOnlyList<BuilderStep> readOnlyList,
            LatencyValidatorBehavior fallbackBehavior)
        {
            List<BuilderStep> steps = readOnlyList.ToList();

            // Remove any from the end that match the fallback
            while (steps.Any() && steps.Last().Behavior == fallbackBehavior)
            {
                steps.RemoveAt(steps.Count - 1);
            }

            return steps;
        }

        private enum BuilderStepType
        {
            IfFasterThan,
            IfSlowerThan
        }

        private sealed class BuilderStep
        {
            public Percent ByAtLeast { get; }
            public BuilderStepType StepType { get; }
            public double ConfidenceLevel { get; }
            public LatencyValidatorBehavior Behavior { get; }

            public BuilderStep(BuilderStepType stepType, double confidenceLevel, LatencyValidatorBehavior behavior, Percent byAtLeast = default(Percent))
            {
                this.ByAtLeast = byAtLeast;
                this.StepType = stepType;
                this.ConfidenceLevel = confidenceLevel;
                this.Behavior = behavior;
            }
        }
    }

    internal sealed class DelegateBenchmarkValidator : IBenchmarkValidator
    {
        public delegate SamplesRequirement GetSampleSizeRequirementDelegate(
            BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults);

        public delegate IEnumerable<ValidationResult> GetValidationResultsDelegate(BenchmarkResults results);

        private readonly GetSampleSizeRequirementDelegate getSampleSize;
        private readonly GetValidationResultsDelegate getValidationResults;

        public DelegateBenchmarkValidator(GetSampleSizeRequirementDelegate sampleSize, GetValidationResultsDelegate validationResults)
        {
            this.getSampleSize = sampleSize;
            this.getValidationResults = validationResults;
        }

        IEnumerable<ValidationResult> IBenchmarkValidator.GetValidationResults(BenchmarkResults results)
        {
            return this.getValidationResults(results);
        }

        SamplesRequirement IBenchmarkValidator.GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            return this.getSampleSize(basedOnPreliminaryResults);
        }
    }

    internal sealed class IfFasterThanValidator : IBenchmarkValidator
    {
        private readonly double alpha;
        private readonly double minimumDetectableDifferenceDesired;
        private readonly double testStatisticalPower;

        public IfFasterThanValidator(
            double alpha,
            // TODO: P2 - per the link, appears this may be the number of standard deviations? http://accord-framework.net/docs/html/T_Accord_Statistics_Testing_Power_TwoSampleTTestPowerAnalysis.htm
            double minimumDetectableDifferenceDesired = 0.00001, // Some number larger than 0
            double testStatisticalPower = 0.8) // the test power that we want. 0.8 is a standard - 0.9 is more conservative -- 1 - probability of rejecting the null hypothesis when the null hypothesis is actually false
        {
            this.alpha = alpha;
            this.minimumDetectableDifferenceDesired = minimumDetectableDifferenceDesired;
            this.testStatisticalPower = testStatisticalPower;
        }

        public IEnumerable<ValidationResult> GetValidationResults(BenchmarkResults results)
        {
            foreach (var result in results.ResultsByCase)
            {
                var parameterInstances = result.Key;
                var resultMeasurement = result.Value;
                
                bool isMatch;
                DoubleRange confidenceInterval;
                var alternateHypothesis = TwoSampleHypothesis.FirstValueIsGreaterThanSecond;
                
                // TODO: We need to use hypothesizedDifference to ensure that we don't say an item is faster
                // when it is statistically faster but also statistically the same - aka practically insignificant
                // TODO: We could use something like a measurement of how long 'nothing' takes on the machine, take as parameter, or hard code a sane value like here.
                var hypothesizedDifference = 0.1;

                if (resultMeasurement.Baseline.ResultStatistics.N < 30 ||
                    resultMeasurement.Treatment.ResultStatistics.N < 30)
                {
                    var test = new TwoSampleTTest(
                        resultMeasurement.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        resultMeasurement.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        // TODO: P1 - Is false okay here? Will it get the variances from the inputs? Or should we use true?
                        assumeEqualVariances: false,
                        hypothesizedDifference: hypothesizedDifference,
                        // AKA: Baseline > Treatment
                        alternate: alternateHypothesis);
                    
                    isMatch = test.Significant;
                    confidenceInterval = test.GetConfidenceInterval(this.alpha);
                }
                else
                {
                    var test = new TwoSampleZTest(
                        resultMeasurement.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        resultMeasurement.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        hypothesizedDifference: hypothesizedDifference,
                        // AKA: Baseline > Treatment
                        alternate: alternateHypothesis);
                    
                    isMatch = test.Significant;
                    confidenceInterval = test.GetConfidenceInterval(this.alpha);
                }

                var confIntervalInMs = new DoubleRange(confidenceInterval.Min * 1e-6, confidenceInterval.Max * 1e-6);

                yield return new ValidationResult(
                    parameterInstances,
                    this,
                    // TODO: P4 - says support even if not support
                    $"We support treatment < baseline (faster than) with Confidence Interval {confIntervalInMs} ms",
                    // TODO: P3 - We are abusing this type here... isViolation != isMatch
                    isViolation: isMatch);
            }
        }

        public SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            if (basedOnPreliminaryResults.Baseline.ResultStatistics.N < 30 ||
                basedOnPreliminaryResults.Treatment.ResultStatistics.N < 30)
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
            else
            {
                var test = new TwoSampleZTest(
                    basedOnPreliminaryResults.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                    basedOnPreliminaryResults.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                    // TODO: P1 - Doing the tests separately like this and doing one tailed is not correct
                    // but achieving the call syntax we want with the semantics statistics needs is hard :(
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

    // TODO: 99.9% copy-paste. Do you want ants? Because copy-paste is how we get ants.
    internal sealed class IfSlowerThanValidator : IBenchmarkValidator
    {
        private readonly double alpha;
        private readonly double minimumDetectableDifferenceDesired;
        private readonly double testStatisticalPower;
        private readonly Percent byAtLeast;

        public IfSlowerThanValidator(
            double alpha,
            // TODO: P2 - per the link, appears this may be the number of standard deviations? http://accord-framework.net/docs/html/T_Accord_Statistics_Testing_Power_TwoSampleTTestPowerAnalysis.htm
            double minimumDetectableDifferenceDesired = 0.00001, // Some number larger than 0
            double testStatisticalPower = 0.8, // the test power that we want. 0.8 is a standard - 0.9 is more conservative -- 1 - probability of rejecting the null hypothesis when the null hypothesis is actually false
            Percent byAtLeast = default(Percent))
        {
            this.alpha = alpha;
            this.minimumDetectableDifferenceDesired = minimumDetectableDifferenceDesired;
            this.testStatisticalPower = testStatisticalPower;
            this.byAtLeast = byAtLeast;
        }

        public IEnumerable<ValidationResult> GetValidationResults(BenchmarkResults results)
        {
            foreach (var result in results.ResultsByCase)
            {
                var parameterInstances = result.Key;
                var resultMeasurement = result.Value;
                
                bool isMatch;
                DoubleRange confidenceInterval;
                var alternateHypothesis = TwoSampleHypothesis.FirstValueIsSmallerThanSecond;
                
                // TODO: We need to use hypothesizedDifference to ensure that we don't say an item is faster
                // when it is statistically faster but also statistically the same - aka practically insignificant
                // TODO: We could use something like a measurement of how long 'nothing' takes on the machine, take as parameter, or hard code a sane value like here.
                //var hypothesizedDifference = 0.1;

                // observed: baseline - treatment -- we are saying First<Second so baseline - treatment should be negative
                var hypothesizedDifference = resultMeasurement.Baseline.ResultStatistics.Mean * -this.byAtLeast.Multiplier;

                double observedDifference = 0;

                if (resultMeasurement.Baseline.ResultStatistics.N < 30 ||
                    resultMeasurement.Treatment.ResultStatistics.N < 30)
                {
                    var test = new TwoSampleTTest(
                        resultMeasurement.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        resultMeasurement.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        // TODO: P1 - Is false okay here? Will it get the variances from the inputs? Or should we use true?
                        assumeEqualVariances: false,
                        hypothesizedDifference: hypothesizedDifference,
                        // AKA: Baseline < Treatment
                        alternate: alternateHypothesis);
                    
                    test.Size = this.alpha;
                    isMatch = test.Significant;
                    confidenceInterval = test.GetConfidenceInterval(1 - this.alpha);
                }
                else
                {
                    var test = new TwoSampleZTest(
                        resultMeasurement.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        resultMeasurement.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                        hypothesizedDifference: hypothesizedDifference,
                        // AKA: Baseline < Treatment
                        alternate: alternateHypothesis);

                    test.Size = this.alpha;
                    isMatch = test.Significant;
                    confidenceInterval = test.GetConfidenceInterval(1 - this.alpha);

                    observedDifference = test.ObservedDifference;
                }

                var confIntervalInMs = new DoubleRange(confidenceInterval.Min * 1e-6, confidenceInterval.Max * 1e-6);

                var message =
                    $"We {(isMatch ? "support" : "cannot support")} treatment > baseline (slower than) with Confidence Interval {confIntervalInMs} ms.\r\n" +
                    $"Alpha: {this.alpha}.\r\n" +
                    $"HypothesizedDifference: {hypothesizedDifference}.\r\n" +
                    $"ObservedDifference: {observedDifference}\r\n" +
                    $"Baseline mean: {resultMeasurement.Baseline.ResultStatistics.Mean}\r\n" +
                    $"Treatment mean: {resultMeasurement.Treatment.ResultStatistics.Mean}";

                yield return new ValidationResult(
                    parameterInstances,
                    this,
                    message,
                    // TODO: P3 - We are abusing this type here... isViolation != isMatch
                    isViolation: isMatch);
            }
        }

        public SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            if (basedOnPreliminaryResults.Baseline.ResultStatistics.N < 30 ||
                basedOnPreliminaryResults.Treatment.ResultStatistics.N < 30)
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
            else
            {
                var test = new TwoSampleZTest(
                    basedOnPreliminaryResults.Baseline.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                    basedOnPreliminaryResults.Treatment.GetResultRuns().Select(run => run.Nanoseconds / run.Operations).ToArray(),
                    // TODO: P1 - Doing the tests separately like this and doing one tailed is not correct
                    // but achieving the call syntax we want with the semantics statistics needs is hard :(
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
}
