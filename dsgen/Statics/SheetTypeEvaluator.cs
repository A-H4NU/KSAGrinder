using CommunityToolkit.Diagnostics;
using dsgen.Excel;
using dsgen.StringDistance;
using dsgen.ColumnInfo;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using dsgen.Extensions;

namespace dsgen.Statics;

public static partial class SheetTypeEvaluator
{
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
        Debug.Assert(Column.IsInitialized);
        var scores =
            from culture in Column.SupportedCultureInfos
            select ClassSheetScoreWithCulture(sheet, culture, calculator);
        return Enumerable.Max(scores);
    }

    /// <summary>
    /// Evaluates the probability for the sheet can be used to generate class
    /// and lecture data, assuming the class sheet is endowed by the specific culture.
    /// </summary>
    /// <param name="sheet">The sheet to evaluate.</param>
    /// <param name="calculator">The strategy used to calculate the similarity between strings.</param>
    /// <returns>Probability. Ranges from 0 to 1.</returns>
    private static float ClassSheetScoreWithCulture(
        ExcelSheet sheet,
        CultureInfo culture,
        StringDistanceCalculator? calculator = null
    )
    {
        Debug.Assert(Column.IsInitialized);
        if (sheet.Hidden || sheet.ColumnCount < 9)
            return 0f;

        // codeMatches[i] = "# of entries in the i-th column that matches LectureCodeRegex()"
        // Similar for timeMatches.
        int[] codeMatches = CountForEachColumn(
            sheet,
            obj => DoesMatchRegex(Regexes.LectureCode, obj),
            out BitArray[] doesMatchCode
        );
        int[] timeMatches = CountForEachColumn(
            sheet,
            obj => DoesMatchRegex(Regexes.Time[culture], obj),
            out BitArray[] doesMatchTime
        );
        var reference = (
            from tuple in Column.ClassSheetTitles
            select tuple.HeaderTitles[culture]
        ).ToArray();
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
        int codeIdxInArray = Column.ClassSheetTitles!.FindIndex(
            tuple => tuple.ColumnName == "Code"
        );
        int timeIdxInArray = Column.ClassSheetTitles!.FindIndex(
            tuple => tuple.ColumnName == "Time"
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
            for (int j = 0; j < Column.ClassSheetTitles!.Count; j++)
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
            obj => DoesMatchRegex(Regexes.StudentId, obj),
            out _
        );
        float idScore = (float)GetMax(idMatches, out _) / sheet.RowCount;
        int[] classMatches = CountForEachRow(
            sheet,
            obj => DoesMatchRegex(Regexes.LectureOrClass, obj),
            out _
        );
        float classScore = (float)GetMax(classMatches, out _) / sheet.ColumnCount;
        return Math.Min(idScore, classScore);
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
    /// <returns>Returns the count for each column.</returns>
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
    /// For each row, count the number of cells that matches <paramref name="predicate"/>.
    /// </summary>
    /// <param name="sheet">The sheet to target and count.</param>
    /// <param name="results">
    /// The result of evaluating <paramref name="predicate"/> for each cell.
    /// <c>results[i][j] := predicate(sheet[i, j])</c>
    /// </param>
    /// <returns>Returns the count for each row.</returns>
    private static int[] CountForEachRow(
        ExcelSheet sheet,
        Predicate<object?> predicate,
        out BitArray[] results
    )
    {
        results = new BitArray[sheet.RowCount];
        for (int i = 0; i < sheet.RowCount; i++)
            results[i] = new BitArray(sheet.ColumnCount);
        int[] res = new int[sheet.RowCount];
        for (int i = 0; i < sheet.RowCount; i++)
        {
            for (int j = 0; j < sheet.ColumnCount; j++)
            {
                if (predicate(sheet[i, j]))
                {
                    results[i].Set(j, true);
                    res[i]++;
                }
            }
        }
        return res;
    }
}
