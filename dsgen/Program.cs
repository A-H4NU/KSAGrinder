﻿using CommandLine;
using dsgen.Excel;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using ExcelDataReader;
using CommunityToolkit.Diagnostics;
using CommandLine.Text;
using System.Diagnostics;
using dsgen.ColumnInfo;

namespace dsgen;

internal class Program
{
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;

    internal const string ColumnInfoFilePath = "columns.xml";

    #region Messages

    /* Messages that are used in Program. */
    private const string NoProperClassSheetMessage =
        "No sheet proper for being a class sheet was found. "
        + "Verify if the file is valid, or specify the class sheets via '--class-sheets'.";
    private const string NoProperStudentSheetMessage =
        "No sheet proper for being a student sheet was found. "
        + "Verify if the file is valid, or specify the class sheets via '--student-sheets'.";
    private const string SheetsOverlappingMessage =
        "'--class-sheets' and '-student-sheets' may not contain a common index.";
    private const string IndicesOutOfRangeMessage =
        "'--class-sheets' or '-student-sheets' contains an invalid index.";
    private const string SelectedLowScoreSheetMessage =
        "You selected a sheet with low probability for being a valid class/student sheet. "
        + "Increase verbosity to see the probabilities.";

    /* Messages that are used in ColumnInfo.Column. */
    public const string RequiredColumnNameNotFoundMessage =
        "There must be a column whose ColumnName is '{0}' in '{1}'.";

    #endregion

    private static bool _lastPrintedNewLine = true;
    private static int _verbose = 0;

    private static bool _canChangeConsoleColor;

    static Program()
    {
        try
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = color;
            _canChangeConsoleColor = true;
        }
        catch (Exception)
        {
            _canChangeConsoleColor = false;
        }
    }

    private static async Task<int> Main(string[] args)
    {
        var columnInitializeTask = Column.InitializeAsync();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var parser = new Parser(config => config.HelpWriter = null);
        var result = parser.ParseArguments<Options>(args);
        result.WithParsed(options => _verbose = options.Verbose);
        try
        {
            await columnInitializeTask;
        }
#if DEBUG
        catch (Exception ex)
        {
            WriteException(ex);
            return EXIT_ERROR;
        }
#else
        catch (Exception)
        {
            WriteError("Failed to initialize.");
            return EXIT_ERROR;
        }
#endif
        return result.MapResult(
            options => RunAndGetExitCode(options, args),
            errors => WriteHelpAndGetExitCode(result)
        );
    }

    private static int RunAndGetExitCode(Options options, string[] args)
    {
        if (String.IsNullOrEmpty(options.FilePath))
        {
            WriteError("File path is not specified.");
            return EXIT_ERROR;
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
                return EXIT_SUCCESS;
            }
            if (Enumerable.Intersect(options.ClassSheets, options.StudentSheets).Any())
            {
                WriteError(SheetsOverlappingMessage);
                return EXIT_ERROR;
            }
            WriteIfVerbose(1, "Loading file...");
            ExcelBook book = ExcelBook.FromFile(options.FilePath);
            string[] sheetNames = book.Keys.ToArray();
            Array.Sort(sheetNames);
            var scores = new (float ClassSheetScore, float StudentSheetScore)[book.Count];
            int pad = ToStringLength(sheetNames.Length - 1);
            string format = $"    [{{0:D{pad}}}] ";
            WriteLineIfVerbose(1, " Done ✓");
            if (
                options.ClassSheets.Any(i => i >= book.Count)
                || options.ClassSheets.Any(i => i >= book.Count)
            )
            {
                WriteError(IndicesOutOfRangeMessage);
                return EXIT_ERROR;
            }
            WriteLineIfVerbose(1, "Evaluating sheets...");
            ConsoleColor oldColor = Console.ForegroundColor;
            for (int i = 0; i < sheetNames.Length; i++)
            {
                string name = sheetNames[i];
                ExcelSheet sheet = book[name];
                WriteIfVerbose(1, format, i, name);
                if (sheet.Hidden)
                    ChangeConsoleForeground(ConsoleColor.DarkGray);
                WriteIfVerbose(1, "\"{0}\"", name);
                if (sheet.Hidden)
                    ChangeConsoleForeground(oldColor);
                WriteIfVerbose(1, "... ");
                scores[i] = (
                    SheetTypeEvaluator.ClassSheetScore(sheet),
                    SheetTypeEvaluator.StudentSheetScore(sheet)
                );
                WriteLineIfVerbose(1, " ✓");
            }
            WriteLineIfVerbose(1, "Done ✓");
            WriteScoreTableIfVerbose(2, sheetNames, scores);

            Span<int> classSheets = options.ClassSheets.Any()
                ? options.ClassSheets.ToArray()
                : Enumerable
                    .Range(0, sheetNames.Length)
                    .Where(i => scores[i].ClassSheetScore > options.Threshold)
                    .ToArray();
            if (classSheets.Length == 0)
            {
                WriteError(NoProperClassSheetMessage);
                return EXIT_ERROR;
            }
            Span<int> studentSheets = options.StudentSheets.Any()
                ? options.StudentSheets.ToArray()
                : Enumerable
                    .Range(0, sheetNames.Length)
                    .Where(i => scores[i].StudentSheetScore > options.Threshold)
                    .ToArray();
            if (studentSheets.Length == 0)
            {
                WriteError(NoProperStudentSheetMessage);
                return EXIT_ERROR;
            }

            if (
                options.ClassSheets.Any(i => scores[i].ClassSheetScore <= options.Threshold)
                || options.StudentSheets.Any(i => scores[i].StudentSheetScore <= options.Threshold)
            )
            {
                WriteWarning(SelectedLowScoreSheetMessage);
            }
        }
        catch (Exception e)
        {
            WriteException(e);
            return EXIT_ERROR;
        }
        return EXIT_SUCCESS;
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
        static bool IsAbnormalError(Error error)
        {
            bool isNormal =
                error.Tag
                    is ErrorType.HelpRequestedError
                        or ErrorType.HelpVerbRequestedError
                        or ErrorType.VersionRequestedError;
            return !isNormal;
        }

        static int OrderOnShortName(ComparableOption attr1, ComparableOption attr2)
        {
            if (attr1.IsOption && attr2.IsOption)
            {
                if (attr1.Required && !attr2.Required)
                    return -1;
                else if (!attr1.Required && attr2.Required)
                    return 1;
                else
                {
                    if (
                        string.IsNullOrEmpty(attr1.ShortName)
                        && !string.IsNullOrEmpty(attr2.ShortName)
                    )
                        return 1;
                    else if (
                        !string.IsNullOrEmpty(attr1.ShortName)
                        && string.IsNullOrEmpty(attr2.ShortName)
                    )
                        return -1;
                    return String.Compare(
                        attr1.ShortName,
                        attr2.ShortName,
                        StringComparison.Ordinal
                    );
                }
            }
            else if (attr1.IsOption && attr2.IsValue)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        int exitCode = result.Errors.Any(IsAbnormalError) ? 1 : 0;
        var testbuilder = new HelpText().SentenceBuilder;
        if (exitCode != 0)
        {
            Error[] errorsToPrint = result.Errors.Where(IsAbnormalError).ToArray();
            StringBuilder sb = new();
            sb.Append("Failed to parse the commandline arguments.");
            for (int i = 0; i < errorsToPrint.Length; i++)
                sb.AppendLine().Append(' ', 4).Append(testbuilder.FormatError(errorsToPrint[i]));
            WriteError(sb.ToString());
            return exitCode;
        }
        var helpText = HelpText.AutoBuild(
            result,
            h =>
            {
                h.MaximumDisplayWidth = 100;
                h.AddNewLineBetweenHelpSections = true;
                h.OptionComparison = OrderOnShortName;
                return h;
            }
        );
        Console.WriteLine(helpText);
        return exitCode;
    }

    private static void WriteSheetNames(string path)
    {
        FileStream? fs = null;
        IExcelDataReader? reader = null;
        try
        {
            fs = File.OpenRead(path);
            reader = ExcelReaderFactory.CreateReader(fs);
            Console.WriteLine("Total {0} sheets found:", reader.ResultsCount);
            Guard.IsNotEqualTo(reader.ResultsCount, 0, "The number of sheets");
            int pad = ToStringLength(reader.ResultsCount - 1);
            string format = $"    [{{0:D{pad}}}] ";
            string[] sheetNames = new string[reader.ResultsCount];
            bool[] isHidden = new bool[reader.ResultsCount];
            int index = 0;
            do
            {
                sheetNames[index] = reader.Name;
                isHidden[index] = reader.VisibleState != "visible";
                index++;
            } while (reader.NextResult());
            Debug.Assert(index == reader.ResultsCount);
            Array.Sort(sheetNames, isHidden);
            ConsoleColor oldColor = Console.ForegroundColor;
            for (int i = 0; i < sheetNames.Length; i++)
            {
                Console.Write(format, i);
                if (isHidden[i])
                    ChangeConsoleForeground(ConsoleColor.DarkGray);
                Console.WriteLine(sheetNames[i]);
                ChangeConsoleForeground(oldColor);
            }
        }
        finally
        {
            reader?.Dispose();
            fs?.Dispose();
        }
    }

    /// <summary>
    /// Prints the warning with the <see cref="format"/> and <see cref="arg"/> provided.
    /// </summary>
    private static void WriteWarning(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        if (!_lastPrintedNewLine)
            Console.Write(Environment.NewLine);
        Console.Write("dsgen.exe: ");
        ChangeConsoleForeground(ConsoleColor.Yellow);
        Console.Write("warning: ");
        ChangeConsoleForeground(oldColor);
        Console.WriteLine(format, arg);
        _lastPrintedNewLine = true;
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
        ChangeConsoleForeground(ConsoleColor.Red);
        Console.Write("fatal error: ");
        ChangeConsoleForeground(oldColor);
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
        if (_verbose >= 3)
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
        (float ClassSheetScore, float StudentSheetScore)[] scores
    )
    {
        const string IndexHeader = "Index";
        const string ClassSheetHeader = "Prob. Class Sheet";
        const string StudentSheetHeader = "Prob. Student Sheet";
        const string SheetNameHeader = "Sheet Name";
        const int precision = 2;

        if (_verbose < verbosityThreshold)
            return;
        Guard.IsEqualTo(sheetNames.Length - scores.Length, 0);

        int length = Console.WindowWidth;
        int pad = ToStringLength(sheetNames.Length - 1);
        int indexLength = Math.Max(pad, IndexHeader.Length);
        int classSheetLength = Math.Max(5 + precision, ClassSheetHeader.Length);
        int studentSheetLength = Math.Max(5 + precision, StudentSheetHeader.Length);
        string headerFormat =
            $"│ {{0,{indexLength}}} │ {{1,{classSheetLength}}} │ {{2,{studentSheetLength}}} │ {{3}}";
        // `classSheetLength - 2` is for the space of " %"
        string tableFormat =
            $"│ {{0,{indexLength}:D{pad}}} │ {{1,{classSheetLength - 2}:F{precision}}} % │ {{2,{studentSheetLength - 2}:F{precision}}} % │ {{3}}";
        Console.WriteLine();
        Console.WriteLine("Evaluation Result:");
        Console.WriteLine(
            GetTableUpperBorder(
                length,
                indexLength + 2, // 2's are for spaces
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
            var tuple = scores[i];
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
                indexLength + 2, // 2's are for spaces
                classSheetLength + 2,
                studentSheetLength + 2
            )
        );
        Console.WriteLine();
        _lastPrintedNewLine = true;
    }

    private static void ChangeConsoleForeground(ConsoleColor color)
    {
        if (!_canChangeConsoleColor)
            return;
        try
        {
            Console.ForegroundColor = color;
        }
        catch (Exception) { }
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
