using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using XPSRenominator.Models;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace XPSRenominator.Controllers
{
    public static class Utils
    {
        public static string Clean(this string name, bool underscoreAllowed = false)
        {
            var result = name.ToLower().Replace(':', ' ').Replace('|', ' ').Trim();
            if (!underscoreAllowed) result = result.Replace('_', ' ');
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

        public static Color ToColor(this IEnumerable<byte> rgba)
        {
            var l = rgba.ToList();
            return Color.FromArgb(l.ElementAt(3), l.ElementAt(0), l.ElementAt(1), l.ElementAt(2));
        }

        public static List<VertexBone> CreateVertexBones(int[] indexes, double[] weights, List<Bone> bones)
        {
            return indexes.Zip(weights).Select(couple => new VertexBone{ Bone = bones.ElementAt(couple.First), Weight = couple.Second }).ToList();
        }

        public static int[] ExtractIntArray(this string s) => s.RemoveComment().Split(' ').Select(int.Parse).ToArray();
        public static byte[] ExtractByteArray(this string s) => s.RemoveComment().Split(' ').Select(byte.Parse).ToArray();
        public static double[] ExtractDoubleArray(this string s) => s.RemoveComment().Split(' ').Select(double.Parse).ToArray();
        public static Point3D ExtractPoint3D(this string s) => ExtractDoubleArray(s).ToPoint3D();
        public static Point3D ToPoint3D(this double[] values) => new(values[0], values[1], values[2]);

        public static string RemoveComment(this string line) => line.Split('#').First().Trim();
    }
}
