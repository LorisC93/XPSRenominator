using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace XPSRenominator.Models
{
    public abstract class Translatable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _translatingName = "";
        private string _translatedName = "";

        public string TranslatedName
        {
            get => _translatedName; set
            {
                _translatedName = value;
                OnPropertyChanged();
            }
        }
        public string? TranslatingName
        {
            get => _translatingName; set
            {
                _translatingName = value;
                OnPropertyChanged();
            }
        }

        public string OriginalName { get; set; } = "";

        public bool ApplyRegex(string pattern, string replacement, int n)
        {
            TranslatingName = null;

            try
            {
                if (!string.IsNullOrEmpty(pattern) && Regex.IsMatch(TranslatedName, pattern))
                {
                    TranslatingName = Regex.Replace(TranslatedName, pattern, replacement.Replace("\\d", n.ToString().PadLeft(2, '0')));
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
