using SoulsFormats;
using SoulsGameSpecs;

namespace DS3PortingTool;

public class GameAsset(string filePath, GameSpec originGame, GameAsset.FSAssetType type)
{
    public enum FSAssetType
    {
        CHRBND,
        ANIBND,
        OBJBND
    }
    
    public string FilePath { get; } = filePath;
    public string FileName { get; } = Path.GetFileName(filePath);
    public string FileNameWithoutExtension { get; } = Path.GetFileNameWithoutExtension(filePath);
    public GameSpec OriginGame { get; } = originGame;
    public FSAssetType Type { get; } = type;
}