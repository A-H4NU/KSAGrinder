using System.Globalization;

namespace dsgen;

internal class AssociatedCultureAttribute : Attribute
{
    public CultureInfo CultureInfo { get; }

    public AssociatedCultureAttribute(int culture)
    {
        CultureInfo = CultureInfo.GetCultureInfo(culture);
    }

    public AssociatedCultureAttribute(string cultureName)
    {
        CultureInfo = CultureInfo.GetCultureInfo(cultureName);
    }
}
