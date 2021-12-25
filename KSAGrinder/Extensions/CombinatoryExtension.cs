using System;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Extensions
{
    public static class CombinatoryExtension
    {
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            int n = sequences.Count();
            if (n == 0)
            {
                yield return Enumerable.Empty<T>();
            }
            else
            {
                foreach (IEnumerable<T> smallerProduct in CartesianProduct(sequences.Take(n - 1)))
                {
                    foreach (T lastElement in sequences.Last())
                    {
                        yield return smallerProduct.Append(lastElement);
                    }
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> GetCombsFromZeroToK<T>(this IEnumerable<T> list, int k)
            where T : IComparable<T>
        {
            IEnumerable<IEnumerable<T>> GetKCombsFromLastCombs(int length, IEnumerable<IEnumerable<T>> lastCombs)
            {
                if (length == 1)
                {
                    foreach (T t in list)
                        yield return new T[] { t };
                }
                else
                {
                    foreach (IEnumerable<T> comb in lastCombs)
                    {
                        foreach (T t in list.Where(o => o.CompareTo(comb.Last()) > 0))
                        {
                            yield return comb.Concat(new T[] { t });
                        }
                    }
                }
            }

            if (k < 0) throw new ArgumentOutOfRangeException("k", k, "k must be nonnegative");
            yield return Enumerable.Empty<T>();
            IEnumerable<IEnumerable<T>> last = new IEnumerable<T>[] { Enumerable.Empty<T>() };
            for (int i = 1; i <= k; ++i)
            {
                List<IEnumerable<T>> newLast = new List<IEnumerable<T>>();
                foreach (IEnumerable<T> comb in GetKCombsFromLastCombs(i, last))
                {
                    yield return comb;
                    newLast.Add(comb);
                }
                last = newLast;
            }
        }
    }
}