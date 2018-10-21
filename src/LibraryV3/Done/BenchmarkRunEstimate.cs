namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BenchmarkRunEstimate
    {
        public TimeSpan EstimatedTime { get; }

        public IReadOnlyDictionary<IBenchmarkValidator, TimeSpan> EstimatedTimeByValidator { get; }

        public BenchmarkRunParameters RunParameters { get; }

        public BenchmarkRunEstimate(TimeSpan estimatedTime, BenchmarkRunParameters runParameters, IReadOnlyDictionary<IBenchmarkValidator, TimeSpan> estimatedTimeByValidator)
        {
            this.EstimatedTime = estimatedTime;
            this.RunParameters = runParameters;
            this.EstimatedTimeByValidator = estimatedTimeByValidator;
        }
    }
}