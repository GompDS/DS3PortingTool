using DS3PortingTool.Util;
using SoulsAssetPipeline.Animation;
using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool;

public abstract class Converter
{
    public virtual void DoConversion(Options op)
    {
        BND4 newBnd = new();
        if (op.SourceFileName.Contains("anibnd"))
        {
            if (!op.PortTaeOnly)
            {
                ConvertHkx(newBnd, op);
            }
            
            BinderFile? file = op.SourceBnd.Files.Find(x => x.Name.Contains(".tae"));
            if (file != null)
            {
                ConvertTae(newBnd, file, op);
            }

            if (!op.PortTaeOnly)
            {
                newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
                File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx",
                    DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
            }
        }
        else if (op.SourceFileName.Contains("chrbnd"))
        {
            ConvertHkx(newBnd, op);

            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedChrId}.hkx")))
            {
                op.SourceBnd.TransferBinderFile(newBnd, $"c{op.SourceChrId}.hkxpwv",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedChrId}\\c{op.PortedChrId}.hkxpwv");
            }
		
            if (newBnd.Files.Any(x => x.Name.ToLower().Contains($"c{op.PortedChrId}_c.hkx")))
            {
                op.SourceBnd.TransferBinderFile(newBnd, $"c{op.SourceChrId}_c.clm2",  
                    @"N:\FDP\data\INTERROOT_win64\chr\" + $"c{op.PortedChrId}\\c{op.PortedChrId}_c.clm2");
            }
            
            BinderFile? file = op.SourceBnd.Files.Find(x => x.Name.Contains(".flver"));
            if (file != null)
            {
                ConvertFlver(newBnd, file, op);
            }

            newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
            File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.chrbnd.dcx",
                DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
        }
    }

    protected abstract void ConvertHkx(BND4 newBnd, Options op);
    protected void ConvertTae(BND4 newBnd, BinderFile taeFile, Options op)
    {
        TAE oldTae = TAE.Read(taeFile.Bytes);
        TAE newTae = new TAE
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

        XmlData data = new XmlData(op);
        
        data.ExcludedAnimations.AddRange(newTae.Animations
            .Where(x => x.GetOffset() > 0 && data.ExcludedAnimations.Contains(x.GetNoOffsetId()))
            .Select(x => Convert.ToInt32(x.ID)));
		
        data.ExcludedAnimations.AddRange(newTae.Animations.Where(x => 
            x.MiniHeader is TAE.Animation.AnimMiniHeader.Standard { ImportsHKX: true } standardHeader && 
            op.SourceBnd.Files.All(y => y.Name != "a" + standardHeader.ImportHKXSourceAnimID.ToString("D9")
                .Insert(3, "_") + ".hkx")).Select(x => Convert.ToInt32(x.ID)));
		
        data.ExcludedAnimations.AddRange(newTae.Animations.Where(x =>
                x.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim otherHeader &&
                data.ExcludedAnimations.Contains(otherHeader.ImportFromAnimID))
            .Select(x => Convert.ToInt32(x.ID)));
		
        data.ExcludedAnimations.AddRange(newTae.GetExcludedOffsetAnimations(op));

        newTae.Animations = oldTae.Animations
            .Where(x => !data.ExcludedAnimations.Contains(Convert.ToInt32(x.ID))).ToList();

        foreach (var anim in newTae.Animations)
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

        flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
            $"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\c{op.PortedChrId}.flver",
            newFlver.Write());
        newBnd.Files.Add(flverFile);
    }

    protected virtual FLVER2 CreateDs3Flver(FLVER2 sourceFlver, XmlData data, Options op)
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
            Materials = sourceFlver.Materials.Select(x => x.ToDummyDs3Material(data.MaterialInfoBank)).ToList(),
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