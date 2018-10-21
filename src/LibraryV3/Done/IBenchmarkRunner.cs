namespace LibraryV3
{
    using System.Collections.Generic;

    /// <summary>
    /// Reuse as much as possible, as it may cache information and reduce runtime.
    /// Can run a benchmark against any benchmark class
    /// </summary>
    public interface IBenchmarkRunner
    {
        BenchmarkRunEstimate GetRunEstimate<TBenchmarkContainer>(IEnumerable<IBenchmarkValidator> validators);
        BenchmarkResults RunBenchmark<TBenchmarkContainer>(BenchmarkRunParameters runParameters);
    }
}