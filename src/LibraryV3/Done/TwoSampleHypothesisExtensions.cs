using System;
using Accord.Statistics.Testing;

namespace LibraryV3
{
    internal static class TwoSampleHypothesisExtensions
    {
        public static string ToDescriptiveString(this TwoSampleHypothesis hypothesis,
            string firstSampleName,
            string secondSampleName)
        {
            switch(hypothesis)
            {
                case TwoSampleHypothesis.ValuesAreDifferent:
                    return $"{firstSampleName} != {secondSampleName}";
                case TwoSampleHypothesis.FirstValueIsGreaterThanSecond:
                    return $"{firstSampleName} > {secondSampleName}";
                case TwoSampleHypothesis.FirstValueIsSmallerThanSecond:
                    return $"{firstSampleName} < {secondSampleName}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(hypothesis), hypothesis, null);
            }
        }
    }
}