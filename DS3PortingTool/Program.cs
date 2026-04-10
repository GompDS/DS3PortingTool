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
			if (asset.Type == GameAsset.FSAssetType.ANIBND)
			{
				IBinder bnd;
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
					continue;
				}
				
				Console.WriteLine(asset.FileName);
				foreach (BinderFile file in bnd.Files.Where(x => Path.GetExtension(x.Name).Equals(".hkx", 
					         StringComparison.OrdinalIgnoreCase)))
				{
					if (HavokConverter.TryGetVersion(file, out HavokConverter.HavokVersion? version))
					{
						Console.WriteLine($"{Path.GetFileName(file.Name),-20} {version}");
					}
				}
				Console.WriteLine();
			}
		}
	}
}
