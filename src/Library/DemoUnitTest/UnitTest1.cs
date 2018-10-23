using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DemoUnitTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using BenchmarkDotNet.Attributes;
    using LibraryV3;

    [TestClass]
    public class UnitTest1
    {
        static IBenchmarkRunner benchmarkRunner = DefaultBenchmarkRunner.Instance;

        [TestMethod]
        public void ArrayEnumerationIsFaster()
        {
            // # Arrange
            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                .IfTreatmentFasterThanBaseline(withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
            var validators = new[] { validator };
            
            // # Act
            ISpecificBenchmarkRunner runner = benchmarkRunner.ForBenchmarkContainer<ArrayEnumerationIsFaster_Benchmarks>();

            // Not strictly necessary
            // TODO: We should change how RunBenchmark is called to incorporate limits on how much time we are willing to spend
            //{
            //    BenchmarkRunEstimate runEstimate = runner.GetRunEstimate(validators);

            //    if (runEstimate.EstimatedTime > TimeSpan.FromMinutes(2))
            //    {
            //        Assert.Inconclusive("Inconclusive - It would take too long");
            //    }
            //}

            BenchmarkResults benchmarkResults = runner.RunBenchmark(forValidators: validators);

            BenchmarkAssert.ValidatorsPassed(
                validators,
                benchmarkResults,
                assertFailDelegate: Assert.Fail);
        }

        public class ArrayEnumerationIsFaster_Benchmarks
        {
            private List<int> list;
            private int[] array;

            [Params(0, 1, 10)] public int Size;

            [GlobalSetup]
            public void Setup()
            {
                this.list = Enumerable.Range(0, this.Size).ToList();
                this.array = this.list.ToArray();
            }

            [Benchmark(Baseline = true)]
            public void ListEnumeration()
            {
                foreach (var item in this.list) ;
            }

            [Benchmark]
            public void ArrayEnumeration()
            {
                foreach (var item in this.array) ;
            }
        }

        [TestMethod]
        public void EnumerableEnumerationIsFaster()
        {
            // # Arrange
            ISpecificBenchmarkRunner runner = benchmarkRunner/*Factory? Context? TODO*/
                .For(
                    baseline: (EnumerableEnumerationIsFaster_Benchmarks container) => container.ListEnumeration(),
                    treatment: (EnumerableEnumerationIsFaster_Benchmarks container) => container.EnumerableEnumeration());

            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                // treatment: EnumerableEnumeration <slower than> baseline: ListEnumeration
                .IfTreatmentSlowerThanBaseline(withConfidenceLevel: 0.9999, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);

            // # Act
            BenchmarkResults benchmarkResults = runner.RunBenchmark(
                // TODO: Would 'sampleSizeDeterminers' be better? I mean, THAT name is horrible, but more accurate
                forValidator: validator);

            // # Assert
            BenchmarkAssert.ValidatorsPassed(
                new [] { validator },
                benchmarkResults,
                assertFailDelegate: Assert.Fail);



            // NOTE - same as above, more succinctly...
            DefaultBenchmarkRunner.Instance
                .For(
                    baseline: (EnumerableEnumerationIsFaster_Benchmarks container) => container.ListEnumeration(),
                    treatment: (EnumerableEnumerationIsFaster_Benchmarks container) => container.EnumerableEnumeration())
                // TODO: Would rather Run() and then Assert(), but both require the arguments right now so wouldn't be able to do that in-line reading like a sentence
                // We are trying to represent...
                // "For baseline ListEnumeration and treatment EnumerableEnumeration if treatment is slower than baseline with confidence level 0.9999 then pass
                //  otherwise fail by calling Assert.Fail()"
                .RunWithValidatorAndAssertPassed(
                    LatencyValidatorFactory.Builder
                        .IfTreatmentSlowerThanBaseline(withConfidenceLevel: 0.9999, then: LatencyValidatorBehavior.Pass)
                        .Otherwise(LatencyValidatorBehavior.Fail),
                    assertFailDelegate: Assert.Fail);
        }

        public class EnumerableEnumerationIsFaster_Benchmarks
        {
            private List<int> list;
            private IEnumerable<int> enumerable;

            [Params(0, 1, 10)] public int Size;

            [GlobalSetup]
            public void Setup()
            {
                this.enumerable = Enumerable.Range(0, this.Size);
                this.list = this.enumerable.ToList();
            }

            [Benchmark(Baseline = true)]
            public void ListEnumeration()
            {
                foreach (var item in this.list) ;
            }

            [Benchmark]
            public void EnumerableEnumeration()
            {
                foreach (var item in this.enumerable) ;
            }
        }
    }
}
