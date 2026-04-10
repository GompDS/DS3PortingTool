using System.Text.RegularExpressions;
using DS3PortingTool.Util;
using SoulsFormats;

namespace DS3PortingTool.Converter;

public class EldenRingConverter : Converter
{
    /// <summary>
    /// All tae and hkx from any source anibnds will be combined into this singular anibnd which will be converted.
    /// </summary>
    private BND4 _combinedAnibnd = new();

    /// <summary>
    /// Flver from geombnd and hkx from geomhkxbnd are stored in here.
    /// </summary>
    private BND4 _combinedObjbnd = new();

    private readonly string _textureRegexPattern = @".+(?=_.+)";
    
    /// <summary>
    /// Performs the steps necessary to convert an Elden Ring binder into a DS3 compatible binder.
    /// </summary>
    public override void DoConversion()
    {
        if (ConversionContext.CurrentTextureSourceFile is TPF tpf)
        {
            foreach (TPF.Texture tex in tpf.Textures)
            {
                bool isLod = false;
                string texName = tex.Name;
                if (tex.Name.EndsWith("_l", StringComparison.OrdinalIgnoreCase))
                {
                    isLod = true;
                    texName = texName[..^2];
                    
                    if (texName.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                    {
                        char[] chars = tex.Name.ToCharArray();
                        chars[^3] = 'r';
                        tex.Name = new string(chars);
                    }
                }
                else if (texName.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                {
                    char[] chars = tex.Name.ToCharArray();
                    chars[^1] = 'r';
                    tex.Name = new string(chars);
                }
                
                if (!isLod)
                {
                    Textures.Add(tex);
                }
                else
                {
                    LODTextures.Add(tex);
                }
            }
            
            Console.WriteLine($"Read TPF: {ConversionContext.CurrentTextureSourceFileName}");
            
            string[] tpfNames = ConversionContext.TextureSourceFileNames.Where(x => x.Contains(".tpf")).ToArray();
            if (Array.IndexOf(tpfNames, ConversionContext.CurrentTextureSourceFileName) == tpfNames.Length - 1)
            {
                foreach (HashSet<string> texGroup in UsedTextureGroups)
                {
                    WritePBRCorrectedDDS(texGroup);
                }
            }
        }
        else if (ConversionContext.CurrentContentSourceFileName.Contains("texbnd"))
        {
            BND4 sourceBnd = (BND4)ConversionContext.CurrentTextureSourceFile;
            
            List<TPF.Texture> textures = new();
            
            foreach (TPF.Texture tex in sourceBnd.Files.SelectMany(x => TPF.Read(x.Bytes).Textures))
            {
                bool isLod = false;
                string texName = tex.Name;
                if (texName.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                {
                    char[] chars = tex.Name.ToCharArray();
                    chars[^1] = 'r';
                    tex.Name = new string(chars);
                }
                
                textures.Add(tex);
            }
            
            Console.WriteLine("Read textures from texbnd");
            
            string[] tpfNames = ConversionContext.TextureSourceFileNames.Where(x => x.Contains(".tpf")).ToArray();
            if (Array.IndexOf(tpfNames, ConversionContext.CurrentTextureSourceFileName) == tpfNames.Length - 1)
            {
                foreach (HashSet<string> texGroup in UsedTextureGroups)
                {
                    WritePBRCorrectedDDS(texGroup);
                }
            }
        }
        else
        {
            BND4 sourceBnd = (BND4)ConversionContext.CurrentContentSourceFile;
            
            BND4 newBnd = new();
            if (ConversionContext.CurrentContentSourceFileName.Contains("anibnd") && ConversionContext.SourceBndsType == ConversionContext.AssetType.Character)
            {
                if (!ConversionContext.PortTaeOnly)
                {
                    ConvertCharacterHkx(sourceBnd, newBnd);
                    _combinedAnibnd.Files.AddRange(newBnd.Files);
                }
                
                BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    _combinedAnibnd.Files.Add(file);
                }

                string[] anibndNames = ConversionContext.ContentSourceFileNames.Where(x => x.Contains(".anibnd")).ToArray();
                if (Array.IndexOf(anibndNames, ConversionContext.CurrentContentSourceFileName) == anibndNames.Length - 1)
                {
                    ConvertCombinedAnibnd();
                }
            }
            else if (ConversionContext.CurrentContentSourceFileName.Contains("chrbnd") && ConversionContext.SourceBndsType == ConversionContext.AssetType.Character)
            {
                if (!ConversionContext.PortFlverOnly)
                {
                    //ConvertCharacterHkx(newBnd);

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
                }

                BinderFile? file = sourceBnd.Files.Find(x => x.Name.Contains(".flver"));
                if (file != null)
                {
                    ConvertFlver(newBnd, file);
                }
                
                if (ConversionContext.PortFlverOnly) return;

                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write($"{ConversionContext.Cwd}\\c{ConversionContext.PortedId}.chrbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
            }
            else if (ConversionContext.CurrentContentSourceFileName.Contains("geombnd") && ConversionContext.SourceBndsType == ConversionContext.AssetType.Object)
            {
                BinderFile? file = sourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
                if (file != null && !ConversionContext.PortFlverOnly)
                {
                    if (!ConversionContext.PortTaeOnly)
                    {
                        ConvertObjectHkx(sourceBnd, newBnd, true);
                    }

                    BND4 anibnd = BND4.Read(file.Bytes);
                    file = anibnd.Files.Find(x => x.Name.Contains(".tae"));
                    if (file != null)
                    {
                        ConvertObjectTae(newBnd, file);
                    }

                    if (!ConversionContext.PortTaeOnly)
                    {
                        newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                        _combinedObjbnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 400,
                            $"N:\\FDP\\data\\INTERROOT_win64\\obj\\" +
                            $"o{ConversionContext.PortedId[..2]}\\o{ConversionContext.PortedId}\\o{ConversionContext.PortedId}.anibnd",
                            newBnd.Write()));
                    }
                }

                foreach (BinderFile flver in sourceBnd.Files.Where(x => FLVER2.Is(x.Bytes)))
                {
                    ConvertFlver(_combinedObjbnd, flver);
                }

                WriteCombinedObjbnd();
            }
            else if (ConversionContext.CurrentContentSourceFileName.Contains("geomhkxbnd") && ConversionContext.SourceBndsType == ConversionContext.AssetType.Object)
            {
                if (ConversionContext.PortTaeOnly || ConversionContext.PortFlverOnly) return;
                ConvertObjectHkx(sourceBnd, newBnd, false);
                if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"o{ConversionContext.PortedId}_c.hkx")))
                {
                    sourceBnd.TransferBinderFile(newBnd, $"o{ConversionContext.SourceId}_c.clm2",
                        @"N:\FDP\data\INTERROOT_win64\obj\" +
                        $"o{ConversionContext.PortedId[..2]}\\o{ConversionContext.PortedId}\\o{ConversionContext.PortedId}_c.clm2");
                }

                _combinedObjbnd.Files.AddRange(newBnd.Files);
                WriteCombinedObjbnd();
            }
            else if (ConversionContext.CurrentContentSourceFileName.Contains("geombnd") && ConversionContext.SourceBndsType == ConversionContext.AssetType.MapPiece)
            {
                foreach (BinderFile flver in sourceBnd.Files.Where(x => FLVER2.Is(x.Bytes)))
                {
                    ConvertFlver(newBnd, flver);
                }

                if (ConversionContext.PortFlverOnly) return;

                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                newBnd.Write(
                    $"{ConversionContext.Cwd}\\m{ConversionContext.PortedId[..2]}_{ConversionContext.PortedId[2..4]}_{ConversionContext.PortedId[4..6]}_{ConversionContext.PortedId[6..8]}_{ConversionContext.PortedId[8..]}.mapbnd.dcx", 
                    new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
            }
        }
    }

    /// <summary>
    /// Finish conversion the combined anibnd. All the hkx should already be converted.
    /// </summary>
    private void ConvertCombinedAnibnd()
    {
        BND4 newBnd = new();
        
        newBnd.Files.AddRange(_combinedAnibnd.Files.Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)));
        
        BinderFile? file = _combinedAnibnd.Files.Find(x => x.Name.Contains(".tae"));
        if (file != null)
        {
            ConvertCharacterTae(_combinedAnibnd, newBnd, file);
        }

        if (!ConversionContext.PortTaeOnly)
        {
            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{ConversionContext.Cwd}\\c{ConversionContext.PortedId}.anibnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
    }

    /// <summary>
    /// Write the combined objbnd.
    /// </summary>
    private void WriteCombinedObjbnd()
    {
        if (ConversionContext.PortTaeOnly || ConversionContext.PortFlverOnly) return;
        
        string[] geombndNames = ConversionContext.ContentSourceFileNames.Where(x => x.Contains(".geombnd") || x.Contains(".geomhkxbnd")).ToArray();
        if (Array.IndexOf(geombndNames, ConversionContext.CurrentContentSourceFileName) == geombndNames.Length - 1)
        {
            _combinedObjbnd.Files = _combinedObjbnd.Files.OrderBy(x => x.ID).ToList();
            _combinedObjbnd.Write($"{ConversionContext.Cwd}\\o{ConversionContext.PortedId}.objbnd.dcx", new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9));
        }
    }

    /// <summary>
    /// Converts an Elden Ring character HKX file into a DS3 compatible HKX file.
    /// </summary>
	protected override void ConvertCharacterHkx(IBinder sourceBnd, BND4 newBnd)
    {
        if (ConversionContext.CurrentContentSourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = sourceBnd.Files
                .Find(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
            if (compendium == null)
            {
                newBnd.Files = sourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => PortHavok(x,$"{ConversionContext.Cwd}HavokDowngrade\\")).ToList();
            }
            else
            {
                newBnd.Files = sourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => PortHavok(x,$"{ConversionContext.Cwd}HavokDowngrade\\", compendium)).ToList();
            }
        }
        else
        {
            newBnd.Files = sourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{ConversionContext.Cwd}HavokDowngrade\\")).ToList();
        }

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{ConversionContext.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();
            
            if (name.EndsWith($"c{ConversionContext.SourceId}.hkx") || name.EndsWith($"c{ConversionContext.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(ConversionContext.SourceId, ConversionContext.PortedId)}";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
                hkx.ID = name.Contains("skeleton") ? 1000000 : int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(0, 2)}");
            }
        }
    }

    /// <summary>
    /// Converts an Elden Ring object HKX file into a DS3 compatible HKX file.
    /// </summary>
    protected override void ConvertObjectHkx(IBinder sourceBnd, BND4 newBnd, bool isInnerAnibnd)
    {
        if (ConversionContext.CurrentContentSourceFileName.Contains("geombnd"))
        {
            BinderFile? anibndFile = sourceBnd.Files.FirstOrDefault(x => x.Name.EndsWith("anibnd"));
            if (anibndFile != null)
            {
                BND4 anibnd = BND4.Read(anibndFile.Bytes);
                BinderFile? compendium = anibnd.Files
                    .Find(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
                if (compendium != null)
                {
                    newBnd.Files = anibnd.Files
                        .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                        .Where(x => PortHavok(x,$"{ConversionContext.Cwd}HavokDowngrade\\", compendium)).ToList();
                }
            }
        }
        else
        {
            newBnd.Files = sourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{ConversionContext.Cwd}HavokDowngrade\\")).ToList();
        }

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{ConversionContext.PortedId[..2]}\\o{ConversionContext.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (ConversionContext.CurrentContentSourceFileName.Contains("_c", StringComparison.OrdinalIgnoreCase))
            {
                hkx.Name = $"{path}o{ConversionContext.PortedId}_c.hkx";
            }
            else if (ConversionContext.CurrentContentSourceFileName.Contains("geomhkxbnd"))
            {
                hkx.Name = name.Contains("_1") ? $"{path}o{ConversionContext.PortedId}_1.hkx" : $"{path}o{ConversionContext.PortedId}.hkx";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
                hkx.ID = name.Contains("skeleton") ? 1000000 : int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(0, 2)}");
            }
        }
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, XmlData data)
    {
        byte[] paramBytes = ev.GetParameterBytes(bigEndian);
		
		switch (ev.Type)
        {
            // InvokeAttackBehavior
            case 1:
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeBulletBehavior
            case 2:
                Array.Clear(paramBytes, 17, 2);
                Array.Resize(ref paramBytes, 32);
                break;
            // SetWeaponStyle
            case 32:
                Array.Resize(ref paramBytes, 16);
                break;
            // SwitchWeapon
            case 33:
                Array.Resize(ref paramBytes, 16);
                break;
            // CastHighlightedMagic
            case 64:
                Array.Resize(ref paramBytes, 17);
                break;
            // AddSpEffect_Multiplayer
            case 66:
                Array.Resize(ref paramBytes, 16);
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // AddSpEffect
            case 67:
                Array.Resize(ref paramBytes, 16);
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // SpawnOneShotFFX_Ember
            case 95:
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnOneShotFFX
            case 96:
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnFFX_104
            case 104:
                ev.ForceChangeType(96);
                ev.Group.GroupType = 96;
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnFFX_General
            case 110:
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_CenterBody
            case 128:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_ByStateInfo
            case 129:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                Array.Clear(paramBytes, 18, 14);
                break;
            // PlaySound_Weapon
            case 132:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                Array.Clear(paramBytes, 8, 8);
                break;
            // Wwise_PlaySound_Unk133
            case 133:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                ev.ForceChangeType(128);
                ev.Group.GroupType = 128;
                Array.Clear(paramBytes, 8, 6);
                break;
            // Wwise_PlaySound_Unk134
            case 134:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                ev.ForceChangeType(128);
                ev.Group.GroupType = 128;
                Array.Clear(paramBytes, 8, 12);
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeDecalParamID_DummyPoly
            case 138:
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeRumbleCam_ByDummyPoly
            case 145:
                Array.Clear(paramBytes, 4, 2);
                Array.Resize(ref paramBytes, 16);
                break;
            // SetLockCamParam
            case 150:
                ev.ChangeLockOnParamId(bigEndian);
                break;
            // SetLockCamParam_Boss
            case 151:
                ev.ChangeLockOnParamId(bigEndian);
                break;
            // SetLockCamParam_Target (ER) > SetLockCamParam
            case 155:
                ev.ForceChangeType(150);
                Array.Clear(paramBytes, 2, 6);
                Array.Resize(ref paramBytes, 16);
                ev.ChangeLockOnParamId(bigEndian);
                break;
            // SetOpacityKeyframe
            case 193:
                Array.Clear(paramBytes, 8, 1);
                break;
            // SetTurnSpeed
            case 224:
                Array.Clear(paramBytes, 5, 1);
                Array.Resize(ref paramBytes, 16);
                break;
            // SetSPRegenRatePercent
            case 225:
                Array.Resize(ref paramBytes, 16);
                break;
            // SetKnockbackPercent
            case 226:
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnAISound
            case 237:
                Array.Resize(ref paramBytes, 16);
                break;
            // AddSpEffect_DragonForm
            case 302:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
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
                break;
            // SetSpecialLockOnParameter
            case 522:
                Array.Clear(paramBytes, 4, 12);
                break;
            // EnableBehaviorFlags
            case 600:
                Array.Resize(ref paramBytes, 16);
                break;
            // AdditiveAnimPlayback
            case 601:
                Array.Clear(paramBytes, 12, 4);
                break;
            // TestParam
            case 604:
                Array.Clear(paramBytes, 0, 12);
                break;
            // InvokeJiggleModifier
            case 606:
                Array.Clear(paramBytes, 1, 3);
                break;
            // BehaviorDataUnk700
            case 700:
                Array.Clear(paramBytes, 21, 3);
                Array.Resize(ref paramBytes, 52);
                break;
            // InvokeFixedRotationDirection
            case 703:
                Array.Resize(ref paramBytes, 16);
                break;
            // FacingAngleCorrection
            case 705:
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeChrTurnSpeed_ForLock
            case 706:
                Array.Clear(paramBytes, 4, 4);
                break;
            // StaggerModuleUnk
            case 714:
                Array.Clear(paramBytes, 4, 4);
                break;
            // OnlyForNon_c0000Enemies
            case 730:
                Array.Resize(ref paramBytes, 16);
                break;
            // RootMotionMultiplierEX
            case 760:
                Array.Resize(ref paramBytes, 32);
                break;
            // DisableDefaultWeaponTrail
            case 790:
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeDS3Poise
            case 795:
                Array.Resize(ref paramBytes, 16);
                break;
            // InvokeSfx?
            case 10096:
                Array.Clear(paramBytes, 12, 4);
                break;
            // PlaySound_WanderGhost
            case 10130:
                paramBytes = ev.ChangeSoundEventId(bigEndian);
                Array.Clear(paramBytes, 12, 4);
                break;
            // InvokeDebugDecal1
            case 10137:
                Array.Clear(paramBytes, 4, 12);
                break;
            // InvokeDebugDecal2
            case 10138:
                Array.Clear(paramBytes, 8, 8);
                break;
        }
		
		ev.SetParameterBytes(bigEndian, paramBytes);
		return ev;
    }
}