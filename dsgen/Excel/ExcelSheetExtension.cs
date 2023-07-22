using System.Collections;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Diagnostics;
using dsgen.StringDistance;

namespace dsgen.Excel;

public static class ExcelSheetExtension
{
    public static StringDistanceCalculator DefaultStringDistanceCalculator = Levenshtein.Instance;

    /// <summary>
    /// Try to match each element of <paramref name="reference"/> to one of
    /// <paramref name="headers"/> uniquely.
    /// <paramref name="reference"/> and <paramref name="similarities"/> are arrays of length
    /// <see cref="reference.Length"/> where
    /// <para>
    ///     - <c>reference[i]</c> is matched to <c>headers[result[i]]</c>
    ///     - <c>similarities[i]</c> is the similarity between them
    /// </para>
    /// for each `i` in `0..reference.Length`.
    /// </summary>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns><see cref="true"/> if matched successfully; otherwise, <see cref="false"/>.</returns>
    private static bool TryMatchHeaders(
        string?[] headers,
        string[] reference,
        [NotNullWhen(true)] out int[]? result,
        [NotNullWhen(true)] out float[]? similarities,
        StringDistanceCalculator? calculator = null
    )
    {
        static int FindBestMatch(
            ReadOnlySpan<char> str,
            string?[] headers,
            StringDistanceCalculator calculator,
            out float similarity
        )
        {
            int bestIdx = -1;
            similarity = Single.MinValue;
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] is null)
                    continue;
                float sim = calculator.Similarity(str, headers[i]);
                if (similarity < sim)
                {
                    similarity = sim;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        Guard.IsNotEmpty(headers);
        Guard.IsNotEmpty(reference);
        result = null;
        similarities = null;
        if (headers.Length < reference.Length)
            return false;
        calculator ??= DefaultStringDistanceCalculator;
        int[] res = new int[reference.Length];
        float[] sims = new float[reference.Length];
        BitArray matched = new(headers.Length);
        for (int i = 0; i < reference.Length; i++)
        {
            int match = FindBestMatch(reference[i], headers, calculator, out sims[i]);
            if (match == -1 || matched[match])
                return false;
            matched[match] = true;
            res[i] = match;
        }
        result = res;
        similarities = sims;
        return true;
    }

    /// <summary>
    /// Try finding the first row that can be considered as a header inferring from <paramref name="reference"/>.
    /// <para>
    ///     - <c>reference[i]</c> is matched to <c>sheet[result, i]</c>
    ///     - <c>similarities[i]</c> is the similarity between them
    /// </para>
    /// </summary>
    /// <param name="result">
    /// On success, contains the zero-based index of the first header row.
    /// On failure, contains <c>-1</c>.</param>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns><c>true</c> if the header row is found; otherwise, <c>false</c>.</returns>
    public static bool TryFindSheetHeaderRow(
        this ExcelSheet sheet,
        string[] reference,
        out int result,
        [NotNullWhen(true)] out int[]? matchResult,
        [NotNullWhen(true)] out float[]? similarities,
        StringDistanceCalculator? calculator = null
    )
    {
        result = -1;
        matchResult = null;
        similarities = null;
        for (int i = 0; i < sheet.RowCount; i++)
        {
            if (
                Enumerable
                    .Range(0, sheet.ColumnCount)
                    .All(j => sheet[i, j] is null || sheet[i, j] is string)
                && TryMatchHeaders(
                    Enumerable
                        .Range(0, sheet.ColumnCount)
                        .Select(j => (string?)sheet[i, j])
                        .ToArray(),
                    reference,
                    out matchResult,
                    out similarities,
                    calculator
                )
            )
            {
                result = i;
                return true;
            }
        }
        return false;
    }
}
