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
	}
}
