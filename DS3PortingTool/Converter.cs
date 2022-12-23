using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool;

public abstract class Converter
{
    /// <summary>
    /// Different types of flvers. Determines mtd for dummy materials.
    /// </summary>
    public enum FlverType
    {
        Character,
        Asset
    }
    
    /// <summary>
    /// Performs the steps necessary to convert a foreign binder into a DS3 compatible binder.
    /// </summary>
    public virtual void DoConversion(Options op)
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
    }
    /// <summary>
    /// Converts a foreign character HKX file into a DS3 compatible HKX file.
    /// </summary>
    protected abstract void ConvertCharacterHkx(BND4 newBnd, Options op);
    /// <summary>
    /// Converts a foreign object HKX file into a DS3 compatible HKX file.
    /// </summary>
    protected abstract void ConvertObjectHkx(BND4 newBnd, Options op);
    /// <summary>
    /// Converts a foreign character TAE file into a DS3 compatible TAE file.
    /// </summary>
    protected virtual void ConvertCharacterTae(BND4 newBnd, BinderFile taeFile, Options op)
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

        foreach (TAE.Animation? anim in newTae.Animations)
        {
            anim.RemapImportAnimationId(data);
			
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

        oldTae.Animations = oldTae.Animations.OrderBy(x => x.ID).ToList();
        newTae.Animations = newTae.Animations.OrderBy(x => x.ID).ToList();
        
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
    /// Converts a foreign object TAE file into a DS3 compatible TAE file.
    /// </summary>
    protected virtual void ConvertObjectTae(BND4 newBnd, BinderFile taeFile, Options op)
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
            Animations = oldTae.Animations,
            EventBank = 19
        };
        
        XmlData data = new(op);

        foreach (TAE.Animation? anim in newTae.Animations)
        {
            anim.SetAnimationProperties(anim.GetNoOffsetId(), anim.GetNoOffsetId(), anim.GetOffset(), op);
			
            anim.Events = anim.Events.Where(ev => 
                    (!data.ExcludedEvents.Contains(ev.Type) || ev.IsAllowedSpEffect(newTae.BigEndian, data)) && 
                    !data.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) && 
                    !data.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
                .Select(ev => EditEvent(ev, newTae.BigEndian, op, data)).ToList();
            
        }
        
        newTae.Animations = newTae.Animations.OrderBy(x => x.ID).ToList();
        
        taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
            $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId[..2]}\\o{op.PortedId}\\tae\\o{op.PortedId}.tae",
            newTae.Write());
		
        if (op.PortTaeOnly)
        {
            File.WriteAllBytes($"{op.Cwd}\\o{op.PortedId}.tae", taeFile.Bytes);
        }
        else
        {
            newBnd.Files.Add(taeFile);
        }
    }

    /// <summary>
    /// Edits parameters of the event so that it will match with its DS3 event equivalent.
    /// </summary>
    protected abstract TAE.Event EditEvent(TAE.Event ev, bool bigEndian, Options op, XmlData data);
    /// <summary>
    /// Converts a foreign FLVER file into a DS3 compatible FLVER file.
    /// </summary>
    protected void ConvertFlver(BND4 newBnd, BinderFile flverFile, Options op)
    {
        XmlData data = new(op);

        FLVER2 oldFlver = FLVER2.Read(flverFile.Bytes);
        FLVER2 newFlver = CreateDs3Flver(oldFlver, data, op);

        List<FLVER2.Material> distinctMaterials = newFlver.Materials.DistinctBy(x => x.MTD).ToList();
        foreach (var distinctMat in distinctMaterials)
        {
            FLVER2.GXList gxList = new FLVER2.GXList();
            gxList.AddRange(data.MaterialInfoBank
                .GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));

            if (newFlver.IsNewGxList(gxList))
            {
                newFlver.GXLists.Add(gxList);
            }
			
            foreach (var mat in newFlver.Materials.Where(x => x.MTD.Equals(distinctMat.MTD)))
            {
                mat.GXIndex = newFlver.GXLists.Count - 1;
            }
        }

        foreach (var mesh in newFlver.Meshes)
        {
            FLVER2MaterialInfoBank.MaterialDef matDef = data.MaterialInfoBank.MaterialDefs.Values
                .First(x => x.MTD.Equals(
                    $"{Path.GetFileName(newFlver.Materials[mesh.MaterialIndex].MTD).ToLower()}"));

            List<FLVER2.BufferLayout> bufferLayouts = matDef.AcceptableVertexBufferDeclarations[0].Buffers;

            // DS3 does not keep track of bone indices in each mesh.
            mesh.BoneIndices.Clear();
			
            mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
            List<int> layoutIndices = newFlver.GetLayoutIndices(bufferLayouts);
            mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();

        }

        if (op.SourceBndsType == Options.AssetType.Character)
        {
            flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
                $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedId}\\c{op.PortedId}.flver",
                newFlver.Write());
        }
        else if (op.SourceBndsType == Options.AssetType.Object)
        {
            if (flverFile.Name.EndsWith("_1.flver", StringComparison.OrdinalIgnoreCase))
            {
                flverFile = new BinderFile(Binder.FileFlags.Flag1, 201,
                    $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId.Substring(0, 2)}\\o{op.PortedId}\\o{op.PortedId}_1.flver",
                    newFlver.Write());
            }
            else
            {
                flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
                    $"N:\\FDP\\data\\INTERROOT_win64\\obj\\o{op.PortedId.Substring(0, 2)}\\o{op.PortedId}\\o{op.PortedId}.flver",
                    newFlver.Write());
            }
        }
        
        newBnd.Files.Add(flverFile);
    }

    /// <summary>
    /// Creates a new DS3 FLVER using data from a foreign FLVER.
    /// </summary>
    protected virtual FLVER2 CreateDs3Flver(FLVER2 sourceFlver, XmlData data, Options op)
    {
        FLVER2 newFlver = new FLVER2
        {
            Header = new FLVER2.FLVERHeader
            {
                BoundingBoxMin = sourceFlver.Header.BoundingBoxMin,
                BoundingBoxMax = sourceFlver.Header.BoundingBoxMax,
                Unicode = sourceFlver.Header.Unicode,
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
}