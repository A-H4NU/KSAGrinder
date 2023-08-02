using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace dsgen;

public static class DataContractSerializerUtils
{
    private static Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// The <see cref="System.Text.Encoding"/> object used to (de)serialize objects.
    /// </summary>
    public static Encoding Encoding
    {
        get => _encoding;
        set
        {
            _encoding = value;
            _xmlSettings = new() { Indent = true, Encoding = Encoding };
        }
    }

    private static XmlWriterSettings _xmlSettings = new() { Indent = true, Encoding = Encoding };

    private static XmlWriterSettings XmlSettings => _xmlSettings;

    /// <summary>
    /// Serialize <paramref name="obj"/> to a string via <see cref="DataContractSerializer"/>.
    /// </summary>
    /// <returns>The result of serialization, encoded with <see cref="Encoding"/>.</returns>
    public static string Serialize<T>(T obj)
    {
        DataContractSerializer serializer = new(typeof(T));
        using MemoryStream ms = new();
        using (XmlWriter xw = XmlWriter.Create(ms, XmlSettings))
            serializer.WriteObject(xw, obj);
        return Encoding.GetString(ms.GetBuffer());
    }

    /// <summary>
    /// Serialize <paramref name="obj"/> and append to <paramref name="sb"/>
    /// via <see cref="DataContractSerializer"/>.
    /// Encoding is done with <see cref="Encoding"/>.
    /// </summary>
    public static void Serialize<T>(T obj, StringBuilder sb)
    {
        DataContractSerializer serializer = new(typeof(T));
        using (XmlWriter xw = XmlWriter.Create(sb, XmlSettings))
            serializer.WriteObject(xw, obj);
    }

    /// <summary>
    /// Serialize <paramref name="obj"/> and write to <paramref name="stream"/>
    /// via <see cref="DataContractSerializer"/>.
    /// Encoding is done with <see cref="Encoding"/>.
    /// </summary>
    public static void Serialize<T>(T obj, Stream stream)
    {
        DataContractSerializer serializer = new(typeof(T));
        using (XmlWriter xw = XmlWriter.Create(stream, XmlSettings))
            serializer.WriteObject(xw, obj);
    }

    /// <summary>
    /// Serialize <paramref name="obj"/> to a string via <see cref="DataContractSerializer"/>.
    /// </summary>
    /// <returns>The result of serialization, encoded with <see cref="Encoding"/>.</returns>
    public static async Task<string> SerializeAsync<T>(
        T obj,
        CancellationToken cancellationToken = default
    )
    {
        return await Task.Run(() => Serialize(obj), cancellationToken);
    }

    /// <summary>
    /// Serialize <paramref name="obj"/> to a string via <see cref="DataContractSerializer"/>
    /// and save to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">A relative or absolute path for the file.</param>
    /// <param name="append">
    /// If true, <see cref="FileStream"/> is opened with <see cref="FileMode.Append"/>;
    /// otherwise, it is opened with <see cref="FileMode.Create"/>.
    /// </param>
    public static void SerializeToFile<T>(T obj, string path, bool append = false)
    {
        FileStreamOptions options = new FileStreamOptions()
        {
            Mode = append ? FileMode.Append : FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None
        };
        using FileStream fs = new(path, options);
        DataContractSerializer serializer = new(typeof(T));
        using (XmlWriter xw = XmlWriter.Create(fs, XmlSettings))
            serializer.WriteObject(xw, obj);
    }

    /// <summary>
    /// Asynchronously serialize <paramref name="obj"/> to a string via <see cref="DataContractSerializer"/>
    /// and save to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">A relative or absolute path for the file.</param>
    /// <param name="append">
    /// If true, <see cref="FileStream"/> is opened with <see cref="FileMode.Append"/>;
    /// otherwise, it is opened with <see cref="FileMode.Create"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    public static async Task SerializeToFileAsync<T>(
        T obj,
        string path,
        bool append = false,
        Encoding? fileEncoding = null,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Run(
            () =>
            {
                fileEncoding ??= Encoding;
                FileStreamOptions options = new FileStreamOptions()
                {
                    Mode = append ? FileMode.Append : FileMode.Create,
                    Access = FileAccess.Write,
                    Share = FileShare.None
                };
                XmlWriterSettings xmlSettings = XmlSettings;
                xmlSettings.Encoding = fileEncoding;
                DataContractSerializer serializer = new(typeof(T));

                using FileStream fs = new(path, options);
                using (XmlWriter xw = XmlWriter.Create(fs, xmlSettings))
                    serializer.WriteObject(xw, obj);
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Deserialize <paramref name="xml"/> to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The deserialized object.</returns>
    public static T Deserialize<T>(ReadOnlySpan<char> xml)
    {
        DataContractSerializer serializer = new(typeof(T));
        byte[] bytes = new byte[Encoding.GetByteCount(xml)];
        Encoding.GetBytes(xml, bytes);
        using MemoryStream ms = new(bytes, false);
        using XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(
            ms,
            new XmlDictionaryReaderQuotas()
        );
        object obj = serializer.ReadObject(reader, true)!;
        return (T)obj;
    }

    /// <summary>
    /// Deserialize the content of <paramref name="path"/> to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="fileEncoding">
    /// An <see cref="System.Text.Encoding"/> object for reading the file.
    /// Defaults to <see cref="Encoding.UTF8"/>.
    /// </param>
    /// <returns>The deserialized object.</returns>
    public static T DeserializeFromFile<T>(string path, Encoding? fileEncoding = null)
    {
        fileEncoding ??= Encoding;
        return Deserialize<T>(File.ReadAllText(path, fileEncoding));
    }

    /// <summary>
    /// Asynchronously deserialize the content of <paramref name="path"/> to an object of type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <param name="fileEncoding">
    /// An <see cref="System.Text.Encoding"/> object for reading the file.
    /// Defaults to <see cref="Encoding.UTF8"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>The deserialized object.</returns>
    public static async Task<T> DeserializeFromFileAsync<T>(
        string path,
        Encoding? fileEncoding = null,
        CancellationToken cancellationToken = default
    )
    {
        fileEncoding ??= Encoding;
        string xml = await File.ReadAllTextAsync(path, fileEncoding, cancellationToken);
        return Deserialize<T>(xml);
    }
}
