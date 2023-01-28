using DS3PortingTool.Converter;
using SoulsAssetPipeline.Animation;
using SoulsFormats;

namespace DS3PortingTool;

static class Program
{
	public static void Main(string[] args)
	{
		Options op = new(args);

		Converter.Converter conv;

		switch (op.Game.Type)
		{
			case Game.GameTypes.Bloodborne:
				conv = new BloodborneConverter();
				break;
			case Game.GameTypes.Sekiro:
				conv = new SekiroConverter();
				break;
			case Game.GameTypes.EldenRing:
				conv = new EldenRingConverter();
				break;
			default:
				throw new ArgumentException("The game this binder originates from is not supported.");
		}

		for (int i = 0; i < op.SourceBnds.Length; i++)
		{
			op.CurrentSourceFileName = op.SourceFileNames[i];
			op.CurrentSourceBnd = op.SourceBnds[i];
			
			conv.DoConversion(op);
		}
	}
}
