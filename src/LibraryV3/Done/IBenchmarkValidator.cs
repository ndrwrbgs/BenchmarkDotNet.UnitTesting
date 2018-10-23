namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using BenchmarkDotNet.Reports;

    public interface IBenchmarkValidator
    {
        IEnumerable<ValidationResult> GetValidationResults(BenchmarkResults results);
        
        // TODO: A bit leaky? to VALIDATE doesn't require getting the sample size...
        SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults);
    }
}