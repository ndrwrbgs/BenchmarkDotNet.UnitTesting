namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public static class BenchmarkAssert
    {
        public static void ValidatorsPassed(
            IEnumerable<IBenchmarkValidator> validators,
            BenchmarkResults results,
            // Delegate to support multiple test frameworks
            Action<string> assertFailDelegate)
        {
            // Get results
            IReadOnlyList<ValidationResult> allResults = validators
                .SelectMany(validator => validator.GetValidationResults(results))
                .ToArray();

            // Output all results for debugging
            Console.WriteLine(GetOutputMessage(allResults));

            // Fail if there are any violations
            IReadOnlyList<ValidationResult> failedResults = allResults
                .Where(validationResult => validationResult.IsViolation)
                .ToArray();

            if (failedResults.Any())
            {
                var failedMessage = GetOutputMessage(failedResults);
                
                assertFailDelegate(failedMessage);
            }
        }

        private static string GetOutputMessage(IEnumerable<ValidationResult> validationResults)
        {
            var sb = new StringBuilder();
            foreach (ValidationResult validationResult in validationResults)
            {
                sb.AppendLine($"{validationResult.Validator?.GetType()} validation for {validationResult.TestCase} -- {(validationResult.IsViolation ? "failed" : "passed")} with message:");
                sb.AppendLine(
                    string.Join(
                        "\r\n",
                        validationResult.Message
                            .Split(new string[] {"\r\n", "\n"}, StringSplitOptions.None)
                            .Select(line => "\t" + line)));
            }

            return sb.ToString();
        }
    }
}