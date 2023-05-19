using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XPSRenominator.Models;

namespace XPSRenominator.Controllers
{
    internal class MeshAsciiLoader
    {
        //private List<string> originalLines = new();
        public List<Bone> Bones { get; private set; } = new();
        public List<Mesh> Meshes { get; private set; } = new();

        public void LoadAsciiFile(string fileName)
        {
            var originalLines = File.ReadLines(fileName).ToList();
            var mergingBones = LoadBones(originalLines);
            MergeBones(mergingBones);
            Meshes.AddRange(LoadMeshes(originalLines, mergingBones));
        }
        public void LoadPoseFile(string fileName)
        {
            Bones = LoadPose(File.ReadLines(fileName).ToList());
            Meshes.Clear();
        }

        private static List<Bone> LoadBones(IReadOnlyList<string> originalLines)
        {
            // bone number
            /* for each bone */
            //  bone name
            //  parent index
            //  position

            //Bones.Clear();
            //bool merge = Bones.Count > 0;
            List<Bone> mergingBones = new();

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
                //if (merge)
                //{
                    mergingBones.Add(bone);
                //    if (!Bones.Exists(b => b.TranslatedName == name))
                //        Bones.Add(bone);
                //}
                //else
                //    Bones.Add(bone);
            }
            foreach (Bone bone in mergingBones)
            {
                int parentIndex = parentIndexes[bone];
                bone.Parent = parentIndex == -1 ? null : mergingBones[parentIndex];
            }
            return mergingBones;
        }
        private void MergeBones(IList<Bone> list2) 
        {
            for (int i = 0; i < list2.Count; i++)
            {
                string name = list2.ElementAt(i).TranslatedName;
                if (!Bones.Exists(b => b.TranslatedName == name)) continue;

                list2.RemoveAt(i);
                list2.Insert(i, Bones.Find(b => b.TranslatedName == name)!);
            }

            list2.Except(Bones).ToList().ForEach(mergingBone =>
            {
                if (mergingBone.Parent != null)
                    mergingBone.Parent = Bones.Find(b => b.TranslatedName == mergingBone.Parent.TranslatedName);

                Bones.Add(mergingBone);
            });
        }

        private static List<Bone> LoadPose(IEnumerable<string> originalLines)
        {
            /* for each bone */
            //  bone name: rot_x rot_y rot_z pos_x pos_y pos_z scale_x scale_y scale_z

            return originalLines.Select(line =>
            {
                var parts = line.Split(':');
                var values = parts[1].ExtractDoubleArray();
                
                return new Bone
                {
                    OriginalName = parts[0].Clean(),
                    TranslatedName = parts[0].Clean(),
                    Rotation = values[..3],
                    Position = values[3..6],
                    Scale = values[6..9],
                    FromMeshAscii = true
                };
            }).ToList();
        }

        private static IEnumerable<Mesh> LoadMeshes(IReadOnlyList<string> originalLines, List<Bone> bonesToUse)
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

            //Meshes.Clear();
            //MaterialManager.Materials.Clear();

            var mergingMeshes = new List<Mesh>();

            int pointer = 1 + bonesToUse.Count * 3;

            int meshCount = int.Parse(originalLines.ElementAt(pointer).RemoveComment());
            pointer++;
            for (int i = 0; i < meshCount; i++)
            {
                Mesh mesh = new();
                (int renderGroup, string name, float[] parameters) = SplitFirstMeshLine(originalLines[pointer++]);
                mesh.OriginalName = name;
                mesh.TranslatedName = name;
                mesh.UvLayers = int.Parse(originalLines[pointer++].RemoveComment());
                var textureCount = int.Parse(originalLines[pointer++].RemoveComment());
                var textures = new List<Texture>();
                for (int j = 0; j < textureCount; j++)
                {
                    string textureName = originalLines[pointer++].Clean(true);
                    int uvLayer = int.Parse(originalLines[pointer++].RemoveComment());
                    textures.Add(new Texture { OriginalName = textureName, TranslatedName = textureName, UvLayer = uvLayer });
                }
                mesh.Material = MaterialManager.FindOrCreate(renderGroup, textures, parameters);
                int verticesCount = int.Parse(originalLines[pointer++].Split('#').First());
                for (int j = 0; j < verticesCount; j++)
                {
                    mesh.Vertices.Add(new Vertex
                    {
                        Position = originalLines[pointer++].ExtractDoubleArray(),
                        Normal = originalLines[pointer++].ExtractDoubleArray(),
                        Color = originalLines[pointer++].ExtractByteArray().ToColor(),
                        UV = originalLines[pointer++].ExtractDoubleArray(),
                        UV2 = mesh.UvLayers == 2 ? originalLines[pointer++].ExtractDoubleArray() : null,
                        Bones = Utils.CreateVertexBones(originalLines[pointer++].ExtractIntArray(), originalLines[pointer++].ExtractDoubleArray(), bonesToUse)
                    });
                }
                int faceCount = int.Parse(originalLines[pointer++].RemoveComment());
                for (int j = 0; j < faceCount; j++)
                    mesh.Faces.Add(new Face { Vertices = originalLines[pointer++].ExtractIntArray() });

                mergingMeshes.Add(mesh);
            }
            return mergingMeshes;
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
            bone.Position = new double[] { 0, 0, 0 };
            bone.TranslatedName = "root ground";
            bone.Parent = null;
            bone.FromMeshAscii = true;

            Bones.Remove(bone);

            Bones.Where(b => b.Parent == null).ToList().ForEach(b => b.Parent = bone);

            Bones.Insert(0, bone);
        }

        public void CloneMesh(Mesh mesh) => Meshes.Add((Mesh)mesh.Clone());
        public void DeleteMesh(Mesh mesh) => Meshes.Remove(mesh);

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

        private List<Bone> GetBoneConflicts(bool onlyFromMesh = true) => Bones
            .Where(b => !onlyFromMesh || b.FromMeshAscii)
            .GroupBy(b => b.TranslatedName)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Distinct()
            .ToList();

        private List<Mesh> GetMeshConflicts() => Meshes
            .GroupBy(b => b.TranslatedName)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Distinct()
            .ToList();

        public bool SaveAscii(string fileName, Action increaseProgress)
        {
            if (GetBoneConflicts().Count > 0 || GetMeshConflicts().Count > 0)
            {
                return false;
            }

            using StreamWriter file = new(fileName, false);
            file.WriteLine(Bones.Count(b => b.FromMeshAscii) + " # bones");
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

        public void SavePose(string fileName, Action increaseProgress)
        {
            using StreamWriter file = new(fileName, false);
            Bones.Where(b => b.FromMeshAscii).Select(b => b.TranslatedName + ": " +
                             string.Join(' ', b.Rotation) + " " +
                             string.Join(' ', b.Position) + " " +
                             string.Join(' ', b.Scale))
                .ToList().ForEach(line =>
                {
                    file.WriteLine(line);
                    increaseProgress();
                });
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
