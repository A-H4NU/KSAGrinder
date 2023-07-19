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

    [GeneratedRegex(pattern: @"\A\d{2}-\d{3}\z", options: RegexOptions.Singleline)]
    private static partial Regex StudentIdRegex();

    private static readonly (
        string HeaderTitle,
        string ColumnName,
        IReadOnlyCollection<Type?> Types
    )[] ClassSheetTitles;

    public static StringDistanceCalculator StringDistanceCalculator { get; set; } =
        Levenshtein.Instance;

    /// <summary>
    /// Initializes <see cref="ClassSheetTitles"/>.
    /// </summary>
    static SheetTypeEvaluator()
    {
        static void SortFieldInfosByName(FieldInfo[] array) =>
            Array.Sort(array.Select(f => f.Name).ToArray(), array);

        /* Initialize ClassSheetTitles with reflection. */
        FieldInfo[] headerTitleFields = typeof(HeaderTitle).GetFields(
            BindingFlags.Public | BindingFlags.Static
        );
        FieldInfo[] columnNameFields = typeof(ColumnName).GetFields(
            BindingFlags.Public | BindingFlags.Static
        );
        FieldInfo[] typesFields = typeof(Types).GetFields(
            BindingFlags.Public | BindingFlags.Static
        );
        Guard.IsNotEmpty(columnNameFields);
        Debug.Assert(headerTitleFields.Length == columnNameFields.Length);
        Debug.Assert(columnNameFields.Length == typesFields.Length);
        /* Sort to assert the names of the fields match. */
        SortFieldInfosByName(headerTitleFields);
        SortFieldInfosByName(columnNameFields);
        SortFieldInfosByName(typesFields);
        int n = columnNameFields.Length;
        for (int i = 0; i < n; i++)
        {
            Debug.Assert(headerTitleFields[i].Name == columnNameFields[i].Name);
            Debug.Assert(columnNameFields[i].Name == typesFields[i].Name);
        }
        ClassSheetTitles = new (string, string, IReadOnlyCollection<Type?>)[n];
        for (int i = 0; i < n; i++)
        {
            object? h = headerTitleFields[i].GetValue(null);
            object? c = columnNameFields[i].GetValue(null);
            object? t = typesFields[i].GetValue(null);
            if (
                h is string headerTitle
                && c is string columnName
                && t is IReadOnlyCollection<Type?> types
            )
            {
                ClassSheetTitles[i] = (headerTitle, columnName, types);
            }
            else
            {
                Debug.Fail("Unreachable.");
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
        if (sheet.Hidden || sheet.ColumnCount < 9)
            return 0f;

        // codeMatches[i] = "# of entries in the i-th column that matches LectureCodeRegex()"
        // Similar for timeMatches.
        int[] codeMatches = CountForEachColumn(
            sheet,
            obj => DoesMatchRegex(LectureCodeRegex(), obj),
            out BitArray[] doesMatchCode
        );
        int[] timeMatches = CountForEachColumn(
            sheet,
            obj => DoesMatchRegex(TimeRegex(), obj),
            out BitArray[] doesMatchTime
        );
        if (
            !TryFindClassSheetHeaderRow(
                sheet,
                out int headerRow,
                out int[]? matchResult,
                out float[]? similarities,
                calculator
            )
        )
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
            || matchResult[codeIdxInArray] != codeHeaderIdx
            || timeIdxInArray == -1
            || matchResult[timeIdxInArray] != timeHeaderIdx
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
                if (!ClassSheetTitles[j].Types.Contains(sheet[i, matchResult[j]]?.GetType()))
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
        if (sheet.Hidden)
            return 0f;
        int[] idMatches = CountForEachColumn(
            sheet,
            obj => DoesMatchRegex(StudentIdRegex(), obj),
            out _
        );
        return (float)GetMax(idMatches, out _) / sheet.RowCount;
    }

    /// <summary>
    /// Return the maximum element of <paramref name="source"/>.
    /// </summary>
    /// <param name="maxIndex">
    /// The index that contains the maximum element. If there are many, it is the first index.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="source"/> is empty.</exception>
    private static int GetMax(ReadOnlySpan<int> source, out int maxIndex)
    {
        Guard.IsNotEmpty(source);
        int max = Int32.MinValue;
        maxIndex = -1;
        for (int i = 0; i < source.Length; i++)
        {
            if (max < source[i])
            {
                max = source[i];
                maxIndex = i;
            }
        }
        return max;
    }

    /// <summary>
    /// Return true if <paramref name="obj"/> is a string that matches <paramref name="regex"/>.
    /// </summary>
    private static bool DoesMatchRegex(Regex regex, object? obj)
    {
        return obj is string str ? regex.IsMatch(str) : false;
    }

    /// <summary>
    /// For each column, count the number of cells that matches <paramref name="predicate"/>.
    /// </summary>
    /// <param name="sheet">The sheet to target and count.</param>
    /// <param name="results">
    /// The result of evaluating <paramref name="predicate"/> for each cell.
    /// <c>results[j][i] := predicate(sheet[i, j])</c>
    /// </param>
    /// <returns>Return</returns>
    private static int[] CountForEachColumn(
        ExcelSheet sheet,
        Predicate<object?> predicate,
        out BitArray[] results
    )
    {
        results = new BitArray[sheet.ColumnCount];
        for (int j = 0; j < sheet.ColumnCount; j++)
            results[j] = new BitArray(sheet.RowCount);
        int[] ret = new int[sheet.ColumnCount];
        for (int i = 0; i < sheet.RowCount; i++)
        {
            for (int j = 0; j < sheet.ColumnCount; j++)
            {
                if (predicate(sheet[i, j]))
                {
                    results[j].Set(i, true);
                    ret[j]++;
                }
            }
        }
        return ret;
    }

    /// <summary>
    /// Try finding the first row that can be considered as a header of a class sheet.
    /// <para>
    ///     - <c>ClassSheetTitles[i].HeaderTitle</c> is matched to <c>sheet[result, i]</c>
    ///     - <c>similarities[i]</c> is the similarity between them
    /// </para>
    /// </summary>
    /// <param name="result">
    /// On success, contains the zero-based index of the first header row.
    /// On failure, contains <c>-1</c>.</param>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns><c>true</c> if the header row is found; otherwise, <c>false</c>.</returns>
    private static bool TryFindClassSheetHeaderRow(
        ExcelSheet sheet,
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
                && TryMatchHeadersToClassSheetTitles(
                    Enumerable
                        .Range(0, sheet.ColumnCount)
                        .Select(j => (string?)sheet[i, j])
                        .ToArray(),
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
