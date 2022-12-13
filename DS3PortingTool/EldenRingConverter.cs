using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool;

public class EldenRingConverter : Converter
{
    /// <summary>
    /// All tae and hkx from any source anibnds will be combined into this singular anibnd which will be converted.
    /// </summary>
    private BND4 _combinedAnibnd = new();
    
    /// <summary>
    /// Performs the steps necessary to convert an Elden Ring binder into a DS3 compatible binder.
    /// </summary>
    public override void DoConversion(Options op)
    {
        BND4 newBnd = new();
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            if (!op.PortTaeOnly)
            {
                ConvertHkx(newBnd, op);
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
            ConvertTae(newBnd, file, op);
        }

        if (!op.PortTaeOnly)
        {
            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            newBnd.Write($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }

    /// <summary>
    /// Converts an Elden Ring HKX file into a DS3 compatible HKX file.
    /// </summary>
	protected override void ConvertHkx(BND4 newBnd, Options op)
    {
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = op.CurrentSourceBnd.Files
                .Find(x => x.Name.EndsWith(".compendium", StringComparison.OrdinalIgnoreCase));
            if (compendium == null)
            {
                newBnd.Files = op.CurrentSourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\")).ToList();
            }
            else
            {
                newBnd.Files = op.CurrentSourceBnd.Files
                    .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                    .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
            }
        }
        else
        {
            newBnd.Files = op.CurrentSourceBnd.Files
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
                if (name.Contains("skeleton"))
                {
                    hkx.ID = 1000000;
                }
                else
                {
                    hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(0, 2)}");
                }
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
                paramBytes = ev.ChangeSoundEventChrId(bigEndian, op);
                Array.Resize(ref paramBytes, 16);
                break;
            // PlaySound_ByStateInfo
            case 129:
                paramBytes = ev.ChangeSoundEventChrId(bigEndian, op);
                Array.Clear(paramBytes, 18, 14);
                break;
            // PlaySound_Weapon
            case 132:
                paramBytes = ev.ChangeSoundEventChrId(bigEndian, op);
                Array.Clear(paramBytes, 8, 8);
                break;
            // Wwise_PlaySound_Unk133
            case 133:
                paramBytes = ev.ChangeSoundEventChrId(bigEndian, op);
                ev.Type = 128;
                ev.Group.GroupType = 128;
                Array.Clear(paramBytes, 8, 6);
                break;
            // Wwise_PlaySound_Unk134
            case 134:
                paramBytes = ev.ChangeSoundEventChrId(bigEndian, op);
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