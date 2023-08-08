namespace dsgen.Extensions;

public static class ListFindIndexExtension
{
    /// <summary>
    /// Searches for an element that matches the conditions defined by the specified predicate,
    /// and returns the zero-based index of the first occurrence within the entire
    /// <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <param name="match">
    /// The <see cref="Predicate{T}"/> delegate
    /// that defines the conditions of the element to search for.
    /// </param>
    /// <returns>
    /// The zero-based index of the first occurrence of an element that matches the conditions
    /// defined by <paramref name="match"/>, if found; otherwise, <c>-1</c>.
    /// </returns>
    public static int FindIndex<T>(this IReadOnlyList<T> collection, Predicate<T> match)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            if (match(collection[i]))
                return i;
        }
        return -1;
    }
}
