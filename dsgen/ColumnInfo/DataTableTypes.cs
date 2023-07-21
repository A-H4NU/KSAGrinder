namespace dsgen.ColumnInfo;

/// <summary>
/// Types of each DataColumn.
/// </summary>
public static class DataTableTypes
{
    public static Type Code = typeof(String);
    public static Type Department = typeof(String);
    public static Type Name = typeof(Dictionary<string, string>);
    public static Type Grade = typeof(Int32);
    public static Type Class = typeof(Int32);
    public static Type Teacher = typeof(String);
    public static Type Time = typeof((DayOfWeek day, int hour)[]);
    public static Type Enrollment = typeof(Int32);
    public static Type Credit = typeof(Int32);
    public static Type Note = typeof(String);
}
