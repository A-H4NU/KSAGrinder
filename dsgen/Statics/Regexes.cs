using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace dsgen.Statics;

internal static partial class Regexes
{
    [GeneratedRegex(
        pattern: @"\A[a-z]{2,}\d{3,}\z",
        options: RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking
    )]
    private static partial Regex LectureCodeRegex();

    [GeneratedRegex(
        pattern: @"\A\d{2}-\d{3}\z",
        options: RegexOptions.Singleline | RegexOptions.NonBacktracking
    )]
    private static partial Regex StudentIdRegex();

    [GeneratedRegex(
        pattern: @"\A(?<name>.+)\((?<grade>[1-3])\)_(?<class>[1-9]|1[0-9])\z",
        options: RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture
    )]
    private static partial Regex ClassRegex();

    [GeneratedRegex(
        pattern: @"\A(?<name>.+)\((?<grade>[1-3])\)",
        options: RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture
    )]
    private static partial Regex LectureRegex();

    [GeneratedRegex(
        pattern: @"\A(?<name>.+)\((?<grade>[1-3])\)(_(?<class>[1-9]|1[0-9]))?\z",
        options: RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture
    )]
    private static partial Regex LectureOrClassRegex();

    /// <summary>
    /// The format for time regexes.
    /// <p>
    /// - The regex <c>Regexes.Time[CultureInfo.GetCultureInfo("en")]</c> has the pattern
    ///   <c>String.Format(Regexes.TimeRegexFormat, "Mon", "Tue", "Wed", "Thu", "Fri")</c>.
    /// - The regexes has the RegexOptions flag
    ///     <c>RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture</c>.
    /// </p>
    /// </summary>
    public const string TimeRegexFormat =
        @"((?<day>{0}|{1}|{2}|{3}|{4})(?<hr>\d{{1,2}}))"
        + @"([\||,|.| |;|/|\ ]+((?<day>{0}|{1}|{2}|{3}|{4})(?<hr>\d{{1,2}})))*";

    [AssociatedCulture("ko-KR")]
    [GeneratedRegex(
        pattern: @"((?<day>월|화|수|목|금)(?<hr>\d{1,2}))"
            + @"([\||,|.| |;|/|\ ]+((?<day>월|화|수|목|금)(?<hr>\d{1,2})))*",
        options: RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
    )]
    private static partial Regex TimeRegex_koKR();

    [AssociatedCulture("en")]
    [GeneratedRegex(
        pattern: @"((?<day>Mon|Tue|Wed|Thu|Fri)(?<hr>\d{1,2}))"
            + @"([\||,|.| |;|/|\ ]+((?<day>Mon|Tue|Wed|Thu|Fri)(?<hr>\d{1,2})))*",
        options: RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
    )]
    private static partial Regex TimeRegex_en();

    /// <summary>
    /// For example, if you try to match <c>"Mon1|Wed2|Fri5"</c> with
    /// <c>Regexes.Time[CultureInfo.GetCultureInfo("en")]</c>, you'll get:
    ///
    /// <p>
    /// Group 0:
    ///     Capture 0: Mon1,Wed2,Fri5 at 0
    /// Group day:
    ///     Capture 0: "Mon" at 0
    ///     Capture 1: "Wed" at 5
    ///     Capture 2: "Fri" at 10
    /// Group hr:
    ///     Capture 0: "1" at 3
    ///     Capture 1: "2" at 8
    ///     Capture 2: "5" at 13
    /// </p>
    /// </summary>
    public static ReadOnlyDictionary<CultureInfo, Regex> Time { get; }

    public static Regex LectureCode => LectureCodeRegex();

    public static Regex StudentId => StudentIdRegex();

    public static Regex Class => ClassRegex();

    public static Regex Lecture => LectureRegex();

    public static Regex LectureOrClass => LectureOrClassRegex();

    /// <summary>
    /// Initializes static members and performs some assertions.
    /// </summary>
    static Regexes()
    {
        static IEnumerable<KeyValuePair<CultureInfo, Regex>> SelectManyKeyValuePairs(MethodInfo m)
        {
            var cultureAtts = m.GetCustomAttributes(typeof(AssociatedCultureAttribute), false);
            foreach (AssociatedCultureAttribute att in cultureAtts)
            {
                yield return KeyValuePair.Create(att.CultureInfo, (Regex)m.Invoke(null, null)!);
            }
        }

        static string[] AbbreviatedWeekNames(DayOfWeek[] days, CultureInfo culture)
        {
            var res = new string[days.Length];
            for (int i = 0; i < days.Length; i++)
                res[i] = culture.DateTimeFormat.GetAbbreviatedDayName(days[i]);
            return res;
        }

        MethodInfo[] timeRegexes = (
            from method in typeof(Regexes).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            where
                method.ReturnType == typeof(Regex)
                && !method.IsGenericMethod
                && !method.IsGenericMethodDefinition
                && !method.IsSpecialName
                && method.Name.StartsWith("timeregex", ignoreCase: true, culture: null)
                && method.GetCustomAttributes(typeof(GeneratedRegexAttribute), false).Length == 1
            select method
        ).ToArray();

        DayOfWeek[] weekdays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };
        foreach (MethodInfo reg in timeRegexes)
        {
            GeneratedRegexAttribute regexAtt = (GeneratedRegexAttribute)
                reg.GetCustomAttributes(typeof(GeneratedRegexAttribute), false)[0];
            var cultureAtts = reg.GetCustomAttributes(typeof(AssociatedCultureAttribute), false);
            foreach (AssociatedCultureAttribute cultureAtt in cultureAtts)
            {
                string patternMustBe = String.Format(
                    TimeRegexFormat,
                    AbbreviatedWeekNames(weekdays, cultureAtt.CultureInfo)
                );
                Debug.Assert(
                    String.Equals(
                        regexAtt.Pattern,
                        patternMustBe,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }
        }

        Time = timeRegexes
            .SelectMany(SelectManyKeyValuePairs)
            .ToDictionary(p => p.Key, p => p.Value)
            .AsReadOnly();
    }
}
