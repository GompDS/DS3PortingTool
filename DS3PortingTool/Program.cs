using System.Xml.Linq;
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
			string sourceFile = args.FirstOrDefault(x => File.Exists(x) && Path.GetFileName(x).Contains(".dcx"), "");
			if (sourceFile.Equals(""))
			{
				throw new ArgumentException("No path to a source binder found in arguments.");
			}
			List<int> flagIndices = args.Where(x => x.Length == 2 && x.Substring(0, 1).Equals("-"))
				.Select(x => Array.IndexOf(args, x))
				.Where(x => x != Array.IndexOf(args, sourceFile)).ToList();
			bool portTaeOnly = false;
			string oldChrId = "";
			string portedChrId = "";
			List<int> excludedOffsets = new();
			if (Path.GetFileName(sourceFile).Substring(1, 4).All(char.IsDigit))
			{
				oldChrId = Path.GetFileName(sourceFile).Substring(1, 4);
				portedChrId = oldChrId;
			}
			foreach (var i in flagIndices)
			{
				if (args[i].Equals("-t"))
				{
					portTaeOnly = true;
				}
				else if (args[i].Equals("-c"))
				{
					if (args.Length < i + 1)
					{
						throw new ArgumentException($"Flag '-c' used, but no character id provided.");
					}
					if (args[i + 1].Length != 4 || !args[i + 1].All(char.IsDigit))
					{
						throw new ArgumentException($"Character id after flag '-c' must be a 4 digit number.");
					}

					portedChrId = args[i + 1];
				}
				else if (args[i].Equals("-o"))
				{
					if (args.Length < i + 1)
					{
						throw new ArgumentException($"Flag '-o' used, but no offsets provided.");
					}
					excludedOffsets = args[i + 1].Split(',')
						.Where(x => x.All(char.IsDigit) && x.Length == 1)
						.Select(x => Int32.Parse(x)).ToList();
				}
				else
				{
					throw new ArgumentException($"Unknown flag: {args[i]}");
				}
			}
			string cwd = AppDomain.CurrentDomain.BaseDirectory;
			string sourceFileName = Path.GetFileName(sourceFile);
			BND4 oldBnd = BND4.Read(sourceFile);
			BND4 newBnd = new BND4();
			if (sourceFileName.Contains("anibnd"))
			{
				// Downgrade HKX files
				if (portTaeOnly == false)
				{
					BinderFile? compendium = oldBnd.Files.Find(x => x.Name.Contains($"c{oldChrId}.compendium"));
					if (compendium == null)
					{
						throw new Exception("Source anibnd contains no compendium.");
					}
					newBnd.Files = oldBnd.Files
						.Where(x => x.Name.IndexOf(".hkx", StringComparison.OrdinalIgnoreCase) >= 0)
						.Where(x => x.Downgrade($"{cwd}HavokDowngrade\\", compendium)).ToList();
					foreach (BinderFile hkx in newBnd.Files)
					{
						if (hkx.Name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) < 0)
						{
							hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
						}

						hkx.Name = $"N:\\FDP\\data\\INTERROOT_win64\\chr\\" + 
						           $"c{portedChrId}\\hkx\\{Path.GetFileName(hkx.Name)}";
					}
				}

				// TAE File
				BinderFile? file = oldBnd.Files.Find(x => x.Name.Contains(".tae"));
				if (file == null)
				{
					throw new Exception("Source anibnd contains no tae.");
				}

				TAE oldTae = TAE.Read(file.Bytes);
				// setup new DS3 tae
				TAE newTae = new TAE
				{
					Format = TAE.TAEFormat.DS3,
					BigEndian = false,
					ID = 200000 + int.Parse(portedChrId),
					Flags = new byte[] { 1, 0, 1, 2, 2, 1, 1, 1 },
					SkeletonName = "skeleton.hkt",
					SibName = $"c{portedChrId}.sib",
					Animations = new List<TAE.Animation>(),
					EventBank = 21
				};

				// Gather a list of animations to exclude from the xml data
				List<int> animationDenyList = XElement.Load($"{cwd}\\Res\\ExcludedAnimations.xml")
					.GetXmlList("Sekiro");

				// Exclude animations which import an HKX that doesn't exist
				List<int> excludedImportHkxAnimations = oldTae.Animations.Where(anim =>
					anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardHeader &&
					standardHeader.ImportsHKX && oldBnd.Files.All(x =>
						x.Name != "a" + standardHeader.ImportHKXSourceAnimID.ToString("D9")
							.Insert(3, "_") + ".hkx"))
								.Select(x => Convert.ToInt32(x.ID)).ToList();

				// Exclude animations which import all from an animation that's excluded
				List<int> excludedImportAllAnimations = oldTae.Animations.Where(anim =>
					anim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim otherHeader &&
					(animationDenyList.Contains(otherHeader.ImportFromAnimID) ||
					 excludedImportHkxAnimations.Contains(otherHeader.ImportFromAnimID)))
						.Select(x => Convert.ToInt32(x.ID)).ToList();

				// Create a dictionary of old animation ids as the keys and new animation ids
				// as the values from xml data
				Dictionary<int, int> animationRemap = XElement.Load($"{cwd}\\Res\\AnimationRemapping.xml")
					.GetXmlDictionary("Sekiro");
				
				// Remove animations from excluded offsets
				List<int> excludedOffsetAnimations = new();
				if (excludedOffsets.Any())
				{
					foreach (int offsetId in excludedOffsets.Where(x => x > 0))
					{
						excludedOffsetAnimations.AddRange(oldTae.Animations.Where(y =>
								y.ID >= offsetId * 100000000 && y.ID < (offsetId + 1) * 100000000)
							.Select(y => Convert.ToInt32(y.ID)).ToList());
					}

					if (excludedOffsets.Contains(0))
					{
						int nextAllowedOffset = 1;
						while (excludedOffsets.Contains(nextAllowedOffset))
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

				// Remove excluded animations from the list and remap animation ids 
				int oldOffset = 0;
				int newOffset = 0;
				newTae.Animations = oldTae.Animations
					.Where(x => !animationDenyList.Contains(x.GetNoOffsetId()) && 
					            !excludedImportHkxAnimations.Contains(Convert.ToInt32(x.ID)) &&
					            !excludedImportAllAnimations.Contains(Convert.ToInt32(x.ID)) &&
					            !excludedOffsetAnimations.Contains(Convert.ToInt32(x.ID)))
					.Select(x => 
					{
						// imports all animation ID remapping
						if (x.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim importMiniHeader)
						{
							string iDString = Convert.ToString(importMiniHeader.ImportFromAnimID);
							for (int i = iDString.Length; i < 9; i++)
							{
								iDString = iDString.Insert(0, "0");
							}

							iDString = iDString.Substring(3);
							int importId = Int32.Parse(iDString);
							if (animationRemap.ContainsKey(importId))
							{
								animationRemap.TryGetValue(importId, out int newImportId);
								importMiniHeader.ImportFromAnimID = newImportId + x.GetOffset();
							}
							else
							{
								importMiniHeader.ImportFromAnimID = importId + x.GetOffset();
							}
						}

						// animation ID remapping
						if (animationRemap.ContainsKey(x.GetNoOffsetId()))
						{
							animationRemap.TryGetValue(x.GetNoOffsetId(), out int newAnimId);
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
						if (excludedOffsets.Contains(0))
						{
							int nextAllowedOffset = 1;
							while (excludedOffsets.Contains(nextAllowedOffset))
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
				
				// Create list of excluded event, jumpTables and rumbleCams from xml data
				List<int> eventDenyList = XElement.Load($"{cwd}\\Res\\ExcludedEvents.xml")
					.GetXmlList("Sekiro");
				List<int> jumpTableDenyList = XElement.Load($"{cwd}\\Res\\ExcludedJumpTables.xml")
					.GetXmlList("Sekiro");
				List<int> rumbleCamDenyList = XElement.Load($"{cwd}\\Res\\ExcludedRumbleCams.xml")
					.GetXmlList("Sekiro");

				newTae.Animations = newTae.Animations.Select(anim =>
				{
					anim.Events = anim.Events.Where(ev =>
						!eventDenyList.Contains(ev.Type) &&
						!jumpTableDenyList.Contains(ev.GetJumpTableId(newTae.BigEndian)) &&
						!rumbleCamDenyList.Contains(ev.GetRumbleCamId(newTae.BigEndian)))
						.Select(ev =>
					{
						// Edit events as needed
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
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, portedChrId);
								break;
							// PlaySound_ByStateInfo
							case 129:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, portedChrId);
								Array.Clear(paramBytes, 18, 2);
								break;
							// PlaySound_ByDummyPoly_PlayerVoice
							case 130:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, portedChrId);
								Array.Clear(paramBytes, 16, 2);
								Array.Resize(ref paramBytes, 32);
								break;
							// PlaySound_DummyPoly
							case 131:
								paramBytes = ev.ChangeSoundEventChrId(newTae.BigEndian, portedChrId);
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

				// Convert tae to binderFile and add it to the new bnd
				BinderFile taeFile = new BinderFile(Binder.FileFlags.Flag1, 3000000,
					$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{portedChrId}\\tae\\c{portedChrId}.tae",
					newTae.Write());
				if (portTaeOnly)
				{
					File.WriteAllBytes($"{cwd}\\c{portedChrId}.tae", taeFile.Bytes);
				}
				else
				{
					newBnd.Files.Add(taeFile);
				}

				if (portTaeOnly == false)
				{
					// Compress the new bnd
					File.WriteAllBytes($"{cwd}\\c{portedChrId}.anibnd.dcx",
						DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
				}
			}
			else if (sourceFileName.Contains("chrbnd"))
			{
				// Downgrade HKX files
				newBnd.Files = oldBnd.Files.Where(x =>
					x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", 
						StringComparison.OrdinalIgnoreCase) >= 0)
					.Where(x => x.Downgrade($"{cwd}HavokDowngrade\\")).Select(x =>
					{
						x.Name =
							@"N:\FDP\data\INTERROOT_win64\chr\" + 
							$"c{portedChrId}\\{Path.GetFileName(x.Name).Replace(oldChrId, portedChrId)}";
						return x;
					}).ToList();

				// Add hkxpwv amd clm2
				newBnd.Files.AddRange(oldBnd.Files
					.Where(x =>
						x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", 
							StringComparison.OrdinalIgnoreCase) < 0)
					.Where(x =>
						(newBnd.Files.Any(y =>
							 y.Name.IndexOf($"c{portedChrId}.hkx", StringComparison.OrdinalIgnoreCase) >= 0) &&
						 x.Name.IndexOf($"c{oldChrId}.hkxpwv", StringComparison.OrdinalIgnoreCase) >= 0)
						|| (newBnd.Files.Any(y =>
							    y.Name.IndexOf($"c{portedChrId}_c.hkx", 
								    StringComparison.OrdinalIgnoreCase) >= 0) &&
						    x.Name.IndexOf($"c{oldChrId}_c.clm2", StringComparison.OrdinalIgnoreCase) >= 0))
					.Select(
						x =>
						{
							x.Name =
								@"N:\FDP\data\INTERROOT_win64\chr\" +
								$"c{portedChrId}\\{Path.GetFileName(x.Name).Replace(oldChrId, portedChrId)}";
							return x;
						}).ToList());
				// FLVER File
				BinderFile? file = oldBnd.Files.Find(x => x.Name.Contains(".flver"));
				if (file == null)
				{
					throw new Exception("Source chrbnd contains no flver.");
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
			        Materials = oldFlver.Materials.Select(x => x.ToDummyDs3Material()).ToList(),
			        Bones = oldFlver.Bones.Select(x =>
			        {
			            if (x.Unk3C > 1)
			            {
			                x.Unk3C = 0;
			            }
			            return x;
			        }).ToList(),
			        Meshes = oldFlver.Meshes
			    };
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
			        
			        // Set material GXIndexes
			        foreach (var mat in newFlver.Materials
				                 .Where(x => x.MTD.Equals(distinctMat.MTD)))
			        {
			            mat.GXIndex = newFlver.GXLists.Count - 1;
			        }
			    }
			    foreach (var mesh in newFlver.Meshes)
			    {
			        FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
			            .First(x => x.MTD.Equals(
				            $"{Path.GetFileName(newFlver.Materials[mesh.MaterialIndex].MTD).ToLower()}"));
			        
			        List<FLVER2.BufferLayout> bufferLayouts = 
				        matDef.AcceptableVertexBufferDeclarations[0].Buffers;
			        mesh.BoneIndices.Clear();
			        mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
			        List<int> layoutIndices = newFlver.GetLayoutIndices(bufferLayouts);
			        mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();
			        
			    }

			    // convert flver to binderFile and add it to the new bnd
				BinderFile flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
					$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{portedChrId}\\c{portedChrId}.flver",
					newFlver.Write());
				newBnd.Files.Add(flverFile);
				
				newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
				// compress the new bnd
				File.WriteAllBytes($"{cwd}\\c{portedChrId}.chrbnd.dcx",
					DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
			}
		}
	}
}
