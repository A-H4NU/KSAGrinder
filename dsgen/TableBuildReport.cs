using System.Data;
using CommunityToolkit.Diagnostics;

namespace dsgen;

public struct RowDiscrepancy
{
    public int ConflictOnNonLocalizableColumns { get; set; }
    public int EmptyLocalizableColumns { get; set; }
}

public class TableBuildReport
{
    public DataTable Table { get; }

    public RowDiscrepancy[] Results { get; }

    public int[] ConflictOnNonLocalizableColumns { get; }
    public int[] EmptyLocalizableColumns { get; }

    public bool IsClear { get; }

    public TableBuildReport(DataTable table, RowDiscrepancy[] results)
    {
        const string ArrayLengthMessage =
            $"'{nameof(results)}' must have the length "
            + $"that is equal to the number of rows of '{nameof(table)}'";
        const string MoreThanNumColumnsMessage =
            $"'{nameof(results)}' has a value larger than "
            + $"the number of columns of '{nameof(table)}'";

        if (table.Rows.Count != results.Length)
        {
            ThrowHelper.ThrowArgumentException(ArrayLengthMessage);
        }

        IsClear = true;
        int columnCount = table.Columns.Count;

        List<int> conflictOnNonLocalizableColumns = new();
        List<int> emptyLocalizableColumns = new();
        for (int i = 0; i < results.Length; i++)
        {
            RowDiscrepancy rd = results[i];
            if ((rd.ConflictOnNonLocalizableColumns | rd.EmptyLocalizableColumns) != 0)
            {
                if (
                    rd.ConflictOnNonLocalizableColumns > columnCount
                    || rd.EmptyLocalizableColumns > columnCount
                )
                {
                    ThrowHelper.ThrowArgumentException(MoreThanNumColumnsMessage);
                }

                if (rd.ConflictOnNonLocalizableColumns > 0)
                    conflictOnNonLocalizableColumns.Add(i);
                if (rd.EmptyLocalizableColumns > 0)
                    emptyLocalizableColumns.Add(i);
                IsClear = false;
            }
        }
        Table = table;
        Results = results;
        ConflictOnNonLocalizableColumns = conflictOnNonLocalizableColumns.ToArray();
        EmptyLocalizableColumns = emptyLocalizableColumns.ToArray();
    }
}
