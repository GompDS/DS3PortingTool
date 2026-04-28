using System.Text.RegularExpressions;
using SoulsFormats;
using SoulsGameSpecs;

namespace DS3PortingTool;

public static class HavokConverter
{
    private static readonly char[]? PackFileVersionDelimiters = ['-', '_'];
    
    public static bool TryGetVersion(BinderFile hkx_file, out GameSpec.HavokVersion? version)
    {
        version = null;

        using BinaryReaderEx reader = new BinaryReaderEx(false, hkx_file.Bytes);
        reader.ReadBytes(12);
        
        try
        {
            // tag file
            string readVersion = reader.ReadASCII(12);
                
            if (Regex.IsMatch(readVersion, @"SDKV20\d+"))
            {
                version = new GameSpec.HavokVersion(readVersion);
                return true;
            }
                
            throw new InvalidDataException();
        }
        catch (InvalidDataException)
        {
            // pack file
            reader.ReadBytes(16);
            string readVersion = string.Empty;
            string next = reader.ReadASCII(1);
            while (next != "\0")
            {
                readVersion += next;
                next = reader.ReadASCII(1);
            }

            if (Regex.IsMatch(readVersion, @"(hk_|Havok-)\d{1,4}\.\d\.\d-r1"))
            {
                version = new GameSpec.HavokVersion(readVersion);
                return true;
            }
        }

        return false;
    }

    public static bool TryConvertAll(IBinder bnd)
    {
        string? compendiumPath = null;
        BinderFile? compendium = bnd.Files.FirstOrDefault(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
        if (compendium is not null)
        {
            File.WriteAllBytes(ConversionContext.ToolsDir + "compendium", compendium.Bytes);
        }

        bool result = true;
        foreach (BinderFile file in bnd.Files.Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)))
        {
            result |= TryConvert(file, compendiumPath);
        }
        
        return result;
    }
    
    public static bool TryConvert(BinderFile hkxFile, string? compendiumPath = null)
    {
        if (ConversionContext.SourceGame.StableHavokVersion == null ||
            ConversionContext.TargetGame.StableHavokVersion == null)
        {
            return false;
        }
        
        GameSpec.HavokVersion sv = ConversionContext.SourceGame.StableHavokVersion.Value;
        GameSpec.HavokVersion tv = ConversionContext.TargetGame.StableHavokVersion.Value;
        
        if (sv is { Major: 2018, Minor: 1, Patch: 0 } && tv is { Major: 2014, Minor: 1, Patch: 0 } &&
            ConversionContext.SourceGame.Platform == "win64" && ConversionContext.TargetGame.Platform == "win64")
        {
            return TryConvert_2018_1_0_Win64_To_2014_1_0_Win64(hkxFile, compendiumPath);
        }
        
        return false;
    }

    private static bool TryConvert_2018_1_0_Win64_To_2014_1_0_Win64(BinderFile hkxFile, string? compendiumPath = null)
    {
        string hkxName = Path.GetFileName(hkxFile.Name);
        File.WriteAllBytes($"{ConversionContext.ToolsDir}\\{hkxName}", hkxFile.Bytes);
        string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
		
        // FileConvert
        if (!ProcessUtil.TryRunProcess(ConversionContext.ToolsDir, "fileConvert.exe",
                $"-x --compendium {compendiumPath} {ConversionContext.ToolsDir}/{hkxName} {ConversionContext.ToolsDir}/{xmlName}"))
        {
            Console.WriteLine($"WARN: Could not convert {hkxName}");
            return false;
        }
        File.Delete($"{ConversionContext.ToolsDir}/{hkxName}");
        
        return true;
    }
}