namespace LibraryV3
{
    using System;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Jobs;

    public sealed class BenchmarkRunParameters
    {
        public TimeSpan DesiredMaxLatency { get; }

        public BenchmarkRunParameters(TimeSpan desiredMaxLatency)
        {
            this.DesiredMaxLatency = desiredMaxLatency;
        }
    }
}