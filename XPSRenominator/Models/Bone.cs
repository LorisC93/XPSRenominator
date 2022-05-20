using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XPSRenominator.Models
{
    public class Bone : INotifyPropertyChanged
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
        public Bone? Parent { get; set; }
        public double[] Position { get; set; } = new double[3];
        public bool FromMeshAscii { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
