namespace DS3PortingTool;
class Program
{
	public static void Main(string[] args)
	{
		Options op = new Options(args);

		Converter conv;

		switch (op.Game.Type)
		{
			case Game.GameTypes.Bloodborne:
				conv = new BloodborneConverter();
				break;
			case Game.GameTypes.Sekiro:
				conv = new SekiroConverter();
				break;
			default:
				throw new ArgumentException("The game this binder originates from is not supported.");
		}
		
		conv.DoConversion(op);
	}
}
