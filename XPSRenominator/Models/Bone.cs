using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Media.Media3D;

namespace XPSRenominator.Models;

public class Bone : Translatable
{        
    public Bone? Parent { get; set; }
    public Point3D Position { get; set; }
    public Point3D Rotation { get; set; }
    public Point3D Scale { get; set; }
    public bool FromMeshAscii { get; set; }

    [MemberNotNullWhen(false, nameof(Parent))]
    public bool IsRoot => Parent == null;

    public IEnumerable<Bone> GetFullTree()
    {
        return IsRoot ? new[] { this } : Parent.GetFullTree().Append(this);
    }
    public bool IsMirrored(Bone bone)
    {
        var pairs = new List<(double, double)> { (bone.Position.X, Position.X), (bone.Position.Y, Position.Y), (bone.Position.Z, Position.Z) };
        return pairs.Count(pair => Math.Abs(pair.Item1 + pair.Item2) < 0.001) == 1 && pairs.Count(pair => Math.Abs(pair.Item1 - pair.Item2) < 0.001) == 2;
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
                .Replace("  ", " ")
                .Trim();
        }
    }
}