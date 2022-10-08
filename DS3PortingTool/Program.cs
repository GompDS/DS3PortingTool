using SoulsFormats;
using SoulsAssetPipeline.Animation;
using SoulsAssetPipeline.FLVERImporting;
using DS3PortingTool.Util;

namespace DS3PortingTool
{
	class Program
	{
		public static void Main(string[] args)
		{
			Options op = new Options(args);
			BND4 newBnd = new BND4();
			if (op.SourceFileName.Contains("anibnd"))
			{
				if (!op.PortTaeOnly)
				{
					ConvertHkx(newBnd, op);
				}

				ConvertTae(newBnd, op);
				
				if (!op.PortTaeOnly)
				{
					File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx",
						DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
				}
			}
			else if (op.SourceFileName.Contains("chrbnd"))
			{
				ConvertHkx(newBnd, op);

				ConvertFlver(newBnd, op);
				
				newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
				File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.chrbnd.dcx",
					DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
			}
		}

		/// <summary>
		/// Convert the flver from the source bnd into a DS3 flver.
		/// </summary>
		static void ConvertFlver(BND4 newBnd, Options op)
		{
			XmlData data = new(op);

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
			if (file == null)
			{
				throw new FileNotFoundException("Source chrbnd contains no flver.");
			}

			FLVER2 oldFlver = FLVER2.Read(file.Bytes);
			FLVER2 newFlver = new FLVER2
			{
				Header = new FLVER2.FLVERHeader
				{
					BoundingBoxMin = oldFlver.Header.BoundingBoxMin,
					BoundingBoxMax = oldFlver.Header.BoundingBoxMax,
					Unk4A = oldFlver.Header.Unk4A,
					Unk4C = oldFlver.Header.Unk4C,
					Unk5C = oldFlver.Header.Unk5C,
					Unk5D = oldFlver.Header.Unk5D,
					Unk68 = oldFlver.Header.Unk68
				},
				Dummies = oldFlver.Dummies,
				Materials = oldFlver.Materials.Select(x => x.ToDummyDs3Material(data.MaterialInfoBank)).ToList(),
				Bones = oldFlver.Bones.Select(x =>
				{
					// Unk3C should only be 0 or 1 in DS3.
					if (x.Unk3C > 1)
					{
						x.Unk3C = 0;
					}

					return x;
				}).ToList(),
				Meshes = oldFlver.Meshes
			};

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
			
			BinderFile flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
				$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\c{op.PortedChrId}.flver",
				newFlver.Write());
			newBnd.Files.Add(flverFile);
		}
		
		/// <summary>
		/// Convert all HKX files from the source bnd into DS3 HKX files.
		/// </summary>
		static void ConvertHkx(BND4 newBnd, Options op)
		{
			if (op.SourceFileName.Contains("anibnd"))
			{
				if (op.Game.Type is Game.GameTypes.Sekiro or Game.GameTypes.EldenRing)
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
			}
			else if (op.Game.Type != Game.GameTypes.EldenRing)
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
					hkx.Name = $"{path}hkx\\{name}";
					if (!name.Contains("skeleton"))
					{
						hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
					}
				}
			}
		}

		/// <summary>
		/// Convert the tae from the source bnd into a DS3 tae.
		/// </summary>
		static void ConvertTae(BND4 newBnd, Options op)
		{
			XmlData data = new XmlData(op);
			
			BinderFile? file = op.SourceBnd.Files.Find(x => x.Name.Contains(".tae"));
			if (file == null)
			{
				throw new FileNotFoundException("Source anibnd contains no tae.");
			}

			TAE oldTae = TAE.Read(file.Bytes);
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

			data.ExcludedAnimations.AddRange(oldTae.Animations
				.Where(x => x.GetOffset() > 0 && data.ExcludedAnimations.Contains(x.GetNoOffsetId()))
				.Select(x => Convert.ToInt32(x.ID)));
			
			data.ExcludedAnimations.AddRange(oldTae.Animations.Where(x => 
					x.MiniHeader is TAE.Animation.AnimMiniHeader.Standard { ImportsHKX: true } standardHeader && 
					op.SourceBnd.Files.All(y => y.Name != "a" + standardHeader.ImportHKXSourceAnimID.ToString("D9")
						.Insert(3, "_") + ".hkx")).Select(x => Convert.ToInt32(x.ID)));
			
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

			BinderFile taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
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
}
