using System.Windows;
using System.Windows.Data;

namespace XPSRenominator
{
    public static class Utils
    {
        public static string Clean(this string name)
        {
            return name.ToLower().Replace('_', ' ').Trim();
        }

        public static void Bind(this DependencyObject control, DependencyProperty property, object source, string field)
        {
            Binding binding = new Binding(field)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(control, property, binding);
        }
    }
}
