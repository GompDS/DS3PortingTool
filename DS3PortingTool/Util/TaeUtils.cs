using SoulsAssetPipeline.Animation;

namespace DS3PortingTool.Util;
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
		anim.AnimFileName = newId.ToString("D9");
		anim.AnimFileName = String.Concat(anim.AnimFileName, ".hkt");
		anim.AnimFileName = anim.AnimFileName.Insert(0, "a");
		anim.AnimFileName = anim.AnimFileName.Insert(4, "_");
		if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardMiniHeader)
		{
			standardMiniHeader.ImportsHKX = true;
			standardMiniHeader.ImportHKXSourceAnimID = newImportHkxSourceAnimId + animOffset * 1000000;
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
	///	Get the animation offset of a given animation ID.
	/// Takes a length 3 string which contains the offset (ex. 001)
	/// </summary>
	public static int GetOffset(this string anim)
	{
		string idString = anim.Substring(0, 3);
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
	/// Given an event that adds a SpEffect, return the SpEffect id.
	/// </summary>
	public static int GetSpEffectId(this TAE.Event ev, bool isBigEndian)
	{
		if (ev.Type is 66 or 67 or 302 or 331 or 401 or 797)
		{
			byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
			byte[] spEffectIdBytes = new byte[4];
			Array.Copy(paramBytes, spEffectIdBytes, 4);
			if (isBigEndian)
			{
				Array.Reverse(spEffectIdBytes);
			}
			return BitConverter.ToInt32(spEffectIdBytes, 0);
		}
		return -1;
	}
	
	/// <summary>
	/// Change the first four digits of the Sound ID parameter of this event to match the new character ID
	/// </summary>
	public static byte[] ChangeSoundEventChrId(this TAE.Event ev, bool isBigEndian, Options op)
	{
		byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
		if (op.ChangeSoundIds)
		{
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
				soundIdString = op.SoundChrId + soundIdString.Substring(4);
				soundId = Int32.Parse(soundIdString);
				byte[] newBytes = BitConverter.GetBytes(soundId);
				Array.Copy(newBytes, 0, paramBytes, 4, 4);
			}
		}

		return paramBytes;
	}
	
	/// <summary>
	/// Change the first four digits of the Sound ID parameter of this event to match the new character ID
	/// </summary>
	public static byte[] ChangeSpEffectId(this TAE.Event ev, bool isBigEndian, XmlData data)
	{
		byte[] paramBytes = ev.GetParameterBytes(isBigEndian);
		byte[] spEffectIdBytes = new byte[4];
		Array.Copy(paramBytes, 0, spEffectIdBytes, 0, 4);
		if (isBigEndian)
		{
			Array.Reverse(spEffectIdBytes);
		}
		
		int spEffectId = BitConverter.ToInt32(spEffectIdBytes, 0);
		if (data.SpEffectRemapping.ContainsKey(spEffectId))
		{
			data.SpEffectRemapping.TryGetValue(spEffectId, out spEffectId);
		}
		byte[] newBytes = BitConverter.GetBytes(spEffectId);
		Array.Copy(newBytes, 0, paramBytes, 0, 4);

		if (ev.Type == 331)
		{
			Array.Copy(paramBytes, 4, spEffectIdBytes, 0, 4);
			spEffectId = BitConverter.ToInt32(spEffectIdBytes, 0);
			if (data.SpEffectRemapping.ContainsKey(spEffectId))
			{
				data.SpEffectRemapping.TryGetValue(spEffectId, out spEffectId);
			}
			newBytes = BitConverter.GetBytes(spEffectId);
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
			
			int importId = Int32.Parse(idString.Substring(3));
			if (data.AnimationRemapping.ContainsKey(importId))
			{
				data.AnimationRemapping.TryGetValue(importId, out int newImportId);
				importMiniHeader.ImportFromAnimID = newImportId + idString.GetOffset() * 1000000;
			}
			else
			{
				importMiniHeader.ImportFromAnimID = importId + idString.GetOffset() * 1000000;
			}
		}
	}
	
	/// <summary>
	/// Remaps the id of any animation's 'ImportFromAnimId' property.
	/// </summary>
	public static void FixImportHkxOffsets(this TAE.Animation anim, XmlData data)
	{
		if (anim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard standardHeader && standardHeader.ImportsHKX)
		{
			string idString = standardHeader.ImportHKXSourceAnimID.ToString("D9");
			
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
	/// Returns true if the event is not a SpEffect event or if the SpEffect id of the event is allowed.
	/// </summary>
	public static bool IsAllowedSpEffect(this TAE.Event ev, bool isBigEndian, XmlData data)
	{
		int spEffectId = ev.GetSpEffectId(isBigEndian);
		return spEffectId >= 0 && data.AllowedSpEffects.Contains(spEffectId);
	}
}