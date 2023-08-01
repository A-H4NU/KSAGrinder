namespace dsgen;

public static class LinqExtension
{
    /// <summary>
    /// Returns the elements that are contained in every list.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return the first element of.</param>
    /// <returns>
    /// Returns an empty enumerable if source is empty;
    /// otherwise returns the intersection of all elements of <paramref name="source"/>.
    /// </returns>
    public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> source)
    {
        source.First();
        using IEnumerator<IEnumerable<T>> e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            return Enumerable.Empty<T>();
        }

        HashSet<T> res = new(e.Current);
        while (e.MoveNext())
        {
            res.IntersectWith(e.Current);
        }
        return res;
    }
}
