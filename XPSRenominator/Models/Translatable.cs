using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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


        public bool ApplyRegex(string pattern, string replacement, Dictionary<string, int> renameIndexes)
        {
            TranslatingName = null;

            var group = string.Join(',', Regex.Matches(TranslatedName, pattern).SelectMany(m => m.Groups.Values).Select(g => g.Value).Skip(1));
            renameIndexes.TryAdd(group, 1);
            try
            {
                if (!string.IsNullOrEmpty(pattern) && Regex.IsMatch(TranslatedName, pattern))
                {
                    TranslatingName = Regex.Replace(TranslatedName, pattern, replacement
                        .Replace("\\d\\d\\d", renameIndexes[group].ToString().PadLeft(3, '0'))
                        .Replace("\\d\\d", renameIndexes[group].ToString().PadLeft(2, '0'))
                        .Replace("\\d", renameIndexes[group].ToString().PadLeft(1, '0')));
                    renameIndexes[group]++;
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
