using System.Text.RegularExpressions;
using SoulsFormats;

namespace DS3PortingTool;

public static class HavokConverter
{
    public struct HavokVersion(HavokVersion.HkFileType fileType, int major, int minor, int patch)
    {
        public enum HkFileType
        {
            PackFile,
            TagFile
        }

        public HkFileType FileType = fileType;
        public int Major = major;
        public int Minor = minor;
        public int Patch = patch;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }

    private static readonly char[]? PackFileVersionDelimiters = ['-', '_'];
    
    public static bool TryGetVersion(BinderFile hkx_file, out HavokVersion? version)
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
                int major = int.Parse(readVersion.Substring(4, 4));
                int minor = int.Parse(readVersion.Substring(8, 2));
                int patch = int.Parse(readVersion.Substring(10, 2));
                version = new HavokVersion(HavokVersion.HkFileType.TagFile, major, minor, patch);
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
                string[] splits = readVersion.Split(PackFileVersionDelimiters, 3);
                splits = splits[1].Split('.', 3);
                
                int major = int.Parse(splits[0]);
                int minor = int.Parse(splits[1]);
                int patch = int.Parse(splits[2]);
                
                version = new HavokVersion(HavokVersion.HkFileType.PackFile, major, minor, patch);
                return true;
            }
        }

        return false;
    }
}