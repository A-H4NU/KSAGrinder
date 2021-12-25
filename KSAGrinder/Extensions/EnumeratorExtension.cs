using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            int count = 0;

            foreach (T item in source)
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count).ToArray();
        }
    }
}
