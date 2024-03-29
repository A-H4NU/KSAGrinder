using CommunityToolkit.Diagnostics;
using dsgen.Exceptions;
using dsgen.Extensions;
using dsgen.Statics;
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
public readonly partial struct Column
{
    /// <summary>
    /// Is this column (a part of) the primary key?
    /// </summary>
    [DataMember]
    public bool IsKey { get; init; }

    /// <summary>
    /// Headers for each language.
    /// </summary>
    [IgnoreDataMember]
    public ReadOnlyDictionary<CultureInfo, string> HeaderTitles { get; init; }

    /// <summary>
    /// The name of <see cref="System.Data.DataColumn"/>.
    /// </summary>
    [DataMember]
    public string ColumnName { get; init; }

    /// <summary>
    /// Allowable types from the table of the excel file.
    /// <see cref="null"/> denotes it might be empty.
    /// </summary>
    [IgnoreDataMember]
    public ReadOnlyCollection<Type?> Types { get; init; }

    /// <summary>
    /// The type of <see cref="System.Data.DataColumn"/>.
    /// </summary>
    [IgnoreDataMember]
    public Type DataTableType { get; init; }

    /// <summary>
    /// May depend on the culture/language?
    /// </summary>
    [DataMember]
    public bool IsLocalizable { get; init; }

    /// <summary>
    /// Is getting different values from the languages is expected?
    /// </summary>
    [DataMember]
    public bool ConflictTolerant { get; init; }

    /// <summary>
    /// May depend on the class number?
    /// </summary>
    [DataMember]
    public bool ClassNoDependent { get; init; }

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

    private static readonly Assembly s_mscorlib = typeof(Int32).Assembly;

    public static ReadOnlyCollection<Column>? ClassSheetTitles { get; private set; }

    public static ReadOnlyCollection<CultureInfo>? SupportedCultureInfos { get; private set; }

    public static ReadOnlyCollection<string> RequiredColumnNames { get; } =
        new string[]
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
            "Hours",
            "Note"
        }.AsReadOnly();

    /// <summary>
    /// Denotes whether or not static properties of <see cref="Column"/> is initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;

    private const string RequiredColumnNameNotFoundMessage =
        "There must be a column whose ColumnName is '{0}' in '{1}'.";
    private const string TypeNotFoundMessage = "Could not find the type '{0}'.";
    private const string TypeInvalidMessage = "Type '{0}' is not serializable.";
    private const string FlagInvalidMessage =
        "A key column cannot be localizable or conflict-tolerant.";
    private const string ColumnNameIncludesUnderscoreMessage =
        "A column name cannot contain an underscore.";
    private const string OverlappingColumnNameMessage = "Column names may not overlap.";

    private static readonly object s_initializeLock = new();

    public static void Initialize()
    {
        Monitor.Enter(s_initializeLock);

        if (IsInitialized)
            return;

        string? path = null;
        bool expectedPath = false;
        try
        {
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            Debug.Assert(entryAssembly is not null, "There must be an entry assembly.");
            string directory = Path.GetDirectoryName(entryAssembly.Location)!;
            Debug.Assert(!String.IsNullOrEmpty(directory));
            path = Path.Combine(directory, Program.ColumnInfoFilePath);
            var result = DataContractSerializerUtils.DeserializeFromFile<Column[]>(path);

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
                        RequiredColumnNameNotFoundMessage,
                        requiredColumnName,
                        path
                    );
                    throw new ColumnConstraintException(message);
                }
            }

            /* Check the columns' flags (IsKey, IsLocalizable, and ConflictTolerant) are valid. */
            /* Check the columns' names do not contain an underscore. */
            foreach (Column column in ClassSheetTitles)
            {
                if (column.IsKey && (column.IsLocalizable || column.ConflictTolerant))
                {
                    throw new ColumnConstraintException(FlagInvalidMessage);
                }

                if (column.ColumnName.Contains('_'))
                {
                    throw new ColumnConstraintException(ColumnNameIncludesUnderscoreMessage);
                }
            }

            /* Check if column names do not overlap. */
            HashSet<string> columnNames = new(capacity: ClassSheetTitles.Count);
            if (!ClassSheetTitles.Select(c => c.ColumnName).All(columnNames.Add))
            {
                throw new ColumnConstraintException(OverlappingColumnNameMessage);
            }

            expectedPath = true;
        }
        catch (Exception ex)
            when (ex is not ColumnConstraintException)
        {
            /* Expected exceptions when failed to read column info from file for any reason.
             * (Over)write the file with defaults. */
            Debug.Assert(path is not null);
            ClassSheetTitles = DefaultClassSheetColumns.DefaultColumns;
            DataContractSerializerUtils.SerializeToFile(ClassSheetTitles.ToArray(), path);

            expectedPath = true;
        }
        finally
        {
            if (expectedPath)
            {
                Debug.Assert(ClassSheetTitles is not null);
                SupportedCultureInfos = new ReadOnlyCollectionBuilder<CultureInfo>(
                    ClassSheetTitles.Select(column => column.HeaderTitles.Keys).IntersectAll()
                ).ToReadOnlyCollection();

                IsInitialized = true;
            }
            Monitor.Exit(s_initializeLock);
        }
    }

    private static void ThrowIfInvalidType(string str, [NotNull] out Type? type)
    {
        Guard.IsNotNull(str);
        type = Type.GetType(str);
        if (type is null)
            throw new TypeException(String.Format(TypeNotFoundMessage, str));
        if (!type.IsSerializable)
            throw new TypeException(String.Format(TypeInvalidMessage, str));
    }
}
