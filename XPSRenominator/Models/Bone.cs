using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace XPSRenominator.Models;

public class Bone : Translatable
{        
    public Bone? Parent { get; set; }
    public double[] Position { get; set; } = new double[3];
    public double[] Rotation { get; set; } = new double[3];
    public double[] Scale { get; set; } = new double[3];
    public bool FromMeshAscii { get; set; }

    [MemberNotNullWhen(false, nameof(Parent))]
    public bool IsRoot => Parent == null;

    public IEnumerable<Bone> GetFullTree()
    {
        return IsRoot ? new[] { this } : Parent.GetFullTree().Append(this);
    }

    public override void ApplyRegex(string pattern, string replacement, Dictionary<string, int> renameIndexes,
        Dictionary<Translatable, int> groupIndexes, Func<Translatable, bool> exclude)
    {
        base.ApplyRegex(pattern, replacement, renameIndexes, groupIndexes, exclude);
        if (TranslatingName == null) return;

        var group = GetFullTree().FirstOrDefault(b => !exclude(b) && Regex.IsMatch(b.TranslatedName, pattern));
        if (group == null) return;

        if (!groupIndexes.ContainsKey(group))
            groupIndexes.TryAdd(group, 1);
        else
            groupIndexes[group]++;
        var groupIndex = groupIndexes.Keys.ToList().IndexOf(group) + 1;
        TranslatingName = TranslatingName
            .Replace("\\gi\\gi\\gi", groupIndex.ToString().PadLeft(3, '0'))
            .Replace("\\gi\\gi", groupIndex.ToString().PadLeft(2, '0'))
            .Replace("\\gi", groupIndex.ToString().PadLeft(1, '0'))
            .Replace("\\gd\\gd\\gd", groupIndexes[group].ToString().PadLeft(3, '0'))
            .Replace("\\gd\\gd", groupIndexes[group].ToString().PadLeft(2, '0'))
            .Replace("\\gd", groupIndexes[group].ToString().PadLeft(1, '0'));
    }
}