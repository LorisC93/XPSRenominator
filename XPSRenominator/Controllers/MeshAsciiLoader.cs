﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;
using XPSRenominator.Models;

namespace XPSRenominator.Controllers
{
    internal class MeshAsciiLoader
    {
        //private List<string> originalLines = new();
        public List<Bone> Bones { get; private set; } = new();
        public List<Mesh> Meshes { get; private set; } = new();

        public void LoadAsciiFile(string fileName, Bone? appendTo = null)
        {
            var originalLines = File.ReadLines(fileName).ToList();
            var newFileBones = LoadBones(originalLines, appendTo);
            MergeBones(newFileBones);
            Meshes.AddRange(LoadMeshes(originalLines, newFileBones, appendTo));
            FixBonesOrder();
            RemoveUnusedBones();
        }

        private void FixBonesOrder()
        {
            Bones.Sort((b1, b2) =>
            {
                if (b1.GetFullTree().Contains(b2)) return 1;
                if (b2.GetFullTree().Contains(b1)) return -1;
                return b1.GetFullTree().Count() - b2.GetFullTree().Count();
            });
        }

        private void RemoveUnusedBones()
        {
            Bones = Meshes.SelectMany(mesh => mesh.UsedBones).Distinct().ToList();
        }

        public void LoadPoseFile(string fileName)
        {
            Bones = LoadPose(File.ReadLines(fileName).ToList());
            Meshes.Clear();
        }

        private static List<Bone> LoadBones(IReadOnlyList<string> originalLines, Bone? appendTo = null)
        {
            // bone number
            /* for each bone */
            //  bone name
            //  parent index
            //  position
            
            var bones = new List<Bone>();
            var boneCount = int.Parse(originalLines[0].RemoveComment());

            var parentIndexes = new Dictionary<Bone, int>();
            for (var i = 1; i <= boneCount * 3; i += 3)
            {
                var name = originalLines[i].RemoveComment().Clean();

                var parentIndex = int.Parse(originalLines[i + 1].RemoveComment());
                var bone = new Bone
                {
                    OriginalName = name,
                    TranslatedName = name,
                    Position = originalLines[i + 2].ExtractPoint3D(),
                    FromMeshAscii = true
                };
                if (appendTo != null) bone.Position.Offset(appendTo.Position.X, appendTo.Position.Y, appendTo.Position.Z);

                parentIndexes.Add(bone, parentIndex);
                bones.Add(bone);
            }

            foreach (var bone in bones)
            {
                var parentIndex = parentIndexes[bone];
                bone.Parent = parentIndex == -1 ? appendTo : bones[parentIndex];
            }
            
            return bones;
        }
        private void MergeBones(List<Bone> list2) 
        {
            for (int i = 0; i < list2.Count; i++)
            {
                if (!Bones.Exists(b => b.TranslatedName == list2[i].TranslatedName)) continue;

                list2.RemoveAt(i);
                list2.Insert(i, Bones.Find(b => b.TranslatedName == list2[i].TranslatedName)!);
            }

            list2.Except(Bones).ToList().ForEach(mergingBone =>
            {
                if (mergingBone.Parent != null)
                    mergingBone.Parent = list2.Find(b => b.TranslatedName == mergingBone.Parent.TranslatedName);

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
                    Rotation = values[..3].ToPoint3D(),
                    Position = values[3..6].ToPoint3D(),
                    Scale = values[6..9].ToPoint3D(),
                    FromMeshAscii = true
                };
            }).ToList();
        }

        private List<Mesh> LoadMeshes(IReadOnlyList<string> originalLines, List<Bone> bonesToUse, Bone? appendTo = null)
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

            var meshes = new List<Mesh>();
            int pointer = 1 + bonesToUse.Count * 3;
            int meshCount = int.Parse(originalLines.ElementAt(pointer).RemoveComment());
            pointer++;
            for (int i = 0; i < meshCount; i++)
            {
                Mesh mesh = new();
                (int renderGroup, string name, float[] parameters) = SplitFirstMeshLine(originalLines[pointer++]);
                var uniqueName = name.Replace('_', '-');
                int n = 1;
                while (Meshes.Any(m => m.TranslatedName == uniqueName) || meshes.Any(m => m.TranslatedName == uniqueName)) uniqueName = $"{name}-{n++}";

                mesh.OriginalName = uniqueName;
                mesh.TranslatedName = uniqueName;
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
                    var hasUv = originalLines[pointer + 3].ExtractDoubleArray().Length == 2;
                    var vertex = new Vertex
                    {
                        Position = originalLines[pointer++].ExtractPoint3D(),
                        Normal = originalLines[pointer++].ExtractPoint3D(),
                        Color = originalLines[pointer++].ExtractByteArray().ToColor(),
                        Uv = hasUv ? originalLines[pointer++].ExtractDoubleArray() : new []{ 0.0, 0.0 },
                        Uv2 = mesh.UvLayers == 2 ? originalLines[pointer++].ExtractDoubleArray() : null,
                        Bones = Utils.CreateVertexBones(originalLines[pointer++].ExtractIntArray(), originalLines[pointer++].ExtractDoubleArray(), bonesToUse)
                    };
                    if (appendTo != null) vertex.Position.Offset(appendTo.Position.X, appendTo.Position.Y, appendTo.Position.Z);
                    mesh.Vertices.Add(vertex);
                }
                int faceCount = int.Parse(originalLines[pointer++].RemoveComment());
                for (int j = 0; j < faceCount; j++)
                    mesh.Faces.Add(new Face { Vertices = originalLines[pointer++].ExtractIntArray() });

                meshes.Add(mesh);
            }
            return meshes;
        }

        private static (int, string, float[]) SplitFirstMeshLine(string line)
        {
            string[] parts = line.Split('_');

            int renderGroupId = int.Parse(parts[0]);

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

            return (renderGroupId, name, parameters);
        }

        public void AddBone(Bone? parent = null)
        {
            Bone toAdd = new()
            {
                OriginalName = "bone" + Bones.Count,
                TranslatedName = "bone" + Bones.Count,
                FromMeshAscii = true,
                Position = parent != null ? new Point3D(parent.Position.X, parent.Position.Y, parent.Position.Z) : new Point3D(),
                Parent = parent
            };
            Bones.Add(toAdd);
        }

        public void MakeRoot(Bone bone)
        {
            bone.Position = new Point3D();
            bone.TranslatedName = "root ground";
            bone.Parent = null;
            bone.FromMeshAscii = true;

            Bones.Remove(bone);

            Bones.Where(b => b.Parent == null && b.FromMeshAscii).ToList().ForEach(b => b.Parent = bone);

            Bones.Insert(0, bone);
        }

        public void CloneMesh(Mesh mesh) => Meshes.Add((Mesh)mesh.Clone());
        public void DeleteMesh(Mesh mesh)
        {
            Meshes.Remove(mesh);
            if (Meshes.All(m => m.Material != mesh.Material))
                MaterialManager.Materials.Remove(mesh.Material);
        }

        public void LoadBoneFile(string fileName, bool keepAll, Action<int> setProgressMax, Action increaseProgress)
        {
            var lines = File.ReadLines(fileName).ToList();
            setProgressMax(lines.Count);
            lines.ForEach(boneLine =>
            {
                if (!boneLine.StartsWith("#") && !string.IsNullOrWhiteSpace(boneLine))
                {
                    var parts = boneLine.Split(';').Select(part => part.Trim()).ToArray();
                    if (parts.Length == 2)
                    {
                        var originalName = parts[0].Clean();
                        var translation = parts[1].Clean();
                        var bone = Bones.Find(b => b.OriginalName == originalName);
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

                increaseProgress();
            });
        }

        private IEnumerable<Bone> GetBoneConflicts(IEnumerable<Bone>? bones = null) => (bones ?? Bones)
            .Where(b => b.FromMeshAscii)
            .GroupBy(b => b.TranslatedName)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Distinct();

        private IEnumerable<Mesh> GetMeshConflicts(IEnumerable<Mesh>? meshes = null) => (meshes ?? Meshes)
            .GroupBy(b => b.TranslatedName)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Distinct();

        public bool SaveAscii(string fileName, Action increaseProgress) => ExportMeshes(Meshes.Where(m => !m.Exclude).ToList(), fileName, increaseProgress);

        public bool ExportMeshes(List<Mesh> meshes, string fileName, Action increaseProgress)
        {
            var usedBones = meshes.SelectMany(mesh => mesh.UsedBones).Distinct().ToList();

            if (GetBoneConflicts(usedBones).Any() || GetMeshConflicts(meshes).Any() || meshes.Any(m => m.Material.RenderGroup == null))
            {
                return false;
            }

            using StreamWriter file = new(fileName, false);
            file.WriteLine(usedBones.Count + " # bones");
            usedBones.ForEach(bone =>
            {
                file.WriteLine(bone.TranslatedName);
                file.WriteLine((bone.Parent == null ? "-1" : usedBones.IndexOf(bone.Parent).ToString()) + " # parent index");
                file.WriteLine($"{bone.Position.X.ToString(CultureInfo.InvariantCulture)} {bone.Position.Y.ToString(CultureInfo.InvariantCulture)} {bone.Position.Z.ToString(CultureInfo.InvariantCulture)}");
                increaseProgress();
            });
            file.WriteLine(meshes.Count + " # meshes");
            meshes.ForEach(mesh =>
            {
                file.WriteLine(mesh.Material.RenderGroup!.Id + "_" + mesh.TranslatedName + "_" + string.Join('_', mesh.Material.RenderParameters));
                file.WriteLine(mesh.UvLayers + " # uv layers");
                file.WriteLine(mesh.Material.RenderGroup.SupportedTextureTypes.Count + " # textures");
                foreach (var textureType in mesh.Material.RenderGroup.SupportedTextureTypes)
                {
                    if (mesh.Material.ActiveTextures.TryGetValue(textureType, out var texture))
                    {
                        file.WriteLine(texture.TranslatedName);
                        file.WriteLine(texture.UvLayer + " # uv layer index");
                    }
                    else
                    {
                        file.WriteLine($"{mesh.TranslatedName}-{textureType}.png");
                        file.WriteLine("0 # uv layer index");
                    }
                }
                file.WriteLine(mesh.Vertices.Count + " # vertices");
                mesh.Vertices.ForEach(v =>
                {
                    file.WriteLine($"{v.Position.X.ToString(CultureInfo.InvariantCulture)} {v.Position.Y.ToString(CultureInfo.InvariantCulture)} {v.Position.Z.ToString(CultureInfo.InvariantCulture)}");
                    file.WriteLine($"{v.Normal.X.ToString(CultureInfo.InvariantCulture)} {v.Normal.Y.ToString(CultureInfo.InvariantCulture)} {v.Normal.Z.ToString(CultureInfo.InvariantCulture)}");
                    file.WriteLine($"{v.Color.R} {v.Color.G} {v.Color.B} {v.Color.A}");
                    file.WriteLine(string.Join(' ', v.Uv.Select(value => value.ToString(CultureInfo.InvariantCulture))));
                    if (mesh.UvLayers == 2) file.WriteLine(string.Join(' ', (v.Uv2 ?? [0, 0]).Select(value => value.ToString(CultureInfo.InvariantCulture))));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => usedBones.IndexOf(b.Bone))));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => b.Weight.ToString(CultureInfo.InvariantCulture))));
                });
                file.WriteLine(mesh.Faces.Count + " # faces");
                mesh.Faces.ForEach(f => file.WriteLine(string.Join(' ', f.Vertices)));
                increaseProgress();
            });
            return true;
        }

        public void SavePose(string fileName, Action increaseProgress)
        {
            using StreamWriter file = new(fileName, false);
            Bones.Where(b => b.FromMeshAscii).Select(b =>
                    $"{b.TranslatedName}: {b.Rotation.X} {b.Rotation.Y} {b.Rotation.Z} {b.Position.X} {b.Position.Y} {b.Position.Z} {b.Scale.X} {b.Scale.Y} {b.Scale.Z}")
                .ToList().ForEach(line =>
                {
                    file.WriteLine(line);
                    increaseProgress();
                });
        }

        public void SaveBonedict(string fileName, Action increaseProgress)
        {
            using StreamWriter file = new(fileName, false);
            Bones.Where(b => b.OriginalName != b.TranslatedName).Select(b => b.OriginalName + ";" + b.TranslatedName).ToList().ForEach(line =>
            {
                file.WriteLine(line);
                increaseProgress();
            });
        }

        public Dictionary<Bone, List<int>> FindBoneGroups(string pattern, Func<Bone, bool> isValid)
        {
            var indexes = new Dictionary<Bone, List<int>>();
            try
            {
                var validBones = Bones.Where(bone => isValid(bone) && Regex.IsMatch(bone.TranslatedName, pattern)).ToList();
                foreach (var bone in validBones)
                {
                    if (bone.Parent == null || !validBones.Contains(bone.Parent))
                    {
                        var index = new List<int> { indexes.Any() ? indexes.Values.Select(i => i.First()).Max() + 1 : 1 };
                        if (validBones.Count(b => b.Parent == bone) == 1)
                        {
                            index.Add(1);
                        }

                        indexes.TryAdd(bone, index);
                    }
                    else if (bone.Parent != null && validBones.Exists(b => b != bone && b.Parent == bone.Parent))
                    {
                        var index = new List<int>(indexes[bone.Parent]) { validBones.Where(b => b.Parent == bone.Parent).ToList().IndexOf(bone) + 1 };
                        if (validBones.Count(b => b.Parent == bone) == 1)
                        {
                            index.Add(1);
                        }

                        indexes.TryAdd(bone, index);
                    }
                    else if (bone.Parent != null && !validBones.Exists(b => b != bone && b.Parent == bone.Parent))
                    {
                        var index = new List<int>(indexes[bone.Parent]);
                        index[^1]++;
                        indexes.TryAdd(bone, index);
                    }
                }
            }
            catch
            {
                // ignored
            }

            return indexes;
        }

        public void MirrorBoneTranslation(Bone source)
        {
            var leftToRight = source.TranslatedName.Contains("left");
            var target = Bones.Find(bone => bone.IsMirrored(source));
            if (target == null) return;
            target.TranslatedName = source.TranslatedName.Replace(leftToRight ? "left" : "right", leftToRight ? "right" : "left");
            foreach (var child in Bones.Where(bone => bone.Parent == source)) MirrorBoneTranslation(child);

        }
    }
}
