namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class BenchmarkRunEstimate
    {
        public TimeSpan EstimatedTime { get; }

        public IReadOnlyDictionary<ISampleSizeDeterminer, TimeSpan> EstimatedTimeBySource { get; }

        public BenchmarkRunParameters RunParameters { get; }

        public BenchmarkRunEstimate(TimeSpan estimatedTime, BenchmarkRunParameters runParameters, IReadOnlyDictionary<ISampleSizeDeterminer, TimeSpan> estimatedTimeBySource)
        {
            this.EstimatedTime = estimatedTime;
            this.RunParameters = runParameters;
            this.EstimatedTimeBySource = estimatedTimeBySource;
        }
    }
}