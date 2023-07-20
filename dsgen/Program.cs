using CommandLine;
using dsgen.Excel;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using ExcelDataReader;
using CommunityToolkit.Diagnostics;
using CommandLine.Text;

namespace dsgen;

internal class Program
{
    private static bool _lastPrintedNewLine = true;
    private static int _verbose = 0;

    private static int Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        List<string> extra = new();
        var parser = new Parser(config =>
        {
            config.HelpWriter = null;
        });
        var result = parser.ParseArguments<Options>(args);
        return result.MapResult(
            options => RunAndGetExitCode(options, args),
            errors => WriteHelpAndGetExitCode(result)
        );
    }

    private static int RunAndGetExitCode(Options options, string[] args)
    {
        _verbose = options.Verbose;
        if (String.IsNullOrEmpty(options.FilePath))
        {
            WriteError("File path is not specified.");
            return 1;
        }
        if (_verbose >= 1)
        {
            Console.WriteLine("Arguments:");
            int pad = ToStringLength(args.Length - 1);
            string format = $"    [{{0:D{pad}}}] {{1}}";
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine(format, i, args[i]);
            }
            Console.WriteLine();
        }

        try
        {
            if (options.ShowSheetList)
            {
                WriteSheetNames(options.FilePath);
                return 0;
            }
            WriteIfVerbose(1, "Loading file...");
            ExcelBook book = ExcelBook.FromFile(options.FilePath);
            string[] sheetNames = book.Keys.ToArray();
            Array.Sort(sheetNames);
            Dictionary<string, (float ClassSheetScore, float StudentSheetScore)> scores =
                new(book.Count);
            int pad = ToStringLength(sheetNames.Length - 1);
            string format = $"    [{{0:D{pad}}}] ";
            WriteLineIfVerbose(1, " Done ✓");
            WriteLineIfVerbose(1, "Evaluating sheets...");
            ConsoleColor oldColor = Console.ForegroundColor;
            for (int i = 0; i < sheetNames.Length; i++)
            {
                string name = sheetNames[i];
                ExcelSheet sheet = book[name];
                WriteIfVerbose(1, format, i, name);
                if (sheet.Hidden)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                WriteIfVerbose(1, "\"{0}\"", name);
                Console.ForegroundColor = oldColor;
                WriteIfVerbose(1, "... ");
                scores[name] = (
                    SheetTypeEvaluator.ClassSheetScore(sheet),
                    SheetTypeEvaluator.StudentSheetScore(sheet)
                );
                WriteLineIfVerbose(1, " ✓");
            }
            WriteLineIfVerbose(1, "Done ✓");
            WriteScoreTableIfVerbose(2, sheetNames, scores);
        }
        catch (Exception e)
        {
            WriteException(e);
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// Prints the usage and return appropriate exit code.
    /// </summary>
    /// <returns>
    /// <c>0</c> if <paramref name="result"/> has no errors other than
    /// <c>ErrorType.HelpRequestedError</c>, <c>ErrorType.HelpVerbRequestedError</c>, and
    /// <c>ErrorType.VersionRequestedError</c>;
    /// otherwise, <c>1</c>.
    /// </returns>
    private static int WriteHelpAndGetExitCode<T>(ParserResult<T> result)
    {
        var helpText = HelpText.AutoBuild(
            result,
            h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.AddNewLineBetweenHelpSections = true;
                h.OptionComparison = HelpText.RequiredThenAlphaComparison;
                return h;
            }
        );
        Console.WriteLine(helpText);
        return result.Errors.All(
            error =>
                error.Tag
                    is ErrorType.HelpRequestedError
                        or ErrorType.HelpVerbRequestedError
                        or ErrorType.VersionRequestedError
        )
            ? 0
            : 1;
    }

    private static void WriteSheetNames(string path)
    {
        FileStream? fs = null;
        IExcelDataReader? reader = null;
        try
        {
            fs = File.OpenRead(path);
            reader = ExcelReaderFactory.CreateReader(fs);
            Guard.IsNotEqualTo(reader.ResultsCount, 0, "The number of sheets");
            int pad = ToStringLength(reader.ResultsCount - 1);
            string format = $"[{{0:D{pad}}}] {{1}}";
            string[] sheetNames = new string[reader.ResultsCount];
            int index = 0;
            do
            {
                sheetNames[index] = reader.Name;
                index++;
            } while (reader.NextResult());
            Array.Sort(sheetNames);
            for (int i = 0; i < sheetNames.Length; i++)
            {
                Console.WriteLine(format, i, sheetNames[i]);
            }
        }
        finally
        {
            reader?.Dispose();
            fs?.Dispose();
        }
    }

    /// <summary>
    /// Prints the error with the <see cref="format"/> and <see cref="arg"/> provided.
    /// </summary>
    private static void WriteError(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        if (!_lastPrintedNewLine)
            Console.Write(Environment.NewLine);
        Console.Write("dsgen.exe: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("fatal error: ");
        Console.ForegroundColor = oldColor;
        Console.WriteLine(format, arg);
        Console.WriteLine("Try `dsgen --help' for more information.");
        _lastPrintedNewLine = true;
    }

    /// <summary>
    /// Print the exception via <see cref="WriteError(string, object?[]?)"/>.
    /// </summary>
    /// <param name="e">Error to print.</param>
    private static void WriteException(Exception e)
    {
        if (_verbose >= 1)
        {
            WriteError("{1}{0}StackTrace:{0}{2}", Environment.NewLine, e.Message, e.StackTrace);
        }
        else
        {
            WriteError(e.Message);
        }
    }

    /// <summary>
    /// Write to stdout if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    private static void WriteIfVerbose(
        int verbosityThreshold,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (_verbose < verbosityThreshold)
            return;
        string toWrite = arg is null ? format : String.Format(format, arg);
        Console.Write(toWrite);
        if (String.IsNullOrEmpty(toWrite))
            return;
        _lastPrintedNewLine = toWrite.EndsWith('\n') || toWrite.EndsWith('\r');
    }

    /// <summary>
    /// Write to stdout if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    private static void WriteLineIfVerbose(
        int verbosityThreshold,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (_verbose < verbosityThreshold)
            return;
        Console.WriteLine(format, arg);
        _lastPrintedNewLine = true;
    }

    /// <summary>
    /// Write the score table to stdout if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    private static void WriteScoreTableIfVerbose(
        int verbosityThreshold,
        string[] sheetNames,
        Dictionary<string, (float ClassSheetScore, float StudentSheetScore)> scores
    )
    {
        const string IndexHeader = "Index";
        const string ClassSheetHeader = "Prob. Class Sheet";
        const string StudentSheetHeader = "Prob. Student Sheet";
        const string SheetNameHeader = "Sheet Name";
        const int precision = 2;

        if (_verbose < verbosityThreshold)
            return;

        int length = Console.WindowWidth;
        int pad = ToStringLength(sheetNames.Length - 1);
        int indexLength = Math.Max(pad, IndexHeader.Length);
        int classSheetLength = Math.Max(5 + precision, ClassSheetHeader.Length);
        int studentSheetLength = Math.Max(5 + precision, StudentSheetHeader.Length);
        string headerFormat =
            $"│ {{0,{indexLength}}} │ {{1,{classSheetLength}}} │ {{2,{studentSheetLength}}} │ {{3}}";
        string tableFormat =
            $"│ {{0,{indexLength}:D{pad}}} │ {{1,{classSheetLength - 2}:F{precision}}} % │ {{2,{studentSheetLength - 2}:F{precision}}} % │ {{3}}";
        Console.WriteLine(
            GetTableUpperBorder(
                length,
                indexLength + 2,
                classSheetLength + 2,
                studentSheetLength + 2
            )
        );
        Console.WriteLine(
            headerFormat,
            IndexHeader,
            ClassSheetHeader,
            StudentSheetHeader,
            SheetNameHeader
        );
        for (int i = 0; i < sheetNames.Length; i++)
        {
            var tuple = scores[sheetNames[i]];
            Console.WriteLine(
                tableFormat,
                i,
                tuple.ClassSheetScore * 100,
                tuple.StudentSheetScore * 100,
                sheetNames[i]
            );
        }
        Console.WriteLine(
            GetTableLowerBorder(
                length,
                indexLength + 2,
                classSheetLength + 2,
                studentSheetLength + 2
            )
        );
        _lastPrintedNewLine = true;
    }

    private static int ToStringLength(object? obj)
    {
        return obj?.ToString()?.Length ?? 0;
    }

    /// <summary>
    /// Get an upper table border.
    /// </summary>
    private static string GetTableUpperBorder(int length, params int[] headerLengths)
    {
        return GetTableBorder(length, '\u2500', '\u250C', '\u252C', null, headerLengths);
    }

    /// <summary>
    /// Get a lower table border.
    /// </summary>
    private static string GetTableLowerBorder(int length, params int[] headerLengths)
    {
        return GetTableBorder(length, '\u2500', '\u2514', '\u2534', null, headerLengths);
    }

    /// <summary>
    /// Get a table border.
    /// </summary>
    private static string GetTableBorder(
        int length,
        char horizontal,
        char begin,
        char middle,
        char? end = null,
        params int[] headerLengths
    )
    {
        Guard.IsGreaterThanOrEqualTo(length, 0);
        StringBuilder sb = new(length);
        sb.Append(begin);
        if (headerLengths.Length > 0)
        {
            sb.Append(horizontal, headerLengths[0]);
            for (int i = 1; i < headerLengths.Length; i++)
            {
                sb.Append(middle);
                sb.Append(horizontal, headerLengths[i]);
            }
            if (sb.Length >= length)
                goto ret;
        }
        if (length <= sb.Length)
            goto ret;
        if (end is null)
        {
            sb.Append(middle);
            sb.Append(horizontal, length - sb.Length);
        }
        else
        {
            sb.Append(end);
        }
        ret:
        return sb.ToString(0, length);
    }
}
