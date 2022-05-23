namespace XPSRenominator.Models
{
    public class Bone : Translatable
    {        
        public Bone? Parent { get; set; }
        public double[] Position { get; set; } = new double[3];
        public bool FromMeshAscii { get; set; }
    }
}
