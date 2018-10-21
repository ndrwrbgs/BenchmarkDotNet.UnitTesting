namespace LibraryV3
{
    using System;

    public sealed class BenchmarkRunParameters
    {
        public TimeSpan DesiredMaxLatency { get; }

        public BenchmarkRunParameters(TimeSpan desiredMaxLatency)
        {
            this.DesiredMaxLatency = desiredMaxLatency;
        }
    }
}