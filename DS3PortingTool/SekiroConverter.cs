using DS3PortingTool.Util;
using SoulsFormats;

namespace DS3PortingTool;
public class SekiroConverter : Converter
{
	protected override void ConvertHkx(BND4 newBnd, Options op)
    {
        if (op.SourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = op.SourceBnd.Files
                .Find(x => x.Name.Contains($"c{op.SourceChrId}.compendium"));
            if (compendium == null)
            {
                throw new FileNotFoundException("Source anibnd contains no compendium.");
            }
			
            newBnd.Files = op.SourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
        }
        else
        {
            newBnd.Files = op.SourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\")).ToList();
        }
		
        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (name.Contains($"c{op.SourceChrId}.hkx") || name.Contains($"c{op.SourceChrId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceChrId, op.PortedChrId)}";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
                if (!name.Contains("skeleton"))
                {
                    hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
                }
            }
        }
    }
}