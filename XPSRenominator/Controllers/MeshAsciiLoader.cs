﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XPSRenominator.Models;

namespace XPSRenominator.Controllers
{
    class MeshAsciiLoader
    {
        private List<string> originalLines = new();
        public List<Bone> Bones { get; private set; } = new();
        public List<Mesh> Meshes { get; private set; } = new();

        public void LoadAsciiFile(string fileName)
        {
            originalLines = File.ReadLines(fileName).ToList();

            LoadBones();
            LoadMeshes();
        }

        private void LoadBones()
        {
            // bone number
            /* for each bone */
            //  bone name
            //  parent index
            //  position

            Bones.Clear();

            int boneCount = int.Parse(originalLines.First().Split('#').First());

            Dictionary<Bone, int> parentIndexes = new();
            for (int i = 1; i <= boneCount * 3; i += 3)
            {
                string name = originalLines[i].Clean();
                int parentIndex = int.Parse(originalLines[i + 1].Split('#').First());
                Bone bone = new()
                {
                    OriginalName = name,
                    TranslatedName = name,
                    Position = originalLines[i + 2].ExtractDoubleArray(),
                    FromMeshAscii = true
                };
                parentIndexes.Add(bone, parentIndex);
                Bones.Add(bone);
            }
            foreach (Bone bone in Bones)
            {
                int parentIndex = parentIndexes[bone];
                bone.Parent = parentIndex == -1 ? null : Bones[parentIndex];
            }
        }
        private void LoadMeshes()
        {
            // mesh number
            /* for each mesh */
            //  mesh type _ mesh name
            //  uv layers number // 1 or 2
            //  texture number
            /***** for each texture *****/
            //      texture name
            //      uv layer index
            //  vertices number
            /***** for each vertex *****/
            //      position - 3 numbers
            //      normal - 3 numbers
            //      color RGBA - 4 numbers
            //      UV - 2 numbers
            //      UV2 - 2 numbers (only if uv layers number == 2)
            //      bone indices - n numbers
            //      bone weights - n numbers
            //  faces number
            /***** for each face *****/
            //      3 numbers

            Meshes.Clear();
            MaterialManager.Materials.Clear();

            int pointer = 1 + Bones.Count * 3;

            int meshCount = int.Parse(originalLines.ElementAt(pointer).RemoveComment());
            pointer++;
            for (int i = 0; i < meshCount; i++)
            {
                Mesh mesh = new();
                (int renderGroup, string name, float[] parameters) = SplitFirstMeshLine(originalLines[pointer++]);
                mesh.OriginalName = name;
                mesh.TranslatedName = name;
                mesh.UvLayers = int.Parse(originalLines[pointer++].RemoveComment());
                int textureCount = int.Parse(originalLines[pointer++].RemoveComment());
                List<Texture> textures = new();
                for (int j = 0; j < textureCount; j++)
                {
                    string textureName = originalLines[pointer++].Clean(true);
                    int uvLayer = int.Parse(originalLines[pointer++].RemoveComment());
                    textures.Add(new() { OriginalName = textureName, TranslatedName = textureName, UvLayer = uvLayer });
                }
                mesh.Material = MaterialManager.FindOrCreate(renderGroup, textures, parameters);
                int verticesCount = int.Parse(originalLines[pointer++].Split('#').First());
                for (int j = 0; j < verticesCount; j++)
                {
                    mesh.Vertices.Add(new()
                    {
                        Position = originalLines[pointer++].ExtractDoubleArray(),
                        Normal = originalLines[pointer++].ExtractDoubleArray(),
                        Color = originalLines[pointer++].ExtractByteArray().ToColor(),
                        UV = originalLines[pointer++].ExtractDoubleArray(),
                        UV2 = mesh.UvLayers == 2 ? originalLines[pointer++].ExtractDoubleArray() : null,
                        Bones = Utils.CreateVertexBones(originalLines[pointer++].ExtractIntArray(), originalLines[pointer++].ExtractDoubleArray(), Bones)
                    });
                }
                int faceCount = int.Parse(originalLines[pointer++].RemoveComment());
                for (int j = 0; j < faceCount; j++)
                    mesh.Faces.Add(new() { Vertices = originalLines[pointer++].ExtractIntArray() });

                Meshes.Add(mesh);
            }
        }

        private static (int, string, float[]) SplitFirstMeshLine(string line)
        {
            string[] parts = line.Split('_');

            int renderGroupID = int.Parse(parts[0]);

            bool paramsPresent = parts.Length >= 5 && parts.TakeLast(3).All(p => float.TryParse(p, out float _));

            string name = "";
            if (parts.Length > 5 && paramsPresent)
            {
                name = string.Join('_', parts.Skip(1).SkipLast(3)).Clean();
            }
            else if (parts.Length > 2 && !paramsPresent)
            {
                name = string.Join('_', parts.Skip(1)).Clean();
            }
            else
            {
                name = parts[1].Clean();
            }

            float[] parameters = new float[3] { 1, 0, 0 };
            if (paramsPresent)
                parameters = parts.TakeLast(3).Select(v => float.Parse(v)).ToArray();

            return (renderGroupID, name, parameters);
        }

        public void AddBone(Bone? parent = null)
        {
            Bone toAdd = new()
            {
                OriginalName = "bone" + Bones.Count,
                TranslatedName = "bone" + Bones.Count,
                FromMeshAscii = true,
                Position = parent != null ? (double[])parent.Position.Clone() : new double[3] { 0, 0, 0 },
                Parent = parent
            };
            Bones.Add(toAdd);
        }

        public void MakeRoot(Bone bone)
        {
            bone.Position = new double[3] { 0, 0, 0 };
            bone.TranslatedName = "root ground";
            bone.Parent = null;

            Bones.Remove(bone);

            Bones.Where(b => b.Parent == null).ToList().ForEach(b => b.Parent = bone);

            Bones.Insert(0, bone);
        }


        public void CloneMesh(Mesh mesh)
        {
            Meshes.Add((Mesh)mesh.Clone());
        }
        public void DeleteMesh(Mesh mesh)
        {
            Meshes.Remove(mesh);
        }

        public void LoadBoneFile(string fileName, bool keepAll = true)
        {
            File.ReadLines(fileName).ToList().ForEach(boneLine =>
            {
                if (!boneLine.StartsWith("#") && !string.IsNullOrWhiteSpace(boneLine))
                {
                    string[] parts = boneLine.Split(';').Select(part => part.Trim()).ToArray();
                    if (parts.Length == 2)
                    {
                        string originalName = parts[0].Clean();
                        string translation = parts[1].Clean();
                        Bone? bone = Bones.Find(b => b.OriginalName == originalName);
                        if (bone != null)
                        {
                            bone.TranslatedName = translation;
                        }
                        else if (keepAll)
                        {
                            Bones.Add(new Bone()
                            {
                                OriginalName = originalName,
                                TranslatedName = translation,
                                FromMeshAscii = false
                            });
                        }
                    }
                }
            });
        }

        private List<Bone> GetBoneConficts(bool onlyFromMesh = true)
        {
            IEnumerable<IGrouping<string, Bone>> groups = Bones.Where(b => !onlyFromMesh || b.FromMeshAscii).GroupBy(b => b.TranslatedName);
            return groups.Where(g => g.Count() > 1).SelectMany(g => g).Distinct().ToList();
        }
        private List<Mesh> GetMeshConficts()
        {
            IEnumerable<IGrouping<string, Mesh>> groups = Meshes.GroupBy(b => b.TranslatedName);
            return groups.Where(g => g.Count() > 1).SelectMany(g => g).Distinct().ToList();
        }

        public bool SaveAscii(string fileName, Action increaseProgress)
        {
            if (GetBoneConficts().Count > 0 || GetMeshConficts().Count > 0)
            {
                return false;
            }

            using StreamWriter file = new(fileName, false);
            file.WriteLine(Bones.Where(b => b.FromMeshAscii).Count() + " # bones");
            Bones.Where(b => b.FromMeshAscii).ToList().ForEach(b =>
            {
                file.WriteLine(b.TranslatedName);
                file.WriteLine((b.Parent == null ? "-1" : Bones.Where(b => b.FromMeshAscii).ToList().IndexOf(b.Parent).ToString()) + " # parent index");
                file.WriteLine(string.Join(" ", b.Position));
                increaseProgress();
            });
            file.WriteLine(Meshes.Count + " # meshes");
            Meshes.ForEach(b =>
            {
                file.WriteLine(b.Material.RenderGroup.ID + "_" + b.TranslatedName + "_" + string.Join('_', b.Material.RenderParameters));
                file.WriteLine(b.UvLayers + " # uv layers");
                file.WriteLine(b.Material.RenderGroup.SupportedTextureTypes.Count + " # textures");
                for (int i = 0; i < b.Material.RenderGroup.SupportedTextureTypes.Count; i++)
                {
                    if (b.Material.Textures.Count > i && !string.IsNullOrEmpty(b.Material.Textures[i].TranslatedName))
                    {
                        file.WriteLine(b.Material.Textures[i].TranslatedName);
                        file.WriteLine(b.Material.Textures[i].UvLayer + " # uv layer index");
                    }
                    else
                    {
                        file.WriteLine("missing.png");
                        file.WriteLine("0 # uv layer index");
                    }
                }
                file.WriteLine(b.Vertices.Count + " # vertices");
                b.Vertices.ForEach(v =>
                {
                    file.WriteLine(string.Join(' ', v.Position));
                    file.WriteLine(string.Join(' ', v.Normal));
                    file.WriteLine(v.Color.R + " " + v.Color.G + " " + v.Color.B + " " + v.Color.A);
                    file.WriteLine(string.Join(' ', v.UV));
                    if (b.UvLayers == 2) file.WriteLine(string.Join(' ', v.UV2 ?? new double[2] { 0, 0 }));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => Bones.Where(b => b.FromMeshAscii).ToList().IndexOf(b.Bone))));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => b.Weight)));
                });
                file.WriteLine(b.Faces.Count + " # faces");
                b.Faces.ForEach(f =>
                {
                    file.WriteLine(string.Join(' ', f.Vertices));
                });
                increaseProgress();
            });
            return true;
        }

        public void SaveBones(string fileName, Action increaseProgress)
        {
            using StreamWriter file = new(fileName, false);
            Bones.Where(b => b.OriginalName != b.TranslatedName).Select(b => b.OriginalName + ";" + b.TranslatedName).ToList().ForEach(line =>
            {
                file.WriteLine(line);
                increaseProgress();
            });
        }
    }
}
