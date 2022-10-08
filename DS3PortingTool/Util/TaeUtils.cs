using SoulsAssetPipeline.Animation;

namespace DS3PortingTool.Util
{
	public static class TaeUtils
	{
		/// <summary>
		///	Change anim ID and anim name to reflect the new ID.
		/// Enables HKX importing for anim and sets HKX to import from.
		/// </summary>
		public static void SetAnimationProperties(this TAE.Animation anim, int newId, int newImportHkxSourceAnimId,
			int animOffset, Options op)
		{
			newId += animOffset * 1000000;
			anim.ID = Convert.ToInt64(newId);
			anim.AnimFileName = Convert.ToString(newId);
			for (int i = anim.AnimFileName.Length; i < 9; i++)
			{
				anim.AnimFileName = anim.AnimFileName.Insert(0, "0");
			}
			anim.AnimFileName = String.Concat(anim.AnimFileName, ".hkt");
			anim.AnimFileName = anim.AnimFileName.Insert(0, "a");
			anim.AnimFileName = anim.AnimFileName.Insert(4, "_");
			if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardMiniHeader)
			{
				standardMiniHeader.ImportsHKX = true;
				standardMiniHeader.ImportHKXSourceAnimID = newImportHkxSourceAnimId + (animOffset * op.Game.Offset);
			}
		}
		
		/// <summary>
		///	Get the animation offset of a given animation ID.
		/// </summary>
		public static int GetOffset(this TAE.Animation anim)
		{
			string idString = anim.ID.ToString("D9").Substring(0, 3);
			idString = idString.Replace("0", "");
			if (!idString.Equals(""))
			{
				return Int32.Parse(idString);
			}

			return 0;
		}

		/// <summary>
		///	Get animation ID without the animation offset included.
		/// </summary>
		public static int GetNoOffsetId(this TAE.Animation anim)
		{
			int animId = Convert.ToInt32(anim.ID);
			if (anim.GetOffset() > 0)
			{
				string idString = animId.ToString("D9");
				idString = idString.Remove(0, 3);
				animId = Convert.ToInt32(idString);
			}
			return animId;
		}
		
		/// <summary>
		/// Given an event that is a jumpTable, return the jumpTable id.
		/// </summary>
		public static int GetJumpTableId(this TAE.Event ev, bool isBigEndian)
		{
			if (ev.Type == 0)
			{
				byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
				byte[] jumpTableIdBytes = new byte[4];
				Array.Copy(paramBytes, jumpTableIdBytes, 4);
				if (isBigEndian)
				{
					Array.Reverse(jumpTableIdBytes);
				}
				return BitConverter.ToInt32(jumpTableIdBytes, 0);
			}
			return -1;
		}
		
		/// <summary>
		/// Given an event that invokes a rumbleCam, return the rumbleCam id.
		/// </summary>
		public static short GetRumbleCamId(this TAE.Event ev, bool isBigEndian)
		{
			if (ev.Type is >= 144 and <= 147)
			{
				byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
				byte[] rumbleCamIdBytes = new byte[2];
				Array.Copy(paramBytes, rumbleCamIdBytes, 2);
				if (isBigEndian)
				{
					Array.Reverse(rumbleCamIdBytes);
				}
				return BitConverter.ToInt16(rumbleCamIdBytes, 0);
			}
			return -1;
		}
		
		/// <summary>
		/// Change the first four digits of the Sound ID parameter of this event to match the new character ID
		/// </summary>
		private static byte[] ChangeSoundEventChrId(this TAE.Event ev, bool isBigEndian, string newChrId)
		{
			byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
			byte[] soundTypeBytes = new byte[4];
			byte[] soundIdBytes = new byte[4];
			Array.Copy(paramBytes, soundTypeBytes, 4);
			Array.Copy(paramBytes, 4, soundIdBytes, 0, 4);
			if (isBigEndian)
			{
				Array.Reverse(soundTypeBytes);
				Array.Reverse(soundIdBytes);
			}
			int soundType = BitConverter.ToInt32(soundTypeBytes, 0);
			int soundId = BitConverter.ToInt32(soundIdBytes, 0);
			string soundIdString = Convert.ToString(soundId);
			if ((soundType == 1 || soundType == 8) && soundIdString.Length == 9 && !soundIdString
				    .Substring(0, 4).Contains("9999"))
			{
				soundIdString = newChrId + soundIdString.Substring(4);
				soundId = Int32.Parse(soundIdString);
				byte[] newBytes = BitConverter.GetBytes(soundId);
				Array.Copy(newBytes, 0, paramBytes, 4, 4);
			}
			return paramBytes;
		}

		/// <summary>
		/// Gets ids of animations that belong to excluded offsets.
		/// </summary>
		public static List<int> GetExcludedOffsetAnimations(this TAE sourceTae, Options op)
		{
			List<int> excludedOffsetAnimations = new();
			if (op.ExcludedAnimOffsets.Any())
			{
				foreach (int offsetId in op.ExcludedAnimOffsets.Where(x => x > 0))
				{
					int idMin = offsetId * op.Game.Offset, idMax = (offsetId + 1) * op.Game.Offset;
					excludedOffsetAnimations.AddRange(sourceTae.Animations.Where(y => 
						y.ID >= idMin && y.ID < idMax).Select(y => Convert.ToInt32(y.ID)).ToList());
				}

				if (op.ExcludedAnimOffsets.Contains(0))
				{
					int nextAllowedOffset = 1;
					while (op.ExcludedAnimOffsets.Contains(nextAllowedOffset))
					{
						nextAllowedOffset++;
					}

					nextAllowedOffset *= op.Game.Offset;
					foreach (var anim in sourceTae.Animations.Where(x => x.ID < op.Game.Offset))
					{
						if (sourceTae.Animations.FindIndex(x => 
							    x.ID == anim.ID + nextAllowedOffset) >= 0 || (anim.ID is >= 3000 and < 4000))
						{
							excludedOffsetAnimations.Add(Convert.ToInt32(anim.ID));
						}
					}
				}
			}

			return excludedOffsetAnimations;
		}

		/// <summary>
		/// Remaps the id of any animation's 'ImportFromAnimId' property.
		/// </summary>
		public static void RemapImportAnimationId(this TAE.Animation anim, XmlData data)
		{
			if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim importMiniHeader)
			{
				string idString = Convert.ToString(importMiniHeader.ImportFromAnimID);
				for (int i = idString.Length; i < 9; i++)
				{
					idString = idString.Insert(0, "0");
				}

				idString = idString.Substring(3);
				int importId = Int32.Parse(idString);
				if (data.AnimationRemapping.ContainsKey(importId))
				{
					data.AnimationRemapping.TryGetValue(importId, out int newImportId);
					importMiniHeader.ImportFromAnimID = newImportId + anim.GetOffset() * 1000000;
				}
				else
				{
					importMiniHeader.ImportFromAnimID = importId + anim.GetOffset() * 1000000;
				}
			}
		}
		
		/// <summary>
		/// Change the offsets of animations in order to fill gaps when offsets are removed.
		/// </summary>
		public static void ShiftAnimationOffsets(this TAE tae, Options op)
		{
			int oldOffset = 0, newOffset = 0;
			foreach (var anim in tae.Animations)
			{
				if (op.ExcludedAnimOffsets.Contains(0))
				{
					int nextAllowedOffset = 1;
					while (op.ExcludedAnimOffsets.Contains(nextAllowedOffset))
					{
						nextAllowedOffset++;
					}

					nextAllowedOffset *= 1000000;
					if (anim.ID >= nextAllowedOffset && anim.ID < nextAllowedOffset + 1000000)
					{
						anim.ID -= nextAllowedOffset;
					}
				}

				if (anim.GetOffset() * 1000000 > oldOffset)
				{
					oldOffset = anim.GetOffset() * 1000000;
					newOffset += 1000000;
				}

				if (newOffset < oldOffset)
				{
					anim.ID = newOffset + anim.GetNoOffsetId();
				}
			}
		}

		/// <summary>
		/// Edit parameters of the event so that it will match with its DS3 event equivalent.
		/// </summary>
		public static TAE.Event Edit(this TAE.Event ev, bool bigEndian, Options op)
		{
			byte[] paramBytes = ev.GetParameterBytes(bigEndian);
			
			if (op.Game.Type == Game.GameTypes.Sekiro)
			{
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
						paramBytes = ev.ChangeSoundEventChrId(bigEndian, op.PortedChrId);
						break;
					// PlaySound_ByStateInfo
					case 129:
						paramBytes = ev.ChangeSoundEventChrId(bigEndian, op.PortedChrId);
						Array.Clear(paramBytes, 18, 2);
						break;
					// PlaySound_ByDummyPoly_PlayerVoice
					case 130:
						paramBytes = ev.ChangeSoundEventChrId(bigEndian, op.PortedChrId);
						Array.Clear(paramBytes, 16, 2);
						Array.Resize(ref paramBytes, 32);
						break;
					// PlaySound_DummyPoly
					case 131:
						paramBytes = ev.ChangeSoundEventChrId(bigEndian, op.PortedChrId);
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
			}
			
			ev.SetParameterBytes(bigEndian, paramBytes);
			return ev;
		}
	}
}