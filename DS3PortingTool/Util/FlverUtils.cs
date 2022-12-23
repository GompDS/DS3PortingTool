using System.Numerics;
using System.Text.RegularExpressions;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool.Util;

public static class FlverUtils
{ 
    /// <summary>
    /// Takes a non-native DS3 material and returns a new material with as close a mtd type as possible.
    /// </summary>
    public static FLVER2.Material ToDummyDs3Material(this FLVER2.Material oldMat, FLVER2MaterialInfoBank materialInfoBank, Options op) 
    {
        FLVER2.Material newMat = new()
        {
			Name = oldMat.Name,
            MTD = op.SourceBndsType == Options.AssetType.Character ? GetDs3Mtd_cARSN(oldMat.MTD) : GetDs3Mtd_mARSN(oldMat.MTD)
        };
        
        FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
	        .First(y => y.MTD.Equals($"{Path.GetFileName(newMat.MTD)}".ToLower()));

        newMat.Textures = matDef.TextureChannels.Values.Select(texName =>
        {
            FLVER2.Texture tex = new() { Type = texName, Path = GetDummyTexPath(texName) };
            return tex;
        }).ToList();

        return newMat;
    }
    
    /// <summary>
    /// Takes a non-native DS3 material and turns it into a DS3 material with original textures.
    /// </summary>
    public static FLVER2.Material ToDs3Material(this FLVER2.Material oldMat, FLVER2MaterialInfoBank materialInfoBank, Options op) 
    {
	    FLVER2.Material newMat = new()
	    {
		    Name = oldMat.Name,
		    MTD = op.SourceBndsType == Options.AssetType.Character ? GetDs3Mtd_cARSN(oldMat.MTD) : GetDs3Mtd_mARSN(oldMat.MTD)
	    };
        
	    FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
		    .First(y => y.MTD.Equals($"{Path.GetFileName(newMat.MTD)}".ToLower()));
	    
	    
	    for (int i = 0; i < matDef.TextureChannels.Values.Count; i++)
        {
            string texType = matDef.TextureChannels.ElementAt(i).Value;
		    FLVER2.Texture tex = new() { Type = texType, Path = GetDummyTexPath(texType) };
            FLVER2.Texture? matchingTex = oldMat.Textures.FirstOrDefault(x => x.Type.Equals(texType));
            if (matchingTex == null) continue;
            Match texContainer = Regex.Match(matchingTex.Path, "c[0-9]{4}", RegexOptions.IgnoreCase);
            if (!texContainer.Success) continue;
            tex.Path = $"N:\\FDP\\data\\Model\\chr\\c{op.PortedId}\\tex\\{Path.GetFileName(matchingTex.Path)}";
            newMat.Textures.Add(tex);
        }

        return newMat;
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
	        {"m", "_m"},
	        {"decal", "_Decal"},
	        {"cloth", "_Cloth"}
        };
        
        List<string> extensions = mtd.ToLower().Split('_', '.').Where(extensionTypes.ContainsKey)
	        .Select(x => extensionTypes[x]).ToList();

        if ((extensions.Contains("_e") || extensions.Contains("_em")) && extensions.Contains("_em_e_Glow"))
        {
            extensions.Remove("_e");
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
                            layoutMember.Type is FLVER.LayoutType.Float4 or FLVER.LayoutType.UVPair;
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