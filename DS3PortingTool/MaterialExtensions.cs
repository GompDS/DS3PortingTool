using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool
{
    public static class MaterialExtensions
    {
        public static FLVER2.Material ToDummyDs3Material(this FLVER2.Material oldMat)
        {
	        FLVER2.Material newMat = new FLVER2.Material(oldMat.Name, GetDs3Mtd(oldMat.MTD), oldMat.Flags);

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
		        //{"e", "_e"},
		        {"m", "_m"},
		        //{"cloth", "_Cloth"}
	        };
            
	        List<string> extensions = mtd.Split('_', '.').Where(extensionTypes.ContainsKey).Select(x => extensionTypes[x]).ToList();

	        string newMtd = "C[ARSN]";
	        newMtd += string.Join("", extensions);
	        
	        return $"N:\\FDP\\data\\Material\\mtd\\character\\{newMtd}.mtd";
        }
    }
}