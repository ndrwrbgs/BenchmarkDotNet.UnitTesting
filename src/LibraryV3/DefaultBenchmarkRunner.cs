namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Horology;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Mathematics;
    using BenchmarkDotNet.Parameters;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Toolchains.CsProj;

    public sealed class DefaultBenchmarkRunner : IBenchmarkRunner
    {
        private Func<Job, Job> jobMutator;

        public DefaultBenchmarkRunner(Func<Job, Job> jobMutator)
        {
            this.jobMutator = jobMutator;
        }

        public BenchmarkRunEstimate GetRunEstimate<TBenchmarkContainer>(
            IEnumerable<ISampleSizeDeterminer> sampleSizeDeterminers)
        {
            // TODO: Point of this class was to cache - needs to cache :)

            // TODO: P2 - Get the run time for the preliminary FROM a simple invocation that sees how long the methods take.
            // E.G. Do not limit to 10s if the method itself takes 30s
            var preliminaryRunParameters = new BenchmarkRunParameters(desiredMaxLatency: TimeSpan.FromSeconds(10));
            var preliminaryRunResults = this.RunBenchmark<TBenchmarkContainer>(preliminaryRunParameters);

            IReadOnlyDictionary<ISampleSizeDeterminer, TimeSpan> estimatedTimeBySource =
                sampleSizeDeterminers
                    .ToDictionary(
                        sampleSizeDeterminer => sampleSizeDeterminer,
                        sampleSizeDeterminer =>
                        {
                            // The time taken for the validator is defined as
                            // Sum(
                            //  each: parameterCase
                            //  case -> 
                            //   iterationCount = max(treatmentIterationCountRequired, baselineIterationCountRequired)
                            //   iterationCount * treatmentTime + iterationCount * baselineTime
                            // )

                            TimeSpan estimatedDuration = TimeSpan.Zero;
                            
                            foreach (var resultAndCase in preliminaryRunResults.ResultsByCase)
                            {
                                var benchmarkCase = resultAndCase.Key;
                                var baselineReport = resultAndCase.Value.Baseline;
                                var treatmentReport = resultAndCase.Value.Treatment;

                                var sampleSizeRequirementForBaseline =
                                    sampleSizeDeterminer.GetSampleSizeRequirement(resultAndCase.Value).SamplesForBaseline;
                                var sampleSizeRequirementForTreatment =
                                    sampleSizeDeterminer.GetSampleSizeRequirement(resultAndCase.Value).SamplesForTreatment;

                                // As of now, we run both for the same amount of samples
                                sampleSizeRequirementForBaseline = 
                                    Math.Max((int) sampleSizeRequirementForBaseline, (int) sampleSizeRequirementForTreatment);
                                sampleSizeRequirementForTreatment = 
                                    Math.Max((int) sampleSizeRequirementForBaseline, (int) sampleSizeRequirementForTreatment);
                                
                                var nanosecondsPerSampleInBaseline = baselineReport
                                    .GetResultRuns()
                                    .Average(workloadMeasurement => workloadMeasurement.Nanoseconds);
                                var timePerSampleInBaseline =
                                    TimeSpan.FromTicks((long) (nanosecondsPerSampleInBaseline / 100));

                                var nanosecondsPerSampleInTreatment = treatmentReport
                                    .GetResultRuns()
                                    .Average(workloadMeasurement => workloadMeasurement.Nanoseconds);
                                var timePerSampleInTreatment =
                                    TimeSpan.FromTicks((long) (nanosecondsPerSampleInTreatment / 100));
                                
                                var totalTimeForBaseline = TimeSpan.FromMilliseconds(timePerSampleInBaseline.TotalMilliseconds * sampleSizeRequirementForBaseline);
                                var totalTimeForTreatment = TimeSpan.FromMilliseconds(timePerSampleInTreatment.TotalMilliseconds * sampleSizeRequirementForTreatment);

                                var totalTimeForCase = totalTimeForBaseline + totalTimeForTreatment;

                                estimatedDuration += totalTimeForCase;
                            }

                            return estimatedDuration;
                        });

            var maxTime = estimatedTimeBySource.Values.Max();
            // TODO: P2 - Need to encapsulate the information about each parameters and how many ITERATIONS they should run for from above
            BenchmarkRunParameters runParameters = new BenchmarkRunParameters(maxTime);

            return new BenchmarkRunEstimate(
                maxTime,
                runParameters,
                estimatedTimeBySource);
        }

        public BenchmarkResults RunBenchmark<TBenchmarkContainer>(BenchmarkRunParameters runParameters)
        {
            var config = new Config(
                runParameters.DesiredMaxLatency,
                this.jobMutator);

            // TODO: P3 - Validate return values to catch invalid usage (e.g. Before throws and After returns - invalid Benchmark comparison because not doing the same thing)
            
            var reports = BenchmarkRunner.Run<TBenchmarkContainer>(config)
                .Reports;
            
            var parameterInstancesComparer = ParameterInstancesComparer.Default;
            var reportsByArgs = reports
                .GroupBy(
                    report => report.BenchmarkCase.Parameters,
                    parameterInstancesComparer);
            
            IDictionary<ParameterInstances, BenchmarkResults.BeforeAndAfter> beforeAndAfters =
                new Dictionary<ParameterInstances, BenchmarkResults.BeforeAndAfter>(parameterInstancesComparer);
            foreach (var reportForArgs in reportsByArgs)
            {
                if (reportForArgs.Count() != 2
                    || reportForArgs.Count(report => report.BenchmarkCase.IsBaseline()) != 1)
                {
                    throw new InvalidOperationException("Expected exactly 1 baseline and 1 treatment");
                }

                var args = reportForArgs.Key;
                var baseline = reportForArgs.Single(report => report.BenchmarkCase.IsBaseline());
                var treatment = reportForArgs.Single(report => !report.BenchmarkCase.IsBaseline());

                beforeAndAfters[args] = new BenchmarkResults.BeforeAndAfter(
                    baseline,
                    treatment);
            }

            return new BenchmarkResults(beforeAndAfters);
        }

        public static IBenchmarkRunner Instance => new DefaultBenchmarkRunner(null);
        
        private class Config : ManualConfig
        {
            public Config(TimeSpan desiredMaxLatency, Func<Job, Job> benchmarkJobMutator)
            {
                // TODO: This does not currently limit to the desired max. Like.. not at all. First it estimates the time forgetting it runs treatment & baseline (x2)
                // it omits warmup time, and it omits reducing the iterationcount in the case that it's IMPOSSIBLE to complete that many iterations in the desiredMaxLatency

                var job = Job.Default;


                job = job.WithEvaluateOverhead(false);

                // TODO: P1 - Dummy for now for testing
                job = job.WithWarmupCount(10);

                // TODO: Issue skipping pilot stage - https://github.com/dotnet/BenchmarkDotNet/issues/837

                int iterationCount = 30; // >30 to use z test

                // TODO: If the invocation count == 1, it's probably not going to be good enough for measuring.
                job = job.WithIterationCount(iterationCount);

                //job = job.With(CsProjClassicNetToolchain.Net46);

                // would rather not have this, but it allows us to have fast unit tests (at the cost of confidence/ False-Positives(Passing))
                var iterationTimeInMilliseconds = desiredMaxLatency.TotalMilliseconds / iterationCount;
                job = job.WithIterationTime(TimeInterval.Millisecond * iterationTimeInMilliseconds);

                // Required for running in unit test frameworks -- TODO: IS IT?
                ////job = job.With(InProcessToolchain.Instance);
                
                // Do not manually remove any outliers, we need them for proper stats
                job = job.WithOutlierMode(OutlierMode.None);

                if (benchmarkJobMutator != null)
                {
                    job = benchmarkJobMutator(job);
                }

                this.Add(job);
                
                // TODO: P3 - We can allow debug with this, but it's dangerous (stuff like alignment hits you hard and makes x != x)
                ////this.Add(JitOptimizationsValidator.DontFailOnError); // To allow running against DEBUG

                // To pipe the output to where most test runners will capture it
                this.Add(ConsoleLogger.Default);
            }
        }
    }
}