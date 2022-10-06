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

        public Game()
        {
            Type = GameTypes.Other;
        }
    }
}