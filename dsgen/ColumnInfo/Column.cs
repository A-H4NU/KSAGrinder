using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace dsgen.ColumnInfo;

[DataContract]
public readonly struct Column
{
    [IgnoreDataMember]
    public ReadOnlyDictionary<CultureInfo, string> HeaderTitles { get; init; }

    [DataMember]
    public string ColumnName { get; init; }

    [IgnoreDataMember]
    public ReadOnlyCollection<Type?> Types { get; init; }

    [IgnoreDataMember]
    public Type DataTableType { get; init; }

    #region Private properties for serialization. DO NOT ACCESS THESE IN ANY WAY.

    [DataMember(Name = nameof(HeaderTitles))]
    private Dictionary<string, string> HeaderTitlesStr
    {
        get => HeaderTitles.ToDictionary(pair => pair.Key.Name, pair => pair.Value);
        init =>
            HeaderTitles = value
                .ToDictionary(pair => CultureInfo.GetCultureInfo(pair.Key), pair => pair.Value)
                .AsReadOnly();
    }

    [DataMember(Name = nameof(Types))]
    private List<string?> TypesStr
    {
        get => Types.Select(type => type?.FullName).ToList();
        init
        {
            // Simply a fast way of:
            // Types = value.Select(str => str is null ? null : Type.GetType(str)!).ToList().AsReadOnly();
            ReadOnlyCollectionBuilder<Type?> builder = new(value.Count);
            foreach (string? str in CollectionsMarshal.AsSpan(value))
            {
                if (str is not null)
                {
                    Type? t = Type.GetType(str);
                    Debug.Assert(t is not null);
                    builder.Add(t);
                }
                else
                    builder.Add(null);
            }
            Types = builder.ToReadOnlyCollection();
        }
    }

    [DataMember(Name = nameof(DataTableType))]
    private string DataTableTypeStr
    {
        get => DataTableType.FullName!;
        init
        {
            Type? t = Type.GetType(value);
            Debug.Assert(t is not null);
            DataTableType = t;
        }
    }

    #endregion

    public static ReadOnlyCollection<Column>? ClassSheetTitles { get; private set; }

    public static async Task InitializeAsync()
    {
        string? path = null;
        try
        {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            Debug.Assert(entryAssembly is not null, "There must be an entry assembly.");
            string directory = Path.GetDirectoryName(entryAssembly.Location)!;
            Debug.Assert(!String.IsNullOrEmpty(directory));
            path = Path.Combine(directory, Program.ColumnInfoFilePath);
            var result = await DataContractSerializerUtils.DeserializeFromFileAsync<Column[]>(path);
            ClassSheetTitles = new ReadOnlyCollection<Column>(result);
        }
        catch (Exception ex)
            when (ex is FileNotFoundException
                || ex is InvalidDataContractException
                || ex is SerializationException
                || ex is System.Xml.XmlException
            )
        {
            // The file does not exist or the content of the file is invalid.
            // Now, we need to serialize and write to the file.
            Debug.Assert(path is not null);
            ClassSheetTitles = DefaultClassSheetColumns.DefaultColumns;
            await DataContractSerializerUtils.SerializeToFileAsync(
                ClassSheetTitles.ToArray(),
                path
            );
        }
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

    /// <summary>
    /// This class provides hard-coded information of default class sheet columns.
    /// </summary>
    private static class DefaultClassSheetColumns
    {
        private static CultureInfo Culture_koKR = CultureInfo.GetCultureInfo("ko-KR");
        private static CultureInfo Culture_enUS = CultureInfo.GetCultureInfo("en-US");

        private static ReadOnlyCollection<Column>? _cachedDefaultColumns = null;
        public static ReadOnlyCollection<Column> DefaultColumns
        {
            get
            {
                if (_cachedDefaultColumns is null)
                {
                    FieldInfo[] fields = typeof(DefaultClassSheetColumns).GetFields(
                        BindingFlags.Public | BindingFlags.Static
                    );
                    ReadOnlyCollectionBuilder<Column> builder = new(fields.Length);
                    foreach (FieldInfo field in fields)
                    {
                        object? obj = field.GetValue(null);
                        if (obj is Column column)
                            builder.Add(column);
                    }
                    _cachedDefaultColumns = builder.ToReadOnlyCollection();
                }
                return _cachedDefaultColumns;
            }
        }

        public static readonly Column Code = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과목코드" }
            }.AsReadOnly(),
            ColumnName = "Code",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String)
        };

        public static readonly Column Department = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과군" }
            }.AsReadOnly(),
            ColumnName = "Department",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String)
        };
        public static readonly Column Name = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "교과목명" }
            }.AsReadOnly(),
            ColumnName = "Name",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String)
        };
        public static readonly Column Grade = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "학년" }
            }.AsReadOnly(),
            ColumnName = "Grade",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32)
        };
        public static readonly Column Class = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "분반" }
            }.AsReadOnly(),
            ColumnName = "Class",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32)
        };
        public static readonly Column Teacher = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "담당교원" }
            }.AsReadOnly(),
            ColumnName = "Teacher",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof(String)
        };
        public static readonly Column Time = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "요일" }
            }.AsReadOnly(),
            ColumnName = "Time",
            Types = new Type?[] { typeof(String) }.AsReadOnly(),
            DataTableType = typeof((DayOfWeek day, int hour)[])
        };
        public static readonly Column Enrollment = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "신청수" }
            }.AsReadOnly(),
            ColumnName = "Enrollment",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32)
        };
        public static readonly Column Credit = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "학점" }
            }.AsReadOnly(),
            ColumnName = "Credit",
            Types = new Type?[] { typeof(Double) }.AsReadOnly(),
            DataTableType = typeof(Int32)
        };
        public static readonly Column Note = new Column()
        {
            HeaderTitles = new Dictionary<CultureInfo, string>
            {
                { Culture_koKR, "비고" }
            }.AsReadOnly(),
            ColumnName = "Note",
            Types = new Type?[] { typeof(String), null }.AsReadOnly(),
            DataTableType = typeof(String)
        };
    }
}
