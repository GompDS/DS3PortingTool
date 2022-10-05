using System.Drawing;
using System.Numerics;
using System.Security.Cryptography;
using HKX2;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool
{
    public static class FlverUtils
    {
	    /*public static FLVER2.Mesh Clone(this FLVER2.Mesh orig)
	    {
		    FLVER2.Mesh clone = new()
		    {
				Dynamic = orig.Dynamic,
				MaterialIndex = orig.MaterialIndex,
				DefaultBoneIndex = orig.DefaultBoneIndex,
				//BoneIndices = new List<int>(orig.BoneIndices),
				FaceSets = orig.FaceSets.Select(x => x.Clone()).ToList(),
				VertexBuffers = orig.VertexBuffers.Select(x => new FLVER2.VertexBuffer(x.LayoutIndex)).ToList(),
				Vertices = orig.Vertices.Select(x => x.Clone()).ToList(),
				BoundingBox = orig.BoundingBox.Clone()
		    };
		    return clone;
	    }
		
	    public static FLVER2.FaceSet Clone(this FLVER2.FaceSet orig)
	    {
		    FLVER2.FaceSet clone = new()
		    {
				Flags = orig.Flags,
				TriangleStrip = orig.TriangleStrip,
				CullBackfaces = orig.CullBackfaces,
				Unk06 = orig.Unk06,
				Indices = new List<int>(orig.Indices)
		    };
		    return clone;
	    }

	    public static FLVER.Vertex Clone(this FLVER.Vertex orig)
	    {
		    FLVER.Vertex clone = new()
		    {
				Position = orig.Position.Clone(),
				BoneWeights = new FLVER.VertexBoneWeights
				{
					[0] = orig.BoneWeights[0],
					[1] = orig.BoneWeights[1],
					[2] = orig.BoneWeights[2],
					[3] = orig.BoneWeights[3]
				},
				BoneIndices = new FLVER.VertexBoneIndices
				{
					[0] = orig.BoneIndices[0],
					[1] = orig.BoneIndices[1],
					[2] = orig.BoneIndices[2],
					[3] = orig.BoneIndices[3]
				},
				Normal = orig.Normal.Clone(),
				NormalW = orig.NormalW,
				UVs = orig.UVs.Select(x => x.Clone()).ToList(),
				Tangents = orig.Tangents.Select(x => new Vector4(x.X, x.Y, x.Z, x.W)).ToList(),
				Bitangent = new Vector4(orig.Bitangent.X, orig.Bitangent.Y, orig.Bitangent.Z, orig.Bitangent.W),
				Colors = orig.Colors.Select(x => new FLVER.VertexColor(x.A, x.R, x.G, x.B)).ToList() 
		    };
		    return clone;
	    }
	    
	    public static FLVER2.Mesh.BoundingBoxes Clone(this FLVER2.Mesh.BoundingBoxes orig)
	    {
		    FLVER2.Mesh.BoundingBoxes clone = new()
		    {
			    Min = new Vector3(orig.Min.X, orig.Min.Y, orig.Min.Z),
			    Max = new Vector3(orig.Max.X, orig.Max.Y, orig.Max.Z)
		    };
		    return clone;
	    }
	    
	    public static Vector3 Clone(this Vector3 orig)
	    {
		    Vector3 clone = new Vector3(orig.X, orig.Y, orig.Z);
		    return clone;
	    }*/

	    public static FLVER2.Material ToDummyDs3Material(this FLVER2.Material oldMat) 
	    {
	        FLVER2.Material newMat = new()
	        {
				Name = oldMat.Name,
				MTD = GetDs3Mtd(oldMat.MTD)
	        };

	        FLVER2MaterialInfoBank materialInfoBank = 
		        FLVER2MaterialInfoBank.ReadFromXML($"{AppDomain.CurrentDomain.BaseDirectory}Res\\BANKDS3.xml");
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
    }   
}