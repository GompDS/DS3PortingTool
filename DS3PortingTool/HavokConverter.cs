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
        
        if (sv is { Major: 5, Minor: 5, Patch: 0 } && tv is { Major: 2014, Minor: 1, Patch: 0 } &&
            ConversionContext.SourceGame.Platform == "ps3" && ConversionContext.TargetGame.Platform == "win64")
        {
            return TryConvert_5_5_0_Ps3_To_2014_1_0_Win64(hkxFile);
        }
        
        if (sv is { Major: 2010, Minor: 2, Patch: 0 } && tv is { Major: 2014, Minor: 1, Patch: 0 } &&
            ConversionContext.SourceGame.Platform == "win64" && ConversionContext.TargetGame.Platform == "win64")
        {
            return TryConvert_2010_2_0_Win64_To_2014_1_0_Win64(hkxFile);
        }
        
        if (sv is { Major: 2018, Minor: 1, Patch: 0 } && tv is { Major: 2014, Minor: 1, Patch: 0 } &&
            ConversionContext.SourceGame.Platform == "win64" && ConversionContext.TargetGame.Platform == "win64")
        {
            return TryConvert_2018_1_0_Win64_To_2014_1_0_Win64(hkxFile, compendiumPath);
        }
        
        return false;
    }

    private static bool TryConvert_5_5_0_Ps3_To_2014_1_0_Win64(BinderFile hkxFile)
    {
        string hkxNameNoExt = Path.GetFileNameWithoutExtension(hkxFile.Name);
        string hkxPathNoExt = $"{ConversionContext.ToolsDir}\\{hkxNameNoExt}";
        string hkx5Path = $"{hkxPathNoExt}.hkx";
        string hkx2010Path = $"{hkxPathNoExt}hk2010.hkx";
        string hkx2014Path = $"{hkxPathNoExt}hk2014.hkx";
        File.WriteAllBytes($"{hkxPathNoExt}.hkx", hkxFile.Bytes);
        
        // despacito (5.5.0 Ps3 -> 2010.2.0 Win32)
        if (!ProcessUtil.TryRunProcess(ConversionContext.ToolsDir, "despacito.exe",
                $"-t -d \"{hkx5Path}\"") || !File.Exists($"{hkx2010Path}"))
        {
            Console.WriteLine($"WARN: Could not convert {hkxNameNoExt}.hkx");
            return false;
        }
        
        // AssetCc2 (2010.2.0 Win32 -> 2014.1.0 Win64)
        if (!ProcessUtil.TryRunProcess(ConversionContext.ToolsDir, "AssetCc2.exe",
                $"--strip --rules=8101 \"{hkx2010Path}\" \"{hkx2014Path}\"") || !File.Exists($"{hkx2014Path}"))
        {
            Console.WriteLine($"WARN: Could not convert {hkxNameNoExt}.hkx");
            return false;
        }
        
        hkxFile.Bytes = File.ReadAllBytes(hkx2014Path);
        File.Delete(hkx5Path);
        File.Delete(hkx2010Path);
        File.Delete(hkx2014Path);
        
        return true;
    }
    
    private static bool TryConvert_2010_2_0_Win64_To_2014_1_0_Win64(BinderFile hkxFile)
    {
        string hkxNameNoExt = Path.GetFileNameWithoutExtension(hkxFile.Name);
        string hkxPathNoExt = $"{ConversionContext.ToolsDir}\\{hkxNameNoExt}";
        string hkx2010Path = $"{hkxPathNoExt}.hkx";
        string hkx2014Path = $"{hkxPathNoExt}hk2014.hkx";
        File.WriteAllBytes($"{hkxPathNoExt}.hkx", hkxFile.Bytes);
        
        // AssetCc2 (2010.2.0 Win32 -> 2014.1.0 Win64)
        if (!ProcessUtil.TryRunProcess(ConversionContext.ToolsDir, "AssetCc2.exe",
                $"--strip --rules=8101 \"{hkx2010Path}\" \"{hkx2014Path}\"") || !File.Exists($"{hkx2014Path}"))
        {
            Console.WriteLine($"WARN: Could not convert {hkxNameNoExt}.hkx");
            return false;
        }
        
        hkxFile.Bytes = File.ReadAllBytes(hkx2014Path);
        File.Delete(hkx2010Path);
        File.Delete(hkx2014Path);
        
        return true;
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
        
        //TODO
        
        return true;
    }
}