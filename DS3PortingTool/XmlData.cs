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
	public Dictionary<int, int> AnimationRemapping { get; }
	/// <summary>
	/// IDs of animations to exclude.
	/// </summary>
	public HashSet<int> ExcludedAnimations { get; }
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
	/// IDs of SpEffects to allow when a SpEffect event is excluded
	/// </summary>
	public HashSet<int> AllowedSpEffects { get; }

	public XmlData(Options op)
	{
	    string gameName = op.Game.Name;
	    
	    MaterialInfoBank = FLVER2MaterialInfoBank.ReadFromXML($"{op.Cwd}Res\\BankDS3.xml");
	    AnimationRemapping = GetXmlDictionary(XElement.Load($"{op.Cwd}\\Res\\AnimationRemapping.xml"), gameName);
	    ExcludedAnimations = GetXmlSet(XElement.Load($"{op.Cwd}\\Res\\ExcludedAnimations.xml"), gameName);
	    ExcludedEvents = GetXmlSet(XElement.Load($"{op.Cwd}\\Res\\ExcludedEvents.xml"), gameName);
	    ExcludedJumpTables = GetXmlSet(XElement.Load($"{op.Cwd}\\Res\\ExcludedJumpTables.xml"), gameName);
	    ExcludedRumbleCams = GetXmlSet(XElement.Load($"{op.Cwd}\\Res\\ExcludedRumbleCams.xml"), gameName);
	    AllowedSpEffects = GetXmlSet(XElement.Load($"{op.Cwd}\\Res\\AllowedSpEffects.xml"), gameName);
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