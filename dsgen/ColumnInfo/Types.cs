using System.Collections.ObjectModel;

namespace dsgen.ColumnInfo;

/// <summary>
/// Types of entries in each column.
/// </summary>
public static class Types
{
    public static readonly ReadOnlyCollection<Type?> Code = (
        new Type?[] { typeof(String) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Department = (
        new Type?[] { typeof(String) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Name = (
        new Type?[] { typeof(String) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Grade = (
        new Type?[] { typeof(Double) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Class = (
        new Type?[] { typeof(Double) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Teacher = (
        new Type?[] { typeof(String) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Time = (
        new Type?[] { typeof(String) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Enrollment = (
        new Type?[] { typeof(Double) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Credit = (
        new Type?[] { typeof(Double) }
    ).AsReadOnly();
    public static readonly ReadOnlyCollection<Type?> Note = (
        new Type?[] { typeof(String), null }
    ).AsReadOnly();
}
