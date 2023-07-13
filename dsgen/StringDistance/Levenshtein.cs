using System.Runtime.CompilerServices;

namespace dsgen.StringDistance;

/// <summary>
/// The Levenshtein distance.
/// <p>
///     https://en.wikipedia.org/wiki/Levenshtein_distance
///     https://stackoverflow.com/a/57961456
/// </p>
/// </summary>
public sealed class Levenshtein : StringDistanceCalculator
{
    public Levenshtein() { }

    public override unsafe int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        int m = a.Length, n = b.Length;

        if (m > n)
            return Distance(b, a);
        if (m == 0)
            return n;

        const int MaxStackLimit = 256;
        //'previous' cost array, horizontally
        Span<int> spanP = m + 1 <= MaxStackLimit ?
            stackalloc int[m + 1] : new int[m + 1];
        // cost array, horizontally
        Span<int> spanD = m + 1 <= MaxStackLimit ?
            stackalloc int[m + 1] : new int[m + 1];
        int* p = (int*)Unsafe.AsPointer(ref spanP.GetPinnableReference());
        int* d = (int*)Unsafe.AsPointer(ref spanD.GetPinnableReference());

        for (int i = 0; i <= m; i++)
        {
            p[i] = i;
        }

        for (int j = 1; j <= n; j++)
        {
            char bJ = b[j - 1]; // jth character of t
            d[0] = j;

            for (int i = 1; i <= m; i++)
            {
                int cost = a[i - 1] == bJ ? 0 : 1; // cost
                                                   // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
            }
            // copy current distance counts to 'previous row' distance counts
            int* placeHolder = p;
            p = d;
            d = placeHolder;
        }

        // our last action in the above loop was to switch d and p, so p now 
        // actually has the most recent cost counts
        return p[m];
    }
}
