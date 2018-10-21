namespace LibraryV3
{
    using BenchmarkDotNet.Parameters;

    public sealed class ValidationResult
    {
        public ParameterInstances TestCase { get; }
        public IBenchmarkValidator Validator { get; }
        public string Message { get; }

        public bool IsViolation { get; }

        public ValidationResult(ParameterInstances testCase, IBenchmarkValidator validator, string message, bool isViolation)
        {
            this.TestCase = testCase;
            this.Validator = validator;
            this.Message = message;
            this.IsViolation = isViolation;
        }

        public override string ToString()
        {
            return this.Message;
        }
    }
}