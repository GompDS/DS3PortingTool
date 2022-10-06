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
        public GameTypes Type { get; set; }

        /// <summary>
        /// Key-value pairs of game types and their respective names.
        /// </summary>
        public Dictionary<GameTypes, string> TypeNames = new()
        {
            { GameTypes.Ds1, "DS1" },
            { GameTypes.Sekiro, "Sekiro" },
            { GameTypes.EldenRing, "EldenRing" }
        };

        public Game(IBinder bnd)
        {
            if (bnd.Files.Any(x => x.Name.Contains(@"N:\FRPG\data\")))
            {
                Type = Game.GameTypes.Ds1;
            }
            else if (bnd.Files.Any(x => x.Name.Contains(@"N:\NTC\data\Target\INTERROOT_win64")))
            {
                Type = Game.GameTypes.Sekiro;
            }
            else if (bnd.Files.Any(x => x.Name.Contains(@"N:\GR\data\INTERROOT_win64")))
            {
                Type = Game.GameTypes.EldenRing;
            }
            else
            {
                Type = Game.GameTypes.Other;
            }

            if (Type != Game.GameTypes.Sekiro)
            {
                throw new ArgumentException(
                    "Source binder does not originate from Sekiro. Currently only Sekiro is supported.");
            }
        }
    }
}