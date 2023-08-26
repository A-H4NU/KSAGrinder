using System.Data;
using System.Runtime.CompilerServices;
using dsgen.ColumnInfo;

namespace dsgen.TableBuilders;

internal sealed class ClassTableBuilder
{
    private DataTable _primitiveTable;

    public ClassTableBuilder(DataTable primitiveTable)
    {
        _primitiveTable = primitiveTable;
    }

    public DataTable Build()
    {
        DataTable res = GetEmptyTable(out List<int> columnIndices);

        void LoopBody(int i)
        {
            object[] row = new object[columnIndices.Count];

            for (int k = 0; k < columnIndices.Count; k++)
            {
                row[k] = _primitiveTable.Rows[i][columnIndices[k]];
            }

            lock (res)
            {
                res.Rows.Add(row);
            }
        }

        if (Program.NoConcurrency)
        {
            for (int i = 0; i < _primitiveTable.Rows.Count; i++)
            {
                LoopBody(i);
            }
        }
        else
        {
            Parallel.For(0, _primitiveTable.Rows.Count, LoopBody);
        }

        res.DefaultView.Sort = "Code,Grade,Class";
        return res.DefaultView.ToTable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DataTable GetEmptyTable(out List<int> columnIndices)
    {
        DataTable res = new("Class");

        string[] classNoDependentColumns = (
            from column in Column.ClassSheetTitles
            where column.ClassNoDependent
            select column.ColumnName
        ).ToArray();

        columnIndices = new();
        for (int i = 0; i < _primitiveTable.Columns.Count; i++)
        {
            DataColumn dataColumn = _primitiveTable.Columns[i];
            var columnName = Column.SeparatorUtil.GetNonlocalizedColumnName(dataColumn.ColumnName);

            if (columnName is "Code" or "Grade" || classNoDependentColumns.Contains(columnName))
            {
                res.Columns.Add(dataColumn.ColumnName, dataColumn.DataType);
                columnIndices.Add(i);
            }
        }

        res.PrimaryKey = new DataColumn[]
        {
            res.Columns["Code"]!,
            res.Columns["Grade"]!,
            res.Columns["Class"]!
        };
        return res;
    }
}
