using System.Collections.Generic;
using System.Drawing;

namespace XPSRenominator
{
    class Mesh
    {
        public int RenderGroup { get; set; } = 6;
        public float[] RenderParameters { get; set; } = new float[3] { 1, 0, 0};
        public string Name { get; set; } = "";
        public int UvLayers { get; set; } = 1;
        public List<Texture> Textures { get; set; } = new();
        public List<Vertex> Vertices { get; set; } = new();
        public List<Face> Faces { get; set; } = new();
    }
    class Texture
    {
        public string Name { get; set; } = "";
        public int UvLayer { get; set; } = 0;
    }
    class Vertex
    {
        public double[] Position { get; set; } = new double[3]; //XYZ
        public double[] Normal { get; set; } = new double[3]; //XYZ
        public Color Color { get; set; } = new(); //RGBA
        public double[] UV { get; set; } = new double[2]; //UV
        public double[]? UV2 { get; set; } //UV
        public List<VertexBone> Bones { get; set; } = new();
    }
    class Face
    {
        public double[] Position { get; set; } = new double[3];
    }
    public class VertexBone
    {
        public Bone Bone { get; set; } = new();
        public double Weight { get; set; } = 0;
    }
}
