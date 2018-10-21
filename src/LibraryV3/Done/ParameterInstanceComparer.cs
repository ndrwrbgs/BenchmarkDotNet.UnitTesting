namespace LibraryV3
{
    using System.Collections.Generic;
    using BenchmarkDotNet.Parameters;

    internal sealed class ParameterInstanceComparer : IEqualityComparer<ParameterInstance>
    {
        public static IEqualityComparer<ParameterInstance> Default = new ParameterInstanceComparer();
        public bool Equals(ParameterInstance x, ParameterInstance y)
        {
            return string.Equals(x.Name, y.Name) && Equals(x.Value, y.Value);
        }

        public int GetHashCode(ParameterInstance obj)
        {
            return obj.Value.GetHashCode();
        }
    }
}