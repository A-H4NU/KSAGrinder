using CommunityToolkit.Diagnostics;

using ExcelDataReader;

namespace dsgen.Excel;

public class ExcelSheet
{
    private readonly object?[,] _array;

    public int RowCount => _array.GetLength(0);
    public int ColumnCount => _array.GetLength(1);
    public object? this[int row, int column]
    {
        get
        {
            Guard.IsInRange(row, 0, RowCount);
            Guard.IsInRange(column, 0, ColumnCount);
            return _array[row, column];
        }
    }

    public bool Hidden { get; init; } = false;

    private ExcelSheet(int rowCount, int columnCount)
    {
        Guard.IsGreaterThanOrEqualTo(rowCount, 0);
        Guard.IsGreaterThanOrEqualTo(columnCount, 0);
        _array = new object?[rowCount, columnCount];
    }

    private ExcelSheet(int rowCount, int columnCount, object?[][] array)
        : this(rowCount, columnCount)
    {
        Guard.IsGreaterThanOrEqualTo(array.Length, rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            Guard.IsGreaterThanOrEqualTo(array[i].Length, columnCount);
        }
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                _array[i, j] = array[i][j];
            }
        }
    }

    public static ExcelSheet FromExcelDataReader(IExcelDataReader reader)
    {
        int currentRow = 0;
        object?[][] array = new object?[reader.RowCount][];
        int rowCount = 0, columnCount = 0;
        while (reader.Read())
        {
            array[currentRow] = new object?[reader.FieldCount];
            for (int j = 0; j < reader.FieldCount; j++)
            {
                object? value = reader.GetValue(j);
                array[currentRow][j] = value;
                if (value is not null)
                {
                    rowCount = currentRow + 1;
                    columnCount = Math.Max(columnCount, j + 1);
                }
            }
            currentRow++;
        }
        return new(rowCount, columnCount, array)
        {
            Hidden = reader.VisibleState != "visible"
        };
    }
}
