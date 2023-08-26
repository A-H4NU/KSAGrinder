using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace dsgen.ColumnInfo;

public readonly partial struct Column
{
    public static class SeparatorUtil
    {
        public const char Separator = '_';

        public static string Combine(string headerTitle, CultureInfo culture) =>
            $"{headerTitle}{Separator}{culture}";

        public static bool Separate(
            string culturedHeaderTitle,
            out string headerTitle,
            [NotNullWhen(true)] out CultureInfo? culture
        )
        {
            ReadOnlySpan<char> span = culturedHeaderTitle;
            culture = null;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == Separator)
                {
                    headerTitle = span[..i].ToString();
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(span[(i + 1)..].ToString());
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
            }
            headerTitle = culturedHeaderTitle;
            return false;
        }

        public static string GetNonlocalizedColumnName(string columnName)
        {
            ReadOnlySpan<char> span = columnName;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == Separator)
                {
                    return span[..i].ToString();
                }
            }
            return columnName;
        }
    }
}
