using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DemoUnitTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using Accord.Statistics.Testing;
    using BenchmarkDotNet.Attributes;
    using LibraryV3;

    [TestClass]
    public class UnitTest1
    {
        static IBenchmarkRunner benchmarkRunner = DefaultBenchmarkRunner.Instance;

        [TestMethod]
        public void Test()
        {
            foreach (var alt in new[]
            {
                TwoSampleHypothesis.ValuesAreDifferent,
                TwoSampleHypothesis.FirstValueIsGreaterThanSecond,
                TwoSampleHypothesis.FirstValueIsSmallerThanSecond
            })
            {
                foreach (double hyp in new[] {0, -0.1, 0.1, -100, 100})
                {
                    var test = new TwoSampleZTest(
                        new double[] {1, 1, 2,},
                        new double[] {100, 100, 300},
                        hypothesizedDifference: hyp,
                        alternate: alt);

                    // Observed is sample1 - sample2

                    // TRUES:
                    // ValuesAreDifferent -100  - we can reject that sample1 - sample2 == -100
                    // ValuesAreDifferent 100   - we can reject that sample1 - sample2 == 100
                    // FirstGreater -100        - we can reject sample1 + -100 > sample2
                    // FirstSmaller 100         - we can reject sample1 +  100 < sample2

                    Console.WriteLine($"{alt} at {hyp:N2}\t{test.Significant}\t{test.ObservedDifference}\t{test.Confidence.ToString()}");
                }
            }
        }

        [TestMethod]
        public void ArrayEnumerationIsFaster()
        {
            // # Arrange
            IBenchmarkValidator validator = LatencyValidatorFactory.Builder
                .IfTreatmentFasterThanBaseline(byAtLeast: 10.Percent(), withConfidenceLevel: 0.99, then: LatencyValidatorBehavior.Pass)
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
                .IfTreatmentSlowerThanBaseline(byAtLeast: 20.Percent(), withConfidenceLevel: 0.95, then: LatencyValidatorBehavior.Pass)
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
            //DefaultBenchmarkRunner.Instance
            //    .For(
            //        baseline: (EnumerableEnumerationIsFaster_Benchmarks container) => container.ListEnumeration(),
            //        treatment: (EnumerableEnumerationIsFaster_Benchmarks container) => container.EnumerableEnumeration())
            //    // TODO: Would rather Run() and then Assert(), but both require the arguments right now so wouldn't be able to do that in-line reading like a sentence
            //    // We are trying to represent...
            //    // "For baseline ListEnumeration and treatment EnumerableEnumeration if treatment is slower than baseline with confidence level 0.9999 then pass
            //    //  otherwise fail by calling Assert.Fail()"
            //    .RunWithValidatorAndAssertPassed(
            //        LatencyValidatorFactory.Builder
            //            .IfTreatmentSlowerThanBaseline(
            //                byAtLeast: 10.Percent(),
            //                withConfidenceLevel: 0.9999,
            //                then: LatencyValidatorBehavior.Pass)
            //            .Otherwise(LatencyValidatorBehavior.Fail),
            //        assertFailDelegate: Assert.Fail);
        }

        public class EnumerableEnumerationIsFaster_Benchmarks
        {
            private List<int> list;
            private IEnumerable<int> enumerable;

            [Params(1000)] public int Size;

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
