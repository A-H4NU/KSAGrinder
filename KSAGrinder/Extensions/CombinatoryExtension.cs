using System;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Extensions
{
    public static class CombinatoryExtension
    {
        public static IEnumerator<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            int n = sequences.Count();
            if (n == 0)
            {
                yield return Enumerable.Empty<T>();
            }
            else
            {
                IEnumerator<IEnumerable<T>> smallerProduct = CartesianProduct(sequences.Take(n - 1));
                while (smallerProduct.MoveNext())
                {
                    IEnumerable<T> currentProduct = smallerProduct.Current;
                    foreach (T lastElement in sequences.Last())
                    {
                        yield return currentProduct.Append(lastElement);
                    }
                }
            }
        }

        public static IEnumerator<IEnumerable<T>> GetCombsWithMaxK<T>(this IEnumerable<T> list, int maxLength)
            where T : IComparable
        {
            if (maxLength == 0)
            {
                yield return Enumerable.Empty<T>();
            }
            else if (maxLength == 1)
            {
                foreach (T t in list)
                    yield return new T[] { t };
            }
            else
            {
                IEnumerator<IEnumerable<T>> combsList = GetCombsWithMaxK(list, maxLength - 1);
                while (combsList.MoveNext())
                {
                    IEnumerable<T> combs = combsList.Current;
                    yield return combs;
                    foreach (T t2 in list.Where(o => o.CompareTo(combs.Last()) > 0))
                    {
                        yield return combs.Concat(new T[] { t2 });
                    }
                }
            }
        }
    }
}