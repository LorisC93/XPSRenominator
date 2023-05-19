namespace XPSRenominator.Models;

public class Bone : Translatable
{        
    public Bone? Parent { get; set; }
    public double[] Position { get; set; } = new double[3];
    public double[] Rotation { get; set; } = new double[3];
    public double[] Scale { get; set; } = new double[3];
    public bool FromMeshAscii { get; set; }

    public bool IsRoot => Parent == null;
}