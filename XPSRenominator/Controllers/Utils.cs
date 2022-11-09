using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using XPSRenominator.Models;
using System.Windows.Media;

namespace XPSRenominator.Controllers
{
    public static class Utils
    {
        public static string Clean(this string name, bool _allowed = false)
        {
            string result = name.ToLower().Replace(':', ' ').Replace('|', ' ').Trim();
            if (!_allowed) result = result.Replace('_', ' ');
            return result;
        }

        public static void Bind(this DependencyObject control, DependencyProperty property, object source, string field)
        {
            Binding binding = new(field)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(control, property, binding);
        }

        public static Color ToColor(this IEnumerable<byte> RGBA)
        {
            return Color.FromArgb(RGBA.ElementAt(3), RGBA.ElementAt(0), RGBA.ElementAt(1), RGBA.ElementAt(2));
        }

        public static List<VertexBone> CreateVertexBones(int[] indexes, double[] weights, List<Bone> bones)
        {
            return indexes.Zip(weights).Select(couple => new VertexBone() { Bone = bones.ElementAt(couple.First), Weight = couple.Second }).ToList();
        }

        public static int[] ExtractIntArray(this string s)
        {
            return s.Trim().Split(' ').Select(v => int.Parse(v)).ToArray();
        }
        public static byte[] ExtractByteArray(this string s)
        {
            return s.Trim().Split(' ').Select(v => byte.Parse(v)).ToArray();
        }
        public static double[] ExtractDoubleArray(this string s)
        {
            return s.Trim().Split(' ').Select(v => double.Parse(v)).ToArray();
        }

        public static string RemoveComment(this string line)
        {
            return line.Split('#').First().Trim();
        }
    }
}
