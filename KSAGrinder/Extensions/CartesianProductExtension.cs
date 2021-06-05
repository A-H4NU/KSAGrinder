using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Extensions
{
    // Author: https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
    public static class CartesianProductExtension
    {
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>
            (this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct =
                new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accSeq in accumulator
                    from item in sequence
                    select accSeq.Concat(new[] { item }));
        }
    }
}