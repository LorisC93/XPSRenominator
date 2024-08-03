using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using XPSRenominator.Models;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Globalization;

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

        public static void Bind(this DependencyObject control, DependencyProperty property, object source, string field, BindingMode mode = BindingMode.TwoWay)
        {
            BindingOperations.SetBinding(control, property, new Binding(field)
            {
                Source = source,
                Mode = mode,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }

        public static Color ToColor(this IEnumerable<byte> rgba)
        {
            var l = rgba.ToList();
            return Color.FromArgb(l[3], l[0], l[1], l[2]);
        }

        public static List<VertexBone> CreateVertexBones(int[] indexes, double[] weights, List<Bone> bones)
        {
            return indexes.Zip(weights).Select(couple => new VertexBone{ Bone = bones[couple.First], Weight = couple.Second }).ToList();
        }

        public static int[] ExtractIntArray(this string s) => s.RemoveComment().Split(' ').Select(int.Parse).ToArray();
        public static byte[] ExtractByteArray(this string s) => s.RemoveComment().Split(' ').Select(byte.Parse).ToArray();
        public static double[] ExtractDoubleArray(this string s) => s.RemoveComment().Split(' ').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        public static Point3D ExtractPoint3D(this string s) => ExtractDoubleArray(s).ToPoint3D();
        public static Point3D ToPoint3D(this double[] values) => new(values[0], values[1], values[2]);

        public static string RemoveComment(this string line) => line.Split('#').First().Trim();
    }
}
