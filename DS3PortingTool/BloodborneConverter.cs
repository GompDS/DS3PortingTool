using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool;
public class BloodborneConverter : Converter
{
    /// <summary>
    /// Performs the steps necessary to convert a Bloodborne binder into a DS3 compatible binder.
    /// </summary>
    public override void DoConversion(Options op)
    {
        BND4 newBnd = new();
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            if (!op.PortTaeOnly)
            {
                ConvertCharacterHkx(newBnd, op);
            }
            
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertCharacterTae(newBnd, file, op);
            }

            if (!op.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write($"{op.Cwd}\\c{op.PortedId}.anibnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            }
        }
        else if (op.CurrentSourceFileName.Contains("chrbnd"))
        {
            //ConvertHkx(newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}.hkxpwv");
            }

            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }
            
            // Convert tpfs into texbnd

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.chrbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }
    /// <summary>
    /// Converts a Bloodborne HKX file into a DS3 compatible HKX file.
    /// </summary>
	protected override void ConvertCharacterHkx(BND4 newBnd, Options op)
    {
	    newBnd.Files = op.CurrentSourceBnd.Files
		    .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
		    .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\")).ToList();
		
        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (name.Contains($"c{op.SourceId}.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceId, op.PortedId)}";
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

    protected override void ConvertObjectHkx(BND4 newBnd, Options op)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Edits parameters of a Bloodborne event so that it will match with its DS3 event equivalent.
    /// </summary>
    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Creates a new DS3 FLVER using data from a Bloodborne FLVER.
    /// </summary>
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
