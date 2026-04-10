using System.Numerics;
using System.Text.RegularExpressions;
using FLVERMaterialHelper.MatShaderInfoBank;
using FLVERMaterialHelper.Shaders;
using SoulsFormats;

namespace DS3PortingTool.Util;

public static class FlverUtils
{ 
    /// <summary>
    /// Takes a non-native DS3 material and returns a new material with as close a mtd type as possible.
    /// </summary>
    public static FLVER2.Material ToDummyDs3Material(this FLVER2.Material oldMat, TextureInfo textureInfo, MatShaderInfoBank infoBank) 
    {
        FLVER2.Material newMat = new()
        {
			Name = oldMat.Name,
            MTD = ConversionContext.SourceBndsType == ConversionContext.AssetType.Character ? GetDs3Mtd_cARSN(oldMat.MTD) : GetDs3Mtd_mARSN(oldMat.MTD)
        };
        
        MatShaderInfoBank.MaterialInfo matDef = infoBank.MaterialInformation
            .First(y => y.MatName.Equals($"{Path.GetFileName(newMat.MTD)}".ToLower()));

        newMat.Textures = matDef.TextureChannels.Select(texChannel =>
        {
            FLVER2.Texture tex = new() { ParamName = texChannel.ParamName, Path = GetDummyTexPath(texChannel.ParamName) };
            return tex;
        }).ToList();

        return newMat;
    }
    
    /// <summary>
    /// Takes a non-native DS3 material and turns it into a DS3 material with original textures.
    /// </summary>
    public static FLVER2.Material ToDs3Material(this FLVER2.Material oldMat, TextureInfo texInfo, FLVER2 oldFlver, MatShaderInfoBank infoBank,
        Dictionary<string,MATBIN> matbins)
    {
        FLVER2.Material newMat = new()
	    {
		    Name = oldMat.Name
	    };
        
        string mtdName = GetBestFitMTD(oldMat, oldFlver, infoBank, texInfo);
        if (mtdName.Length > 0)
        {
            newMat.MTD = ConversionContext.SourceBndsType == ConversionContext.AssetType.Character
                ? newMat.MTD = $"N:\\FDP\\data\\Material\\mtd\\character\\{mtdName}"
                : newMat.MTD = $"N:\\FDP\\data\\Material\\mtd\\map\\{mtdName}";
            
            MatShaderInfoBank.MaterialInfo matDef = infoBank.MaterialInformation
            .First(y => y.MatName.Equals($"{Path.GetFileName(newMat.MTD.ToLower())}"));
            
            bool isMetallicPBR = false;
            for (int i = 0; i < matDef.TextureChannels.Count; i++)
            {
                string texType = matDef.TextureChannels.ElementAt(i).ParamName;
                FLVER2.Texture tex = new() { ParamName = texType };
                Dictionary<string,FLVER2.Texture>? associatedTextures = texInfo.GetTextureType( 
                    tex.ParamName, out bool newIsMetallicPBR);
                if (associatedTextures != null && associatedTextures.Count > 0)
                {
                    if (newIsMetallicPBR && !isMetallicPBR) isMetallicPBR = newIsMetallicPBR;
                    KeyValuePair<string, FLVER2.Texture> texKvp = associatedTextures.First();
                    associatedTextures.Remove(texKvp.Key);
                    tex.TilingScale = texKvp.Value.TilingScale;
                    tex.TilingTypeU = texKvp.Value.TilingTypeU;
                    tex.TilingTypeV = texKvp.Value.TilingTypeV;
                    string texName = Path.GetFileName(texKvp.Value.Path).Replace("_m.tif", "_r.tif");
                    //if (texName.Length == 0 && texInfo.Material != null)
                    //{
                    //    texName = Path.GetFileName(texInfo.Material.Samplers.First(x => x.Type == texKvp.Key).Path);
                    //}
                    switch (ConversionContext.SourceBndsType)
                    {
                        case ConversionContext.AssetType.Character:
                            tex.Path = $"N:\\FDP\\data\\Model\\chr\\c{ConversionContext.PortedId}\tex\\" + texName;
                            break;
                        case ConversionContext.AssetType.Object:
                            tex.Path = $"N:\\FDP\\data\\Model\\obj\\o{ConversionContext.PortedId[..2]}\\o{ConversionContext.PortedId}\\tex\\" + texName;
                            break;
                        case ConversionContext.AssetType.MapPiece:
                            tex.Path = $"N:\\FDP\\data\\Model\\map\\m{ConversionContext.PortedId[..2]}\\tex\\" + texName;
                            break;
                    }
                    
                }
                else if (associatedTextures != null)
                {
                    tex.Path = GetDummyTexPath(associatedTextures, texInfo);
                }
                //FLVER2.Texture? matchingTex = oldMat.Textures.FirstOrDefault(x => x.ParamName.Equals(texType));
                //if (matchingTex == null) continue;
                //Match texContainer = Regex.Match(matchingTex.Path, "c[0-9]{4}", RegexOptions.IgnoreCase);
                //if (!texContainer.Success) continue;
                //tex.Path = $"N:\\FDP\\data\\Model\\chr\\c{Options.PortedId}\\tex\\{Path.GetFileName(matchingTex.Path)}";
                newMat.Textures.Add(tex);
            }
        }

        return newMat;
    }
    
    /// <summary>
    /// Get the path to a dummy texture based on the texture type.
    /// </summary>
    private static string GetDummyTexPath(Dictionary<string,FLVER2.Texture> textureGroup, TextureInfo textureInfo)
    {
        if (textureGroup == textureInfo.AlbedoTextures)
        {	
            return "";
        }
        
        if (textureGroup == textureInfo.SpecularTextures)
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummySpecular.tga";
        }
        
        if (textureGroup == textureInfo.ShininessTextures)
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyShininess.tga";
        }
        
        if (textureGroup == textureInfo.NormalTextures || textureGroup == textureInfo.DetailNormalTextures)
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyNormal.tga";
        }
        
        if (textureGroup == textureInfo.ScatteringMaskTextures)
        {
            return @"N:\FDP\data\Other\SysTex\SYSTEX_DummyScatteringMask.tga";
        }
        
        if (textureGroup == textureInfo.EmissiveTextures)
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyEmissive.tga";
        }
        
        if (textureGroup == textureInfo.BloodMaskTextures || textureGroup == textureInfo.DisplacementTextures)
        {
            return @"N:\LiveTokyo\data\model\common\tex\dummy128.tga";
        }
        
        return "";
    }

    /// <summary>
    /// Get the path to a dummy texture based on the texture type.
    /// </summary>
    private static string GetDummyTexPath(string texName)
    {
        if (texName.Contains("Diffuse"))
        {	
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_BLACK.tga";
        }
        
        if (texName.Contains("Specular"))
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummySpecular.tga";
        }
        
        if (texName.Contains("Shininess"))
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyShininess.tga";
        }
        
        if (texName.Contains("Bumpmap"))
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyNormal.tga";
        }
        
        if (texName.Contains("ScatteringMask"))
        {
            return @"N:\FDP\data\Other\SysTex\SYSTEX_DummyScatteringMask.tga";
        }
        
        if (texName.Contains("Emissive"))
        {
            return @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyEmissive.tga";
        }
        
        if (texName.Contains("BloodMask") || texName.Contains("Displacement"))
        {
            return @"N:\LiveTokyo\data\model\common\tex\dummy128.tga";
        }
        
        return "";
    }

    private static string GetBestFitMTD(FLVER2.Material oldMat, FLVER2 oldFlver, MatShaderInfoBank infoBank, TextureInfo texInfo)
    {
        int minTangentCount = 0;
        bool hasBitangent = false;
        int minColorCount = 0;
        bool hasJoints = false;
        int minUVCount = 0;
        FLVER2.Mesh? matMesh = oldFlver.Meshes.FirstOrDefault(x => x.MaterialIndex == oldMat.Index);
        if (matMesh != null)
        {
            foreach (FLVER2.VertexBuffer vb in matMesh.VertexBuffers)
            {
                foreach (FLVER.LayoutMember member in oldFlver.BufferLayouts[vb.LayoutIndex])
                {
                    switch (member.Semantic)
                    {
                        case FLVER.LayoutSemantic.Tangent:
                            minTangentCount++;
                            break;
                        case FLVER.LayoutSemantic.Bitangent:
                            hasBitangent = true;
                            break;
                        case FLVER.LayoutSemantic.VertexColor:
                            minColorCount++;
                            break;
                        case FLVER.LayoutSemantic.BoneWeights:
                        case FLVER.LayoutSemantic.BoneIndices:
                            hasJoints = true;
                            break;
                        case FLVER.LayoutSemantic.UV:
                            minUVCount++;
                            break;
                    }
                }
            }
        }

        //FLVER2.GXList? gxList;
        //if (oldMat.GXIndex >= 0 && oldFlver.GXLists.Count > oldMat.GXIndex)
        //{
        //    gxList = oldFlver.GXLists[oldMat.GXIndex];
        //}
        
        if (texInfo.Material == null)
        {
            Console.WriteLine();
            return "";
        }

        MatShaderInfoBank.MaterialInfo? bestFitMat = null;

        List<MatShaderInfoBank.MaterialInfo> narrowedMatInfos;
        if (ConversionContext.SourceBndsType == ConversionContext.AssetType.Character)
        {
            narrowedMatInfos = infoBank.MaterialInformation.Where(x => x.MatName.StartsWith("c", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else
        {
            narrowedMatInfos = infoBank.MaterialInformation.Where(x => x.MatName.StartsWith("m", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        narrowedMatInfos = narrowedMatInfos.Where(texInfo.CheckIfMatDefIsValid).ToList();
        narrowedMatInfos = narrowedMatInfos.Where(x => IsShaderBufferValid(x, infoBank, minTangentCount,
            minColorCount, minUVCount, hasBitangent, hasJoints)).ToList();
        if (narrowedMatInfos.Any())
        {
            bestFitMat = narrowedMatInfos.First();
        }
        
        return bestFitMat != null ? bestFitMat.MatName : "";
    }

    private static bool IsShaderBufferValid(MatShaderInfoBank.MaterialInfo matInfo, MatShaderInfoBank infoBank,
        int minTangentCount, int minColorCount, int minUVCount, bool hasBitangent, bool hasJoints)
    {
        MatShaderInfoBank.ShaderInfo? shaderInfo =
            infoBank.ShaderInformation.FirstOrDefault(x => x.SpxName == matInfo.SpxName);
        if (shaderInfo == null) return false;

        int shaderTangentCount = 0;
        int shaderColorCount = 0;
        int shaderUVCount = 0;
        bool shaderHasBitangent = false;
        bool shaderHasJoints = false;
        
        foreach (MatShaderInfoBank.ShaderInfo.LayoutMember member in shaderInfo.VertexBufferLayout)
        {
            switch (member.Semantic)
            {
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.TANGENT:
                    shaderTangentCount++;
                    break;
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BINORMAL:
                    shaderHasBitangent = true;
                    break;
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.COLOR:
                    shaderColorCount++;
                    break;
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BLENDWEIGHT:
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.BLENDINDICES:
                    shaderHasJoints = true;
                    break;
                case MatShaderInfoBank.ShaderInfo.LayoutSemanticType.TEXCOORD:
                    shaderUVCount++;
                    break;
            }
        }
        
        if (shaderTangentCount < minTangentCount) return false;
        if (shaderColorCount < minColorCount) return false;
        //if (shaderUVCount < minUVCount) return false;
        //if (shaderHasBitangent != hasBitangent) return false;
        //if (shaderHasJoints != hasJoints) return false;
        
        return true;
    }
    
    /// <summary>
    /// Gets the closest DS3 equivalent of a c[arsn] mtd name that's not native to DS3.
    /// </summary>
    private static string GetDs3Mtd_cARSN(string mtd)
    {
        Dictionary<string, string> extensionTypes = new()
        {
	        {"add", "_Add"},
	        {"sss", "_SSS"},
	        {"em", "_em"},
	        {"e", "_e"},
	        {"glow", "_em_e_Glow"},
            {"decal", "_Decal"},
	        {"cloth", "_Cloth"}
        };
        
        List<string> extensions = mtd.ToLower().Split('_', '.').Where(extensionTypes.ContainsKey)
	        .Select(x => extensionTypes[x]).ToList();

        if ((extensions.Contains("_e") || extensions.Contains("_em")) && extensions.Contains("_em_e_Glow"))
        {
            extensions.Remove("_e");
            extensions.Remove("_em");
        }
        
        if (extensions.Contains("_em") && extensions.Contains("_Cloth"))
        {
            extensions.Remove("_em");
            extensions.Remove("_Cloth");
            extensions.Add("_em_e_Glow");
            extensions.Add("_Cloth");
        }

        string newMtd = "C[ARSN]";
        newMtd += string.Join("", extensions);
        
        return $"N:\\FDP\\data\\Material\\mtd\\character\\{newMtd}.mtd";
    }
    
    /// <summary>
    /// Gets the closest DS3 equivalent of a m[arsn] mtd name that's not native to DS3.
    /// </summary>
    private static string GetDs3Mtd_mARSN(string mtd)
    {
        Dictionary<string, string> extensionTypes = new()
        {
            {"sss", "_SSS"},
            {"em", "_em"},
            {"e", "_e"},
            {"glow", "_em_Glow"},
            {"m", "_m"},
            {"cloth", "_e_Cloth_Decal"},
            {"decal", "_Decal"}
        };
        
        List<string> extensions = mtd.ToLower().Split('_', '.').Where(extensionTypes.ContainsKey)
            .Select(x => extensionTypes[x]).ToList();

        if ((extensions.Contains("_e") || extensions.Contains("_em")) && extensions.Contains("_em_Glow"))
        {
            extensions.Remove("_e");
        }
        
        if ((extensions.Contains("_e") || extensions.Contains("_Decal")) && extensions.Contains("_e_Cloth_Decal"))
        {
            extensions.Remove("_e");
            extensions.Remove("_Decal");
        }

        string newMtd = "M[ARSN]";
        newMtd += string.Join("", extensions);
        
        return $"N:\\FDP\\data\\Material\\mtd\\map\\{newMtd}.mtd";
    }
    
    /// <summary>
    /// From The12thAvenger's FBXImporter
    /// </summary>
    public static FLVER.Vertex Pad(this FLVER.Vertex vertex, List<FLVER2.BufferLayout> bufferLayouts)
    {
        Dictionary<FLVER.LayoutSemantic, int> usageCounts = new();
        FLVER.LayoutSemantic[] paddedProperties =
            {FLVER.LayoutSemantic.Tangent, FLVER.LayoutSemantic.UV, FLVER.LayoutSemantic.VertexColor};

        IEnumerable<FLVER.LayoutMember> layoutMembers = bufferLayouts.SelectMany(bufferLayout => bufferLayout)
            .Where(x => paddedProperties.Contains(x.Semantic));
        foreach (FLVER.LayoutMember layoutMember in layoutMembers)
        {
            bool isDouble = layoutMember.Semantic == FLVER.LayoutSemantic.UV &&
                            layoutMember.Type is FLVER.LayoutType.Float4 or FLVER.LayoutType.Short4;
            int count = isDouble ? 2 : 1;
                
            if (usageCounts.ContainsKey(layoutMember.Semantic))
            {
                usageCounts[layoutMember.Semantic] += count;
            }
            else
            {
                usageCounts.Add(layoutMember.Semantic, count);
            }
        }

        if (usageCounts.ContainsKey(FLVER.LayoutSemantic.Tangent))
        {
            int missingTangentCount = usageCounts[FLVER.LayoutSemantic.Tangent] - vertex.Tangents.Count;
            for (int i = 0; i < missingTangentCount; i++)
            {
                vertex.Tangents.Add(Vector4.Zero);
            }
        }
        
        if (usageCounts.ContainsKey(FLVER.LayoutSemantic.UV))
        {
            int missingUvCount = usageCounts[FLVER.LayoutSemantic.UV] - vertex.UVs.Count;
            for (int i = 0; i < missingUvCount; i++)
            {
                vertex.UVs.Add(Vector3.Zero);
            }
        }
        
        if (usageCounts.ContainsKey(FLVER.LayoutSemantic.VertexColor))
        {
            int missingColorCount = usageCounts[FLVER.LayoutSemantic.VertexColor] - vertex.Colors.Count;
            for (int i = 0; i < missingColorCount; i++)
            {
                vertex.Colors.Add(new FLVER.VertexColor(255, 255, 0, 255));
            }
        }

        return vertex;
    }
    
    /// <summary>
    /// From The12thAvenger's FBXImporter, edited to include an exception when the flver
    /// bufferLayouts list is empty.
    /// </summary>
    public static List<int> GetLayoutIndices(this FLVER2 flver, List<FLVER2.BufferLayout> bufferLayouts)
    {
        List<int> indices = new();

        foreach (FLVER2.BufferLayout referenceBufferLayout in bufferLayouts)
        {
            for (int i = 0; i < flver.BufferLayouts.Count; i++)
            {
                FLVER2.BufferLayout bufferLayout = flver.BufferLayouts[i];
                if (bufferLayout.Select(x => (x.Type, x.Semantic)).SequenceEqual(referenceBufferLayout
                        .Select(x => (x.Type, x.Semantic))))
                {
                    indices.Add(i);
                    break;
                }
                
                if (i == flver.BufferLayouts.Count - 1)
                {
                    indices.Add(i + 1);
                    flver.BufferLayouts.Add(referenceBufferLayout);
                    break;
                }
            }

            if (flver.BufferLayouts.Count == 0)
            {
                indices.Add(0);
                flver.BufferLayouts.Add(referenceBufferLayout);
            }
        }

        return indices;
    }

    /// <summary>
    /// Checks if the given GXList already exists in the flver GxLists.
    /// </summary>
    public static bool IsNewGxList(this FLVER2 flver, FLVER2.GXList gxList)
    {
		foreach (var gxl in flver.GXLists)
	    {
		    if (gxl.Count == gxList.Count)
		    {
			    for (int i = 0; i < gxl.Count; i++)
			    {
				    if (gxl[i].Data.Length == gxList[i].Data.Length &&
				        gxl[i].Unk04 == gxList[i].Unk04 && gxl[i].ID.Equals(gxList[i].ID))
				    {
					    return false;
				    }
			    }
		    }
	    }

		return true;
    }
}   