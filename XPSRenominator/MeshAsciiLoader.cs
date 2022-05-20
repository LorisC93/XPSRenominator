﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XPSRenominator
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
            //bone name
            //parent index
            //position

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
            //  uv layers number
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
            //      UV2 - 2 numbers (only if uv layers number > 1)
            //      bone indices - n numbers
            //      bone weights - n numbers
            //  faces number
            /***** for each face *****/
            //      3 numbers

            Meshes.Clear();

            int pointer = 1 + Bones.Count * 3;

            int meshCount = int.Parse(originalLines.ElementAt(pointer).Split('#').First());
            pointer++;
            for (int i = 0; i < meshCount; i++)
            {
                Mesh mesh = new();
                string[] nameLine = originalLines[pointer].Split('_');
                mesh.RenderGroup = int.Parse(nameLine[0]);
                mesh.Name = nameLine[1];
                if (nameLine.Length == 5)
                    mesh.RenderParameters = nameLine.Skip(2).Select(v => float.Parse(v)).ToArray();
                pointer++;
                mesh.UvLayers = int.Parse(originalLines[pointer++].Split('#').First());
                int textureCount = int.Parse(originalLines[pointer++].Split('#').First());
                for (int j = 0; j < textureCount; j++)
                    mesh.Textures.Add(new() { Name = originalLines[pointer++], UvLayer = int.Parse(originalLines[pointer++].Split('#').First()) });
                int verticesCount = int.Parse(originalLines[pointer++].Split('#').First());
                for (int j = 0; j < verticesCount; j++)
                {
                    mesh.Vertices.Add(new()
                    {
                        Position = originalLines[pointer++].ExtractDoubleArray(),
                        Normal = originalLines[pointer++].ExtractDoubleArray(),
                        Color = originalLines[pointer++].ExtractIntArray().ToColor(),
                        UV = originalLines[pointer++].ExtractDoubleArray(),
                        UV2 = mesh.UvLayers == 2 ? originalLines[pointer++].ExtractDoubleArray() : null,
                        Bones = Utils.CreateVertexBones(originalLines[pointer++].ExtractIntArray(), originalLines[pointer++].ExtractDoubleArray(), Bones)
                    });
                }
                int faceCount = int.Parse(originalLines[pointer++].Split('#').First());
                for (int j = 0; j < faceCount; j++)
                    mesh.Faces.Add(new() { Position = originalLines[pointer++].ExtractDoubleArray() });

                Meshes.Add(mesh);
            }
        }

        public void AddBone(Bone? parent = null)
        {
            Bone toAdd = new()
            {
                OriginalName = "bone" + Bones.Count,
                TranslatedName = "bone" + Bones.Count,
                FromMeshAscii = false,
                Position = parent?.Position ?? new double[3] { 0, 0, 0 },
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

        private bool ConfictExists(out string[] conficting)
        {
            IEnumerable<IGrouping<string, Bone>> groups = Bones.Where(b => b.FromMeshAscii).GroupBy(b => b.TranslatedName);
            conficting = groups.Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            return conficting.Length > 0;
        }

        public bool SaveAscii(string fileName, out string[] conficting, Action increaseProgress)
        {
            if (ConfictExists(out conficting))
            {
                return false;
            }

            using StreamWriter file = new(fileName, false);
            file.WriteLine(Bones.Count + " # bones");
            Bones.ForEach(b =>
            {
                file.WriteLine(b.TranslatedName);
                file.WriteLine((b.Parent == null ? "-1" : Bones.IndexOf(b.Parent).ToString()) + " # parent index");
                file.WriteLine(string.Join(" ", b.Position));
                increaseProgress();
            });
            file.WriteLine(Meshes.Count + " # meshes");
            Meshes.ForEach(b =>
            {
                file.WriteLine(b.RenderGroup + "_" + b.Name + "_" + string.Join('_', b.RenderParameters));
                file.WriteLine(b.UvLayers + " # uv layers");
                file.WriteLine(b.Textures.Count + " # textures");
                b.Textures.ForEach(t =>
                {
                    file.WriteLine(t.Name);
                    file.WriteLine(t.UvLayer + " # uv layer index");
                });
                file.WriteLine(b.Vertices.Count + " # vertices");
                b.Vertices.ForEach(v =>
                {
                    file.WriteLine(string.Join(' ', v.Position));
                    file.WriteLine(string.Join(' ', v.Normal));
                    file.WriteLine(v.Color.R + " " + v.Color.G + " " + v.Color.B + " " + v.Color.A);
                    file.WriteLine(string.Join(' ', v.UV));
                    if (b.UvLayers == 2) file.WriteLine(string.Join(' ', v.UV2 ?? new double[2] { 0, 0 } ));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => Bones.IndexOf(b.Bone))));
                    file.WriteLine(string.Join(' ', v.Bones.Select(b => b.Weight)));
                });
                file.WriteLine(b.Faces.Count + " # faces");
                b.Faces.ForEach(f =>
                {
                    file.WriteLine(string.Join(' ', f.Position));
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