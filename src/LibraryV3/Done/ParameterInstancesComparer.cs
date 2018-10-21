namespace LibraryV3
{
    using System.Collections.Generic;
    using System.Linq;
    using BenchmarkDotNet.Parameters;

    internal sealed class ParameterInstancesComparer : IEqualityComparer<ParameterInstances>
    {
        public static IEqualityComparer<ParameterInstances> Default = new ParameterInstancesComparer(ParameterInstanceComparer.Default);

        private readonly IEqualityComparer<ParameterInstance> parameterInstanceComparer;

        public ParameterInstancesComparer(IEqualityComparer<ParameterInstance> parameterInstanceComparer)
        {
            this.parameterInstanceComparer = parameterInstanceComparer;
        }

        public bool Equals(ParameterInstances x, ParameterInstances y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }
            
            return x.Items.SequenceEqual(y.Items, this.parameterInstanceComparer);
        }
        
        public int GetHashCode(ParameterInstances obj)
        {
            unchecked
            {
                var hashCode = 0;
                foreach (var item in obj.Items)
                {
                    hashCode = (hashCode * 397) ^ (item != null ? this.parameterInstanceComparer.GetHashCode(item) : 0);
                }
                return hashCode;
            }
        }
    }
}