using CommunityToolkit.Diagnostics;
using dsgen.Exceptions;
using dsgen.Extensions;
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
    [DataMember]
    public bool IsKey { get; init; }

    [IgnoreDataMember]
    public ReadOnlyDictionary<CultureInfo, string> HeaderTitles { get; init; }

    [DataMember]
    public string ColumnName { get; init; }

    [IgnoreDataMember]
    public ReadOnlyCollection<Type?> Types { get; init; }

    [IgnoreDataMember]
    public Type DataTableType { get; init; }

    [DataMember]
    public bool IsLocalizable { get; init; }

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
        "Hours",
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

        SupportedCultureInfos = new ReadOnlyCollectionBuilder<CultureInfo>(
            ClassSheetTitles.Select(column => column.HeaderTitles.Keys).IntersectAll()
        ).ToReadOnlyCollection();
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
}
