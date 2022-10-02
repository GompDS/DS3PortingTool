using SoulsAssetPipeline.Animation;

namespace DS3PortingTool
{
	public static class EventExtensions
	{	
		/// <summary>
		/// Check if event is not one of the excluded jumpTables
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
		/// Change the first four digits of the Sound ID parameter of this event to match the new character ID
		/// </summary>
		public static byte[] ChangeSoundEventChrId(this TAE.Event ev, bool isBigEndian, string newChrId)
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
			if ((soundType == 1 || soundType == 8) && soundIdString.Length == 9 && !soundIdString.Substring(0, 4).Contains("9999"))
			{
				soundIdString = newChrId + soundIdString.Substring(4);
				soundId = Int32.Parse(soundIdString);
				byte[] newBytes = BitConverter.GetBytes(soundId);
				Array.Copy(newBytes, 0, paramBytes, 4, 4);
			}
			return paramBytes;
		}
	}
}