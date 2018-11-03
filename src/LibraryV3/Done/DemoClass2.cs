namespace LibraryV3
{
    using System.Diagnostics;
    using Accord.Statistics.Distributions.Univariate;

    internal static class DemoClass2
    {
        static IBenchmarkRunner benchmarkRunner = DefaultBenchmarkRunner.Instance;

        private static void Main()
        {
            // # Arrange
            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                //.IfTreatmentSlowerThanBaseline(withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Fail)
                .IfTreatmentFasterThanBaseline(byAtLeast: 0.Percent(), withConfidenceLevel: 0.95, then: LatencyValidatorBehavior.Pass)
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
}