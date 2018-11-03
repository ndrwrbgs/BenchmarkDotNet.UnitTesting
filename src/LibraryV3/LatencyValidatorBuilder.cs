using System;
using BenchmarkDotNet.Horology;

namespace LibraryV3
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Accord.Statistics.Testing;
    using Accord.Statistics.Testing.Power;
    using BenchmarkDotNet.Parameters;
    using BenchmarkDotNet.Reports;

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

        public LatencyValidatorBuilder IfTreatmentFasterThanBaseline(
            Percent byAtLeast,
            double withConfidenceLevel, LatencyValidatorBehavior then)
        {
            return new LatencyValidatorBuilder(
                this.steps
                    .Append(new BuilderStep(BuilderStepType.IfFasterThan, withConfidenceLevel, then, byAtLeast))
                    .ToList());
        }

        public LatencyValidatorBuilder IfTreatmentFasterThanBaseline(
            TimeInterval byAtLeast,
            double withConfidenceLevel, LatencyValidatorBehavior then)
        {
            return new LatencyValidatorBuilder(
                this.steps
                    .Append(new BuilderStep(BuilderStepType.IfFasterThan, withConfidenceLevel, then, byAtLeast))
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

        public LatencyValidatorBuilder IfTreatmentSlowerThanBaseline(
            // TODO: Convert all these to OneOf when we have internet
            TimeInterval byAtLeast,
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

                    TwoSampleHypothesis alternate;
                    switch (step.StepType)
                    {
                        case BuilderStepType.IfFasterThan:
                            alternate = TwoSampleHypothesis.FirstValueIsGreaterThanSecond;
                            break;
                        case BuilderStepType.IfSlowerThan:
                            alternate = TwoSampleHypothesis.FirstValueIsSmallerThanSecond;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    BothLatencyValidator validator;
                    if (step.ByAtLeastTimeInterval != null)
                    {
                        validator = new BothLatencyValidator(
                            // TODO: P3 - Pass in things like the detectable difference and power?
                            modifiedAlpha, alternate,
                            byAtLeast: step.ByAtLeastTimeInterval.Value);
                    }
                    else if (step.ByAtLeastPercent != null)
                    {
                        validator = new BothLatencyValidator(
                            // TODO: P3 - Pass in things like the detectable difference and power?
                            modifiedAlpha, alternate,
                            byAtLeast: step.ByAtLeastPercent.Value);
                    }
                    else
                    {
                        throw new Exception();
                    }

                    return new
                    {
                        validator = (IBenchmarkValidator)validator,
                        sampleSizeDeterminer = (ISampleSizeDeterminer)validator,
                        ifViolation = step.Behavior
                    };
                })
                .ToArray();

            // TODO: Too much logic here
            return new DelegateBenchmarkValidator(
                basedOnPreliminaryResults =>
                {
                    var samplesRequirements = validatorsAndBehaviors
                        .Select(valWithBehavior => valWithBehavior.sampleSizeDeterminer)
                        .Select(sampleSizeDeterminer => sampleSizeDeterminer.GetSampleSizeRequirement(basedOnPreliminaryResults))
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
            public Percent? ByAtLeastPercent { get; }
            public TimeInterval? ByAtLeastTimeInterval { get; }
            public BuilderStepType StepType { get; }
            public double ConfidenceLevel { get; }
            public LatencyValidatorBehavior Behavior { get; }

            public BuilderStep(BuilderStepType stepType, double confidenceLevel, LatencyValidatorBehavior behavior, Percent byAtLeast)
            {
                this.ByAtLeastPercent = byAtLeast;
                this.StepType = stepType;
                this.ConfidenceLevel = confidenceLevel;
                this.Behavior = behavior;
            }

            public BuilderStep(BuilderStepType stepType, double confidenceLevel, LatencyValidatorBehavior behavior, TimeInterval byAtLeast)
            {
                this.ByAtLeastTimeInterval = byAtLeast;
                this.StepType = stepType;
                this.ConfidenceLevel = confidenceLevel;
                this.Behavior = behavior;
            }
        }
    }
}
