using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Diagnostics;
using dsgen.ColumnInfo;
using dsgen.Excel;
using dsgen.Extensions;

namespace dsgen.Statics;

public static class DataExtractor
{
    static DataExtractor() { }

    private static bool TryProcessCellInClassSheet(
        int headerIndex,
        object? cellContent,
        CultureInfo cultureInfo,
        [NotNullWhen(true)] out object? result
    )
    {
        Debug.Assert(Column.IsInitialized);
        if (headerIndex < 0 || headerIndex >= Column.ClassSheetTitles!.Count)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(headerIndex));

        Type dataTableType = Column.ClassSheetTitles[headerIndex].DataTableType;

        /* Special cell types */
        if (Column.ClassSheetTitles[headerIndex].ColumnName == "Time")
        {
            return TryResolveTimeCell(cellContent, cultureInfo, out result);
        }

        /* General things. */
        if (dataTableType == typeof(String))
        {
            return TryResolveStringCell(cellContent, out result);
        }
        if (dataTableType == typeof(Int32))
        {
            return TryResolveInt32Cell(cellContent, out result);
        }
        if (dataTableType == typeof(Double))
        {
            return TryResolveDoubleCell(cellContent, out result);
        }
        throw new NotSupportedException();
    }

    public static bool TryExtractAsClassSheet(
        ExcelSheet sheet,
        [NotNullWhen(true)] out DataTable? result,
        [NotNullWhen(true)] out CultureInfo? culture
    )
    {
        Debug.Assert(Column.IsInitialized);
        result = null;
        culture = null;
        int minSkip = Int32.MaxValue;
        foreach (CultureInfo c in Column.SupportedCultureInfos!)
        {
            if (
                !TryExtractAsClassSheetWithCultureInfo(
                    sheet,
                    c,
                    out DataTable? res,
                    out int skipped
                )
            )
                continue;

            if (minSkip > skipped)
            {
                minSkip = skipped;
                result = res;
                culture = c;
            }
        }
        return result is not null;
    }

    private static bool TryExtractAsClassSheetWithCultureInfo(
        ExcelSheet sheet,
        CultureInfo culture,
        [NotNullWhen(true)] out DataTable? result,
        out int skippedRows
    )
    {
        Debug.Assert(Column.IsInitialized);
        Debug.Assert(
            Column.ClassSheetTitles!.All(tuple => tuple.HeaderTitles.ContainsKey(culture))
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
                out _
            )
        )
        {
            result = null;
            skippedRows = -1;
            return false;
        }

        DataTable _result = new();
        int _skippedRows = 0;
        for (int j = 0; j < matchResult.Length; j++)
        {
            string name = Column.ClassSheetTitles![j].ColumnName;
            Type type = Column.ClassSheetTitles[j].DataTableType;
            _result.Columns.Add(new DataColumn(name, type));
        }

        int nColumns = matchResult.Length;

        using SemaphoreSlim tableInUse = new(1, 1);

        void LoopBody(int i)
        {
            /* First check if their type matches! */
            for (int j = 0; j < matchResult!.Length; j++)
            {
                bool typeMatch = false;
                foreach (Type? type in Column.ClassSheetTitles![j].Types)
                {
                    object? cell = sheet[i, matchResult[j]];
                    if (type is null ? cell is null : type.IsInstanceOfType(cell))
                    {
                        typeMatch = true;
                        break;
                    }
                }
                if (!typeMatch)
                {
                    goto skip_this_row;
                }
            }

            /* Now that types are matching, we may process those cells. */
            object[] row = new object[nColumns];
            for (int j = 0; j < matchResult.Length; j++)
            {
                if (
                    TryProcessCellInClassSheet(
                        j,
                        sheet[i, matchResult[j]],
                        culture,
                        out object? obj
                    )
                )
                {
                    row[j] = obj;
                }
                else
                {
                    goto skip_this_row;
                }
            }
            try
            {
                tableInUse.Wait();
                _result.Rows.Add(row);
            }
            finally
            {
                tableInUse.Release();
            }
            return;

            skip_this_row:
            Interlocked.Increment(ref _skippedRows);
        }

        if (Program.NoConcurrency)
        {
            for (int i = headerRow + 1; i < sheet.RowCount; i++)
            {
                LoopBody(i);
            }
        }
        else
        {
            Parallel.For(headerRow + 1, sheet.RowCount, LoopBody);
        }

        Debug.Assert(_skippedRows + _result.Rows.Count == sheet.RowCount - headerRow - 1);

        result = _result;
        skippedRows = _skippedRows;

        return true;
    }

    private static bool TryResolveStringCell(
        object? cellContent,
        [NotNullWhen(true)] out object? result
    )
    {
        if (cellContent is null)
        {
            result = String.Empty;
            return true;
        }
        if (cellContent is string)
        {
            result = cellContent;
            return true;
        }
        result = null;
        return false;
    }

    private static bool TryResolveInt32Cell(
        object? cellContent,
        [NotNullWhen(true)] out object? result
    )
    {
        if (cellContent is int)
        {
            result = cellContent;
            return true;
        }
        if (cellContent is double value)
        {
            result = (int)Math.Round(value, 0, MidpointRounding.ToEven);
            return true;
        }
        if (cellContent is string str && Int32.TryParse(str, out int i))
        {
            result = i;
            return true;
        }
        result = null;
        return false;
    }

    private static bool TryResolveDoubleCell(
        object? cellContent,
        [NotNullWhen(true)] out object? result
    )
    {
        if (cellContent is double)
        {
            result = cellContent;
            return true;
        }
        if (cellContent is int value)
        {
            result = (double)value;
            return true;
        }
        if (cellContent is string str && Double.TryParse(str, out double i))
        {
            result = i;
            return true;
        }
        result = null;
        return false;
    }

    public const char TimeDelimiter = '|';

    private static readonly DayOfWeek[] s_daysInAWeek = (DayOfWeek[])
        typeof(DayOfWeek).GetEnumValues();

    private static readonly ReadOnlyDictionary<DayOfWeek, string> s_daysToStr = new Dictionary<
        DayOfWeek,
        string
    >()
    {
        { DayOfWeek.Sunday, "SU" },
        { DayOfWeek.Monday, "MO" },
        { DayOfWeek.Tuesday, "TU" },
        { DayOfWeek.Wednesday, "WE" },
        { DayOfWeek.Thursday, "TH" },
        { DayOfWeek.Friday, "FR" },
        { DayOfWeek.Saturday, "SA" },
    }.AsReadOnly();

    private static bool TryResolveTimeCell(
        object? cellContent,
        CultureInfo cultureInfo,
        [NotNullWhen(true)] out object? result
    )
    {
        static int CaptureIndexComparer(Capture a, Capture b) => a.Index.CompareTo(b.Index);

        result = null;
        if (cellContent is not string)
            return false;
        string str = (string)cellContent;
        if (!Regexes.Time.TryGetValue(cultureInfo, out Regex? regex))
            return false;
        var match = regex.Match(str);

        string[] groupNames = new string[] { "day", "hr" };
        if (
            !match.Success
            || match.Groups.Count != 3
            || !match.Groups.TryFindGroupsWithName(groupNames, out Group[]? groups)
        )
            return false;

        Group dayGroup = groups[0];
        Group hrGroup = groups[1];

        Capture[] dayCaptures = dayGroup.Captures.ToArray();
        Capture[] hrCaptures = hrGroup.Captures.ToArray();

        Debug.Assert(dayCaptures.Length == hrCaptures.Length);
        Array.Sort(dayCaptures, CaptureIndexComparer);
        Array.Sort(hrCaptures, CaptureIndexComparer);

        var res = new (DayOfWeek Day, int Hour)[dayCaptures.Length];
        for (int i = 0; i < dayCaptures.Length; i++)
        {
            string dayCaptureValue = dayCaptures[i].Value;
            int index = Array.FindIndex(
                s_daysInAWeek,
                day =>
                    String.Equals(
                        cultureInfo.DateTimeFormat.GetAbbreviatedDayName(day),
                        dayCaptureValue,
                        StringComparison.InvariantCultureIgnoreCase
                    )
            );
            if (index == -1)
                return false;
            res[i] = (s_daysInAWeek[index], Int32.Parse(hrCaptures[i].ValueSpan));
        }
        Array.Sort(res);
        result = String.Join(TimeDelimiter, res.Select(t => $"{s_daysToStr[t.Day]}{t.Hour}"));
        return true;
    }
}
