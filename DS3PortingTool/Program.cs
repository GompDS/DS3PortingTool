using SoulsFormats;
using SoulsGameSpecs;

namespace DS3PortingTool;

static class Program
{
	public static void Main(string[] args)
	{
		JsonHelper.ReadValidGameSpecsFromDisk();

		if (!ConversionContext.TryReadArguments(args))
		{
			ConversionContext.PrintHelp();
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
			Environment.Exit(1);
		}

		foreach (GameAsset asset in ConversionContext.SourceAssets)
		{
			byte[] assetBytesOut = null;
			bool result = false;
			
			if (asset.Type == GameAsset.FSAssetType.ANIBND)
			{
				result = TryProcessAnibnd(asset, out assetBytesOut);
			}
			else if (asset.Type == GameAsset.FSAssetType.OBJBND)
			{
				result = TryProcessObjbnd(asset, out assetBytesOut);
			}

			if (result && assetBytesOut != null)
			{
				File.WriteAllBytes(asset.FilePath + ".new", assetBytesOut);
			}
			else
			{
				Console.WriteLine($"ERROR: Could not process asset: {asset.FileName}");
			}
		}
	}

	private static bool TryReadDiskBnd(GameAsset asset, out IBinder bnd)
	{
		bnd = null;
		if (asset.OriginGame.BndType == "BND4")
		{
			bnd = BND4.Read(asset.FilePath);
		}
		else if (asset.OriginGame.BndType == "BND3")
		{
			bnd = BND3.Read(asset.FilePath);
		}
		else
		{
			return false;
		}

		return true;
	}
	
	private static bool TryReadInternalBnd(IBinder sourceBinder, BinderFile bndFile, out IBinder bnd)
	{
		bnd = null;
		if (sourceBinder is BND4)
		{
			bnd = BND4.Read(bndFile.Bytes);
		}
		else if (sourceBinder is BND3)
		{
			bnd = BND3.Read(bndFile.Bytes);
		}
		else
		{
			return false;
		}

		return true;
	}

	private static bool TryWriteBndBytes(IBinder sourceBinder, out byte[] bndBytesOut)
	{
		bndBytesOut = null;
		
		if (sourceBinder is BND4 bnd4)
		{
			bndBytesOut = bnd4.Write();
		}
		else if (sourceBinder is BND3 bnd3)
		{
			bndBytesOut = bnd3.Write();
		}
		else
		{
			return false;
		}

		return true;
	}

	private static bool TryProcessAnibnd(GameAsset asset, out byte[] assetBytesOuts)
	{
		assetBytesOuts = null;
		if (!TryReadDiskBnd(asset, out IBinder bnd)) return false;
				
		Console.WriteLine(asset.FileName);
		HavokConverter.TryConvertAll(bnd);

		return TryWriteBndBytes(bnd, out assetBytesOuts);
	}
	
	private static bool TryProcessInternalAnibnd(IBinder sourceBinder, BinderFile anibndFile, out byte[] assetBytesOuts)
	{
		assetBytesOuts = null;
		if (!TryReadInternalBnd(sourceBinder, anibndFile, out IBinder bnd)) return false;
		
		HavokConverter.TryConvertAll(bnd);

		return TryWriteBndBytes(bnd, out assetBytesOuts);
	}
	
	private static bool TryProcessObjbnd(GameAsset asset, out byte[] assetBytesOuts)
	{
		assetBytesOuts = null;
		if (!TryReadDiskBnd(asset, out IBinder bnd)) return false;
				
		Console.WriteLine(asset.FileName);
		foreach (BinderFile file in bnd.Files)
		{
			if (Path.GetExtension(file.Name).Equals(".anibnd", StringComparison.OrdinalIgnoreCase))
			{
				if (TryProcessInternalAnibnd(bnd, file, out assetBytesOuts))
				{
					file.Bytes = assetBytesOuts;
				}
			}
		}

		return TryWriteBndBytes(bnd, out assetBytesOuts);
	}
}
