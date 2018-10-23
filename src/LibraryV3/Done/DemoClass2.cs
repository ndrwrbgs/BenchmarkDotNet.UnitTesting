namespace LibraryV3
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Accord.Statistics.Distributions.Univariate;
    using BenchmarkDotNet.Attributes;

    internal static class DemoClass2
    {
        static IBenchmarkRunner benchmarkRunner = DefaultBenchmarkRunner.Instance;

        private static void Main()
        {
            // # Arrange
            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                //.IfTreatmentSlowerThanBaseline(withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Fail)
                .IfTreatmentFasterThanBaseline(withConfidenceLevel: 0.95, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
            var validators = new[] { validator };
            
            // # Act
            ISpecificBenchmarkRunner runnerForString = benchmarkRunner.ForBenchmarkContainer<A>();

            // Not strictly necessary
            //{
            //    BenchmarkRunEstimate runEstimate = runnerForString.GetRunEstimate(validators);
            //    var alternativeEstimate = benchmarkRunner.GetRunEstimate<string>(validators);

            //    if (runEstimate.EstimatedTime > TimeSpan.FromMinutes(2))
            //    {
            //        Debug.Fail("Inconclusive - It would take too long");
            //    }
            //}

            BenchmarkResults benchmarkResults = runnerForString.RunBenchmark(validators);

            BenchmarkAssert.ValidatorsPassed(
                validators,
                benchmarkResults,
                assertFailDelegate: s => Debug.Fail(s));
        }
    }

    internal static class RandEx
    {
        public static double Next(this Random rand, double lower, double upper)
        {
            return lower + (upper - lower) * rand.NextDouble();
        }
    }
    public class A
    {
        private Random rand = new Random();

        [Benchmark(Baseline = true)]
        public void Baseline()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(
                rand.Next(2.6, 3.6)));
        }

        [Benchmark]
        public void Treatment(){
            Thread.Sleep(TimeSpan.FromMilliseconds(
                rand.Next(2.5, 3.5)));

        }
    }
}