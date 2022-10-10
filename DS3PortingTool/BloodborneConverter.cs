using DS3PortingTool.Util;
using SoulsFormats;

namespace DS3PortingTool;
public class BloodborneConverter : Converter
{
    public override void DoConversion(Options op)
    {
        BND4 newBnd = new();
        if (op.SourceFileName.Contains("anibnd"))
        {
            if (!op.PortTaeOnly)
            {
                ConvertHkx(newBnd, op);
            }
            
            BinderFile? file = op.SourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertTae(newBnd, file, op);
            }

            if (!op.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx",
                    DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
            }
        }
        else if (op.SourceFileName.Contains("chrbnd"))
        {
            //ConvertHkx(newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedChrId}.hkx")))
            {
                op.SourceBnd.TransferBinderFile(newBnd, $"c{op.SourceChrId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedChrId}\\c{op.PortedChrId}.hkxpwv");
            }

            BinderFile? file = op.SourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }
            
            // Convert tpfs into texbnd

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.chrbnd.dcx",
                DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
        }
    }
    
	protected override void ConvertHkx(BND4 newBnd, Options op)
    {
	    newBnd.Files = op.SourceBnd.Files
		    .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
		    .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\")).ToList();
		
        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (name.Contains($"c{op.SourceChrId}.hkx"))
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
    
    protected override FLVER2 CreateDs3Flver(FLVER2 sourceFlver, XmlData data, Options op)
    {
        FLVER2 newFlver = new FLVER2
        {
            Header = new FLVER2.FLVERHeader
            {
                BoundingBoxMin = sourceFlver.Header.BoundingBoxMin,
                BoundingBoxMax = sourceFlver.Header.BoundingBoxMax,
                Unk4A = sourceFlver.Header.Unk4A,
                Unk4C = sourceFlver.Header.Unk4C,
                Unk5C = sourceFlver.Header.Unk5C,
                Unk5D = sourceFlver.Header.Unk5D,
                Unk68 = sourceFlver.Header.Unk68
            },
            Dummies = sourceFlver.Dummies,
            Materials = sourceFlver.Materials.Select(x => x.ToDs3Material(data.MaterialInfoBank, op)).ToList(),
            Bones = sourceFlver.Bones.Select(x =>
            {
                // Unk3C should only be 0 or 1 in DS3.
                if (x.Unk3C > 1)
                {
                    x.Unk3C = 0;
                }

                return x;
            }).ToList(),
            Meshes = sourceFlver.Meshes
        };
        
        return newFlver;
    }
}