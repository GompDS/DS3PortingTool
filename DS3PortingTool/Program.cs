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
			XmlData xd = new XmlData(op);
			BND4 newBnd = new BND4();
			if (op.SourceFileName.Contains("anibnd"))
			{
				// Downgrade HKX files
				if (op.PortTaeOnly == false)
				{
					BinderFile? compendium = op.SourceBnd.Files
						.Find(x => x.Name.Contains($"c{op.SourceChrId}.compendium"));
					if (compendium == null)
					{
						throw new FileNotFoundException("Source anibnd contains no compendium.");
					}
					newBnd.Files = op.SourceBnd.Files
						.Where(x => x.Name.IndexOf(".hkx", StringComparison.OrdinalIgnoreCase) >= 0)
						.Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\", compendium)).ToList();
					foreach (BinderFile hkx in newBnd.Files)
					{
						if (hkx.Name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) < 0)
						{
							hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
						}

						hkx.Name = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\" + 
						           $"c{op.PortedChrId}\\hkx\\{Path.GetFileName(hkx.Name)}";
					}
				}
				
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

				// Exclude animations which import an HKX that doesn't exist.
				List<int> excludedImportHkxAnimations = oldTae.Animations.Where(anim =>
					anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardHeader &&
					standardHeader.ImportsHKX && op.SourceBnd.Files.All(x =>
						x.Name != "a" + standardHeader.ImportHKXSourceAnimID.ToString("D9")
							.Insert(3, "_") + ".hkx"))
								.Select(x => Convert.ToInt32(x.ID)).ToList();

				// Exclude animations which import all from an animation that's excluded.
				List<int> excludedImportAllAnimations = oldTae.Animations.Where(anim =>
					anim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim otherHeader &&
					(xd.ExcludedAnimations.Contains(otherHeader.ImportFromAnimID) ||
					 excludedImportHkxAnimations.Contains(otherHeader.ImportFromAnimID)))
						.Select(x => Convert.ToInt32(x.ID)).ToList();

				// Remove animations from excluded offsets.
				List<int> excludedOffsetAnimations = new();
				if (op.ExcludedAnimOffsets.Any())
				{
					foreach (int offsetId in op.ExcludedAnimOffsets.Where(x => x > 0))
					{
						excludedOffsetAnimations.AddRange(oldTae.Animations.Where(y =>
								y.ID >= offsetId * 100000000 && y.ID < (offsetId + 1) * 100000000)
							.Select(y => Convert.ToInt32(y.ID)).ToList());
					}

					if (op.ExcludedAnimOffsets.Contains(0))
					{
						int nextAllowedOffset = 1;
						while (op.ExcludedAnimOffsets.Contains(nextAllowedOffset))
						{
							nextAllowedOffset++;
						}
						nextAllowedOffset *= 100000000;
						foreach (var anim in oldTae.Animations.Where(x => x.ID < 1000000))
						{
							if (oldTae.Animations.FindIndex(x => x.ID == anim.ID + 
								nextAllowedOffset) >= 0 || (anim.ID >= 3000 && anim.ID < 4000))
							{
								excludedOffsetAnimations.Add(Convert.ToInt32(anim.ID));
							}
						}
					}
				}

				// Remove excluded animations from the list and remap animation ids.
				int oldOffset = 0;
				int newOffset = 0;
				newTae.Animations = oldTae.Animations
					.Where(x => !xd.ExcludedAnimations.Contains(x.GetNoOffsetId()) && 
					            !excludedImportHkxAnimations.Contains(Convert.ToInt32(x.ID)) &&
					            !excludedImportAllAnimations.Contains(Convert.ToInt32(x.ID)) &&
					            !excludedOffsetAnimations.Contains(Convert.ToInt32(x.ID)))
					.Select(x => 
					{
						// Animation id remapping for animations that import all from another.
						if (x.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim importMiniHeader)
						{
							string iDString = Convert.ToString(importMiniHeader.ImportFromAnimID);
							for (int i = iDString.Length; i < 9; i++)
							{
								iDString = iDString.Insert(0, "0");
							}

							iDString = iDString.Substring(3);
							int importId = Int32.Parse(iDString);
							if (xd.AnimationRemapping.ContainsKey(importId))
							{
								xd.AnimationRemapping.TryGetValue(importId, out int newImportId);
								importMiniHeader.ImportFromAnimID = newImportId + x.GetOffset();
							}
							else
							{
								importMiniHeader.ImportFromAnimID = importId + x.GetOffset();
							}
						}

						// Remap animation ids.
						if (xd.AnimationRemapping.ContainsKey(x.GetNoOffsetId()))
						{
							xd.AnimationRemapping.TryGetValue(x.GetNoOffsetId(), out int newAnimId);
							x.SetAnimationProperties(newAnimId, 
								x.GetNoOffsetId(), x.GetOffset());
						}
						else
						{
							x.SetAnimationProperties(x.GetNoOffsetId(), 
								x.GetNoOffsetId(), x.GetOffset());
						}

						return x;
					}).Select(x =>
					{
						// Shift animation offsets to fill in gaps when offsets are removed.
						if (op.ExcludedAnimOffsets.Contains(0))
						{
							int nextAllowedOffset = 1;
							while (op.ExcludedAnimOffsets.Contains(nextAllowedOffset))
							{
								nextAllowedOffset++;
							}
							nextAllowedOffset *= 1000000;
							if (x.ID >= nextAllowedOffset && x.ID < nextAllowedOffset + 1000000)
							{
								x.ID -= nextAllowedOffset;
							}
						}
						if (x.GetOffset() > oldOffset)
						{
							oldOffset = x.GetOffset();
							newOffset += 1000000;
						}

						if (newOffset < oldOffset)
						{
							x.ID = newOffset + x.GetNoOffsetId();
						}
						return x;
					}).OrderBy(x => x.ID).ToList();

				newTae.Animations = newTae.Animations.Select(anim =>
				{
					anim.Events = anim.Events.Where(ev =>
						!xd.ExcludedEvents.Contains(ev.Type) &&
						!xd.ExcludedJumpTables.Contains(ev.GetJumpTableId(newTae.BigEndian)) &&
						!xd.ExcludedRumbleCams.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
						.Select(ev =>
					{
						byte[] paramBytes = ev.GetParameterBytes(newTae.BigEndian);
						switch (ev.Type)
						{
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
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, op.PortedChrId);
								break;
							// PlaySound_ByStateInfo
							case 129:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, op.PortedChrId);
								Array.Clear(paramBytes, 18, 2);
								break;
							// PlaySound_ByDummyPoly_PlayerVoice
							case 130:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, op.PortedChrId);
								Array.Clear(paramBytes, 16, 2);
								Array.Resize(ref paramBytes, 32);
								break;
							// PlaySound_DummyPoly
							case 131:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, op.PortedChrId);
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
							// InvokeChrClothState
							case 310:
								Array.Resize(ref paramBytes, 8);
								break;
							// AddSpEffect_Multiplayer_401
							case 401:
								Array.Clear(paramBytes, 8, 4);
								break;
							// EnableBehaviorFlags
							case 600:
								Array.Resize(ref paramBytes, 16);
								break;
							// AdditiveAnimPlayback
							case 601:
								Array.Clear(paramBytes, 12, 4);
								break;
							// 
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
							// PlaySound_WanderGhost
							case 10130:
								Array.Clear(paramBytes, 12, 4);
								Array.Resize(ref paramBytes, 16);
								break;
						}
						
						ev.SetParameterBytes(newTae.BigEndian, paramBytes);
						return ev;
					}).ToList();
					return anim;
				}).ToList();

				// Convert tae to binderFile and add it to the new bnd.
				BinderFile taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
					$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\tae\\c{op.PortedChrId}.tae",
					newTae.Write());
				
				// Write tae to disc and skip writing the binder if PortTaeOnly is enabled.
				if (op.PortTaeOnly)
				{
					File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.tae", taeFile.Bytes);
				}
				else
				{
					newBnd.Files.Add(taeFile);
				}

				if (op.PortTaeOnly == false)
				{
					// Compress the new binder.
					File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.anibnd.dcx",
						DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
				}
			}
			else if (op.SourceFileName.Contains("chrbnd"))
			{
				// Downgrade HKX files.
				newBnd.Files = op.SourceBnd.Files.Where(x =>
					x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", 
						StringComparison.OrdinalIgnoreCase) >= 0)
					.Where(x => x.Downgrade($"{op.Cwd}HavokDowngrade\\")).Select(x =>
					{
						x.Name =
							@"N:\FDP\data\INTERROOT_win64\chr\" + 
							$"c{op.PortedChrId}\\{Path.GetFileName(x.Name).Replace(op.SourceChrId, op.PortedChrId)}";
						return x;
					}).ToList();

				// Add hkxpwv amd clm2.
				newBnd.Files.AddRange(op.SourceBnd.Files
					.Where(x =>
						x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", 
							StringComparison.OrdinalIgnoreCase) < 0)
					.Where(x =>
						(newBnd.Files.Any(y =>
							 y.Name.IndexOf($"c{op.PortedChrId}.hkx", StringComparison.OrdinalIgnoreCase) >= 0) &&
						 x.Name.IndexOf($"c{op.SourceChrId}.hkxpwv", StringComparison.OrdinalIgnoreCase) >= 0)
						|| (newBnd.Files.Any(y =>
							    y.Name.IndexOf($"c{op.PortedChrId}_c.hkx", 
								    StringComparison.OrdinalIgnoreCase) >= 0) &&
						    x.Name.IndexOf($"c{op.SourceChrId}_c.clm2", StringComparison.OrdinalIgnoreCase) >= 0))
					.Select(
						x =>
						{
							x.Name =
								@"N:\FDP\data\INTERROOT_win64\chr\" +
								$"c{op.PortedChrId}\\{Path.GetFileName(x.Name).Replace(op.SourceChrId, op.PortedChrId)}";
							return x;
						}).ToList());
				// Flver File
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
			        Materials = oldFlver.Materials.Select(x => x.ToDummyDs3Material(xd.MaterialInfoBank)).ToList(),
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
				
				// Import MaterialInfoBank which contains data for creating new materials.
				FLVER2MaterialInfoBank materialInfoBank = FLVER2MaterialInfoBank
					.ReadFromXML($"{AppDomain.CurrentDomain.BaseDirectory}Res\\BankDS3.xml");
				
			    List<FLVER2.Material> distinctMaterials = newFlver.Materials
				    .DistinctBy(x => x.MTD).ToList();
			    foreach (var distinctMat in distinctMaterials)
			    {
			        // GXLists
			        FLVER2.GXList gxList = new FLVER2.GXList();
			        gxList.AddRange(materialInfoBank
				        .GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));
			        // Check if GXList is new.
			        bool isNewGxList = true;
			        foreach (var gxl in newFlver.GXLists)
			        {
			            if (gxl.Count == gxList.Count)
			            {
			                for (int i = 0; i < gxl.Count; i++)
			                {
			                    if (gxl[i].Data.Length == gxList[i].Data.Length &&
			                        gxl[i].Unk04 == gxList[i].Unk04 && gxl[i].ID.Equals(gxList[i].ID))
			                    {
			                        isNewGxList = false;
			                    }
			                }
			            }
			        }
			        if (isNewGxList)
			        {
			            newFlver.GXLists.Add(gxList);
			        }
			        
			        // Set material GXIndexes.
			        foreach (var mat in newFlver.Materials
				                 .Where(x => x.MTD.Equals(distinctMat.MTD)))
			        {
			            mat.GXIndex = newFlver.GXLists.Count - 1;
			        }
			    }
			    foreach (var mesh in newFlver.Meshes)
			    {
				    // Definition for the material the mesh uses.
			        FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
			            .First(x => x.MTD.Equals(
				            $"{Path.GetFileName(newFlver.Materials[mesh.MaterialIndex].MTD).ToLower()}"));
			        
			        List<FLVER2.BufferLayout> bufferLayouts = 
				        matDef.AcceptableVertexBufferDeclarations[0].Buffers;
			        
			        // DS3 does not keep track of bone indices in each mesh.
			        mesh.BoneIndices.Clear();
			        
			        // Pad vertices and map vertices to new buffer layouts.
			        mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
			        List<int> layoutIndices = newFlver.GetLayoutIndices(bufferLayouts);
			        mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();
			        
			    }

			    // convert flver to binderFile and add it to the new bnd
				BinderFile flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
					$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{op.PortedChrId}\\c{op.PortedChrId}.flver",
					newFlver.Write());
				newBnd.Files.Add(flverFile);
				
				newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
				// Compress the new binder.
				File.WriteAllBytes($"{op.Cwd}\\c{op.PortedChrId}.chrbnd.dcx",
					DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
			}
			
		}
	}
}
