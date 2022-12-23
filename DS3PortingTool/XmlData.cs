using System.Xml.Linq;
using SoulsAssetPipeline.FLVERImporting;

namespace DS3PortingTool;

public class XmlData
{
	/// <summary>
	/// Contains templates for creating new materials.
	/// </summary>
	public FLVER2MaterialInfoBank MaterialInfoBank { get; }

	/// <summary>
	/// Old animations ids paired with replacement ids.
	/// </summary>
	public Dictionary<int, int> AnimationRemapping = new();
	/// <summary>
	/// IDs of animations to exclude.
	/// </summary>
	public HashSet<int> ExcludedAnimations = new();
	/// <summary>
	/// IDs of events to exclude.
	/// </summary>
	public HashSet<int> ExcludedEvents { get; }
	/// <summary>
	/// IDs of jumpTables to exclude.
	/// </summary>
	public HashSet<int> ExcludedJumpTables { get; }
	/// <summary>
	/// IDs of rumbleCams to exclude.
	/// </summary>
	public HashSet<int> ExcludedRumbleCams { get; }
	/// <summary>
	/// IDs of SpEffects to allow when a SpEffect event is excluded.
	/// </summary>
	public HashSet<int> AllowedSpEffects = new();
	/// <summary>
	/// Old SpEffect IDs paired with replacements.
	/// </summary>
	public Dictionary<int, int> SpEffectRemapping = new();

	public XmlData(Options op)
	{
	    string gameName = op.Game.Name;
	    string? xmlDirectory;
	    switch (op.SourceBndsType)
	    {
		    case Options.AssetType.Character:
			    xmlDirectory = $"{op.Cwd}Res\\CharacterXML\\";
			    AnimationRemapping = GetXmlDictionary(XElement.Load($"{xmlDirectory}AnimationRemapping.xml"), gameName);
			    ExcludedAnimations = GetXmlSet(XElement.Load($"{xmlDirectory}ExcludedAnimations.xml"), gameName);
			    AllowedSpEffects = GetXmlSet(XElement.Load($"{xmlDirectory}AllowedSpEffects.xml"), gameName);
			    SpEffectRemapping = GetXmlDictionary(XElement.Load($"{xmlDirectory}SpEffectRemapping.xml"), gameName);
			    break;
		    case Options.AssetType.Object:
			    xmlDirectory = $"{op.Cwd}Res\\ObjectXML\\";
			    break;
		    default:
			    throw new ArgumentException("Unsupported bnd type.");
	    }
	    
	    MaterialInfoBank = FLVER2MaterialInfoBank.ReadFromXML($"{op.Cwd}\\Res\\BankDS3.xml");
	    ExcludedEvents = GetXmlSet(XElement.Load($"{xmlDirectory}ExcludedEvents.xml"), gameName);
	    ExcludedJumpTables = GetXmlSet(XElement.Load($"{xmlDirectory}ExcludedJumpTables.xml"), gameName);
	    ExcludedRumbleCams = GetXmlSet(XElement.Load($"{xmlDirectory}ExcludedRumbleCams.xml"), gameName);
	}
    
	/// <summary>
	/// Reads data from an xml itemList and returns a list of the data.
	/// </summary>
	private HashSet<int> GetXmlSet(XElement xmlElements, string gameName)
	{
		List<int> itemSet = xmlElements.Elements($"itemList")
			.Where(x => x.Attribute("game")!.Value == gameName).Elements("item")
			.Select(x => int.Parse(x.Attribute("id")!.Value)).ToList(); 
		List<XElement> itemRanges = xmlElements.Elements("itemList")
			.Where(x => x.Attribute("game")!.Value == gameName)
			.Elements($"itemRange").ToList();
		foreach (XElement x in itemRanges)
		{
			int repeat = int.Parse(x.Attribute("repeat")!.Value);
			int increment = int.Parse(x.Attribute("increment")!.Value);
			for (int j = 0; j < repeat; j++)
			{
				List<XElement> rumbleCamsInRange = x.Elements("item").ToList();
				foreach (XElement y in rumbleCamsInRange)
				{
					itemSet.Add(int.Parse(y.Attribute("id")!.Value) + 
					                       (j * increment));
				}
			}
		}

		return itemSet.OrderBy(x => x).ToHashSet();
	}

	/// <summary>
	/// Reads data from an xml itemDictionary and returns a dictionary of the data.
	/// </summary>
	private Dictionary<int, int> GetXmlDictionary(XElement xmlElements, string gameName)
	{
		Dictionary<int, int> itemDict = xmlElements.Elements("itemDictionary")
			.Where(x => x.Attribute("game")!.Value == gameName).Elements("item")
			.ToDictionary(x => int.Parse(x.Attribute("key")!.Value), 
				x => int.Parse(x.Attribute("value")!.Value)); 
					
		List<XElement> itemRanges = xmlElements.Elements("itemDictionary")
			.Where(x => x.Attribute("game")!.Value == gameName)
			.Elements("itemRange").ToList();
		foreach (XElement x in itemRanges)
		{
			int repeat = int.Parse(x.Attribute("repeat")!.Value);
			int keyIncrement = int.Parse(x.Attribute("keyIncrement")!.Value);
			int valueIncrement = int.Parse(x.Attribute("valueIncrement")!.Value);
			for (int j = 0; j < repeat; j++)
			{
				List<XElement> animationsInRange = x.Elements("item").ToList();
				foreach (XElement y in animationsInRange)
				{
					itemDict.Add(int.Parse(y.Attribute("key")!.Value) + (j * keyIncrement),
						int.Parse(y.Attribute("value")!.Value) + (j * valueIncrement));
				}
			}
		}

		return itemDict;
	}
}