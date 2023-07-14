using CommunityToolkit.Diagnostics;
using dsgen.Excel;
using dsgen.StringDistance;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace dsgen;

public static partial class SheetTypeEvaluator
{
    [GeneratedRegex(
        pattern: @"\A[a-z]{2,}\d{3,}\z",
        options: RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    private static partial Regex LectureCodeRegex();

    [GeneratedRegex(
        pattern: @"\A([월|화|수|목|금]\d{1,2})(?:[\||,|.| |;|/]([월|화|수|목|금]\d{1,2}))*\z",
        options: RegexOptions.Singleline
    )]
    private static partial Regex TimeRegex();

    public static readonly string[] ClassSheetTitles =
    {
        "교과목코드",
        "교과군",
        "교과목명",
        "학년",
        "분반",
        "담당교원",
        "요일/시간",
        "신청수",
        "학점",
        "비고",
    };

    public static StringDistanceCalculator StringDistanceCalculator { get; set; } =
        Levenshtein.Instance;

    /// <summary>
    /// Evaluates the probability for the sheet can be used to generate class
    /// and lecture data.
    /// </summary>
    /// <param name="sheet">The sheet to evaluate.</param>
    /// <returns>Probability. Ranges from 0 to 1.</returns>
    public static float ClassSheetProbability(ExcelSheet sheet)
    {
        static int GetMax(ReadOnlySpan<int> span)
        {
            int max = Int32.MinValue;
            for (int i = 0; i < span.Length; i++)
                if (max < span[i])
                    max = span[i];
            return max;
        }

        if (sheet.Hidden || sheet.ColumnCount < 9)
            return 0.0f;

        const int MaxStackLimit = 0x100;
        Span<int> lectureCodeMatches =
            sheet.ColumnCount <= MaxStackLimit
                ? stackalloc int[sheet.ColumnCount]
                : new int[sheet.ColumnCount];
        Span<int> timeMatches =
            sheet.ColumnCount <= MaxStackLimit
                ? stackalloc int[sheet.ColumnCount]
                : new int[sheet.ColumnCount];
        for (int i = 0; i < sheet.RowCount; i++)
        {
            for (int j = 0; j < sheet.ColumnCount; j++)
            {
                string? str = sheet[i, j]?.ToString();
                if (str is null)
                    continue;
                if (LectureCodeRegex().IsMatch(str))
                    lectureCodeMatches[j]++;
                if (TimeRegex().IsMatch(str))
                    timeMatches[j]++;
            }
        }
        return (float)Math.Min(GetMax(lectureCodeMatches), GetMax(timeMatches)) / sheet.RowCount;
    }

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
        string[] headers,
        string[] reference,
        [NotNullWhen(true)] out int[]? result,
        [NotNullWhen(true)] out float[]? similarities,
        StringDistanceCalculator? calculator = null
    )
    {
        static int FindBestMatch(
            ReadOnlySpan<char> str,
            string[] headers,
            StringDistanceCalculator calculator,
            out float similarity
        )
        {
            int bestIdx = -1;
            similarity = Single.MinValue;
            for (int i = 0; i < headers.Length; i++)
            {
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
        if (calculator is null)
            calculator = StringDistanceCalculator;
        int[] res = new int[reference.Length];
        float[] sims = new float[reference.Length];
        BitArray matched = new(headers.Length);
        for (int i = 0; i < reference.Length; i++)
        {
            int match = FindBestMatch(reference[i], headers, calculator, out sims[i]);
            if (matched[match])
                return false;
            matched[match] = true;
            res[i] = match;
        }
        result = res;
        similarities = sims;
        return true;
    }

    public static bool TryMatchHeadersToClassSheetTitles(
        string[] headers,
        [NotNullWhen(true)] out int[]? result,
        [NotNullWhen(true)] out float[]? similarities,
        StringDistanceCalculator? calculator = null
    )
    {
        return TryMatchHeaders(headers, ClassSheetTitles, out result, out similarities, calculator);
    }
}
