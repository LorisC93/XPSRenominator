using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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

        public bool ApplyRegex(string pattern, string replacement)
        {
            TranslatingName = null;

            try
            {
                if (Regex.IsMatch(TranslatedName, pattern))
                {
                    TranslatingName = Regex.Replace(TranslatedName, pattern, replacement);
                    return true;
                }
            }
            catch { }
            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
