namespace LibraryV3
{
    public sealed class SamplesRequirement
    {
        public int SamplesForBaseline { get; }
        public int SamplesForTreatment { get; }

        public SamplesRequirement(int samplesForBaseline, int samplesForTreatment)
        {
            this.SamplesForBaseline = samplesForBaseline;
            this.SamplesForTreatment = samplesForTreatment;
        }
    }
}