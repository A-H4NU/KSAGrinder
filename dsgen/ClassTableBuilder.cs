using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using dsgen.ColumnInfo;
using dsgen.Exceptions;
using dsgen.Extensions;

namespace dsgen;

public sealed class ClassTableBuilder
{
    private const string ListEmptyMessage = "There are no data tables to build from.";
    private const string ConflictMessage = "Had a conflict building the table.";
    private const string BuildFailedMessage = "Building class table failed.";
    private const string OverlappingCultureMessage =
        "The builder already contains a table from '{0}'.";
    private const string DataTableIllFormedMessage = "The columns of the table does not match";

    /// <summary>
    /// Simple wrapper for (DataTable, CultureInfo) tuple.
    /// </summary>
    private record TableAndCulture(DataTable Table, CultureInfo Culture)
    {
        public void Deconstruct(out DataTable table, out CultureInfo culture)
        {
            table = Table;
            culture = Culture;
        }
    }

    private readonly string _columnNameFormat = "{0}_{1}";

    private List<TableAndCulture> _list;

    public ClassTableBuilder()
    {
        _list = new();
    }

    /// <param name="columnNameFormat">
    /// Format used for column names of localizable column names.
    /// </param>
    public ClassTableBuilder(string columnNameFormat)
    {
        _list = new();
        _columnNameFormat = columnNameFormat;
    }

    /// <summary>
    /// Build.
    /// </summary>
    /// <returns>The built <see cref="DataTable"/> object.</returns>
    /// <exception cref="DataTableBuilderException">Whenever fails.</exception>
    public DataTable Build(
        CultureInfo primaryCulture,
        bool throwOnConflict,
        out TableBuildReport buildReport
    )
    {
        Debug.Assert(Column.IsInitialized);

        if (_list.Count == 0)
        {
            throw new DataTableBuilderException(ListEmptyMessage);
        }

        Dictionary<EquatableArray, DataRow>? rows = null;
        Dictionary<EquatableArray, RowDiscrepancy>? discrepancies = null;
        try
        {
            int maxRow = Enumerable.Max(_list.Select(tac => tac.Table.Rows.Count));
            rows = new(capacity: maxRow);
            discrepancies = new(capacity: maxRow);

            DataTable res = GetEmptyTable(out var keyColumns);

            res.PrimaryKey = keyColumns;

            for (int i = 0; i < _list.Count; i++)
            {
                ExtractAndAddFromTable(
                    rows: rows,
                    discrepancies: discrepancies,
                    newRow: () => res.NewRow(),
                    tac: _list[i],
                    overwriteConflictTolerant: _list[i].Culture == primaryCulture,
                    exitOnConflict: throwOnConflict,
                    out bool hadConflict
                );

                if (throwOnConflict && hadConflict)
                {
                    throw new DataTableBuilderException(ConflictMessage);
                }
            }

            var culturedColumnNames = (
                from column in Column.ClassSheetTitles
                where column.IsLocalizable
                from tac in _list
                select GetCulturedColumnName(column, tac.Culture)
            ).ToArray();
            foreach (var (key, row) in rows)
            {
                foreach (string culturedColumnName in culturedColumnNames)
                {
                    if (
                        row[culturedColumnName] is DBNull
                        || row[culturedColumnName] is string str && String.IsNullOrEmpty(str)
                    )
                    {
                        IncrementEmptyLocalizableColumns(discrepancies, key);
                    }
                }
            }

            var discrepancyArr = new RowDiscrepancy[rows.Count];
            int index = 0;
            foreach (var (key, row) in rows)
            {
                res.Rows.Add(row);
                discrepancyArr[index++] = discrepancies.GetValueOrDefault(key);
            }

            buildReport = new(res, discrepancyArr);
            return res;
        }
        catch (Exception ex) when (ex is not DataTableBuilderException)
        {
            throw new DataTableBuilderException(BuildFailedMessage, ex);
        }
        finally
        {
            rows?.Clear();
            discrepancies?.Clear();
        }
    }

    /// <summary>
    /// Add <paramref name="item"/> to the builder.
    /// </summary>
    public ClassTableBuilder Add(DataTable table, CultureInfo culture)
    {
        /* Check the input is valid. */
        Debug.Assert(Column.IsInitialized);
        if (_list.FindIndex(a => a.Culture == culture) >= 0)
            throw new DataTableBuilderException(OverlappingCultureMessage);
        if (table.Columns.Count != Column.ClassSheetTitles!.Count)
            goto table_ill_formatted;
        for (int i = 0; i < table.Columns.Count; i++)
        {
            DataColumn column = table.Columns[i];
            int index = Column.ClassSheetTitles.FindIndex(c => c.ColumnName == column.ColumnName);
            if (index == -1 || Column.ClassSheetTitles[index].DataTableType != column.DataType)
                goto table_ill_formatted;
        }

        /* Everything is good; add to our list. */
        _list.Add(new(table, culture));
        return this;

        table_ill_formatted: // goto label for less size :)
        throw new DataTableBuilderException(DataTableIllFormedMessage);
    }

    private string GetCulturedColumnName(Column column, CultureInfo culture)
    {
        if (!column.IsLocalizable)
            return column.ColumnName;
        return String.Format(_columnNameFormat, column.ColumnName, culture);
    }

    private string GetCulturedColumnName(DataColumn column, CultureInfo culture)
    {
        Debug.Assert(Column.IsInitialized);

        int index = Column.ClassSheetTitles!.FindIndex(c => c.ColumnName == column.ColumnName);
        if (index == -1 || !Column.ClassSheetTitles![index].IsLocalizable)
            return column.ColumnName;
        return GetCulturedColumnName(Column.ClassSheetTitles[index], culture);
    }

    /// <summary>
    /// Generate an empty table with columns initialized.
    /// </summary>
    /// <param name="keyColumns">List of primary key columns.</param>
    /// <returns>
    /// DataTable with columns named and typed after <see cref="Column.ClassSheetTitles"/>.
    /// Localizable columns are named by <see cref="GetCulturedColumnName(Column, CultureInfo)"/>.
    /// </returns>
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private DataTable GetEmptyTable(out DataColumn[] keyColumns, string? tableName = null)
    {
        Debug.Assert(Column.IsInitialized);

        DataTable res = new(tableName);
        keyColumns = new DataColumn[Column.ClassSheetTitles!.Count(c => c.IsKey)];
        int keyColumnIdx = 0;
        foreach (Column columnInfo in Column.ClassSheetTitles!)
        {
            if (!columnInfo.IsLocalizable)
            {
                var column = res.Columns.Add(columnInfo.ColumnName, columnInfo.DataTableType);
                if (columnInfo.IsKey)
                {
                    keyColumns[keyColumnIdx++] = column;
                }
                continue;
            }
            Debug.Assert(!columnInfo.IsKey);

            string columnName;
            foreach (CultureInfo culture in _list.Select(tac => tac.Culture))
            {
                columnName = GetCulturedColumnName(columnInfo, culture);
                res.Columns.Add(columnName, columnInfo.DataTableType);
            }
        }
        return res;
    }

    /// <summary>
    /// Extraction of the internal logic of <see cref="Build"/>.
    /// </summary>
#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void ExtractAndAddFromTable(
        Dictionary<EquatableArray, DataRow> rows,
        Dictionary<EquatableArray, RowDiscrepancy> discrepancies,
        Func<DataRow> newRow,
        TableAndCulture tac,
        bool overwriteConflictTolerant,
        bool exitOnConflict,
        out bool hadUnresolvedConflicts
    )
    {
        Debug.Assert(Column.IsInitialized);

        var (table, culture) = tac;
        DataColumn[] primaryKey = Column.ClassSheetTitles!
            .Where(c => c.IsKey)
            .Select(c => table.Columns[c.ColumnName]!)
            .ToArray();
        Column[] nonKeyColumns = (
            from column in Column.ClassSheetTitles
            where !column.IsKey
            select column
        ).ToArray();
        ref DataRow? resRow = ref Unsafe.NullRef<DataRow?>();
        hadUnresolvedConflicts = false;
        object[] keyArr = new object[primaryKey.Length];
        foreach (DataRow row in table.Rows)
        {
            for (int index = 0; index < primaryKey.Length; index++)
            {
                keyArr[index] = row.Field<object>(primaryKey[index])!;
            }
            EquatableArray key = new(keyArr);

            resRow = ref CollectionsMarshal.GetValueRefOrAddDefault(rows, key, out bool existed);

            /* If the class did not exist, just write and continue. */
            if (!existed)
            {
                resRow = newRow();
                foreach (DataColumn column in table.Columns)
                {
                    string culturedColumnName = GetCulturedColumnName(column, culture);
                    resRow[culturedColumnName] = row[column];
                }
                continue;
            }

            /* Otherwise, check for any discrepancies and write. */
            foreach (Column column in nonKeyColumns)
            {
                string columnName = GetCulturedColumnName(column, culture);
                object? rowContent = row[column.ColumnName];
                object? resRowContent = resRow![columnName];
                bool isConflict = resRowContent is not DBNull && !Equals(resRowContent, rowContent);
                switch ((isConflict, column.ConflictTolerant, overwriteConflictTolerant))
                {
                    case (false, _, _):
                    case (true, true, true):
                        resRow![columnName] = rowContent;
                        break;
                    case (true, false, _):
                        hadUnresolvedConflicts = true;
                        IncrementConflictOnNonLocalizableColumns(discrepancies, key);
                        if (exitOnConflict)
                            return;
                        break;
                }
            }
        }
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void IncrementConflictOnNonLocalizableColumns(
        Dictionary<EquatableArray, RowDiscrepancy> discrepancies,
        EquatableArray key
    )
    {
        ref var d = ref CollectionsMarshal.GetValueRefOrAddDefault(discrepancies, key, out _);
        d.ConflictOnNonLocalizableColumns++;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void IncrementEmptyLocalizableColumns(
        Dictionary<EquatableArray, RowDiscrepancy> discrepancies,
        EquatableArray key
    )
    {
        ref var d = ref CollectionsMarshal.GetValueRefOrAddDefault(discrepancies, key, out _);
        d.EmptyLocalizableColumns++;
    }
}
