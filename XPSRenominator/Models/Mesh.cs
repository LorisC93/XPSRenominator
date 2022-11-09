using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace XPSRenominator.Models
{
    class Mesh : Translatable, ICloneable
    {
        public Material Material { get; set; } = new();

        public int UvLayers { get; set; } = 1;
        public List<Vertex> Vertices { get; set; } = new();
        public List<Face> Faces { get; set; } = new();

        public object Clone()
        {
            return new Mesh
            {
                OriginalName = this.OriginalName + "-clone",
                TranslatedName = this.TranslatedName + "-clone",
                Material = this.Material,
                UvLayers = this.UvLayers,
                Vertices = new(this.Vertices),
                Faces = new(this.Faces),

            };
        }
    }

    public class Material : INotifyPropertyChanged, ICloneable
    {
        private float[] renderParameters = new float[3] { 1, 0, 0 };
        private RenderGroup renderGroup = RenderGroup.OnlyDiffuse;

        public event PropertyChangedEventHandler? PropertyChanged;

        public RenderGroup RenderGroup
        {
            get => renderGroup; set
            {
                renderGroup = value;
                OnPropertyChanged();
            }
        }
        public float[] RenderParameters
        {
            get => renderParameters; set
            {
                renderParameters = value;
                OnPropertyChanged();
            }
        }
        public List<Texture> Textures { get; set; } = new();

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public object Clone()
        {
            return new Material
            {
                RenderGroup = this.RenderGroup,
                RenderParameters = this.RenderParameters,
                Textures = new (this.Textures)
            };
        }
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
