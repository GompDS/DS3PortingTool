using System.Diagnostics;
using SoulsFormats;

namespace DS3PortingTool.Util;

public static class BinderUtils
{
	/// <summary>
	/// Searches for a binder file in the source bnd based on a search term and copies it to the dest bnd.
	/// </summary>
	public static void TransferBinderFile(this IBinder sourceBnd, BND4 destBnd, string searchTerm,
		string destFilePath)
	{
		BinderFile? file = sourceBnd.Files.Find(x => x.Name.ToLower().Contains(searchTerm));
		if (file != null)
		{
			file.Name = destFilePath;
			destBnd.Files.Add(file);
		}
	}
}