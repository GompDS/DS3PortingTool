using SoulsAssetPipeline.Animation;

namespace DS3PortingTool
{
	public static class AnimationExtensions
	{
		/// <summary>
		///	Change anim ID and anim name to reflect the new ID. Enables HKX importing for anim and sets HKX to import from
		/// </summary>
		public static void SetAnimationProperties(this TAE.Animation anim, int newId, int newImportHkxSourceAnimId, int animOffset)
		{
			newId += animOffset;
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
				standardMiniHeader.ImportHKXSourceAnimID = newImportHkxSourceAnimId + (animOffset * 100);
			}
		}
		
		/// <summary>
		///	Get the animation offset of a given animation ID
		/// </summary>
		public static int GetOffset(this TAE.Animation anim)
		{
			int animOffset = 0;
			if (anim.ID >= 500000000)
			{
				animOffset = 5000000;
			}
			else if (anim.ID >= 400000000)
			{
				animOffset = 4000000;
			}
			else if (anim.ID >= 300000000)
			{
				animOffset = 3000000;
			}
			else if (anim.ID >= 200000000)
			{
				animOffset = 2000000;
			}
			else if (anim.ID >= 100000000)
			{
				animOffset = 1000000;
			}
			else if (anim.ID >= 5000000)
			{
				animOffset = 5000000;
			}
			else if (anim.ID >= 4000000)
			{
				animOffset = 4000000;
			}
			else if (anim.ID >= 3000000)
			{
				animOffset = 3000000;
			}
			else if (anim.ID >= 2000000)
			{
				animOffset = 2000000;
			}
			else if (anim.ID >= 1000000)
			{
				animOffset = 1000000;
			}
			return animOffset;
		}

		/// <summary>
		///	Get animation ID without the animation offset included
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
	}
}