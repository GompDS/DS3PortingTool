using System.Numerics;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool.Util
{
    public static class FlverUtils
    { 
	    /// <summary>
	    /// Takes a non-native DS3 material and returns a new material with as close a mtd type as possible.
	    /// </summary>
	    public static FLVER2.Material ToDummyDs3Material(this FLVER2.Material oldMat, FLVER2MaterialInfoBank materialInfoBank) 
	    {
	        FLVER2.Material newMat = new()
	        {
				Name = oldMat.Name,
				MTD = GetDs3Mtd(oldMat.MTD)
	        };
	        
	        FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
		        .First(y => y.MTD.Equals($"{Path.GetFileName(newMat.MTD)}".ToLower()));

	        newMat.Textures = matDef.TextureChannels.Values.Select(texName =>
            {
            	FLVER2.Texture tex = new FLVER2.Texture { Type = texName };
            	if (texName.Contains("Diffuse"))
            	{	
            		tex.Path = @"N:\SPRJ\data\Other\SysTex\SYSTEX_BLACK.tga";
            	}
            	else if (texName.Contains("Specular"))
            	{
            		tex.Path = @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummySpecular.tga";
            	}
                else if (texName.Contains("Shininess"))
                {
	                tex.Path = @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyShininess.tga";
                }
            	else if (texName.Contains("Bumpmap"))
            	{
            		tex.Path = @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyNormal.tga";
            	}
                else if (texName.Contains("ScatteringMask"))
                {
	                tex.Path = @"N:\FDP\data\Other\SysTex\SYSTEX_DummyScatteringMask.tga";
                }
                else if (texName.Contains("Emissive"))
                {
	                tex.Path = @"N:\SPRJ\data\Other\SysTex\SYSTEX_DummyEmissive.tga";
                }
            	else if (texName.Contains("BloodMask") || texName.Contains("Displacement"))
            	{
            		tex.Path = @"N:\LiveTokyo\data\model\common\tex\dummy128.tga";
            	}
            	return tex;
            }).ToList();

            return newMat;
        }

	    /// <summary>
	    /// Gets the closest DS3 equivalent of a mtd name that's not native to DS3.
	    /// </summary>
	    private static string GetDs3Mtd(string mtd)
        {
	        Dictionary<string, string> extensionTypes = new()
	        {
		        {"add", "_Add"},
		        {"sss", "_SSS"},
		        {"em", "_em"},
		        {"e", "_e"},
		        {"glow", "_Glow"},
		        {"m", "_m"},
		        {"decal", "_Decal"},
		        {"cloth", "_Cloth"}
	        };
            
	        List<string> extensions = mtd.ToLower().Split('_', '.').Where(extensionTypes.ContainsKey)
		        .Select(x => extensionTypes[x]).ToList();

	        string newMtd = "C[ARSN]";
	        newMtd += string.Join("", extensions);
	        
	        return $"N:\\FDP\\data\\Material\\mtd\\character\\{newMtd}.mtd";
        }
        
	    /// <summary>
	    /// From The12thAvenger's FBXImporter
	    /// </summary>
	    public static FLVER.Vertex Pad(this FLVER.Vertex vertex, IEnumerable<FLVER2.BufferLayout> bufferLayouts)
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
}