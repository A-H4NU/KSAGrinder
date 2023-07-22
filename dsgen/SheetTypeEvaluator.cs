using CommunityToolkit.Diagnostics;
using dsgen.Excel;
using dsgen.StringDistance;
using dsgen.ColumnInfo;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Immutable;

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

    public static StringDistanceCalculator StringDistanceCalculator { get; set; } =
        Levenshtein.Instance;

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
        var reference = (from tuple in Column.ClassSheetTitles select tuple.HeaderTitle).ToArray();
        if (
            !sheet.TryFindSheetHeaderRow(
                reference,
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
        int codeIdxInArray = Column.ClassSheetTitles.FindIndex(
            tuple => tuple.ColumnName == ColumnName.Code
        );
        int timeIdxInArray = Column.ClassSheetTitles.FindIndex(
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
            for (int j = 0; j < Column.ClassSheetTitles.Count; j++)
            {
                if (!Column.ClassSheetTitles[j].Types.Contains(sheet[i, matchResult[j]]?.GetType()))
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
        return obj is string str && regex.IsMatch(str);
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
}
