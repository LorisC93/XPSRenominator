using System.Collections.Generic;
using System.Drawing;

namespace XPSRenominator.Models
{
    class Mesh : Translatable
    {
        public Material Material { get; set; } = new();

        public int UvLayers { get; set; } = 1;
        public List<Vertex> Vertices { get; set; } = new();
        public List<Face> Faces { get; set; } = new();
    }

    public class Material
    {
        public RenderGroup RenderGroup { get; set; } = RenderGroup.OnlyDiffuse;
        public float[] RenderParameters { get; set; } = new float[3] { 1, 0, 0 };
        public List<Texture> Textures { get; set; } = new();
    }

    public class Texture : Translatable
    {
        public int UvLayer { get; set; } = 0;

        public override bool Equals(object? obj) => obj is Texture t && TranslatedName == t.TranslatedName && UvLayer == t.UvLayer;
        public override int GetHashCode() => base.GetHashCode();
    }
    public class Vertex
    {
        public double[] Position { get; set; } = new double[3]; //XYZ
        public double[] Normal { get; set; } = new double[3]; //XYZ
        public Color Color { get; set; } = new(); //RGBA
        public double[] UV { get; set; } = new double[2]; //UV
        public double[]? UV2 { get; set; } //UV
        public List<VertexBone> Bones { get; set; } = new();
    }
    public class Face
    {
        public int[] Vertices { get; set; } = new int[3];
    }
    public class VertexBone
    {
        public Bone Bone { get; set; } = new();
        public double Weight { get; set; } = 0;
    }
}
