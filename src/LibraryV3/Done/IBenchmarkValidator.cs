namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using BenchmarkDotNet.Reports;

    public interface IBenchmarkValidator
    {
        IEnumerable<ValidationResult> GetValidationResults(BenchmarkResults results);
    }
}