using Mono.Options;

using ExcelDataReader;

using System.Text;
using System.Diagnostics.CodeAnalysis;

internal class Program
{
    private static bool _showHelp = false;
    private static bool _verbose = false;
    private static string? _outputPath = null;
    private static string? _filePath = null;

    private static readonly OptionSet _options = new()
        {
            {
                "o|output=",
                "Specify where the output file is placed",
                o => _outputPath = o
            },
            {
                "v|verbose",
                "Be verbose",
                v => _verbose = v is not null
            },
            {
                "h|help",
                "Show this meesage and exit",
                h => _showHelp = h is not null
            },
        };

    private static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        List<string> extra;
        try
        {
            extra = _options.Parse(args);
        }
        catch (OptionException e)
        {
            PrintException(e);
            return;
        }

        if (_showHelp)
        {
            ShowHelp();
            return;
        }

        if (extra.Count == 0)
        {
            PrintError("File path is not specified.");
            return;
        }

        _filePath = extra[0];
        try
        {
            ExcelBook book = ExcelBook.FromFile(_filePath);
            foreach (string name in book.Keys)
            {
                Console.WriteLine(name);
            }
        }
        catch (Exception e)
        {
            PrintException(e);
        }
    }

    /// <summary>
    /// Prints the usage.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("Usage: dsgen.exe [options] <file_path>");
        Console.WriteLine("Generate a dataset file from an Excel file provided by Office of Academic Affairs of KSA.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        _options.WriteOptionDescriptions(Console.Out);
    }

    /// <summary>
    /// Prints the error with the <see cref="format"/> and <see cref="arg"/> provided.
    /// </summary>
    private static void PrintError(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg)
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.Write("dsgen.exe: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("fatal error: ");
        Console.ForegroundColor = oldColor;
        Console.WriteLine(format, arg);
        Console.WriteLine("Try `dsgen --help' for more information.");
    }

    /// <summary>
    /// Print the exception via <see cref="PrintError(string, object?[]?)"/>.
    /// </summary>
    /// <param name="e">Error to print.</param>
    private static void PrintException(Exception e)
    {
        if (_verbose)
        {
            PrintError(
                "{1}{0}StackTrace:{0}{2}",
                Environment.NewLine,
                e.Message,
                e.StackTrace);
        }
        else
        {
            PrintError(e.Message);
        }
    }
}
