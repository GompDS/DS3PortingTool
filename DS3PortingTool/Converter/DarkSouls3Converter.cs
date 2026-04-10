using DS3PortingTool.Util;
using SoulsFormats;

namespace DS3PortingTool.Converter;

public class DarkSouls3Converter : Converter
{
    /// <summary>
    /// Performs the steps necessary to convert a DS3 binder into a new DS3 binder.
    /// </summary>
    public override void DoConversion()
    {
        BND4 sourceBnd = (BND4)ConversionContext.CurrentTextureSourceFile;
        
        BND4 newBnd = new();
        if (ConversionContext.CurrentTextureSourceFileName.Contains("anibnd"))
        {
            if (!ConversionContext.PortTaeOnly)
            {
                ConvertCharacterHkx(sourceBnd, newBnd);
            }
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertCharacterTae(sourceBnd, newBnd, file);
            }

            if (!ConversionContext.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write($"{ConversionContext.Cwd}\\c{ConversionContext.PortedId}.anibnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
            }
        }
        else if (ConversionContext.CurrentTextureSourceFileName.Contains("chrbnd"))
        {
            ConvertCharacterHkx(sourceBnd, newBnd);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{ConversionContext.PortedId}.hkx")))
            {
                sourceBnd.TransferBinderFile(newBnd, $"c{ConversionContext.SourceId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{ConversionContext.PortedId}\\c{ConversionContext.PortedId}.hkxpwv");
            }
		
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{ConversionContext.PortedId}_c.hkx")))
            {
                sourceBnd.TransferBinderFile(newBnd, $"c{ConversionContext.SourceId}_c.clm2",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{ConversionContext.PortedId}\\c{ConversionContext.PortedId}_c.clm2");
            }
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file);
            }

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{ConversionContext.Cwd}\\c{ConversionContext.PortedId}.chrbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
        else if (ConversionContext.CurrentTextureSourceFileName.Contains("objbnd"))
        {
            BinderFile file1 = sourceBnd.Files.First(x => FLVER2.Is(x.Bytes));
            FLVER2 flver1 = FLVER2.Read(file1.Bytes);
            BinderFile file2 = (ConversionContext.ContentSourceFiles[Array.IndexOf(ConversionContext.ContentSourceFiles, ConversionContext.CurrentTextureSourceFile) + 1] as BND4)
                .Files.First(x => FLVER2.Is(x.Bytes));
            FLVER2 flver2 = FLVER2.Read(file2.Bytes);
            
            
            BinderFile? file = sourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
            if (file != null)
            {
                file = BND4.Read(file.Bytes).Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    ConvertObjectTae(newBnd, file);
                }
            }
        }
    }

    protected override void ConvertCharacterHkx(IBinder sourceBnd, BND4 newBnd)
    {
        newBnd.Files = sourceBnd.Files
            .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{ConversionContext.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();
            
            if (name.Contains($"c{ConversionContext.SourceId}.hkx") || name.Contains($"c{ConversionContext.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(ConversionContext.SourceId, ConversionContext.PortedId)}";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
            }
        }
    }

    protected override void ConvertObjectHkx(IBinder sourceBnd, BND4 newBnd, bool isInnerAnibnd)
    {
        throw new NotImplementedException();
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, XmlData data)
    {
        return ev;
    }

    /// <summary>
    /// Converts a ds3 FLVER file into a new DS3 FLVER file.
    /// </summary>
    private new void ConvertFlver(BND4 newBnd, BinderFile flverFile)
    {
        FLVER2 newFlver = FLVER2.Read(flverFile.Bytes);

        if (ConversionContext.ContentSourceFileNames.Any(x => x.Contains(".texbnd")))
        {
            foreach (FLVER2.Material mat in newFlver.Materials)
            {
                foreach (FLVER2.Texture tex in mat.Textures)
                {
                    tex.Path = tex.Path.Replace($"c{ConversionContext.SourceId}", $"c{ConversionContext.PortedId}");
                }
            }
        }
        
        flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{ConversionContext.PortedId}\\c{ConversionContext.PortedId}.flver",
            newFlver.Write());
        newBnd.Files.Add(flverFile);
    }
}