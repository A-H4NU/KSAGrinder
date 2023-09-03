using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace dsgen.Extensions;

public static class GroupExtension
{
    /// <summary>
    /// Get the group with the name <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name to match.</param>
    /// <param name="group">
    /// On success, it is the group with the name <paramref name="name"/>
    /// </param>
    /// <returns><c>true</c> if match succeeds; otherwise, <c>false</c>.</returns>
    public static bool TryFindGroupWithName(
        this GroupCollection groupCollection,
        string name,
        [NotNullWhen(true)] out Group? group
    )
    {
        foreach (Group g in groupCollection)
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

    /// <summary>
    /// Get the list of groups of matching names from <paramref name="groupCollection"/>.
    /// </summary>
    /// <param name="names">The names to match.</param>
    /// <param name="groups">
    /// The result. On success, <c>groups[i]</c> matches <c>names[i]</c>.
    /// </param>
    /// <returns><c>true</c> if match succeeds; otherwise, <c>false</c>.</returns>
    public static bool TryFindGroupsWithName(
        this GroupCollection groupCollection,
        IList<string> names,
        [NotNullWhen(true)] out Group[]? groups
    )
    {
        groups = null;
        if (groupCollection.Count < names.Count)
            return false;
        Dictionary<string, int> nameIdx = new(names.Count);
        for (int i = 0; i < names.Count; i++)
        {
            if (!nameIdx.TryAdd(names[i], i))
                return false;
        }
        groups = new Group[names.Count];
        int count = 0;
        foreach (Group g in groupCollection)
        {
            if (nameIdx.TryGetValue(g.Name, out int index))
            {
                groups[index] = g;
                count++;
            }
        }
        if (count == names.Count)
            return true;
        groups = null;
        return false;
    }
}
