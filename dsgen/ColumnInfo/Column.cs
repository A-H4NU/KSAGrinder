using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Diagnostics;

namespace dsgen.ColumnInfo;

public record struct Column(
    string HeaderTitle,
    string ColumnName,
    IReadOnlyCollection<Type?> Types,
    Type DataTableType
)
{
    public static ImmutableList<Column> ClassSheetTitles;

    static Column()
    {
        static void SortFieldInfosByName(FieldInfo[] array) =>
            Array.Sort(array.Select(f => f.Name).ToArray(), array);

        /* Initialize ClassSheetTitles with reflection. */
        BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;
        FieldInfo[] headerTitleFields = typeof(HeaderTitle).GetFields(publicStatic);
        FieldInfo[] columnNameFields = typeof(ColumnName).GetFields(publicStatic);
        FieldInfo[] typesFields = typeof(Types).GetFields(publicStatic);
        FieldInfo[] typeFields = typeof(DataTableTypes).GetFields(publicStatic);
        Guard.IsNotEmpty(columnNameFields);
        Debug.Assert(
            AllEqual(
                headerTitleFields.Length,
                columnNameFields.Length,
                typesFields.Length,
                typeFields.Length
            )
        );
        /* Sort to assert the names of the fields match. */
        ApplyActionToAll(
            SortFieldInfosByName,
            headerTitleFields,
            columnNameFields,
            typesFields,
            typeFields
        );
        int n = columnNameFields.Length;
        for (int i = 0; i < n; i++)
        {
            Debug.Assert(
                AllEqual(
                    headerTitleFields[i].Name,
                    columnNameFields[i].Name,
                    typesFields[i].Name,
                    typeFields[i].Name
                )
            );
        }
        var builder = ImmutableList.CreateBuilder<Column>();
        for (int i = 0; i < n; i++)
        {
            object? h = headerTitleFields[i].GetValue(null);
            object? c = columnNameFields[i].GetValue(null);
            object? ts = typesFields[i].GetValue(null);
            object? t = typeFields[i].GetValue(null);
            if (
                h is string headerTitle
                && c is string columnName
                && ts is IReadOnlyCollection<Type?> types
                && t is Type type
            )
            {
                builder.Add(new(headerTitle, columnName, types, type));
            }
            else
            {
                Debug.Fail("Unreachable.");
            }
        }
        ClassSheetTitles = builder.ToImmutable();
    }

    private static bool AllEqual<T>(params T[] objs)
        where T : IEquatable<T>
    {
        if (objs.Length < 2)
            return true;
        for (int i = 1; i < objs.Length; i++)
            if (!objs[i].Equals(objs[i - 1]))
                return false;
        return true;
    }

    private static void ApplyActionToAll<T>(Action<T> action, params T[] objs)
    {
        for (int i = 0; i < objs.Length; i++)
        {
            action(objs[i]);
        }
    }
}
