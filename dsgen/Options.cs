using CommandLine;
using CommandLine.Text;

namespace dsgen;

public record Options
{
    private const string RandomScheduleFile = "path/to/schedule.xlsx";
    private const string RandomDataSetFile = "another path/to/dataset.ds";
    private const string RandomScheduleFileWithQuote = $"\"{RandomScheduleFile}\"";
    private const string RandomDataSetFileWithQuote = $"\"{RandomDataSetFile}\"";

    [Value(0, MetaName = "<path>", Required = true, HelpText = "The excel file to process.")]
    public string FilePath { get; init; } = "";

    [Option('v', "verbose", Default = 0, HelpText = "Be verbose with the specified level.")]
    public int Verbose { get; init; }

    [Option(
        'l',
        "show-list",
        Default = false,
        HelpText = "Display the names of sheets in <file_path> and exit."
    )]
    public bool ShowSheetList { get; init; }

    [Option(
        't',
        "threshold",
        Default = 95f,
        HelpText = "Select sheets with probability for being a valid class/student sheet greater "
            + "than VALUE. This must range from 0 to 100."
    )]
    public float Threshold { get; init; }

    [Option('o', HelpText = "Specify where the output file is placed.")]
    public string Output { get; init; } = "";

    [Usage]
    public static IEnumerable<Example> Examples { get; } =
        new List<Example>()
        {
            new(
                $"Generate a dataset from {RandomScheduleFileWithQuote} and save it as {RandomDataSetFileWithQuote}",
                new Options() { FilePath = RandomScheduleFile, Output = RandomDataSetFile }
            ),
            new(
                $"Show the names and the indices of {RandomScheduleFileWithQuote}",
                new Options() { FilePath = RandomScheduleFile, ShowSheetList = true }
            )
        };
}
