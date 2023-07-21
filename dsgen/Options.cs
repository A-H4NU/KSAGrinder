using CommandLine;
using CommandLine.Text;

namespace dsgen;

public record Options
{
    private const string RandomScheduleFile = "path/to/schedule.xlsx";
    private const string RandomDataSetFile = "another path/to/dataset.ds";

    [Option(
        'p',
        "path",
        MetaValue = "XLPATH",
        Required = true,
        HelpText = "The excel file to process."
    )]
    public string FilePath { get; init; } = "";

    [Option(
        'v',
        "verbose",
        Default = 0,
        HelpText = "Be verbose with the specified level. " + "Must be a nonnegative integer."
    )]
    public int Verbose { get; init; }

    [Option('l', "show-list", HelpText = "Display the names of sheets in XLPATH and exit.")]
    public bool ShowSheetList { get; init; } = false;

    [Option(
        't',
        "threshold",
        MetaValue = "T",
        Default = .95f,
        HelpText = "Specify the threshold for selecting proper sheets. "
            + "Sheets with probability higher than T will be considered proper. "
            + "Must range from 0 to 1."
    )]
    public float Threshold { get; init; }

    [Option(
        'o',
        "output",
        MetaValue = "OUTPATH",
        HelpText = "Specify where the output file is placed."
    )]
    public string? Output { get; init; } = null;

    [Option(
        "class-sheets",
        Separator = ',',
        HelpText = "Forcibly and explicitly select the sheets that are going to be used as class sheets. "
            + "See USAGE."
    )]
    public IEnumerable<int> ClassSheets { get; init; } = Enumerable.Empty<int>();

    [Option(
        "student-sheets",
        Separator = ',',
        HelpText = "Forcibly and explicitly select the sheets that are going to be used as student sheets. "
            + "See USAGE."
    )]
    public IEnumerable<int> StudentSheets { get; init; } = Enumerable.Empty<int>();

    [Usage]
    public static IEnumerable<Example> Examples { get; } =
        new List<Example>()
        {
            new(
                $"Generate a dataset from '{RandomScheduleFile}' with default options",
                new Options() { FilePath = RandomScheduleFile }
            ),
            new(
                $"Show the names and the indices of the sheets in '{RandomScheduleFile}'",
                new Options() { FilePath = RandomScheduleFile, ShowSheetList = true }
            ),
            new(
                $"Generate a dataset from '{RandomScheduleFile}' and save it as '{RandomDataSetFile}'",
                new Options() { FilePath = RandomScheduleFile, Output = RandomDataSetFile }
            ),
            new(
                $"Specify the sheets of indices 2 and 3 as class sheets and generate a dataset from '{RandomScheduleFile}'",
                new Options() { FilePath = RandomScheduleFile, ClassSheets = new int[] { 2, 3 } }
            ),
        };
}
