using CommunityToolkit.Diagnostics;
using dsgen.Exceptions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        get => Types.Select(type => type?.ToString()).ToList();
        init
        {
            // Simply a fast way of:
            // Types = value.Select(str => str is null ? null : Type.GetType(str)!).ToList().AsReadOnly();
            ReadOnlyCollectionBuilder<Type?> builder = new(value.Count);
            foreach (string? str in CollectionsMarshal.AsSpan(value))
            {
                if (str is not null)
                {
                    ThrowIfInvalidType(str, out Type t);
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
        get => DataTableType.ToString();
        init
        {
            ThrowIfInvalidType(value, out Type t);
            DataTableType = t;
        }
    }

    #endregion

    private static Assembly s_mscorlib = typeof(Int32).Assembly;

    public static ReadOnlyCollection<Column>? ClassSheetTitles { get; private set; }

    public static ReadOnlyCollection<CultureInfo>? SupportedCultureInfos { get; private set; }

    public static ReadOnlyCollection<string> RequiredColumnNames = new string[]
    {
        "Code",
        "Department",
        "Name",
        "Grade",
        "Class",
        "Teacher",
        "Time",
        "Enrollment",
        "Credit",
        "Note"
    }.AsReadOnly();

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

            /* Now, reading and deserializing were successful.
             * Any failures after this point is just simply an error,
             * throwing an exception that is passed to the caller of this function. */

            ClassSheetTitles = new ReadOnlyCollection<Column>(result);

            /* Check if required columns exist. */
            foreach (string requiredColumnName in RequiredColumnNames)
            {
                if (ClassSheetTitles.FindIndex(c => c.ColumnName == requiredColumnName) == -1)
                {
                    string message = String.Format(
                        Program.RequiredColumnNameNotFoundMessage,
                        requiredColumnName,
                        path
                    );
                    throw new Exception(message);
                }
            }

            SupportedCultureInfos = new ReadOnlyCollectionBuilder<CultureInfo>(
                ClassSheetTitles.Select(column => column.HeaderTitles.Keys).IntersectAll()
            ).ToReadOnlyCollection();
        }
        catch (Exception ex)
            when (ex is FileNotFoundException
                || ex is InvalidDataContractException
                || ex is SerializationException
                || ex is System.Xml.XmlException
            )
        {
            /* Expected exceptions when failed to read column info from file for any reason.
             * (Over)write the file with defaults. */
            Debug.Assert(path is not null);
            ClassSheetTitles = DefaultClassSheetColumns.DefaultColumns;
            await DataContractSerializerUtils.SerializeToFileAsync(
                ClassSheetTitles.ToArray(),
                path
            );
        }
    }

    private static void ThrowIfInvalidType(string str, [NotNull] out Type? type)
    {
        Guard.IsNotNull(str);
        type = Type.GetType(str);
        if (type is null)
            throw new TypeException(String.Format(Program.TypeNotFoundMessage, str));
        if (!type.IsSerializable)
            throw new TypeException(String.Format(Program.TypeInvalidMessage, str));
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