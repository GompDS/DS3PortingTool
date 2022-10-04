using System.Xml.Linq;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using SoulsAssetPipeline.FLVERImporting;

namespace DS3PortingTool
{
	class Program
	{
		public static void Main(string[] args)
		{
			List<string> errorMessages = new();
			List<string> sourceFiles = new();
			bool portTaeOnly = false;
			string cwd = AppDomain.CurrentDomain.BaseDirectory;
			string portedChrId = "";
			List<int> excludedOffsets = new();

			for (int i = 0; i < args.Length; i++)
			{
				// Edit TAE file only
				if (args[i] == "-t")
				{
					portTaeOnly = true;
				}
				// Specify a chr ID which overrides the one in the config file
				else if (args[i] == "-c")
				{
					if (args.Length > i + 1)
					{
						if (args[i + 1].Length == 4 && args[i + 1].All(char.IsDigit))
						{
							portedChrId = args[i + 1];
							i++;
						}
						else
						{
							errorMessages.Add("Character ID specified after flag '-c' " +
							                  "must be 4 digits and contain only numbers.");
						}
					}
					else
					{
						errorMessages.Add("Flag '-c' used, but no character ID specified.");
					}
				}
				// Exclude certain animation offsets
				else if (args[i] == "-o")
				{
					if (args.Length > i + 1)
					{
						string csv = args[i + 1];
						int startIndex = 0;
						int delimIndex = csv.Substring(startIndex).IndexOf(",");
						string section;
						while (delimIndex >= 0)
						{
							delimIndex += startIndex;
							section = csv.Substring(startIndex, delimIndex - startIndex);
							if (section.Length == 1 && section.All(char.IsDigit))
							{
								excludedOffsets.Add(Int32.Parse(section));
							}
							else
							{
								errorMessages.Add("Excluded offsets specified after flag '-o' " +
								                  "must be 1 digit numbers separated by commas.\nExample: -o 1,3");
							}
							startIndex = delimIndex + 1;
							delimIndex = csv.Substring(startIndex).IndexOf(",");
						}
						section = csv.Substring(startIndex);
						if (section.Length == 1 && section.All(char.IsDigit))
						{
							excludedOffsets.Add(Int32.Parse(section));
						}
						else
						{
							errorMessages.Add("Excluded offsets specified after flag '-o' " +
							                  "must be 1 digit numbers separated by commas.\nExample: -o 1,3");
						}
						i++;
						excludedOffsets = excludedOffsets.OrderByDescending(x => x).ToList();
					}
					else
					{
						errorMessages.Add("Flag '-o' used, but no offsets to exclude specified.\nExample: -o 1,3");
					}

				}
				// Get source bnd
				else if (File.Exists(args[i]))
				{
					sourceFiles.Add(args[i]);
				}
				else
				{
					errorMessages.Add("Unknown flag or path");
				}
			}

			if (!sourceFiles.Any())
			{
				errorMessages.Add(
					"No path to a binder specified. Specify the path to a binder after any flags you want to use.");
			}
			else if (string.IsNullOrEmpty(portedChrId))
			{
				Console.WriteLine("Enter character ID that the ported binder(s) will have:");
				portedChrId = Console.ReadLine();
				if (portedChrId.Length != 4 || !portedChrId.All(char.IsDigit))
				{
					errorMessages.Add("Character ID must be 4 digits and contain only numbers.");
				}
			}

			if (errorMessages.Any())
			{
				Console.Error.WriteLine("Fatal Error(s):");
				foreach (string err in errorMessages)
				{
					Console.Error.WriteLine($"\n{err}");
				}

				Console.Error.WriteLine("\nPress any key to exit...");
				Console.ReadKey(true);
				Environment.Exit(0);
			}

			foreach (string source in sourceFiles)
			{
				string sourceFileName = Path.GetFileName(source);
				string oldChrId = sourceFileName.Substring(1, 4);
				BND4 oldBnd = BND4.Read(source);
				BND4 newBnd = new BND4();
				if (sourceFileName.Contains("anibnd"))
				{
					// Downgrade HKX files
					if (portTaeOnly == false)
					{
						newBnd.Files = oldBnd.Files
							.Where(x => x.Name.IndexOf(".hkx", StringComparison.OrdinalIgnoreCase) >= 0)
							.Where(x => x.Downgrade($"{cwd}HavokDowngrade\\", oldBnd.Files
								.Find(y => y.Name.Contains($"c{oldChrId}.compendium")))).ToList();
						foreach (BinderFile hkx in newBnd.Files)
						{
							if (hkx.Name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) < 0)
							{
								hkx.ID = int.Parse($"100{hkx.ID.ToString("D9")[1..].Remove(1, 2)}");
							}

							hkx.Name =
								$"N:\\FDP\\data\\INTERROOT_win64\\chr\\" +
								$"c{portedChrId}\\hkx\\{Path.GetFileName(hkx.Name)}";
						}
					}

					// TAE File
					BinderFile file = oldBnd.Files.Find(x => x.Name.Contains(".tae"));
					if (file != null)
					{
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
						//TAE.Template sdtTemplate = TAE.Template.ReadXMLFile($"{cwd}\\Res\\TAE.Template.DS3.xml");
						//newTae.ApplyTemplate(sdtTemplate);

						// Gather a list of animations to exclude from the xml data
						XElement xmlElements = XElement.Load($"{cwd}\\Res\\ExcludedAnimations.xml");
						List<int> excludedAnimations = xmlElements.Elements("animationDenyList")
							.Where(x => x.Attribute("game")!.Value == "Sekiro").Elements("animation")
							.Select(x => int.Parse(x.Attribute("id")!.Value)).ToList();
						List<XElement> animationRanges = xmlElements.Elements("animationDenyList")
							.Where(x => x.Attribute("game")!.Value == "Sekiro")
							.Elements("animationRange").ToList();
						foreach (XElement x in animationRanges)
						{
							int repeat = int.Parse(x.Attribute("repeat")!.Value);
							int increment = int.Parse(x.Attribute("increment")!.Value);
							for (int j = 0; j < repeat; j++)
							{
								List<XElement> animationsInRange = x.Elements("animation").ToList();
								foreach (XElement y in animationsInRange)
								{
									excludedAnimations.Add(int.Parse(y.Attribute("id")!.Value) + 
										(j * increment));
								}
							}
						}

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
							(excludedAnimations.Contains(otherHeader.ImportFromAnimID) ||
							 excludedImportHkxAnimations.Contains(otherHeader.ImportFromAnimID)))
								.Select(x => Convert.ToInt32(x.ID)).ToList();

						// Create a dictionary of old animation ids as the keys and new animation ids
						// as the values from xml data
						xmlElements = XElement.Load($"{cwd}\\Res\\AnimationRemapping.xml");
						Dictionary<int, int> animationRemap = xmlElements.Elements("animationRemapDictionary")
							.Where(x => x.Attribute("game")!.Value == "Sekiro").Elements("animation")
							.ToDictionary(x => int.Parse(x.Attribute("old")!.Value), 
								x => int.Parse(x.Attribute("new")!.Value)); 
						
						animationRanges = xmlElements.Elements("animationRemapDictionary")
							.Where(x => x.Attribute("game")!.Value == "Sekiro")
							.Elements("animationRange").ToList();
						foreach (XElement x in animationRanges)
						{
							int repeat = int.Parse(x.Attribute("repeat")!.Value);
							int oldIncrement = int.Parse(x.Attribute("oldIncrement")!.Value);
							int newIncrement = int.Parse(x.Attribute("newIncrement")!.Value);
							for (int j = 0; j < repeat; j++)
							{
								List<XElement> animationsInRange = x.Elements("animation").ToList();
								foreach (XElement y in animationsInRange)
								{
									animationRemap.Add(int.Parse(y.Attribute("old")!.Value) + (j * oldIncrement),
										int.Parse(y.Attribute("new")!.Value) + (j * newIncrement));
								}
							}
						}
						
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
								};
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
							.Where(x => !excludedAnimations.Contains(x.GetNoOffsetId()) && 
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
										int newImportId;
										animationRemap.TryGetValue(importId, out newImportId);
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
									int newAnimId;
									animationRemap.TryGetValue(x.GetNoOffsetId(), out newAnimId);
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
									};
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
						
						// Create list of excluded events and jumpTables from xml data
						xmlElements = XElement.Load($"{cwd}\\Res\\ExcludedEvents.xml");
						List<int> eventDenyList = xmlElements.Elements("eventDenyList")
							.Where(x => x.Attribute("game")!.Value == "Sekiro").Elements("event")
							.Select(x => int.Parse(x.Attribute("id")!.Value)).ToList();
						List<int> jumpTableDenyList = xmlElements.Elements("eventDenyList")
							.Where(x => x.Attribute("game")!.Value == "Sekiro").Elements("jumpTables")
							.Elements("jumpTable")
							.Select(x => int.Parse(x.Attribute("id")!.Value)).ToList();
						newTae.Animations = newTae.Animations.Select(anim =>
						{
							anim.Events = anim.Events.Where(ev =>
								!eventDenyList.Contains(ev.Type) &&
								!jumpTableDenyList.Contains(ev.GetJumpTableId(newTae.BigEndian)))
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
						newBnd.Files.Add(taeFile);
					}

					// Compress the new bnd
					File.WriteAllBytes($"{cwd}\\c{portedChrId}.anibnd.dcx",
						DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
				}
				else if (sourceFileName.Contains("chrbnd"))
				{
					// Downgrade HKX files
					newBnd.Files = oldBnd.Files.Where(x =>
							x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", StringComparison.OrdinalIgnoreCase) >=
							0)
						.Where(
							x =>
								x.Downgrade($"{cwd}HavokDowngrade\\")).Select(x =>
						{
							x.Name =
								$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{portedChrId}\\{Path.GetFileName(x.Name).Replace(oldChrId, portedChrId)}";
							return x;
						}).ToList();

					// Add hkxpwv amd clm2
					newBnd.Files.AddRange(oldBnd.Files
						.Where(x =>
							x.Name.Substring(x.Name.Length - 4).IndexOf(".hkx", StringComparison.OrdinalIgnoreCase) < 0)
						.Where(x =>
							(newBnd.Files.Any(y =>
								 y.Name.IndexOf($"c{portedChrId}.hkx", StringComparison.OrdinalIgnoreCase) >= 0) &&
							 x.Name.IndexOf($"c{oldChrId}.hkxpwv", StringComparison.OrdinalIgnoreCase) >= 0)
							|| (newBnd.Files.Any(y =>
								    y.Name.IndexOf($"c{portedChrId}_c.hkx", StringComparison.OrdinalIgnoreCase) >= 0) &&
							    x.Name.IndexOf($"c{oldChrId}_c.clm2", StringComparison.OrdinalIgnoreCase) >= 0))
						.Select(
							x =>
							{
								x.Name =
									$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{portedChrId}\\{Path.GetFileName(x.Name).Replace(oldChrId, portedChrId)}";
								return x;
							}).ToList());
					// FLVER File
					BinderFile file = oldBnd.Files.Find(x => x.Name.Contains(".flver"));
					if (file != null)
					{
						FLVER2 oldFlver = FLVER2.Read(file.Bytes);
						FLVER2 newFlver = new FLVER2
					    {
					        Header = new FLVER2.FLVERHeader
					        {
					            BoundingBoxMin = oldFlver.Header.BoundingBoxMin,
					            BoundingBoxMax = oldFlver.Header.BoundingBoxMax
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
						FLVER2MaterialInfoBank materialInfoBank = FLVER2MaterialInfoBank.ReadFromXML($"{AppDomain.CurrentDomain.BaseDirectory}Res\\BANKDS3.xml");
					    List<FLVER2.Material> distinctMaterials = newFlver.Materials.DistinctBy(x => x.MTD).ToList();
					    foreach (var distinctMat in distinctMaterials)
					    {
					        // GXLists
					        FLVER2.GXList gxList = new FLVER2.GXList();
					        gxList.AddRange(materialInfoBank.GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));
					        bool isNewGxList = true;
					        foreach (var gxl in newFlver.GXLists)
					        {
					            if (gxl.Count == gxList.Count)
					            {
					                for (int i = 0; i < gxl.Count; i++)
					                {
					                    if (gxl[i].Data.Length == gxList[i].Data.Length && gxl[i].Unk04 == gxList[i].Unk04 && gxl[i].ID.Equals(gxList[i].ID))
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
					        foreach (var mat in newFlver.Materials.Where(x => x.MTD.Equals(distinctMat.MTD)))
					        {
					            mat.GXIndex = newFlver.GXLists.Count - 1;
					        }
					    }
					    foreach (var mesh in newFlver.Meshes)
					    {
					        FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
					            .First(x => x.MTD.Equals($"{Path.GetFileName(newFlver.Materials[mesh.MaterialIndex].MTD).ToLower()}"));
					        
					        List<FLVER2.BufferLayout> bufferLayouts = matDef.AcceptableVertexBufferDeclarations[0].Buffers;
					        mesh.BoneIndices.Clear();
					        mesh.Vertices = mesh.Vertices.Select(x => x.Pad(bufferLayouts)).ToList();
					        List<int> layoutIndices = newFlver.GetLayoutIndices(bufferLayouts);
					        mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();
					        
					    }

					    Console.WriteLine(CompareMeshes(oldFlver.Meshes, newFlver.Meshes));
						
						// convert flver to binderFile and add it to the new bnd
						BinderFile flverFile = new BinderFile(Binder.FileFlags.Flag1, 200,
							$"N:\\FDP\\data\\INTERROOT_win64\\chr\\c{portedChrId}\\c{portedChrId}.flver",
							newFlver.Write());
						newBnd.Files.Add(flverFile);
					}

					newBnd.Files = newBnd.Files.OrderBy(x => x.ID).ToList();
					// compress the new bnd
					File.WriteAllBytes($"{cwd}\\c{portedChrId}.chrbnd.dcx",
						DCX.Compress(newBnd.Write(), DCX.Type.DCX_DFLT_10000_44_9));
				}
			}
		}

		private static bool CompareMeshes(List<FLVER2.Mesh> meshesA, List<FLVER2.Mesh> meshesB)
		{
			if (meshesA.Count == meshesB.Count)
			{
				for (int i = 0; i < meshesA.Count; i++)
				{
					FLVER2.Mesh ma = meshesA[i];
					FLVER2.Mesh mb = meshesB[i];
					if (ma.Dynamic == mb.Dynamic &&
					    ma.MaterialIndex == mb.MaterialIndex &&
					    ma.DefaultBoneIndex == mb.DefaultBoneIndex &&
					    ma.BoneIndices.SequenceEqual(mb.BoneIndices) &&
					    CompareFaceSets(ma.FaceSets, mb.FaceSets) &&
					    CompareVertexBuffers(ma.VertexBuffers, mb.VertexBuffers) &&
						CompareVertices(ma.Vertices, mb.Vertices) &&
					    ma.BoundingBox.Equals(mb.BoundingBox))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool CompareFaceSets(List<FLVER2.FaceSet> faceSetsA, List<FLVER2.FaceSet> faceSetsB)
		{
			if (faceSetsA.Count == faceSetsB.Count)
			{
				for (int i = 0; i < faceSetsA.Count; i++)
				{
					FLVER2.FaceSet fa = faceSetsA[i];
					FLVER2.FaceSet fb = faceSetsB[i];
					if (fa.Flags == fb.Flags &&
					    fa.TriangleStrip == fb.TriangleStrip &&
					    fa.CullBackfaces == fb.CullBackfaces &&
					    fa.Unk06 == fb.Unk06 &&
					    fa.Indices.SequenceEqual(fb.Indices))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool CompareVertexBuffers(List<FLVER2.VertexBuffer> vertexBuffersA,
			List<FLVER2.VertexBuffer> vertexBuffersB)
		{
			if (vertexBuffersA.Count == vertexBuffersB.Count)
			{
				for (int i = 0; i < vertexBuffersA.Count; i++)
				{
					FLVER2.VertexBuffer va = vertexBuffersA[i];
					FLVER2.VertexBuffer vb = vertexBuffersB[i];
					if (va.LayoutIndex == vb.LayoutIndex)
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool CompareVertices(List<FLVER.Vertex> verticiesA, List<FLVER.Vertex> verticiesB)
		{
			if (verticiesA.Count == verticiesB.Count)
			{
				for (int i = 0; i < verticiesA.Count; i++)
				{
					FLVER.Vertex va = verticiesA[i];
					FLVER.Vertex vb = verticiesB[i];
					if (va.Position.Equals(vb.Position) &&

					    va.BoneWeights.Equals(vb.BoneWeights) &&

					    va.BoneIndices.Equals(vb.BoneIndices) &&

					    va.Normal.Equals(vb.Normal) &&

					    va.NormalW == vb.NormalW &&
										    
					    va.UVs.SequenceEqual(vb.UVs) &&
										    
					    va.Tangents.SequenceEqual(vb.Tangents) &&
										    
					    va.Bitangent.Equals(vb.Bitangent) &&
											
					    va.Colors.SequenceEqual(vb.Colors))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
