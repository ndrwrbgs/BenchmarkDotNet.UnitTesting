using System.Collections.Generic;

namespace LibraryV3
{
    internal sealed class DelegateBenchmarkValidator : IBenchmarkValidator, ISampleSizeDeterminer
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

        SamplesRequirement ISampleSizeDeterminer.GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults)
        {
            return this.getSampleSize(basedOnPreliminaryResults);
        }
    }
}