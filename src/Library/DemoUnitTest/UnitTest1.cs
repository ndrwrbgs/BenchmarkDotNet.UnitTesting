using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DemoUnitTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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
                .IfFasterThan(withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
            var validators = new[] { validator };
            
            // # Act
            ISpecificBenchmarkRunner runnerForString = benchmarkRunner.ForBenchmarkContainer<ArrayEnumerationIsFaster_Benchmarks>();

            // Not strictly necessary
            // TODO: We should change how RunBenchmark is called to incorporate limits on how much time we are willing to spend
            //{
            //    BenchmarkRunEstimate runEstimate = runnerForString.GetRunEstimate(validators);

            //    if (runEstimate.EstimatedTime > TimeSpan.FromMinutes(2))
            //    {
            //        Assert.Inconclusive("Inconclusive - It would take too long");
            //    }
            //}

            BenchmarkResults benchmarkResults = runnerForString.RunBenchmark(forValidators: validators);

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
            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                .IfFasterThan(withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
            var validators = new[] { validator };
            
            // # Act
            ISpecificBenchmarkRunner runnerForString = benchmarkRunner.ForBenchmarkContainer<EnumerableEnumerationIsFaster_Benchmarks>();

            BenchmarkResults benchmarkResults = runnerForString.RunBenchmark(forValidators: validators);

            BenchmarkAssert.ValidatorsPassed(
                validators,
                benchmarkResults,
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
