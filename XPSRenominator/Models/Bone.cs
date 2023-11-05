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

    public void ApplyRegex(string pattern, string replacement, Dictionary<string, int> renameIndexes, List<int> groupIndexes,
        Func<Translatable, bool> exclude)
    {
        base.ApplyRegex(pattern, replacement, renameIndexes, exclude);

        string Group(int padding) => string.Join(" ", groupIndexes.Take(..^1).Select(n => n.ToString().PadLeft(padding, '0')));
        string Index(int padding) => string.Join(" ", groupIndexes.Last().ToString().PadLeft(padding, '0'));

        if (groupIndexes.Any())
        {
            TranslatingName = TranslatingName?
                .Replace("\\g\\g\\g", $"{Group(3)} {Index(3)}")
                .Replace("\\g\\g", $"{Group(2)} {Index(2)}")
                .Replace("\\g", $"{Group(1)} {Index(1)}")
                .Replace("\\gi\\gi\\gi", Group(3))
                .Replace("\\gi\\gi", Group(2))
                .Replace("\\gi", Group(1))
                .Replace("\\gd\\gd\\gd", Index(3))
                .Replace("\\gd\\gd", Index(2))
                .Replace("\\gd", Index(1))
                .Replace("  ", " ")
                .Trim();
        }
    }
}