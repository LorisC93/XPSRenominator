using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XPSRenominator.Models
{
    public class RenderGroup
    {
        public int ID { get; internal set; } = 5;
        public bool Alpha { get; internal set; } = false;
        public List<TextureType> SupportedTextureTypes { get; internal set; } = new();

        public override string ToString()
        {
            return ID + " - " + string.Join(", ", SupportedTextureTypes.Select(t => t.Code())) + (Alpha ? " with Transparency" : " without Transparency");
        }

        internal RenderGroup() { }

        public static RenderGroup OnlyDiffuse { get; } = new() { ID = 5, SupportedTextureTypes = new() { TextureType.Diffuse } };
        public static RenderGroup OnlyDiffuseAlpha { get; } = OnlyDiffuse.WithAlpha(7);
        public static RenderGroup DiffuseLightmap { get; } = new() { ID = 3, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap } };
        public static RenderGroup DiffuseLightmapAlpha { get; } = DiffuseLightmap.WithAlpha(9);
        public static RenderGroup DiffuseBump { get; } = new() { ID = 4, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump } };
        public static RenderGroup DiffuseBumpAlpha { get; } = DiffuseBump.WithAlpha(6);
        public static RenderGroup DiffuseLightmapBump { get; } = new() { ID = 2, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump } };
        public static RenderGroup DiffuseLightmapBumpAlpha { get; } = DiffuseLightmapBump.WithAlpha(8);
        public static RenderGroup DiffuseLightmapBumpMask { get; } = new()
        {
            ID = 1,
            SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2 }
        };
        public static RenderGroup DiffuseLightmapBumpMaskAlpha { get; } = DiffuseLightmapBumpMask.WithAlpha(20);
        public static RenderGroup DiffuseLightmapBumpMaskSpecular { get; } = new()
        {
            ID = 22,
            SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2, TextureType.Specular }
        };
        public static RenderGroup DiffuseLightmapBumpMaskSpecularAlpha { get; } = DiffuseLightmapBumpMaskSpecular.WithAlpha(23);
        public static RenderGroup DiffuseLightmapBumpSpecular { get; } = new() { ID = 24, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Specular } };
        public static RenderGroup DiffuseLightmapBumpSpecularAlpha { get; } = DiffuseLightmapBumpSpecular.WithAlpha(25);
        public static RenderGroup DiffuseBumpEnvironment { get; } = new() { ID = 26, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump, TextureType.Environment } };
        public static RenderGroup DiffuseBumpEnvironmentAlpha { get; } = DiffuseBumpEnvironment.WithAlpha(27);
        public static RenderGroup DiffuseLightmapBumpMaskEnvironment { get; } = new()
        {
            ID = 28,
            SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Lightmap, TextureType.Bump, TextureType.Mask, TextureType.MiniBump1, TextureType.MiniBump2, TextureType.Environment }
        };
        public static RenderGroup DiffuseLightmapBumpMaskEnvironmentAlpha { get; } = DiffuseLightmapBumpMaskEnvironment.WithAlpha(29);
        public static RenderGroup DiffuseBumpEmission { get; } = new() { ID = 30, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump, TextureType.Emission } };
        public static RenderGroup DiffuseBumpEmissionAlpha { get; } = DiffuseBumpEmission.WithAlpha(31);
        public static RenderGroup DiffuseHighlights { get; } = new() { ID = 32, SupportedTextureTypes = new() { TextureType.Diffuse } };
        public static RenderGroup DiffuseHighlightsAlpha { get; } = DiffuseHighlights.WithAlpha(33);
        public static RenderGroup DiffuseBumpMiniEmission { get; } = new() { ID = 36, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump, TextureType.Emission } };
        public static RenderGroup DiffuseBumpMiniEmissionAlpha { get; } = DiffuseBumpMiniEmission.WithAlpha(37);
        public static RenderGroup DiffuseBumpSpecularEmission { get; } = new() { ID = 38, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump, TextureType.Specular, TextureType.Emission } };
        public static RenderGroup DiffuseBumpSpecularEmissionAlpha { get; } = DiffuseBumpSpecularEmission.WithAlpha(39);
        public static RenderGroup DiffuseBumpSpecular { get; } = new() { ID = 40, SupportedTextureTypes = new() { TextureType.Diffuse, TextureType.Bump, TextureType.Specular } };
        public static RenderGroup DiffuseBumpSpecularAlpha { get; } = DiffuseBumpSpecular.WithAlpha(41);

        public static List<RenderGroup> List { get; } = typeof(RenderGroup).GetProperties().Where(f => f.PropertyType == typeof(RenderGroup)).Select(f => (RenderGroup)f.GetValue(null)!).ToList();

        public static RenderGroup? ByID(int id)
        {
            return List.Find(rg => rg.ID == id);
        }
    }

    static class RenderGroupUtils
    {
        internal static RenderGroup WithAlpha(this RenderGroup rg, int id)
        {
            return new RenderGroup() { ID = id, Alpha = true, SupportedTextureTypes = rg.SupportedTextureTypes };
        }
        internal static string Code(this TextureType textureType) 
        {
            switch (textureType)
            {
                case TextureType.Diffuse: return "Diffuse";
                case TextureType.Lightmap: return "Lightmap";
                case TextureType.Bump: return "Bump";
                case TextureType.Mask:return "Mask";
                case TextureType.MiniBump1: return "MiniBump1";
                case TextureType.MiniBump2: return "MiniBump2";
                case TextureType.Specular: return "Specular";
                case TextureType.Environment: return "Environment";
                case TextureType.Emission: return "Emission";
                default: return "";
            }
        }
    }

    public enum TextureType
    {
        Diffuse, Lightmap, Bump, Mask, MiniBump1, MiniBump2, Specular, Environment, Emission
    }
}
