using System;
using System.Collections.Generic;
using System.Linq;
using XPSRenominator.Models;

namespace XPSRenominator.Controllers
{
    public static class MaterialManager
    {
        public static List<Material> Materials { get; } = new();

        public static Material FindOrCreate(int renderGroupID, List<Texture> textures, float[] parameters)
        {
            Material? found = Materials.Find(m => m.RenderGroup.ID == renderGroupID && m.Textures.SequenceEqual(textures) && m.RenderParameters.SequenceEqual(parameters));
            if (found != null)
                return found;
            else 
            {
                Material newMat = new()
                {
                    Textures = textures,
                    RenderGroup = RenderGroup.ByID(renderGroupID)!,
                    RenderParameters = parameters
                };
                Materials.Add(newMat);
                return newMat;
            }
        }
    }
}
