namespace LibraryV3
{
    using JetBrains.Annotations;

    /// <summary>
    /// Builder/factory methods for <see cref="IBenchmarkValidator"/> based on latency
    /// </summary>
    public static class LatencyValidatorFactory
    {
        [PublicAPI]
        public static LatencyValidatorBuilder Builder => new LatencyValidatorBuilder();
        
        #region Factory methods for common patterns

        [PublicAPI]
        public static IBenchmarkValidator FailIfCanSaySlowerThan(double confidenceLevel)
        {
            return Builder
                .IfSlowerThan(confidenceLevel, then: LatencyValidatorBehavior.Fail)
                .Otherwise(LatencyValidatorBehavior.Pass);
        }
        
        [PublicAPI]
        public static IBenchmarkValidator PassOnlyIfFasterThan(double withConfidenceLevel)
        {
            return Builder
                .IfFasterThan(withConfidenceLevel, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
        }
        
        [PublicAPI]
        public static IBenchmarkValidator FailIfCannotSayFasterThan(double withConfidenceLevel)
        {
            return Builder
                .IfFasterThan(withConfidenceLevel, then: LatencyValidatorBehavior.Pass)
                .Otherwise(LatencyValidatorBehavior.Fail);
        }

        #endregion
    }
}