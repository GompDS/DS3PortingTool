using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool.Converter;
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
            ConvertCharacterHkx(newBnd, op);

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
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            newBnd.Files = op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx") && x.Name.Contains("chr"))
                .Where(x => PortHavok(x, $"{op.Cwd}HavokDowngrade\\")).ToList();
        }
        else if (op.CurrentSourceFileName.Contains("chrbnd"))
        {
            newBnd.Files = op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx") && !Path.GetFileName(x.Name).ToLower().Contains("_c"))
                .Where(x => PortHavokRagdoll(x, $"{op.Cwd}HavokDowngrade\\")).ToList();
        }

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

    protected override void ConvertObjectHkx(BND4 newBnd, Options op, bool isInnerAnibnd)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Edits parameters of a Bloodborne event so that it will match with its DS3 event equivalent.
    /// </summary>
    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data)
    {
        byte[] paramBytes = ev.GetParameterBytes(bigEndian);

        switch (ev.Type)
        {
            // AddSpEffect_Multiplayer
            case 66:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // AddSpEffect
            case 67:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // SpawnFFX_108
            case 108:
                ev.Type = 96;
                ev.Group.GroupType = 96;
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnFFX_109
            case 109:
                ev.Type = 96;
                ev.Group.GroupType = 96;
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_CenterBody
            case 128:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // PlaySound_StateInfo
            case 129:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // PlaySound_ByDummyPoly_PlayerVoice
            case 130:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // PlaySound_ByDummyPoly
            case 131:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // PlaySound_Weapon
            case 132:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // AddSpEffect_DragonForm
            case 302:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // SprjChrActionFlagModule
            case 312:
                Array.Resize(ref paramBytes, 32);
                break;
            // PlayerInputCheck
            case 320:
                Array.Resize(ref paramBytes, 16);
                Array.Clear(paramBytes, 7, 9);
                break;
            // AddSpEffect_WeaponArts
            case 331:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // AddSpEffect_Multiplayer_401
            case 401:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // IgnoreHitsPartsMask
            case 500:
                Array.Resize(ref paramBytes, 16);
                Array.Clear(paramBytes, 2, 14);
                break;
            // AddSpEffect_CultRitualCompletion
            case 797:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // PlaySound_WanderGhost
            case 10130:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
        }
        
        ev.SetParameterBytes(bigEndian, paramBytes);
        return ev;
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
            Materials = sourceFlver.Materials.Select(x => x.ToDummyDs3Material(data.MaterialInfoBank, op)).ToList(),
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
    
    /// <summary>
    ///	Converts 2014 PS4 hkx to PC hkx
    /// </summary>
    protected override bool PortHavok(BinderFile hkxFile, string toolsDirectory)
    {
        string hkxName = Path.GetFileName(hkxFile.Name).ToLower();
        File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
        string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
        
        // Unpack havok file
        bool result = RunProcess(toolsDirectory,"hkxpackbb.exe",
            $"{toolsDirectory}\\{hkxName}"); 
        File.Delete($"{toolsDirectory}\\{hkxName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
        
        // Repack havok file
        result = RunProcess(toolsDirectory,"hkxpackds3.exe",
            $"{toolsDirectory}\\{xmlName}"); 
        File.Delete($"{toolsDirectory}\\{xmlName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }

        hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        Console.WriteLine($"Ported {hkxName}");
        return true;
    }
    
    /// <summary>
    ///	Converts 2014 PS4 hkx to PC hkx
    /// </summary>
    private bool PortHavokRagdoll(BinderFile hkxFile, string toolsDirectory)
    {
        string hkxName = Path.GetFileName(hkxFile.Name).ToLower();
        File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
        string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
        
        // Unpack havok file
        bool result = RunProcess(toolsDirectory,"hkxpackbb.exe",
            $"{toolsDirectory}\\{hkxName}"); 
        File.Delete($"{toolsDirectory}\\{hkxName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
        
        // Use Avenger's tool
        File.Move($"{toolsDirectory}\\{xmlName}", $"{toolsDirectory}\\AvengersTool\\{xmlName}");
        result = RunProcess(toolsDirectory,"test.exe",
            $"{toolsDirectory}\\AvengersTool\\{xmlName}"); 
        File.Delete($"{toolsDirectory}\\AvengersTool\\{xmlName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
        File.Move($"{toolsDirectory}\\AvengersTool\\{Path.GetFileNameWithoutExtension(hkxFile.Name)}-out.xml", 
            $"{toolsDirectory}\\{xmlName}");
        
        // Repack havok file
        result = RunProcess(toolsDirectory,"hkxpackds3.exe",
            $"{toolsDirectory}\\{xmlName}"); 
        File.Delete($"{toolsDirectory}\\{xmlName}");
        if (result == false)
        {
            Console.WriteLine($"Could not port {hkxName}");
            return false;
        }
		
        hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
        File.Delete($"{toolsDirectory}\\{hkxName}");
        Console.WriteLine($"Ported {hkxName}");
        return true;
    }
}
