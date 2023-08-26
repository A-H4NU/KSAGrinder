using System.Data;
using System.Diagnostics;
using dsgen.ColumnInfo;

#if !DEBUG
using System.Runtime.CompilerServices;
#endif

namespace dsgen.TableBuilders;

internal sealed class LectureTableBuilder
{
    private DataTable _primitiveTable;

    private bool _conflictTolerant = false;

    public LectureTableBuilder(DataTable primitiveTable)
    {
        _primitiveTable = primitiveTable;
    }

    public LectureTableBuilder SetConflictTolerant(bool conflictTolerant)
    {
        _conflictTolerant = conflictTolerant;
        return this;
    }

    public DataTable Build(out List<(string Code, int Grade)> conflicts)
    {
        Debug.Assert(Column.IsInitialized);

        DataTable res = GetEmptyTable(out List<int> columnIndices);

        List<(string, int)> _conflicts = new();

        var groupsByLecture = _primitiveTable.Rows
            .Cast<DataRow>()
            .GroupBy(row => (row.Field<string>("Code")!, row.Field<int>("Grade")));

        void LoopBody(IGrouping<(string, int), DataRow> group)
        {
            (string code, int grade) = group.Key;
            var row = GetRowFromGroup(res, code, grade, group, columnIndices, out bool hasConflict);
            lock (res)
            {
                res.Rows.Add(row);
            }
            if (hasConflict)
            {
                lock (_conflicts)
                {
                    _conflicts.Add(group.Key);
                }
            }
        }

        if (Program.NoConcurrency)
        {
            foreach (var group in groupsByLecture)
            {
                LoopBody(group);
            }
        }
        else
        {
            Parallel.ForEach(groupsByLecture, LoopBody);
        }

        conflicts = _conflicts;
        return res;
    }

    private DataTable GetEmptyTable(out List<int> columnIndices)
    {
        DataTable res = new();

        HashSet<string> ClassNoIndependentColumns = (
            from column in Column.ClassSheetTitles
            where !column.ClassNoDependent
            select column.ColumnName
        ).ToHashSet();

        columnIndices = new();

        for (int i = 0; i < _primitiveTable.Columns.Count; i++)
        {
            DataColumn dataColumn = _primitiveTable.Columns[i];
            var columnName = Column.SeparatorUtil.GetNonlocalizedColumnName(dataColumn.ColumnName);

            if (ClassNoIndependentColumns.Contains(columnName))
            {
                res.Columns.Add(dataColumn.ColumnName, dataColumn.DataType);
                columnIndices.Add(i);
            }
        }

        Debug.Assert(columnIndices.Count == res.Columns.Count);

        res.PrimaryKey = new DataColumn[] { res.Columns["Code"]!, res.Columns["Grade"]! };

        return res;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static object[] GetRowFromGroup(
        DataTable table,
        string code,
        int grade,
        IEnumerable<DataRow> rows,
        List<int> columnIndices,
        out bool hasConflict
    )
    {
        object[] rowToFill = new object[table.Columns.Count];

        using IEnumerator<DataRow> e = rows.GetEnumerator();

        e.MoveNext();
        for (int i = 0; i < columnIndices.Count; i++)
        {
            int k = columnIndices[i];
            rowToFill[i] = e.Current[k];
        }

        hasConflict = false;
        while (e.MoveNext())
        {
            for (int i = 0; i < columnIndices.Count && !hasConflict; i++)
            {
                if (table.Columns[i].ColumnName is "Code" or "Grade")
                    continue;

                int k = columnIndices[i];
                hasConflict |= !Equals(rowToFill[i], e.Current[k]);
            }
        }

        return rowToFill;
    }
}
