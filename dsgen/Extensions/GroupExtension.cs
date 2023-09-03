using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace dsgen.Extensions;

public static class GroupExtension
{
    public static bool TryFindGroupWithName(
        this GroupCollection groups,
        string name,
        [NotNullWhen(true)] out Group? group
    )
    {
        foreach (Group g in groups)
        {
            if (g.Name == name)
            {
                group = g;
                return true;
            }
        }
        group = null;
        return false;
    }
}
