namespace dsgen.ColumnInfo;

using T = System.Collections.ObjectModel.ReadOnlyCollection<Type?>;

/// <summary>
/// Types of entries in each column.
/// </summary>
public static class Types
{
    public static readonly T Code = new Type?[] { typeof(String) }.AsReadOnly();
    public static readonly T Department = new Type?[] { typeof(String) }.AsReadOnly();
    public static readonly T Name = new Type?[] { typeof(String) }.AsReadOnly();
    public static readonly T Grade = new Type?[] { typeof(Double) }.AsReadOnly();
    public static readonly T Class = new Type?[] { typeof(Double) }.AsReadOnly();
    public static readonly T Teacher = new Type?[] { typeof(String) }.AsReadOnly();
    public static readonly T Time = new Type?[] { typeof(String) }.AsReadOnly();
    public static readonly T Enrollment = new Type?[] { typeof(Double) }.AsReadOnly();
    public static readonly T Credit = new Type?[] { typeof(Double) }.AsReadOnly();
    public static readonly T Note = new Type?[] { typeof(String), null }.AsReadOnly();
}
