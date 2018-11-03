namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using BenchmarkDotNet.Jobs;

    /// <summary>
    /// Reuse as much as possible, as it may cache information and reduce runtime.
    /// Can run a benchmark against any benchmark class
    /// </summary>
    public interface IBenchmarkRunner
    {
        BenchmarkRunEstimate GetRunEstimate<TBenchmarkContainer>(IEnumerable<ISampleSizeDeterminer> sampleSizeDeterminers);
        BenchmarkResults RunBenchmark<TBenchmarkContainer>(BenchmarkRunParameters runParameters);
    }
}