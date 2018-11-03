namespace LibraryV3
{
    public interface ISampleSizeDeterminer
    {
        SamplesRequirement GetSampleSizeRequirement(BenchmarkResults.BeforeAndAfter basedOnPreliminaryResults);
    }
}