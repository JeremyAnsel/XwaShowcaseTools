using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.Xwa.Dat;
using JeremyAnsel.Xwa.HooksConfig;
using JeremyAnsel.Xwa.Opt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XwaSizeComparison
{
    class SceneData
    {
        public Dictionary<string, SceneTextureData> Textures { get; } = new(StringComparer.InvariantCultureIgnoreCase);

        public List<XMFloat3> Positions { get; } = new();

        public List<XMFloat3> Normals { get; } = new();

        public List<XMFloat2> TextureCoordinates { get; } = new();

        public List<XMUInt4> SolidMeshIndices { get; } = new();

        public List<XMUInt4> TransparentMeshIndices { get; } = new();

        public List<SceneMeshData> Meshes { get; } = new();

        public void AddOpt(SceneOpt sceneOpt)
        {
            if (!File.Exists(sceneOpt.OptFilename))
            {
                return;
            }

            OptFile opt = string.IsNullOrEmpty(sceneOpt.OptFilename) ? new OptFile() : OptFile.FromFile(sceneOpt.OptFilename);

            var objectProfiles = OptModel.GetObjectProfiles(opt.FileName);
            //var objectSkins = OptModel.GetSkins(opt.FileName);

            if (!objectProfiles.TryGetValue(sceneOpt.OptObjectProfile, out List<int> objectProfile))
            {
                objectProfile = objectProfiles["Default"];
            }

            opt = OptModel.GetTransformedOpt(opt, sceneOpt.OptVersion, objectProfile, sceneOpt.OptObjectSkins);

            opt.Scale(OptFile.ScaleFactor);

            sceneOpt.OptSize = opt.Size;
            sceneOpt.OptSpanSize = opt.SpanSize;

            Vector max = opt.MaxSize;
            Vector min = opt.MinSize;

            sceneOpt.OptMaxSize = max;
            sceneOpt.OptMinSize = min;

            sceneOpt.OptCenter = new Vector()
            {
                X = (max.X + min.X) / 2,
                Y = (max.Y + min.Y) / 2,
                Z = (max.Z + min.Z) / 2
            };

            opt.ConvertTextures8To32();

            this.CreateTextures(sceneOpt, opt);
            this.CreateMeshes(sceneOpt, opt);
        }

        private void CreateTextures(SceneOpt sceneOpt, OptFile opt)
        {
            string optName = Path.GetFileNameWithoutExtension(opt.FileName);
            string optDirectory = Path.GetDirectoryName(opt.FileName);
            string rootDirectory = Path.GetFullPath(Path.Combine(optDirectory, ".."));

            string materialsDirectory = Path.GetFullPath(Path.Combine(optDirectory, "..", "Materials"));
            string materialFilename = Path.Combine(materialsDirectory, optName + ".mat");

            foreach (var textureKey in opt.Textures)
            {
                string optTextureName = optName + "_" + textureKey.Key;

                if (this.Textures.ContainsKey(optTextureName))
                {
                    continue;
                }

                Texture optTexture = textureKey.Value;
                Texture optNormalMap = GetNormalMap(textureKey.Key, materialFilename, rootDirectory);

                if (optNormalMap is not null)
                {
                    if (optTexture.Width != optNormalMap.Width || optTexture.Height != optNormalMap.Height)
                    {
                        //throw new NotSupportedException($"The normal map dimensions are not the same as the base texture in {optName}.");
                        // todo
                        optNormalMap = null;
                    }
                }

                this.Textures.Add(optTextureName, new SceneTextureData
                {
                    Name = optTextureName,
                    Width = optTexture.Width,
                    Height = optTexture.Height,
                    Texture = optTexture,
                    NormalMap = optNormalMap
                });
            }
        }

        private Texture GetNormalMap(string textureKey, string materialFilename, string rootDirectory)
        {
            if (!File.Exists(materialFilename))
            {
                return null;
            }

            string textureSection = textureKey;
            int textureFgIndex = textureSection.IndexOf("_fg_");

            if (textureFgIndex != -1)
            {
                textureSection = textureSection[..textureFgIndex];
            }

            IList<string> materialLines = XwaHooksConfig.GetFileLines(materialFilename, textureSection);
            string normalMapEntry = XwaHooksConfig.GetFileKeyValue(materialLines, "NormalMap");
            string[] normalMapEntryParts = normalMapEntry.Split('-');

            if (normalMapEntryParts.Length != 3)
            {
                return null;
            }

            string normalMapDatFilename = Path.Combine(rootDirectory, normalMapEntryParts[0]);

            if (!File.Exists(normalMapDatFilename))
            {
                return null;
            }

            if (!int.TryParse(normalMapEntryParts[1], out int normalMapDatGroupId) || !int.TryParse(normalMapEntryParts[2], out int normalMapDatImageId))
            {
                return null;
            }

            DatFile normalMapDat = DatFile.FromFile(normalMapDatFilename);
            DatGroup normalMapDatGroup = normalMapDat.Groups.FirstOrDefault(t => t.GroupId == normalMapDatGroupId);

            if (normalMapDatGroup is null)
            {
                return null;
            }

            DatImage normalMapDatImage = normalMapDatGroup.Images.FirstOrDefault(t => t.GroupId == normalMapDatGroupId && t.ImageId == normalMapDatImageId);

            if (normalMapDatImage is null)
            {
                return null;
            }

            normalMapDatImage.ConvertToFormat25();
            normalMapDatImage.FlipUpsideDown();

            Texture texture = ConvertDatImageToTexture(normalMapDatImage);
            texture.GenerateMipmaps();

            return texture;
        }

        private static Texture ConvertDatImageToTexture(DatImage datImage)
        {
            int length = datImage.Width * datImage.Height;

            var texture = new Texture
            {
                Width = datImage.Width,
                Height = datImage.Height
            };

            byte[] bytes = datImage.GetImageData();

            bool hasAlpha = false;

            for (int i = 0; i < length; i++)
            {
                byte a = bytes[i * 4 + 3];

                if (a != (byte)255)
                {
                    hasAlpha = true;
                    break;
                }
            }

            texture.ImageData = bytes;

            if (hasAlpha)
            {
                texture.Palette[2] = 0xff;
            }

            return texture;
        }

        private void CreateMeshes(SceneOpt sceneOpt, OptFile opt)
        {
            string optName = Path.GetFileNameWithoutExtension(opt.FileName);

            XMFloat3 offsetTransform = new(0, -opt.MinSize.Z, -opt.SpanSize.Y / 2);
            XMMatrix worldTransform = sceneOpt.WorldTransform * XMMatrix.TranslationFromVector(offsetTransform);

            foreach (var optMesh in opt.Meshes)
            {
                uint positionsIndexStart = (uint)this.Positions.Count;
                uint normalsIndexStart = (uint)this.Normals.Count;
                uint textureCoordinatesIndexStart = (uint)this.TextureCoordinates.Count;

                for (int i = 0; i < optMesh.Vertices.Count; i++)
                {
                    var t = optMesh.Vertices[i];
                    XMFloat3 v = new(t.X, t.Z, -t.Y);
                    v = (XMFloat3)XMVector3.Transform(v, worldTransform);
                    this.Positions.Add(v);
                }

                for (int i = 0; i < optMesh.VertexNormals.Count; i++)
                {
                    var t = optMesh.VertexNormals[i];
                    XMFloat3 v = new(t.X, t.Z, -t.Y);
                    v = (XMFloat3)XMVector3.TransformNormal(v, worldTransform);
                    this.Normals.Add(v);
                }

                for (int i = 0; i < optMesh.TextureCoordinates.Count; i++)
                {
                    var t = optMesh.TextureCoordinates[i];
                    XMFloat2 v = new(t.U, -t.V);
                    this.TextureCoordinates.Add(v);
                }

                var optLod = optMesh.Lods.FirstOrDefault();

                if (optLod == null)
                {
                    continue;
                }

                foreach (var optFaceGroup in optLod.FaceGroups)
                {
                    string optTextureName = optFaceGroup.Textures[0];
                    string sceneMeshTexture = optName + "_" + optTextureName;

                    opt.Textures.TryGetValue(optTextureName, out Texture texture);
                    bool sceneMeshHasAlpha = texture is null ? false : texture.HasAlpha;

                    var meshData = new SceneMeshData(sceneMeshHasAlpha ? (uint)this.TransparentMeshIndices.Count : (uint)this.SolidMeshIndices.Count, 0, offsetTransform, sceneMeshTexture, sceneMeshHasAlpha);

                    XMUInt4 index = default;

                    if (sceneMeshHasAlpha)
                    {
                        foreach (var optFace in optFaceGroup.Faces)
                        {
                            Indices positionsIndex = optFace.VerticesIndex;
                            Indices normalsIndex = optFace.VertexNormalsIndex;
                            Indices textureCoordinatesIndex = optFace.TextureCoordinatesIndex;

                            index.X = positionsIndexStart + (uint)positionsIndex.C;
                            index.Y = normalsIndexStart + (uint)normalsIndex.C;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.C;
                            index.W = uint.MaxValue;
                            this.TransparentMeshIndices.Add(index);

                            index.X = positionsIndexStart + (uint)positionsIndex.B;
                            index.Y = normalsIndexStart + (uint)normalsIndex.B;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.B;
                            index.W = uint.MaxValue;
                            this.TransparentMeshIndices.Add(index);

                            index.X = positionsIndexStart + (uint)positionsIndex.A;
                            index.Y = normalsIndexStart + (uint)normalsIndex.A;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.A;
                            index.W = uint.MaxValue;
                            this.TransparentMeshIndices.Add(index);

                            if (positionsIndex.D >= 0)
                            {
                                index.X = positionsIndexStart + (uint)positionsIndex.C;
                                index.Y = normalsIndexStart + (uint)normalsIndex.C;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.C;
                                index.W = uint.MaxValue;
                                this.TransparentMeshIndices.Add(index);

                                index.X = positionsIndexStart + (uint)positionsIndex.A;
                                index.Y = normalsIndexStart + (uint)normalsIndex.A;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.A;
                                index.W = uint.MaxValue;
                                this.TransparentMeshIndices.Add(index);

                                index.X = positionsIndexStart + (uint)positionsIndex.D;
                                index.Y = normalsIndexStart + (uint)normalsIndex.D;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.D;
                                index.W = uint.MaxValue;
                                this.TransparentMeshIndices.Add(index);
                            }
                        }

                        meshData.IndexCount = (uint)this.TransparentMeshIndices.Count - meshData.IndexStart;
                    }
                    else
                    {
                        foreach (var optFace in optFaceGroup.Faces)
                        {
                            Indices positionsIndex = optFace.VerticesIndex;
                            Indices normalsIndex = optFace.VertexNormalsIndex;
                            Indices textureCoordinatesIndex = optFace.TextureCoordinatesIndex;

                            index.X = positionsIndexStart + (uint)positionsIndex.C;
                            index.Y = normalsIndexStart + (uint)normalsIndex.C;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.C;
                            index.W = uint.MaxValue;
                            this.SolidMeshIndices.Add(index);

                            index.X = positionsIndexStart + (uint)positionsIndex.B;
                            index.Y = normalsIndexStart + (uint)normalsIndex.B;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.B;
                            index.W = uint.MaxValue;
                            this.SolidMeshIndices.Add(index);

                            index.X = positionsIndexStart + (uint)positionsIndex.A;
                            index.Y = normalsIndexStart + (uint)normalsIndex.A;
                            index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.A;
                            index.W = uint.MaxValue;
                            this.SolidMeshIndices.Add(index);

                            if (positionsIndex.D >= 0)
                            {
                                index.X = positionsIndexStart + (uint)positionsIndex.C;
                                index.Y = normalsIndexStart + (uint)normalsIndex.C;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.C;
                                index.W = uint.MaxValue;
                                this.SolidMeshIndices.Add(index);

                                index.X = positionsIndexStart + (uint)positionsIndex.A;
                                index.Y = normalsIndexStart + (uint)normalsIndex.A;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.A;
                                index.W = uint.MaxValue;
                                this.SolidMeshIndices.Add(index);

                                index.X = positionsIndexStart + (uint)positionsIndex.D;
                                index.Y = normalsIndexStart + (uint)normalsIndex.D;
                                index.Z = textureCoordinatesIndexStart + (uint)textureCoordinatesIndex.D;
                                index.W = uint.MaxValue;
                                this.SolidMeshIndices.Add(index);
                            }
                        }

                        meshData.IndexCount = (uint)this.SolidMeshIndices.Count - meshData.IndexStart;
                    }

                    this.Meshes.Add(meshData);
                }
            }
        }
    }
}
