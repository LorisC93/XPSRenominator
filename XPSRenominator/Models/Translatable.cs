using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XPSRenominator.Models
{
    public abstract class Translatable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? translatingName = "";
        private string translatedName = "";

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

        public string OriginalName { get; set; } = "";

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
