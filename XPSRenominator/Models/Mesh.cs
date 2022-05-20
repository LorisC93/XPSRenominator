using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace XPSRenominator.Models
{
    class Mesh : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? translatingName = "";
        private string translatedName = "";
        private RenderGroup renderGroup = RenderGroup.OnlyDiffuse;
        private List<float> renderParameters = new() { 1, 0, 0 };

        public string TranslatedName
        {
            get => translatedName; set
            {
                translatedName = value;
                OnPropertyChanged();
            }
        }
        public string? TranslatingName
        {
            get => translatingName; set
            {
                translatingName = value;
                OnPropertyChanged();
            }
        }

        public RenderGroup RenderGroup
        {
            get => renderGroup; set
            {
                renderGroup = value;
                OnPropertyChanged();
            }
        }
        public List<float> RenderParameters
        {
            get => renderParameters; set
            {
                renderParameters = value;
                OnPropertyChanged();
            }
        }
        public string OriginalName { get; set; } = "";
        public int UvLayers { get; set; } = 1;
        public List<Texture> Textures { get; set; } = new();
        public List<Vertex> Vertices { get; set; } = new();
        public List<Face> Faces { get; set; } = new();

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SetFirstLine(string line)
        {
            string[] parts = line.Split('_');

            RenderGroup = RenderGroup.ByID(int.Parse(parts[0]))!;

            bool paramsPresent = parts.Length >= 5 && parts.TakeLast(3).All(p => float.TryParse(p, out float _));

            if (parts.Length > 5 && paramsPresent)
            {
                OriginalName = string.Join('_', parts.Skip(1).SkipLast(3)).Clean();
            }
            else if (parts.Length > 2 && !paramsPresent)
            {
                OriginalName = string.Join('_', parts.Skip(1)).Clean();
            }
            else
            {
                OriginalName = parts[1].Clean();
            }
            TranslatedName = OriginalName;

            if (paramsPresent)
                RenderParameters = parts.TakeLast(3).Select(v => float.Parse(v)).ToList();

        }
    }
    class Texture : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? translatingName = "";
        private string translatedName = "";

        public string OriginalName { get; set; } = "";
        public string TranslatedName
        {
            get => translatedName; set
            {
                translatedName = value;
                OnPropertyChanged();
            }
        }
        public string? TranslatingName
        {
            get => translatingName; set
            {
                translatingName = value;
                OnPropertyChanged();
            }
        }
        public int UvLayer { get; set; } = 0;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
