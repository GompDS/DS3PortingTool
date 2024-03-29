using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool.Converter;
public class SekiroConverter : Converter
{
    /// <summary>
    /// Converts a Sekiro HKX file into a DS3 compatible HKX file.
    /// </summary>
	protected override void ConvertCharacterHkx(BND4 newBnd, Options op)
    {
        if (op.CurrentSourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = op.CurrentSourceBnd.Files
                .Find(x => x.Name.Contains($"c{op.SourceId}.compendium"));
            if (compendium == null)
            {
                throw new FileNotFoundException("Source anibnd contains no compendium.");
            }
			
            newBnd.Files = op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
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

            if (name.Contains($"c{op.SourceId}.hkx") || name.Contains($"c{op.SourceId}_c.hkx"))
            {
                hkx.Name = $"{path}{name.Replace(op.SourceId, op.PortedId)}";
            }
            else
            {
                if (name.Contains("skeleton"))
                {
                    hkx.Name = $"{path}hkx\\{name}";
                }
                else
                {
                    if (name[1..].GetOffset() > 0)
                    {
                        string offset = name.Substring(1, 3);
                        char[] offsetArray = offset.ToCharArray();
                        Array.Reverse(offsetArray);
                        hkx.Name = $"{path}hkx\\a00{name[1..].GetOffset()}{name[4..]}";
                    }
                    else
                    {
                        hkx.Name = $"{path}hkx\\{name}";
                    }
                    hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
                }
            }
        }
    }

    // Need to figure this one out.
    protected override void ConvertObjectHkx(BND4 newBnd, Options op, bool isInnerAnibnd)
    {
        if (isInnerAnibnd)
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
                else
                {
                    newBnd.Files = anibnd.Files
                        .Where(x => x.Name.EndsWith(".hkx", StringComparison.OrdinalIgnoreCase))
                        .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\")).ToList();
                }
            }
        }
        else
        {
            newBnd.Files.AddRange(op.CurrentSourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => PortHavok(x,$"{op.Cwd}HavokDowngrade\\")).ToList());
        }

        foreach (BinderFile hkx in newBnd.Files)
        {
            string path = $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId[..2]}\\o{op.PortedId}\\";
            string name = Path.GetFileName(hkx.Name).ToLower();

            if (op.CurrentSourceFileName.Contains("_c", StringComparison.OrdinalIgnoreCase) && !isInnerAnibnd)
            {
                hkx.Name = $"{path}o{op.PortedId}_c.hkx";
            }
            else if (!isInnerAnibnd)
            {
                hkx.Name = name.Contains("_1") ? $"{path}o{op.PortedId}_1.hkx" : $"{path}o{op.PortedId}.hkx";
            }
            else
            {
                if (name.Contains("skeleton"))
                {
                    hkx.Name = $"{path}hkx\\{name}";
                }
                else
                {
                    if (name[1..].GetOffset() > 0)
                    {
                        string offset = name.Substring(1, 3);
                        char[] offsetArray = offset.ToCharArray();
                        Array.Reverse(offsetArray);
                        hkx.Name = $"{path}hkx\\a00{name[1..].GetOffset()}{name[4..]}";
                    }
                    else
                    {
                        hkx.Name = $"{path}hkx\\{name}";
                    }
                    hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
                }
            }
        }
    }

    /// <summary>
    /// Converts a Sekiro TAE file into a DS3 compatible TAE file.
    /// </summary>
    protected override void ConvertCharacterTae(BND4 newBnd, BinderFile taeFile, Options op)
    {
        TAE oldTae = TAE.Read(taeFile.Bytes);
        TAE newTae = new()
        {
            Format = TAE.TAEFormat.DS3,
            BigEndian = false,
            ID = 200000 + int.Parse(op.PortedId),
            Flags = new byte[] { 1, 0, 1, 2, 2, 1, 1, 1 },
            SkeletonName = "skeleton.hkt",
            SibName = $"c{op.PortedId}.sib",
            Animations = new List<TAE.Animation>(),
            EventBank = 21
        };

        XmlData data = new(op);

        data.ExcludedAnimations.AddRange(oldTae.Animations
            .Where(x => x.GetOffset() > 0 && data.ExcludedAnimations.Contains(x.GetNoOffsetId()))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x => 
            x.MiniHeader is TAE.Animation.AnimMiniHeader.Standard { ImportsHKX: true } standardHeader && 
            op.CurrentSourceBnd.Files.All(y => y.Name != "a00" + standardHeader.ImportHKXSourceAnimID.ToString("D3").GetOffset() +
                "_" + standardHeader.ImportHKXSourceAnimID.ToString("D9")[3..] + ".hkx"))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x =>
                x.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim otherHeader &&
                data.ExcludedAnimations.Contains(otherHeader.ImportFromAnimID))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.GetExcludedOffsetAnimations(op));

        newTae.Animations = oldTae.Animations
            .Where(x => !data.ExcludedAnimations.Contains(Convert.ToInt32(x.ID))).ToList();

        foreach (var anim in newTae.Animations)
        {
            anim.RemapImportAnimationId(data);
            
            if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardHeader && standardHeader.ImportsHKX)
            {
                string idString = standardHeader.ImportHKXSourceAnimID.ToString("D9");
                if (!idString.Substring(0, 1).Equals("0"))
                {
                    idString = idString.Remove(1, 2);
                    standardHeader.ImportHKXSourceAnimID = int.Parse(idString);
                }
            }
			
            if (data.AnimationRemapping.ContainsKey(anim.GetNoOffsetId()))
            {
                data.AnimationRemapping.TryGetValue(anim.GetNoOffsetId(), out int newAnimId);
                anim.SetAnimationProperties(newAnimId, anim.GetNoOffsetId(), anim.GetOffset(), op);
            }
            else
            {
                anim.SetAnimationProperties(anim.GetNoOffsetId(), anim.GetNoOffsetId(), anim.GetOffset(), op);
            }
			
            anim.Events = anim.Events.Where(ev => 
                    (!data.ExcludedEvents.Contains(ev.Type) || ev.IsAllowedSpEffect(newTae.BigEndian, data)) && 
                    !data.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) && 
                    !data.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
                .Select(ev => EditEvent(ev, newTae.BigEndian, op, data)).ToList();
        }
		
        if (op.ExcludedAnimOffsets.Any())
        {
            newTae.ShiftAnimationOffsets(op);
        }

        taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\tae\\c{op.PortedId}.tae",
            newTae.Write());
		
        if (op.PortTaeOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\c{op.PortedId}.tae", taeFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(taeFile);
        }
    }
    /// <summary>
	/// Edits parameters of a Sekiro event so that it will match with its DS3 event equivalent.
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
            // SpawnOneShotFFX
            case 96:
                Array.Resize(ref paramBytes, 16);
                break;
            // SpawnFFX_100_BB
            case 100:
                ev.Type = 96;
                ev.Group.GroupType = 96;
                paramBytes[13] = paramBytes[12];
                paramBytes[12] = 0;
                break;
            // PlaySound_CenterBody
            case 128:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // PlaySound_ByStateInfo
            case 129:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                switch (op.SourceBndsType)
                {
                    case Options.AssetType.Character:
                        Array.Clear(paramBytes, 18, 2);
                        break;
                    case Options.AssetType.Object:
                        Array.Clear(paramBytes, 12, 4);
                        Array.Resize(ref paramBytes, 32);
                        break;
                }
                break;
            // PlaySound_ByDummyPoly_PlayerVoice
            case 130:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                Array.Clear(paramBytes, 16, 2);
                Array.Resize(ref paramBytes, 32);
                break;
            // PlaySound_DummyPoly
            case 131:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                break;
            // SetLockCamParam_Boss
            case 151:
                Array.Clear(paramBytes, 4, 12);
                Array.Resize(ref paramBytes, 16);
                break;
            // SetOpacityKeyframe
            case 193:
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
            // InvokeChrClothState
            case 310:
                Array.Resize(ref paramBytes, 8);
                break;
            // AddSpEffect_Multiplayer_401
            case 401:
                Array.Clear(paramBytes, 8, 4);
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // EnableBehaviorFlags
            case 600:
                Array.Resize(ref paramBytes, 16);
                break;
            // AdditiveAnimPlayback
            case 601:
                Array.Clear(paramBytes, 12, 4);
                break;
            // BehaviorDataUnk700
            case 700:
                Array.Resize(ref paramBytes, 52);
                break;
            // FacingAngleCorrection
            case 705:
                Array.Clear(paramBytes, 8, 4);
                break;
            // CultCatchAttach
            case 720:
                Array.Clear(paramBytes, 1, 1);
                break;
            // OnlyForNon_c0000Enemies
            case 730:
                Array.Clear(paramBytes, 8, 4);
                break;
            // AddSpEffect_CultRitualCompletion
            case 797:
                paramBytes = ev.ChangeSpEffectId(bigEndian, data);
                break;
            // PlaySound_WanderGhost
            case 10130:
                paramBytes = ev.ChangeSoundEventId(bigEndian, op);
                Array.Clear(paramBytes, 12, 4);
                Array.Resize(ref paramBytes, 16);
                break;
        }
		
		ev.SetParameterBytes(bigEndian, paramBytes);
		return ev;
	}
}