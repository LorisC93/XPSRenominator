using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace XPSRenominator.Models
{
    internal class Mesh : Translatable, ICloneable
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
                Vertices = this.Vertices.Select(v => (Vertex)v.Clone()).ToList(),
                Faces = new List<Face>(this.Faces),
            };
        }
    }

    public class Material : INotifyPropertyChanged, ICloneable
    {
        private float[] _renderParameters = { 1, 0, 0 };
        private bool _alphaEnabled = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public float[] RenderParameters
        {
            get => _renderParameters;
            set
            {
                _renderParameters = value;
                OnPropertyChanged();
            }
        }
        public bool AlphaEnabled
        {
            get => _alphaEnabled;
            set
            {
                _alphaEnabled = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<TextureType, Texture> Textures { get; set; } = new();

        public Dictionary<TextureType, Texture> ActiveTextures => Textures
            .Where(d => !string.IsNullOrWhiteSpace(d.Value.TranslatedName))
            .ToDictionary(x => x.Key, x => x.Value);

        public RenderGroup? RenderGroup => RenderGroup.ByTextures(ActiveTextures.Keys, AlphaEnabled);

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public object Clone()
        {
            return new Material
            {
                RenderParameters = RenderParameters,
                Textures = new Dictionary<TextureType, Texture>(Textures)
            };
        }
    }

    public class Texture : Translatable
    {
        public int UvLayer { get; set; } = 0;

        public override bool Equals(object? obj) => obj is Texture t && TranslatedName == t.TranslatedName && UvLayer == t.UvLayer;
        public override int GetHashCode() => TranslatedName.GetHashCode();
    }
    public class Vertex: ICloneable
    {
        public double[] Position { get; set; } = new double[3]; //XYZ
        public double[] Normal { get; set; } = new double[3]; //XYZ
        public Color Color { get; set; } = new(); //RGBA
        public double[] Uv { get; set; } = new double[2]; //UV
        public double[]? Uv2 { get; set; } //UV
        public List<VertexBone> Bones { get; set; } = new();

        public object Clone()
        {
            return new Vertex
            {
                Position = this.Position,
                Normal = this.Normal,
                Color = System.Windows.Media.Color.FromArgb(this.Color.A, this.Color.R, this.Color.G, this.Color.B),
                Uv = this.Uv,
                Uv2 = this.Uv2,
                Bones = this.Bones,
            };
        }
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
