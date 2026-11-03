using System.Reflection;
using Inventor;

namespace Inventor2027AICodingKit.Examples;

public static class AssemblyTraversalExample
{
    public static IReadOnlyList<string> GetUnsuppressedLeafOccurrenceNames(
        AssemblyDocument assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var leaves = assembly.ComponentDefinition.Occurrences
            .AllLeafOccurrences[Type.Missing];
        var names = new List<string>(leaves.Count);

        for (var index = 1; index <= leaves.Count; index++)
        {
            var occurrence = leaves[index];
            if (!occurrence.Suppressed)
            {
                names.Add(occurrence.Name);
            }
        }

        return names;
    }
}
