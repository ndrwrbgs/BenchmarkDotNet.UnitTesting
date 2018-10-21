namespace LibraryV3
{
    using System;
    using System.Collections.Generic;

    internal sealed class SpecificBenchmarkRunner : ISpecificBenchmarkRunner
    {
        private readonly IBenchmarkRunner runner;

        public SpecificBenchmarkRunner(
            IBenchmarkRunner runner,
            Type benchmarkContainerType)
        {
            this.runner = runner;
            this.BenchmarkClass = benchmarkContainerType;
        }

        public Type BenchmarkClass {get;}

        public BenchmarkRunEstimate GetRunEstimate(IEnumerable<IBenchmarkValidator> validators)
        {
            var genericMethod = typeof(IBenchmarkRunner)
                .GetMethod(nameof(IBenchmarkRunner.GetRunEstimate));

            var method = genericMethod
                .MakeGenericMethod(this.BenchmarkClass);

            var result = method.Invoke(this.runner, parameters: new object[] {validators});

            var castResult = (BenchmarkRunEstimate) result;

            return castResult;
        }

        public BenchmarkResults RunBenchmark(BenchmarkRunParameters runParameters)
        {
            var genericMethod = typeof(IBenchmarkRunner)
                .GetMethod(nameof(IBenchmarkRunner.RunBenchmark));

            var method = genericMethod
                .MakeGenericMethod(this.BenchmarkClass);

            var result = method.Invoke(this.runner, parameters: new object[] {runParameters});

            var castResult = (BenchmarkResults) result;

            return castResult;
        }
    }
}