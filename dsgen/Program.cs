using CommandLine;
using dsgen.Excel;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using ExcelDataReader;
using CommunityToolkit.Diagnostics;
using CommandLine.Text;
using System.Diagnostics;
using dsgen.ColumnInfo;
using System.Data;
using System.Globalization;

namespace dsgen;

internal class Program
{
    #region Exit Codes

    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;

    #endregion

    #region Verbosity Levels

    /// <summary>
    /// Verbose at minimum. Only writes warnings and errors.
    /// </summary>
    public const int VERBOSE_MINIMAL = 1;

    /// <summary>
    /// Writes what the program is currently doing, or have done.
    /// </summary>
    public const int VERBOSE_PROGRESS = 2;

    /// <summary>
    /// Writes detailed information that is referenced by the program to decide behaviors.
    /// </summary>
    public const int VERBOSE_DETAILS = 3;

    /// <summary>
    /// Writes the stacktrace when writing exceptions. (Only enabled the debug build.)
    /// See <see cref="WriteException(Exception)"/>.
    /// </summary>
    public const int VERBOSE_STACKTRACE = 4;

    // csharpier-ignore-start
    /// <summary>
    /// Maximum verbosity.
    /// Having verbosity higher than <see cref="VERBOSE_MAX"/> will not be different
    /// from using <see cref="VERBOSE_MAX"/>.
    /// </summary>
    public const int VERBOSE_MAX =
#if DEBUG
        VERBOSE_STACKTRACE;
#else
        VERBOSE_DETAILS;
#endif

    /// <summary>
    /// Simply <c>Program.VERBOSE_MAX.ToString()</c> but constant.
    /// </summary>
    public const string VERBOSE_MAX_AS_STRING =
#if DEBUG
        "4";
#else
        "3";
#endif
    // csharpier-ignore-end

    #endregion

    internal const string ColumnInfoFilePath = "columns.xml";

    #region Messages

    /* Messages that are used in Program. */
#if !DEBUG
    private const string FailedToInitializeMessage = "Failed to initialize.";
#endif
    private const string ParseFailedMessage = "Failed to parse the commandline arguments.";
    private const string VerbosityNegativeMessage = "Verbosity cannot be a negative number.";
    private const string NoFilePathMessage = "File path is not specified.";
    private const string NoProperClassSheetMessage =
        "No sheet proper for being a class sheet was found. "
        + "Verify if the file is valid, or specify the class sheets via '--class-sheets' option.";
    private const string NoProperStudentSheetMessage =
        "No sheet proper for being a student sheet was found. "
        + "Verify if the file is valid, or specify the class sheets via '--student-sheets' option.";
    private const string SheetsOverlappingMessage =
        "'--class-sheets' and '-student-sheets' may not contain a common index.";
    private const string IndicesOutOfRangeMessage =
        "'--class-sheets' or '-student-sheets' contains an invalid index.";
    private const string SelectedLowScoreSheetMessage =
        "You selected a sheet with low probability for being a valid class/student sheet. "
        + "Increase verbosity to see the probabilities.";
    private const string ExtractionFailedMessage = "Failed to extract data from '{0}'.";
    private const string CultureOverlapMessage =
        "Sheets '{0}' and '{1}' seem to be written with the same language. "
        + "Try specifying the class sheets via '--class-sheeets' option.";
    private const string EmptyLocalizableMessage =
        "Class {{ Code = {0}, Grade = {1}, Class No. = {2} }} has an empty field '{3}'.";
    private const string ManyRowsLackingMessage =
        "More than {0} rows lack one or more field(s) to be filled in.";

    #endregion

    private static bool _lastPrintedNewLine = true;
    private static int _verbose = 1;

    private static bool _canChangeConsoleColor;

    static Program()
    {
        Debug.Assert(String.Equals(VERBOSE_MAX.ToString(), VERBOSE_MAX_AS_STRING));

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
        var columnInitializeTask = Task.Run(Column.Initialize);
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
        catch (Exception ex)
        {
            if (ex is ColumnConstraintException)
                WriteException(ex);
            else
                WriteError(FailedToInitializeMessage);
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
        if (String.IsNullOrWhiteSpace(options.FilePath))
        {
            WriteError(NoFilePathMessage);
            return EXIT_ERROR;
        }
        if (_verbose < 0)
        {
            WriteError(VerbosityNegativeMessage);
            return EXIT_ERROR;
        }
        if (_verbose >= VERBOSE_DETAILS)
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
            /* It is not allowed for user-selected class sheets and student sheets to overlap. */
            if (Enumerable.Intersect(options.ClassSheets, options.StudentSheets).Any())
            {
                WriteError(SheetsOverlappingMessage);
                return EXIT_ERROR;
            }
            WriteIfVerbose(VERBOSE_PROGRESS, "Loading file...");
            ExcelBook book = ExcelBook.FromFile(options.FilePath);
            string[] sheetNames = book.Keys.ToArray();
            Array.Sort(sheetNames);
            var scores = new (float ClassSheetScore, float StudentSheetScore)[book.Count];
            int pad = ToStringLength(sheetNames.Length - 1);
            string format = $"    [{{0:D{pad}}}] ";
            WriteLineIfVerbose(VERBOSE_PROGRESS, " Done ✓");
            if (
                options.ClassSheets.Any(i => i >= book.Count)
                || options.ClassSheets.Any(i => i >= book.Count)
            )
            {
                WriteError(IndicesOutOfRangeMessage);
                return EXIT_ERROR;
            }
            WriteIfVerbose(VERBOSE_PROGRESS, "Evaluating sheets... ");
            WriteLineIfVerbose(VERBOSE_DETAILS, "");
            ConsoleColor oldColor = Console.ForegroundColor;
            for (int i = 0; i < sheetNames.Length; i++)
            {
                string name = sheetNames[i];
                ExcelSheet sheet = book[name];
                WriteIfVerbose(VERBOSE_DETAILS, format, i, name);
                WriteColoredIfVerbose(
                    VERBOSE_DETAILS,
                    sheet.Hidden ? ConsoleColor.DarkGray : null,
                    "\"{0}\"",
                    name
                );
                WriteIfVerbose(VERBOSE_DETAILS, "... ");
                scores[i] = (
                    SheetTypeEvaluator.ClassSheetScore(sheet),
                    SheetTypeEvaluator.StudentSheetScore(sheet)
                );
                WriteLineIfVerbose(VERBOSE_DETAILS, " ✓");
            }
            WriteLineIfVerbose(VERBOSE_PROGRESS, "Done ✓");
            WriteScoreTableIfVerbose(VERBOSE_DETAILS, book, sheetNames, scores);

            /* Select the sheets from which we extract data. */
            ExcelSheet[] classSheets = (
                options.ClassSheets.Any()
                    ? options.ClassSheets
                    : Enumerable
                        .Range(0, sheetNames.Length)
                        .Where(i => scores[i].ClassSheetScore > options.Threshold)
            )
                .Select(i => book[sheetNames[i]])
                .ToArray();
            ExcelSheet[] studentSheets = (
                options.StudentSheets.Any()
                    ? options.StudentSheets
                    : Enumerable
                        .Range(0, sheetNames.Length)
                        .Where(i => scores[i].StudentSheetScore > options.Threshold)
            )
                .Select(i => book[sheetNames[i]])
                .ToArray();

            /* If no sheets are selected, exit with error. */
            if (classSheets.Length == 0)
            {
                WriteError(NoProperClassSheetMessage);
                return EXIT_ERROR;
            }
            if (studentSheets.Length == 0)
            {
                WriteError(NoProperStudentSheetMessage);
                return EXIT_ERROR;
            }

            WriteLineIfVerbose(
                VERBOSE_DETAILS,
                $"Total {classSheets.Length} class sheets are selected."
            );
            for (int i = 0; i < classSheets.Length; i++)
            {
                WriteLineIfVerbose(VERBOSE_DETAILS, $"    - {classSheets[i].Name}");
            }
            WriteLineIfVerbose(VERBOSE_DETAILS, "");

            WriteLineIfVerbose(
                VERBOSE_DETAILS,
                $"Total {studentSheets.Length} student sheets are selected."
            );
            for (int i = 0; i < studentSheets.Length; i++)
            {
                WriteLineIfVerbose(VERBOSE_DETAILS, $"    - {studentSheets[i].Name}");
            }
            WriteLineIfVerbose(VERBOSE_DETAILS, "");

            /* Warn the user if they selected a sheet with low score. */
            if (
                options.ClassSheets.Any(i => scores[i].ClassSheetScore <= options.Threshold)
                || options.StudentSheets.Any(i => scores[i].StudentSheetScore <= options.Threshold)
            )
            {
                WriteWarning(SelectedLowScoreSheetMessage);
            }

            WriteIfVerbose(VERBOSE_PROGRESS, "Extracting data from class sheets... ");
            WriteLineIfVerbose(VERBOSE_DETAILS, "");

            var classSheetResults = new (DataTable Table, CultureInfo Culture)?[classSheets.Length];
            for (int i = 0; i < classSheets.Length; i++)
            {
                WriteIfVerbose(VERBOSE_DETAILS, "    {0}... ", classSheets[i].Name);

                if (
                    DataExtractor.TryExtractAsClassSheet(
                        classSheets[i],
                        out DataTable? table,
                        out CultureInfo? culture
                    )
                )
                {
                    classSheetResults[i] = (table, culture);
                }

                /* Check if extraction was successful. */
                if (
                    classSheetResults[i] is null
                    || classSheetResults[i]!.Value.Table.Rows.Count == 0
                )
                {
                    WriteError(ExtractionFailedMessage, classSheets[i].Name);
                    return EXIT_ERROR;
                }

                /* Check if `classSheetResults` has unique `CultureInfo`s. */
                for (int j = 0; j < i; j++)
                {
                    CultureInfo culture1 = classSheetResults[i]!.Value.Culture;
                    CultureInfo culture2 = classSheetResults[j]!.Value.Culture;
                    if (culture1 == culture2)
                    {
                        WriteError(CultureOverlapMessage, classSheets[j].Name, classSheets[i].Name);
                        return EXIT_ERROR;
                    }
                }
                WriteLineIfVerbose(VERBOSE_DETAILS, "Done ✓");
            }
            WriteLineIfVerbose(VERBOSE_PROGRESS, "Done ✓");

            WriteIfVerbose(VERBOSE_PROGRESS, "Building class table... ");
            ClassTableBuilder classTableBuilder = new();
            foreach (var tuple in classSheetResults)
            {
                classTableBuilder.Add(tuple!.Value.Table, tuple!.Value.Culture);
            }
            CultureInfo primaryCulture = CultureInfo.GetCultureInfo(options.PrimaryCulture);
            DataTable classTable = classTableBuilder.Build(
                primaryCulture,
                true,
                out TableBuildReport report
            );
            WriteLineIfVerbose(VERBOSE_PROGRESS, "Done ✓");

            const int WriteRowsThreshold = 5;
            if (!report.IsClear)
            {
                int length = Math.Min(report.EmptyLocalizableColumns.Length, WriteRowsThreshold);
                for (int i = 0; i < length; i++)
                {
                    DataRow row = report.Table.Rows[report.EmptyLocalizableColumns[i]];
                    int nullIdx = Array.FindIndex(row.ItemArray, item => item is DBNull);
                    WriteWarning(
                        EmptyLocalizableMessage,
                        row["Code"],
                        row["Grade"],
                        row["Class"],
                        report.Table.Columns[nullIdx].ColumnName
                    );
                }
                if (report.EmptyLocalizableColumns.Length > WriteRowsThreshold)
                {
                    WriteWarning(ManyRowsLackingMessage, WriteRowsThreshold);
                }
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
                if (attr1.Required ^ attr2.Required)
                    return attr1.Required ? -1 : 1;
                else
                {
                    bool mt1 = String.IsNullOrEmpty(attr1.ShortName);
                    bool mt2 = String.IsNullOrEmpty(attr2.ShortName);
                    if (mt1 ^ mt2)
                        return mt1 ? 1 : -1;
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

        int exitCode = result.Errors.Any(IsAbnormalError) ? EXIT_ERROR : EXIT_SUCCESS;
        var testbuilder = new HelpText().SentenceBuilder;
        if (exitCode != EXIT_SUCCESS)
        {
            Error[] errorsToPrint = result.Errors.Where(IsAbnormalError).ToArray();
            StringBuilder sb = new();
            sb.Append(ParseFailedMessage);
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
        if (_verbose <= 0)
            return;
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
                WriteLineColoredIfVerbose(
                    -1,
                    isHidden[i] ? ConsoleColor.DarkGray : null,
                    sheetNames[i]
                );
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
        if (_verbose <= 0)
            return;
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
    private static void WriteException(Exception e, bool root = true)
    {
#if DEBUG
        if (_verbose >= VERBOSE_STACKTRACE)
        {
            Exception? ex = e;
            StringBuilder format = new();
            int index = 1;
            List<object?> arguments = new() { Environment.NewLine };
            while (ex is not null)
            {
                if (!ReferenceEquals(e, ex))
                {
                    format.Append("[Inner] ");
                }
                format.Append($"{{{index++}}}{{0}}StackTrace:{{0}}{{{index++}}}{{0}}");
                arguments.Add(ex.Message);
                arguments.Add(ex.StackTrace);
                ex = ex.InnerException;
            }
            format.Remove(format.Length - 3, 3);
            WriteError(format.ToString(), arguments.ToArray());
        }
        else
#endif
        {
            WriteError(e.Message);
        }
    }

    private static void WriteRawException(Exception e)
    {
        Console.WriteLine("{1}{0}StackTrace:{0}{2}", Environment.NewLine, e.Message, e.StackTrace);
        if (e.InnerException is not null)
        {
            WriteRawException(e.InnerException);
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
    /// Write to stdout with the specified color
    /// if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    /// <param name="color">
    /// The color to print with. If it is set to <c>null</c>,
    /// it is practically same as <see cref="WriteLineIfVerbose"/>.
    /// </param>
    private static void WriteColoredIfVerbose(
        int verbosityThreshold,
        ConsoleColor? color,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        if (color is not null)
            ChangeConsoleForeground(color.Value);
        WriteIfVerbose(verbosityThreshold, format, arg);
        ChangeConsoleForeground(oldColor);
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
    /// Write to stdout with the specified color
    /// if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    /// <param name="color">
    /// The color to print with. If it is set to <c>null</c>,
    /// it is practically same as <see cref="WriteLineIfVerbose"/>.
    /// </param>
    private static void WriteLineColoredIfVerbose(
        int verbosityThreshold,
        ConsoleColor? color,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        ConsoleColor oldColor = Console.ForegroundColor;
        if (color is not null)
            ChangeConsoleForeground(color.Value);
        WriteLineIfVerbose(verbosityThreshold, format, arg);
        ChangeConsoleForeground(oldColor);
    }

    /// <summary>
    /// Write the score table to stdout if verbosity level is not smaller than <paramref name="verbosityThreshold"/>.
    /// </summary>
    private static void WriteScoreTableIfVerbose(
        int verbosityThreshold,
        ExcelBook book,
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
            $"│ {{0,{indexLength}:D{pad}}} │ {{1,{classSheetLength - 2}:F{precision}}} % │ {{2,{studentSheetLength - 2}:F{precision}}} % │ ";
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
            bool hidden = book[sheetNames[i]].Hidden;
            Console.Write(
                tableFormat,
                i,
                tuple.ClassSheetScore * 100,
                tuple.StudentSheetScore * 100
            );
            ConsoleColor oldColor = Console.ForegroundColor;
            WriteLineColoredIfVerbose(-1, hidden ? ConsoleColor.DarkGray : null, sheetNames[i]);
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
