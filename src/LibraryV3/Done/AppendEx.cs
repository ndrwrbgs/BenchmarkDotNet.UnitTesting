namespace LibraryV3
{
    using System.Collections.Generic;

    internal static class AppendEx{
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
        {
            foreach (var i in source)
            {
                yield return i;
            }

            yield return item;
        }
    }
}
