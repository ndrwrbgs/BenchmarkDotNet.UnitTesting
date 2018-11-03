using Accord;

namespace LibraryV3
{
    public sealed class TwoSampleHypothesisTestResult
    {
        public bool IsSignificant { get; }
        public DoubleRange ConfidenceInterval { get; }
        public double ObservedDifference { get; }

        public TwoSampleHypothesisTestResult(bool isSignificant, DoubleRange confidenceInterval, double observedDifference)
        {
            IsSignificant = isSignificant;
            ConfidenceInterval = confidenceInterval;
            ObservedDifference = observedDifference;
        }
    }
}