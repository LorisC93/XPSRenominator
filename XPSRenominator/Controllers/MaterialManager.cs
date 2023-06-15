using System.Collections.Generic;
using System.Linq;
using XPSRenominator.Models;

namespace XPSRenominator.Controllers;

public static class MaterialManager
{
    public static List<Material> Materials { get; } = new();

    public static Material FindOrCreate(int renderGroupId, List<Texture> textures, float[] parameters)
    {
        var found = Materials.Find(m => m.RenderGroup?.Id == renderGroupId && m.ActiveTextures.Values.SequenceEqual(textures) && m.RenderParameters.SequenceEqual(parameters));
        if (found != null)
            return found;

        var rg = RenderGroup.ById(renderGroupId)!;
        var newMat = new Material
        {
            Textures = rg.SupportedTextureTypes.Zip(textures).ToDictionary(x => x.First, x => x.Second),
            RenderParameters = parameters,
            AlphaEnabled = rg.Alpha,
        };
        Materials.Add(newMat);
        return newMat;
    }
}