namespace LibraryV3
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Jobs;

    public static class BenchmarkRunnerExtensions
    {
        public static ISpecificBenchmarkRunner ForBenchmarkContainer<TBenchmarkContainer>(
            this IBenchmarkRunner runner)
        {
            return new SpecificBenchmarkRunner(runner, typeof(TBenchmarkContainer));
        }

        public static void RunWithValidatorAndAssertPassed(
            this ISpecificBenchmarkRunner runner,
            IBenchmarkValidator validator,
            Action<string> assertFailDelegate)
        {
            RunWithValidatorsAndAssertPassed(
                runner,
                new[] {validator},
                assertFailDelegate);
        }

        public static void RunWithValidatorsAndAssertPassed(
            this ISpecificBenchmarkRunner runner,
            IEnumerable<IBenchmarkValidator> validators,
            Action<string> assertFailDelegate)
        {
            // Single enumeration
            validators = validators.ToList();

            var results = runner.RunBenchmark(validators);
            BenchmarkAssert.ValidatorsPassed(
                validators,
                results,
                assertFailDelegate);
        }

        public static BenchmarkResults RunBenchmark(
            this ISpecificBenchmarkRunner runner,
            IBenchmarkValidator forValidator)
        {
            return RunBenchmark(runner, new[] {forValidator});
        }

        public static BenchmarkResults RunBenchmark(
            this ISpecificBenchmarkRunner runner,
            IEnumerable<IBenchmarkValidator> forValidators)
        { 
            // TODO: Restore this
            //var estimate = runner.GetRunEstimate(forSampleSizeDeterminers);
            //return runner.RunBenchmark(estimate.RunParameters);
            return runner.RunBenchmark(new BenchmarkRunParameters(TimeSpan.FromSeconds(1)));
        }

        public static BenchmarkResults RunBenchmark<TBenchmarkContainer>(
            this IBenchmarkRunner runner,
            IEnumerable<ISampleSizeDeterminer> forSampleSizeDeterminers)
        {
            var estimate = runner.GetRunEstimate<TBenchmarkContainer>(forSampleSizeDeterminers);
            return runner.RunBenchmark<TBenchmarkContainer>(estimate.RunParameters);
        }

        public static ISpecificBenchmarkRunner For<TContainerBaseline, TContainerTreatment>(
            this IBenchmarkRunner runner,
            Expression<Action<TContainerBaseline>> baseline,
            Expression<Action<TContainerTreatment>> treatment)
        {
            ValidateForMethodPreconditions(baseline, treatment);

            var baselineMethod = ((MethodCallExpression) baseline.Body).Method;
            var treatmentMethod = ((MethodCallExpression) treatment.Body).Method;

            var type = baselineMethod.DeclaringType;

            var methodInfo = typeof(BenchmarkRunnerExtensions).GetMethod(nameof(ForBenchmarkContainer));
            var gen = methodInfo.MakeGenericMethod(type);
            
            object target = null; // Because it's static
            return (ISpecificBenchmarkRunner) gen.Invoke(target, new object[] { runner });
        }

        private static void ValidateForMethodPreconditions<TContainerBaseline, TContainerTreatment>(
            Expression<Action<TContainerBaseline>> baseline,
            Expression<Action<TContainerTreatment>> treatment)
        {
            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }

            if (treatment == null)
            {
                throw new ArgumentNullException(nameof(treatment));
            }

            var baselineBody = baseline.Body;
            var treatmentBody = treatment.Body;

            if (baselineBody == null || treatmentBody == null)
            {
                throw new ArgumentException();
            }

            var baselineBodyMethodExpression = baselineBody as MethodCallExpression;
            var treatmentBodyMethodExpression = treatmentBody as MethodCallExpression;

            if (baselineBodyMethodExpression == null || treatmentBodyMethodExpression == null)
            {
                throw new ArgumentException("Presently, the delegate is limited to being a direct method call. E.g. `(BenchmarkContainer container) => container.CallMethod()` ");
            }

            var baselineBodyMethod = baselineBodyMethodExpression.Method;
            var treatmentBodyMethod = treatmentBodyMethodExpression.Method;

            if (baselineBodyMethod.DeclaringType != treatmentBodyMethod.DeclaringType)
            {
                throw new ArgumentException("Presently, the delegates must both be for the same class");
            }
            var containingType = baselineBodyMethod.DeclaringType;

            var baselineAttributes = baselineBodyMethod.GetCustomAttributes<BenchmarkAttribute>().ToArray();
            var treatmentAttributes = treatmentBodyMethod.GetCustomAttributes<BenchmarkAttribute>().ToArray();
            if (!baselineAttributes.Any()
                || !treatmentAttributes.Any())
            {
                throw new ArgumentException("Presently, methods must have [BenchmarkAttribute] annotations");
            }

            var baselineBenchmarkAttribute = baselineAttributes.Single();
            if (!baselineBenchmarkAttribute.Baseline)
            {
                throw new ArgumentException("Presently, baseline must have the [Benchmark(Baseline = true)] attribute.");
            }
            
            var treatmentBenchmarkAttribute = treatmentAttributes.Single();
            if (treatmentBenchmarkAttribute.Baseline)
            {
                throw new ArgumentException("Presently, treatment must have the [Benchmark(Baseline = false)] attribute.");
            }

            var allMethodsInClass = containingType
                .GetMethods();
            var benchmarkMethodsInClass = allMethodsInClass
                .Where(method => method.GetCustomAttributes<BenchmarkAttribute>().Any());

            if (benchmarkMethodsInClass.Count() != 2)
            {
                throw new ArgumentException("Presently, the container class must have exactly 2 Benchmark methods - the Baseline and the Treatment.");
            }
        }
    }
}