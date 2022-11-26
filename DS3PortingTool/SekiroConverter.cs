using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool;
public class SekiroConverter : Converter
{
	protected override void ConvertHkx(BND4 newBnd, Options op)
    {
        if (op.SourceFileName.Contains("anibnd"))
        {
            BinderFile? compendium = op.SourceBnd.Files
                .Find(x => x.Name.Contains($"c{op.SourceChrId}.compendium"));
            if (compendium == null)
            {
                throw new FileNotFoundException("Source anibnd contains no compendium.");
            }
			
            newBnd.Files = op.SourceBnd.Files
                .Where(x => Path.GetExtension(x.Name).ToLower().Equals(".hkx"))
                .Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
        }
        else
        {
            newBnd.Files = op.SourceBnd.Files
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
    
    protected override void ConvertTae(BND4 newBnd, BinderFile taeFile, Options op)
    {
        TAE oldTae = TAE.Read(taeFile.Bytes);
        TAE newTae = new()
        {
            Format = TAE.TAEFormat.DS3,
            BigEndian = false,
            ID = 200000 + int.Parse(op.PortedChrId),
            Flags = new byte[] { 1, 0, 1, 2, 2, 1, 1, 1 },
            SkeletonName = "skeleton.hkt",
            SibName = $"c{op.PortedChrId}.sib",
            Animations = new List<TAE.Animation>(),
            EventBank = 21
        };

        XmlData data = new(op);

        data.ExcludedAnimations.AddRange(oldTae.Animations
            .Where(x => x.GetOffset() > 0 && data.ExcludedAnimations.Contains(x.GetNoOffsetId()))
            .Select(x => Convert.ToInt32(x.ID)));

        data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x => 
            x.MiniHeader is TAE.Animation.AnimMiniHeader.Standard { ImportsHKX: true } standardHeader && 
            op.SourceBnd.Files.All(y => y.Name != "a00" + standardHeader.ImportHKXSourceAnimID.ToString("D3").GetOffset() +
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
                    !data.ExcludedEvents.Contains(ev.Type) && 
                    !data.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) && 
                    !data.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
                .Select(ev => ev.Edit(newTae.BigEndian, op)).ToList();
        }
		
        if (op.ExcludedAnimOffsets.Any())
        {
            newTae.ShiftAnimationOffsets(op);
        }

        taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\tae\\c{op.PortedChrId}.tae",
            newTae.Write());
		
        if (op.PortTaeOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.tae", taeFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(taeFile);
        }
    }
}