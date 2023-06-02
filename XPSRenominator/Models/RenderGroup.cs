using System.Collections.Generic;
using System.Linq;

namespace XPSRenominator.Models
{
    public class RenderGroup
    {
        public int Id { get; internal set; } = 5;
        public bool Alpha { get; internal set; } = false;
        public List<TextureType> SupportedTextureTypes { get; internal set; } = new();

        public override string ToString()
        {
            return Id + " - " + string.Join(", ", SupportedTextureTypes.Select(t => t.Code())) + (Alpha ? " with Transparency" : " without Transparency");
        }

        internal RenderGroup() { }

        public static RenderGroup OnlyDiffuse { get; } = new() { Id = 5, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse } };
        public static RenderGroup OnlyDiffuseAlpha { get; } = OnlyDiffuse.WithAlpha(7);
        public static RenderGroup DiffuseLightmap { get; } = new() { Id = 3, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap } };
        public static RenderGroup DiffuseLightmapAlpha { get; } = DiffuseLightmap.WithAlpha(9);
        public static RenderGroup DiffuseBump { get; } = new() { Id = 4, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump } };
        public static RenderGroup DiffuseBumpAlpha { get; } = DiffuseBump.WithAlpha(6);
        public static RenderGroup DiffuseLightmapBump { get; } = new() { Id = 2, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump } };
        public static RenderGroup DiffuseLightmapBumpAlpha { get; } = DiffuseLightmapBump.WithAlpha(8);
        public static RenderGroup DiffuseLightmapBumpMask { get; } = new()
        {
            Id = 1,
            SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2 }
        };
        public static RenderGroup DiffuseLightmapBumpMaskAlpha { get; } = DiffuseLightmapBumpMask.WithAlpha(20);
        public static RenderGroup DiffuseLightmapBumpMaskSpecular { get; } = new()
        {
            Id = 22,
            SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2, TextureType.Specular }
        };
        public static RenderGroup DiffuseLightmapBumpMaskSpecularAlpha { get; } = DiffuseLightmapBumpMaskSpecular.WithAlpha(23);
        public static RenderGroup DiffuseLightmapBumpSpecular { get; } = new() { Id = 24, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Specular } };
        public static RenderGroup DiffuseLightmapBumpSpecularAlpha { get; } = DiffuseLightmapBumpSpecular.WithAlpha(25);
        public static RenderGroup DiffuseBumpEnvironment { get; } = new() { Id = 26, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump, TextureType.Environment, TextureType.Mask } };
        public static RenderGroup DiffuseBumpEnvironmentAlpha { get; } = DiffuseBumpEnvironment.WithAlpha(27);
        public static RenderGroup DiffuseLightmapBumpMaskEnvironment { get; } = new()
        {
            Id = 28,
            SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2, TextureType.Environment }
        };
        public static RenderGroup DiffuseLightmapBumpMaskEnvironmentAlpha { get; } = DiffuseLightmapBumpMaskEnvironment.WithAlpha(29);
        public static RenderGroup DiffuseBumpEmission { get; } = new() { Id = 30, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump, TextureType.Emission } };
        public static RenderGroup DiffuseBumpEmissionAlpha { get; } = DiffuseBumpEmission.WithAlpha(31);
        public static RenderGroup DiffuseHighlights { get; } = new() { Id = 32, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse } };
        public static RenderGroup DiffuseHighlightsAlpha { get; } = DiffuseHighlights.WithAlpha(33);
        public static RenderGroup DiffuseBumpMiniEmission { get; } = new() { Id = 36, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump, TextureType.Emission } };
        public static RenderGroup DiffuseBumpMiniEmissionAlpha { get; } = DiffuseBumpMiniEmission.WithAlpha(37);
        public static RenderGroup DiffuseBumpSpecularEmission { get; } = new() { Id = 38, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump, TextureType.Specular, TextureType.Emission } };
        public static RenderGroup DiffuseBumpSpecularEmissionAlpha { get; } = DiffuseBumpSpecularEmission.WithAlpha(39);
        public static RenderGroup DiffuseBumpSpecular { get; } = new() { Id = 40, SupportedTextureTypes = new List<TextureType> { TextureType.Diffuse, TextureType.Bump, TextureType.Specular } };
        public static RenderGroup DiffuseBumpSpecularAlpha { get; } = DiffuseBumpSpecular.WithAlpha(41);

        public static List<RenderGroup> List { get; } = typeof(RenderGroup).GetProperties().Where(f => f.PropertyType == typeof(RenderGroup)).Select(f => (RenderGroup)f.GetValue(null)!).ToList();

        public static RenderGroup? ById(int id) => List.Find(rg => rg.Id == id);

        public static RenderGroup? ByTextures(IEnumerable<TextureType> textureTypes, bool alphaEnabled)
        {
            return List.Where(renderGroup => renderGroup.Alpha == alphaEnabled && textureTypes.All(tt => renderGroup.SupportedTextureTypes.Contains(tt))).MinBy(m => m.SupportedTextureTypes.Count);
        }
    }

    internal static class RenderGroupUtils
    {
        public static List<TextureType> TextureTypes = new()
        {
            TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2,
            TextureType.Specular, TextureType.Environment, TextureType.Emission
        };

        internal static RenderGroup WithAlpha(this RenderGroup rg, int id) => new() { Id = id, Alpha = true, SupportedTextureTypes = rg.SupportedTextureTypes };

        internal static string Code(this TextureType textureType)
        {
            return textureType switch
            {
                TextureType.Diffuse => "Diffuse",
                TextureType.Lightmap => "Lightmap",
                TextureType.Bump => "Bump",
                TextureType.Mask => "Mask",
                TextureType.MiniBump1 => "MiniBump1",
                TextureType.MiniBump2 => "MiniBump2",
                TextureType.Specular => "Specular",
                TextureType.Environment => "Environment",
                TextureType.Emission => "Emission",
                _ => ""
            };
        }
    }

    public enum TextureType
    {
        Diffuse, Lightmap, Bump, Mask, MiniBump1, MiniBump2, Specular, Environment, Emission
    }
}
