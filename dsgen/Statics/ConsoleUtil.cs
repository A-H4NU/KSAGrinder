using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace dsgen.Statics;

internal static class ConsoleUtil
{
    public static int Verbose;
    private static readonly bool _canChangeConsoleColor;
    public static bool NoErrorMessage;
    private static bool _lastPrintedNewLine = true;

    static ConsoleUtil()
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

    /// <summary>
    /// Informs that a newline character is just printed.
    /// </summary>
    public static void JustPrintedNewLine()
    {
        _lastPrintedNewLine = true;
    }

    /// <summary>
    /// Prints the warning with the <see cref="format"/> and <see cref="arg"/> provided.
    /// </summary>
    public static void WriteWarning(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (Verbose <= 0)
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
    public static void WriteError(
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (NoErrorMessage)
            return;
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
    public static void WriteException(Exception e)
    {
#if DEBUG
        if (Verbose >= Program.VERBOSE_STACKTRACE)
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

    public static void WriteRawException(Exception e)
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
    public static void WriteIfVerbose(
        int verbosityThreshold,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (Verbose < verbosityThreshold)
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
    public static void WriteColoredIfVerbose(
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
    public static void WriteLineIfVerbose(
        int verbosityThreshold,
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format,
        params object?[]? arg
    )
    {
        if (Verbose < verbosityThreshold)
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
    public static void WriteLineColoredIfVerbose(
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

    public static void ChangeConsoleForeground(ConsoleColor color)
    {
        if (!_canChangeConsoleColor)
            return;
        try
        {
            Console.ForegroundColor = color;
        }
        catch (Exception) { }
    }

}
