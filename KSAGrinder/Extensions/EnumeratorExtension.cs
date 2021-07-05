using System.Collections.Generic;

namespace KSAGrinder.Extensions
{
    public static class EnumeratorExtension
    {
        /// <summary>
        /// Converts a finite <see cref="IEnumerator{T}"/> to an <see cref="IEnumerable{T}"/> instance.
        /// </summary>
        /// <param name="enumerator">A finite enumerator.</param>
        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}
