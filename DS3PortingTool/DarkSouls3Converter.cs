using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool;

public class DarkSouls3Converter : Converter
{
    /// <summary>
    /// Performs the steps necessary to convert a DS3 binder into a new DS3 binder.
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
            ConvertCharacterHkx(newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}.hkxpwv");
            }
		
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedId}_c.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"c{op.SourceId}_c.clm2",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedId}\\c{op.PortedId}_c.clm2");
            }
            
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.chrbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
        else if (op.CurrentSourceFileName.Contains("objbnd"))
        {
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
            if (file != null)
            {
                file = BND4.Read(file.Bytes).Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    ConvertObjectTae(newBnd, file, op);
                }
            }
        }
    }

    protected override void ConvertCharacterHkx(BND4 newBnd, Options op)
    {
        newBnd.Files = op.CurrentSourceBnd.Files
            .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();
            
            if (name.Contains($"c{op.SourceId}.hkx") || name.Contains($"c{op.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceId, op.PortedId)}";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
            }
        }
    }

    protected override void ConvertObjectHkx(BND4 newBnd, Options op)
    {
        throw new NotImplementedException();
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data)
    {
        return ev;
    }

    /// <summary>
    /// Converts a ds3 FLVER file into a new DS3 FLVER file.
    /// </summary>
    private new void ConvertFlver(BND4 newBnd, BinderFile flverFile, Options op)
    {
        FLVER2 newFlver = FLVER2.Read(flverFile.Bytes);

        if (op.SourceFileNames.Any(x => x.Contains(".texbnd")))
        {
            foreach (FLVER2.Material mat in newFlver.Materials)
            {
                foreach (FLVER2.Texture tex in mat.Textures)
                {
                    tex.Path = tex.Path.Replace($"c{op.SourceId}", $"c{op.PortedId}");
                }
            }
        }
        
        flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\c{op.PortedId}.flver",
            newFlver.Write());
        newBnd.Files.Add(flverFile);
    }
}