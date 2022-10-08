using SoulsFormats;

namespace DS3PortingTool
{
    public class Game
    {
        /// <summary>
        /// The different types of games that assets can be ported from using the tool.
        /// </summary>
        public enum GameTypes
        {
            Other,

            Ds1,
            
            Sekiro,
            
            EldenRing
        }

        /// <summary>
        /// The game that the source binder originates from.
        /// </summary>
        public GameTypes Type { get; }

        /// <summary>
        /// The name of the game the source binder originates from, used in extracting XmlData.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The base animation offset used by the game.
        /// </summary>
        public int Offset { get; }

        public Game(IBinder bnd)
        {
            if (bnd.Files.Any(x => x.Name.Contains(@"N:\FRPG\data\")))
            {
                Type = GameTypes.Ds1;
                Name = "Ds1";
                Offset = 1000000;
            }
            else if (bnd.Files.Any(x => x.Name.Contains(@"N:\NTC\data\Target\INTERROOT_win64")))
            {
                Type = GameTypes.Sekiro;
                Name = "Sekiro";
                Offset = 100000000;
            }
            else if (bnd.Files.Any(x => x.Name.Contains(@"N:\GR\data\INTERROOT_win64")))
            {
                Type = GameTypes.EldenRing;
                Name = "EldenRing";
                Offset = 1000000;
            }
            else
            {
                Type = GameTypes.Other;
                Name = "";
                Offset = -1;
            }

            if (Type != GameTypes.Sekiro)
            {
                throw new ArgumentException(
                    "Source binder does not originate from Sekiro. Currently only Sekiro is supported.");
            }
        }
    }
}