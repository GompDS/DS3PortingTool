using System;
using System.IO;
using System.Linq;
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
                ConvertHkx(newBnd, op);
            }
            
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertTae(newBnd, file, op);
            }

            if (!op.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
            }
        }
        else if (op.CurrentSourceFileName.Contains("chrbnd"))
        {
            ConvertHkx(newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedChrId}.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"c{op.SourceChrId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedChrId}\\c{op.PortedChrId}.hkxpwv");
            }
		
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedChrId}_c.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"c{op.SourceChrId}_c.clm2",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedChrId}\\c{op.PortedChrId}_c.clm2");
            }
            
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedChrId}.chrbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
        else if (op.CurrentSourceFileName.Contains("texbnd"))
        {
            //TPF oldTpf = 
            //TPF newTpf = new();
            
        }
    }

    protected override void ConvertHkx(BND4 newBnd, Options op)
    {
        newBnd.Files = op.CurrentSourceBnd.Files
            .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)).ToList();

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
            }
        }
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op)
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
                    tex.Path = tex.Path.Replace($"c{op.SourceChrId}", $"c{op.PortedChrId}");
                }
            }
        }
        
        flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\c{op.PortedChrId}.flver",
            newFlver.Write());
        newBnd.Files.Add(flverFile);
    }
}