using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
}