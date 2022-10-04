using System.Diagnostics;
using SoulsFormats;

namespace DS3PortingTool
{
	public static class BinderUtils
	{	
		/// <summary>
		///	Downgrades a newer HKX file to Havok 2014.
		/// </summary>
		public static bool Downgrade(this BinderFile hkxFile, string toolsDirectory)
		{
			string hkxName = Path.GetFileName(hkxFile.Name);
			File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
			string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
			
			// FileConvert
			RunProcess($"{toolsDirectory}\\fileConvert.exe", $"-x {toolsDirectory}\\{hkxName} {toolsDirectory}\\{xmlName}");
			File.Delete($"{toolsDirectory}\\{hkxName}");
			if (File.Exists($"{toolsDirectory}\\{xmlName}") == false)
			{
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}
			
			// DS3HavokConverter
			RunProcess($"{toolsDirectory}\\DS3HavokConverter\\DS3HavokConverter.exe", $"{toolsDirectory}\\{xmlName}");
			if (File.Exists($"{toolsDirectory}\\{xmlName}.bak"))
			{
				File.Delete($"{toolsDirectory}\\{xmlName}.bak");
			}
			else
			{
				File.Delete($"{toolsDirectory}\\{xmlName}");
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}
			
			// Repack xml file
			RunProcess($"{toolsDirectory}\\Hkxpack\\hkxpackds3.exe", $"{toolsDirectory}\\{xmlName}"); 
			File.Delete($"{toolsDirectory}\\{xmlName}");
			if (File.Exists($"{toolsDirectory}\\{hkxName}") == false)
			{
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}
			
			hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
			File.Delete($"{toolsDirectory}\\{hkxName}");
			Console.WriteLine($"Downgraded {hkxName}");
			return true;
		}
		
		/// <summary>
		/// Downgrades a newer HKX file to Havok 2014 using an HKX Compendium.
		/// Use when porting animations from Sekiro and Elden Ring.
		/// </summary>
		public static bool Downgrade(this BinderFile hkxFile, string toolsDirectory, BinderFile compendium)
		{
			if (compendium == null)
			{
				Console.WriteLine($"Compendium could not be found.");
				return false;
			}
			
			// Copy compendium
			string compendiumPath = $"{toolsDirectory}\\" + Path.GetFileName(compendium.Name);
			File.WriteAllBytes(compendiumPath, compendium.Bytes);
			
			string hkxName = Path.GetFileName(hkxFile.Name);
			File.WriteAllBytes($"{toolsDirectory}\\{hkxName}", hkxFile.Bytes);
			string xmlName = Path.GetFileNameWithoutExtension(hkxFile.Name) + ".xml";
			
			// FileConvert
			RunProcess($"{toolsDirectory}\\fileConvert.exe", $"-x --compendium {compendiumPath} {toolsDirectory}\\{hkxName} {toolsDirectory}\\{xmlName}");
			File.Delete($"{toolsDirectory}\\{hkxName}");
			if (File.Exists($"{toolsDirectory}\\{xmlName}") == false)
			{
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}
			
			// DS3HavokConverter
			RunProcess($"{toolsDirectory}\\DS3HavokConverter\\DS3HavokConverter.exe", $"{toolsDirectory}\\{xmlName}");
			if (File.Exists($"{toolsDirectory}\\{xmlName}.bak"))
			{
				File.Delete($"{toolsDirectory}\\{xmlName}.bak");
			}
			else
			{ File.Delete($"{toolsDirectory}\\{xmlName}");
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}
			
			// Repack xml file
			RunProcess($"{toolsDirectory}\\Hkxpack\\hkxpackds3.exe", $"{toolsDirectory}\\{xmlName}"); 
			File.Delete($"{toolsDirectory}\\{xmlName}");
			if (File.Exists($"{toolsDirectory}\\{hkxName}") == false)
			{
				Console.WriteLine($"Could not downgrade {hkxName}");
				return false;
			}

			hkxFile.Bytes = File.ReadAllBytes($"{toolsDirectory}\\{hkxName}");
			File.Delete($"{toolsDirectory}\\{hkxName}");
			File.Delete(compendiumPath);
			Console.WriteLine($"Downgraded {hkxName}");
			return true;
		}
		
		public static BND4 CopyTemplateToBinder(BND4 bnd, string externalPath, string binderPath, int id)
		{
			if (File.Exists(externalPath))
			{
				byte[] bytes = File.ReadAllBytes(externalPath);
				BinderFile newFile = new(Binder.FileFlags.Flag1, id, binderPath, bytes);
				bnd.Files.Add(newFile);
				bnd.Files = bnd.Files.OrderBy(file => file.ID).ToList();
			}
			return bnd;
		}
		
		/// <summary>
		///	Run an external tool with the given arguments.
		/// </summary>
		private static void RunProcess(string applicationName, string args)
		{
			Process tool = new Process();
			tool.StartInfo.FileName = applicationName;
			tool.StartInfo.Arguments = args;
			tool.Start();
			tool.WaitForExit();
		}
	}
}