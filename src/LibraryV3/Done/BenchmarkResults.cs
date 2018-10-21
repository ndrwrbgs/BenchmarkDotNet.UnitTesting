namespace LibraryV3
{
    using System.Collections.Generic;
    using BenchmarkDotNet.Parameters;
    using BenchmarkDotNet.Reports;

    public sealed class BenchmarkResults
    {
        public BenchmarkResults(IDictionary<ParameterInstances, BeforeAndAfter> resultsByCase)
        {
            this.ResultsByCase = resultsByCase;
        }

        public IDictionary<ParameterInstances, BeforeAndAfter> ResultsByCase { get; }

        public sealed class BeforeAndAfter
        {
            public BenchmarkReport Baseline { get; }
            public BenchmarkReport Treatment { get; }

            public BeforeAndAfter(BenchmarkReport baseline, BenchmarkReport treatment)
            {
                this.Baseline = baseline;
                this.Treatment = treatment;
            }
        }
    }
}