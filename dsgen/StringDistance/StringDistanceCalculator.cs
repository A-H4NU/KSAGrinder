namespace dsgen.StringDistance;

/// <summary>
/// Provide the strategy for calculating the edit distance between two strings.
/// <p>
/// https://en.wikipedia.org/wiki/Edit_distance
/// </p>
/// </summary>
public abstract class StringDistanceCalculator
{
    /// <summary>
    /// Calculate the distance between two strings.
    /// </summary>
    /// <returns>The distance.</returns>
    public abstract int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b);

    /// <summary>
    /// Calculate the distance between two strings.
    /// </summary>
    /// <returns>The distance.</returns>
    public virtual int Distance(string a, string b)
    {
        return Distance((ReadOnlySpan<char>)a, (ReadOnlySpan<char>)b);
    }

    /// <summary>
    /// Calculate the similarity between two strings.
    /// </summary>
    /// <returns>The similarity. Ranges from 0 to 1.</returns>
    public virtual float Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        return (float)Distance(a, b) / Math.Max(a.Length, b.Length);
    }

    /// <summary>
    /// Calculate the similarity between two strings.
    /// </summary>
    /// <returns>The similarity. Ranges from 0 to 1.</returns>
    public virtual float Similarity(string a, string b)
    {
        return Similarity((ReadOnlySpan<char>)a, (ReadOnlySpan<char>)b);
    }
}
