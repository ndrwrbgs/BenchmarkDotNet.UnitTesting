namespace LibraryV3
{
    using System;
    using System.Collections.Generic;

    public static class BenchmarkRunnerExtensions
    {
        public static ISpecificBenchmarkRunner ForBenchmarkContainer<TBenchmarkContainer>(
            this IBenchmarkRunner runner)
        {
            return new SpecificBenchmarkRunner(runner, typeof(TBenchmarkContainer));
        }

        public static BenchmarkResults RunBenchmark(
            this ISpecificBenchmarkRunner runner,
            IEnumerable<IBenchmarkValidator> forValidators)
        { 
            var estimate = runner.GetRunEstimate(forValidators);
            Console.WriteLine("running with argument: " + estimate.RunParameters.DesiredMaxLatency);
            Console.WriteLine("Hit enter to continue");
            Console.ReadLine();
            return runner.RunBenchmark(estimate.RunParameters);
            //return runner.RunBenchmark(new BenchmarkRunParameters(TimeSpan.FromSeconds(1)));
        }

        public static BenchmarkResults RunBenchmark<TBenchmarkContainer>(
            this IBenchmarkRunner runner,
            IEnumerable<IBenchmarkValidator> forValidators)
        {
            var estimate = runner.GetRunEstimate<TBenchmarkContainer>(forValidators);
            return runner.RunBenchmark<TBenchmarkContainer>(estimate.RunParameters);
        }
    }
}