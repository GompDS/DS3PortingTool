using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
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
    
    /// <summary>
    /// Performs the steps necessary to convert an Elden Ring binder into a DS3 compatible binder.
    /// </summary>
    public override void DoConversion(Options op)
    {
        BND4 newBnd = new();
        if (op.CurrentSourceFileName.Contains("anibnd") && op.SourceBndsType == Options.AssetType.Character)
        {
            if (!op.PortTaeOnly)
            {
                ConvertCharacterHkx(newBnd, op);
                _combinedAnibnd.Files.AddRange(newBnd.Files);
            }
            
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                _combinedAnibnd.Files.Add(file);
            }

            string[] anibndNames = op.SourceFileNames.Where(x => x.Contains(".anibnd")).ToArray();
            if (Array.IndexOf(anibndNames, op.CurrentSourceFileName) == anibndNames.Length - 1)
            {
                ConvertCombinedAnibnd(op);
            }
        }
        else if (op.CurrentSourceFileName.Contains("chrbnd") && op.SourceBndsType == Options.AssetType.Character)
        {
            if (!op.PortFlverOnly)
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
            }

            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }
            
            if (op.PortFlverOnly) return;

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedId}.chrbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
        else if (op.CurrentSourceFileName.Contains("geombnd") && op.SourceBndsType == Options.AssetType.Object)
        {
            BinderFile? file = op.CurrentSourceBnd.Files.Find(x => x.Name.EndsWith(".anibnd"));
            if (file != null && !op.PortFlverOnly)
            {
                if (!op.PortTaeOnly)
                {
                    ConvertObjectHkx(newBnd, op, true);
                }

                BND4 anibnd = BND4.Read(file.Bytes);
                file = anibnd.Files.Find(x => x.Name.Contains(".tae"));
                if (file != null)
                {
                    ConvertObjectTae(newBnd, file, op);
                }

                if (!op.PortTaeOnly)
                {
                    newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                    _combinedObjbnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 400,
                        $"N:\\FDP\\data\\INTERROOT_win64\\obj\\" +
                        $"o{op.PortedId[..2]}\\o{op.PortedId}\\o{op.PortedId}.anibnd",
                        newBnd.Write()));
                }
            }

            foreach (BinderFile flver in op.CurrentSourceBnd.Files.Where(x => FLVER2.Is(x.Bytes)))
            {
                ConvertFlver(_combinedObjbnd, flver, op);
            }

            WriteCombinedObjbnd(op);
        }
        else if (op.CurrentSourceFileName.Contains("geomhkxbnd") && op.SourceBndsType == Options.AssetType.Object)
        {
            if (op.PortTaeOnly || op.PortFlverOnly) return;
            ConvertObjectHkx(newBnd, op, false);
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"o{op.PortedId}_c.hkx")))
            {
                op.CurrentSourceBnd.TransferBinderFile(newBnd, $"o{op.SourceId}_c.clm2",
                    @"N:\FDP\data\INTERROOT_win64\obj\" +
                    $"o{op.PortedId[..2]}\\o{op.PortedId}\\o{op.PortedId}_c.clm2");
            }

            _combinedObjbnd.Files.AddRange(newBnd.Files);
            WriteCombinedObjbnd(op);
        }
    }

    /// <summary>
    /// Finish conversion the combined anibnd. All the hkx should already be converted.
    /// </summary>
    private void ConvertCombinedAnibnd(Options op)
    {
        BND4 newBnd = new();
        
        newBnd.Files.AddRange(_combinedAnibnd.Files.Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase)));
        
        BinderFile? file = _combinedAnibnd.Files.Find(x => x.Name.Contains(".tae"));
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

    /// <summary>
    /// Write the combined objbnd.
    /// </summary>
    private void WriteCombinedObjbnd(Options op)
    {
        if (op.PortTaeOnly || op.PortFlverOnly) return;
        
        string[] geombndNames = op.SourceFileNames.Where(x => x.Contains(".geombnd") || x.Contains(".geomhkxbnd")).ToArray();
        if (Array.IndexOf(geombndNames, op.CurrentSourceFileName) == geombndNames.Length - 1)
        {
            _combinedObjbnd.Files = _combinedObjbnd.Files.OrderBy(x => x.ID).ToList();
            _combinedObjbnd.Write($"{op.Cwd}\\o{op.PortedId}.objbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }

    /// <summary>
    /// Converts an Elden Ring character HKX file into a DS3 compatible HKX file.
    /// </summary>
	protected override void ConvertCharacterHkx(BND4 newBnd, Options op)
    {
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = op.CurrentSourceBnd.Files
                .Find(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
            if (compendium == null)
            {
                newBnd.Files = op.CurrentSourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\")).ToList();
            }
            else
            {
                newBnd.Files = op.CurrentSourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
            }
        }
        else
        {
            newBnd.Files = op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\")).ToList();
        }

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();
            
            if (name.EndsWith($"c{op.SourceId}.hkx") || name.EndsWith($"c{op.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceId, op.PortedId)}";
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
    protected override void ConvertObjectHkx(BND4 newBnd, Options op, bool isInnerAnibnd)
    {
        if (op.CurrentSourceFileName.Contains("geombnd"))
        {
            BinderFile? anibndFile = op.CurrentSourceBnd.Files.FirstOrDefault(x => x.Name.EndsWith("anibnd"));
            if (anibndFile != null)
            {
                BND4 anibnd = BND4.Read(anibndFile.Bytes);
                BinderFile? compendium = anibnd.Files
                    .Find(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
                if (compendium != null)
                {
                    newBnd.Files = anibnd.Files
                        .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                        .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
                }
            }
        }
        else
        {
            newBnd.Files = op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\")).ToList();
        }

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId[..2]}\\o{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (op.CurrentSourceFileName.Contains("_c", StringComparison.OrdinalIgnoreCase))
            {
                hkx.Name = $"{path}o{op.PortedId}_c.hkx";
            }
            else if (op.CurrentSourceFileName.Contains("geomhkxbnd"))
            {
                hkx.Name = name.Contains("_1") ? $"{path}o{op.PortedId}_1.hkx" : $"{path}o{op.PortedId}.hkx";
            }
            else
            {
                hkx.Name = $"{path}hkx\\{name}";
                hkx.ID = name.Contains("skeleton") ? 1000000 : int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(0, 2)}");
            }
        }
    }

    protected override TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data)
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
                ev.Type = 96;
                ev.Group.GroupType = 96;
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnFFX_General
            case 110:
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_CenterBody
            case 128:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_ByStateInfo
            case 129:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                Array.Clear(paramBytes, 18, 14);
                break;
            // PlaySound_Weapon
            case 132:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                Array.Clear(paramBytes, 8, 8);
                break;
            // Wwise_PlaySound_Unk133
            case 133:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                ev.Type = 128;
                ev.Group.GroupType = 128;
                Array.Clear(paramBytes, 8, 6);
                break;
            // Wwise_PlaySound_Unk134
            case 134:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                ev.Type = 128;
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
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
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