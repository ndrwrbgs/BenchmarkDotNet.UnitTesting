namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;

    /// <summary>
    /// Runs a benchmark against a specific benchmark class.
    /// Optional syntax to prevent repeating yourself in the test e.g. GetEstimate{T} and then Run{T} again
    /// </summary>
    [PublicAPI]
    public interface ISpecificBenchmarkRunner
    {
        Type BenchmarkClass { get; }

        BenchmarkRunEstimate GetRunEstimate(IEnumerable<ISampleSizeDeterminer> sampleSizeDeterminers);
        BenchmarkResults RunBenchmark(BenchmarkRunParameters runParameters);
    }
}