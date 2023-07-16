using CommunityToolkit.Diagnostics;
using dsgen.Excel;
using dsgen.StringDistance;
using dsgen.ColumnInfo;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

    private static readonly (
        string HeaderTitle,
        string ColumnName,
        IEnumerable<Type?> Types
    )[] ClassSheetTitles;

    public static StringDistanceCalculator StringDistanceCalculator { get; set; } =
        Levenshtein.Instance;

    /// <summary>
    /// Initializes <see cref="ClassSheetTitles"/>.
    /// </summary>
    static SheetTypeEvaluator()
    {
        /* Initialize ClassSheetTitles with reflection. */
        FieldInfo[] headerTitleProps = typeof(HeaderTitle).GetFields(
            BindingFlags.Public | BindingFlags.Static
        );
        FieldInfo[] columnNameProps = typeof(ColumnName).GetFields(
            BindingFlags.Public | BindingFlags.Static
        );
        FieldInfo[] typesProps = typeof(Types).GetFields(BindingFlags.Public | BindingFlags.Static);
        Guard.IsNotEmpty(columnNameProps);
        Guard.IsEqualTo(columnNameProps.Length - headerTitleProps.Length, 0);
        Guard.IsEqualTo(headerTitleProps.Length - typesProps.Length, 0);
        int n = columnNameProps.Length;
        ClassSheetTitles = new (string, string, IEnumerable<Type?>)[n];
        for (int i = 0; i < n; i++)
        {
            object? h = headerTitleProps[i].GetValue(null);
            object? c = columnNameProps[i].GetValue(null);
            object? t = typesProps[i].GetValue(null);
            if (h is string headerTitle && c is string columnName && t is IEnumerable<Type?> types)
            {
                ClassSheetTitles[i] = (headerTitle, columnName, types);
            }
            else
            {
                throw new UnreachableException();
            }
        }
    }

    /// <summary>
    /// Evaluates the probability for the sheet can be used to generate class
    /// and lecture data.
    /// </summary>
    /// <param name="sheet">The sheet to evaluate.</param>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns>Probability. Ranges from 0 to 1.</returns>
    public static float ClassSheetScore(
        ExcelSheet sheet,
        StringDistanceCalculator? calculator = null
    )
    {
        static int GetMax(ReadOnlySpan<int> span, out int maxIndex)
        {
            Guard.IsNotEmpty(span);
            int max = Int32.MinValue;
            maxIndex = -1;
            for (int i = 0; i < span.Length; i++)
            {
                if (max < span[i])
                {
                    max = span[i];
                    maxIndex = i;
                }
            }
            return max;
        }

        if (sheet.Hidden || sheet.ColumnCount < 9)
            return 0f;

        const int MaxStackLimit = 0x100;
        // lectureCodeMatches[i] = "# of entries in the i-th column that matches LectureCodeRegex()"
        // Similar for timeMatches.
        Span<int> codeMatches =
            sheet.ColumnCount <= MaxStackLimit
                ? stackalloc int[sheet.ColumnCount]
                : new int[sheet.ColumnCount];
        BitArray[] doesMatchCode = Enumerable
            .Range(0, sheet.ColumnCount)
            .Select(i => new BitArray(sheet.RowCount))
            .ToArray();
        Span<int> timeMatches =
            sheet.ColumnCount <= MaxStackLimit
                ? stackalloc int[sheet.ColumnCount]
                : new int[sheet.ColumnCount];
        BitArray[] doesMatchTime = Enumerable
            .Range(0, sheet.ColumnCount)
            .Select(i => new BitArray(sheet.RowCount))
            .ToArray();
        // The first row that is matched to the header.
        int headerRow = -1;
        int[]? matchResult = null;
        float[]? similarities = null;
        for (int i = 0; i < sheet.RowCount; i++)
        {
            for (int j = 0; j < sheet.ColumnCount; j++)
            {
                string? str = sheet[i, j]?.ToString();
                if (str is null)
                    continue;
                doesMatchCode[j].Set(i, LectureCodeRegex().IsMatch(str));
                doesMatchTime[j].Set(i, TimeRegex().IsMatch(str));
                if (doesMatchCode[j].Get(i))
                    codeMatches[j]++;
                if (doesMatchTime[j].Get(i))
                    timeMatches[j]++;
            }
            if (
                headerRow == -1
                && Enumerable.Range(0, sheet.ColumnCount).All(j => sheet[i, j] is null || sheet[i, j] is string)
            )
            {
                string?[] headers = Enumerable
                    .Range(0, sheet.ColumnCount)
                    .Select(j => (string?)sheet[i, j])
                    .ToArray();
                if (
                    TryMatchHeadersToClassSheetTitles(
                        headers,
                        out matchResult,
                        out similarities,
                        calculator
                    )
                )
                    headerRow = i;
            }
        }
        if (headerRow == -1)
            return 0f;
        float codeScore = (float)GetMax(codeMatches, out int codeHeaderIdx) / sheet.RowCount;
        float timeScore = (float)GetMax(timeMatches, out int timeHeaderIdx) / sheet.RowCount;
        if (codeScore == 0f || timeScore == 0f)
            return 0f;
        int codeIdxInArray = Array.FindIndex(
            ClassSheetTitles,
            tuple => tuple.ColumnName == ColumnName.Code
        );
        int timeIdxInArray = Array.FindIndex(
            ClassSheetTitles,
            tuple => tuple.ColumnName == ColumnName.Time
        );
        if (
            codeIdxInArray == -1
            || matchResult![codeIdxInArray] != codeHeaderIdx
            || timeIdxInArray == -1
            || matchResult![timeIdxInArray] != timeHeaderIdx
        )
            return 0f;
        float matchingRows = 0;
        for (int i = headerRow + 1; i < sheet.RowCount; i++)
        {
            if (!(doesMatchCode[codeHeaderIdx][i] && doesMatchTime[timeHeaderIdx][i]))
                continue;
            bool matching = true;
            for (int j = 0; j < ClassSheetTitles.Length; j++)
            {
                if (!ClassSheetTitles[j].Types.Contains(sheet[i, matchResult![j]]?.GetType()))
                {
                    matching = false;
                    break;
                }
            }
            if (matching)
                matchingRows++;
        }
        float matchScore = (float)matchingRows / (sheet.RowCount - headerRow - 1);
        return Enumerable.Min(new float[] { codeScore, timeScore, matchScore });
    }

    /// <summary>
    /// Evaluates the probability for the sheet can be used to generate student data.
    /// </summary>
    /// <param name="sheet">The sheet to evaluate.</param>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns>Probability. Ranges from 0 to 1.</returns>
    public static float StudentSheetScore(
        ExcelSheet sheet,
        StringDistanceCalculator? calculator = null
    )
    {
        return 0f;
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
        if (calculator is null)
            calculator = StringDistanceCalculator;
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

    private static bool TryMatchHeadersToClassSheetTitles(
        string?[] headers,
        [NotNullWhen(true)] out int[]? result,
        [NotNullWhen(true)] out float[]? similarities,
        StringDistanceCalculator? calculator = null
    )
    {
        var reference = (from tuple in ClassSheetTitles select tuple.HeaderTitle).ToArray();
        return TryMatchHeaders(headers, reference, out result, out similarities, calculator);
    }
}
