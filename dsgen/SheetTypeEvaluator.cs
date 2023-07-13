using System.Text.RegularExpressions;

public static partial class SheetTypeEvaluator
{
    [GeneratedRegex(
        pattern: @"\A[a-z]{2,}\d{3,}\z",
        options: RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LectureCodeRegex();

    [GeneratedRegex(
        pattern: @"\A([월|화|수|목|금]\d{1,2})(?:[\||,|.| |;|/]([월|화|수|목|금]\d{1,2}))*\z",
        options: RegexOptions.Singleline)]
    private static partial Regex TimeRegex();

    /// <summary>
    /// Evaluates the probability for the sheet can be used to generate class
    /// and lecture data.
    /// </summary>
    /// <param name="reader">Reader at the start of the sheet to evaluate.</param>
    /// <returns>Probability. Ranges from 0 to 1.</returns>
    public static double ClassSheetProbability(ExcelSheet sheet)
    {
        static int GetMax(ReadOnlySpan<int> span)
        {
            int max = Int32.MinValue;
            for (int i = 0; i < span.Length; i++)
                if (max < span[i]) max = span[i];
            return max;
        }

        if (sheet.Hidden)
            return 0.0;

        const int MaxStackLimit = 1024;
        Span<int> lectureCodeMatches = sheet.ColumnCount < MaxStackLimit ?
            stackalloc int[sheet.ColumnCount] : new int[sheet.ColumnCount];
        Span<int> timeMatches = sheet.ColumnCount < MaxStackLimit ?
            stackalloc int[sheet.ColumnCount] : new int[sheet.ColumnCount];
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
        return (double)Math.Min(GetMax(lectureCodeMatches), GetMax(timeMatches)) / sheet.RowCount;
    }
}
