using SoulsFormats;
using ArgumentException = System.ArgumentException;

namespace DS3PortingTool
{
    public class Options
    {
        /// <summary>
        /// The current-working directory.
        /// </summary>
        public string Cwd { get; }
        /// <summary>
        /// Name of the source dcx file without the path.
        /// </summary>
        public string SourceFileName { get; }
        /// <summary>
        /// The binder where data being ported is sourced from.
        /// </summary>
        public IBinder SourceBnd { get;  }
        /// <summary>
        /// The game that the source binder comes from.
        /// </summary>
        public Game Game { get; }
        /// <summary>
        /// The character id of the source binder.
        /// </summary>
        public string SourceChrId { get; }
        /// <summary>
        /// The character id of the ported binder.
        /// </summary>
        public string PortedChrId { get; }
        
        /// <summary>
        /// The character id that sound events will use.
        /// </summary>
        public string SoundChrId { get; }
        /// <summary>
        /// Flag setting which if true means only the tae will be ported when porting an anibnd.
        /// </summary>
        public bool PortTaeOnly { get; }
        /// <summary>
        /// Flag setting which if true means that sound ids will be changed to match new character id.
        /// </summary>
        public bool ChangeSoundIds { get; }
        /// <summary>
        /// List of animation offsets which are excluded when porting an anibnd.
        /// </summary>
        public List<int> ExcludedAnimOffsets { get; }

        public Options(string[] args)
        {
            Cwd = AppDomain.CurrentDomain.BaseDirectory;
            SourceChrId = "";
            PortedChrId = "";
            SoundChrId = "";
            ChangeSoundIds = true;
            ExcludedAnimOffsets = new();
            
            string? sourceFile = Array.Find(args, x => File.Exists(x) && 
                                                       Path.GetFileName(x).Contains(".dcx"));
            if (sourceFile == null)
            {
                throw new ArgumentException("No path to a source binder found in arguments.");
            }

            SourceFileName = Path.GetFileName(sourceFile);

            if (BND4.Is(sourceFile))
            {
                SourceBnd = BND4.Read(sourceFile);
            }
            else
            {
                SourceBnd = BND3.Read(sourceFile);
            }

            Game = new(SourceBnd);
            
            List<int> flagIndices = args.Where(x => x.Length == 2 && x.Substring(0, 1).Equals("-"))
                .Select(x => Array.IndexOf(args, x))
                .Where(x => x != Array.IndexOf(args, sourceFile)).ToList();
			
            if (!flagIndices.Any())
            {
                Console.Write("Enter flags: ");
                string? flagString = Console.ReadLine();
                if (flagString != null)
                {
                    string[] flagArgs = flagString.Split(" ");
                    flagIndices = flagArgs.Where(x => x.Length == 2 && x.Substring(0, 1).Equals("-"))
                        .Select(x => Array.IndexOf(flagArgs, x)).ToList();
                    args = flagArgs.Concat(args).ToArray();
                }
            }
            
            if (Path.GetFileName(sourceFile).Substring(1, 4).All(char.IsDigit))
            {
                SourceChrId = Path.GetFileName(sourceFile).Substring(1, 4);
                PortedChrId = SourceChrId;
            }
			
            foreach (var i in flagIndices)
            {
                if (args[i].Equals("-t"))
                {
                    PortTaeOnly = true;
                }
                else if (args[i].Equals("-c"))
                {
                    if (args.Length < i + 1)
                    {
                        throw new ArgumentException($"Flag '-c' used, but no character id provided.");
                    }
                    if (args[i + 1].Length != 4 || !args[i + 1].All(char.IsDigit))
                    {
                        throw new ArgumentException($"Character id after flag '-c' must be a 4 digit number.");
                    }

                    PortedChrId = args[i + 1];
                    if (SoundChrId.Equals(""))
                    {
                        SoundChrId = PortedChrId;
                    }
                }
                else if (args[i].Equals("-o"))
                {
                    if (args.Length < i + 1)
                    {
                        throw new ArgumentException($"Flag '-o' used, but no offsets provided.");
                    }
                    ExcludedAnimOffsets = args[i + 1].Split(',')
                        .Where(x => x.All(char.IsDigit) && x.Length == 1)
                        .Select(Int32.Parse).ToList();
                }
                else if (args[i].Equals("-s"))
                {
                    if (args.Length < i + 1)
                    {
                        ChangeSoundIds = false;
                    }
                    else if (flagIndices.Contains(i + 1) || i + 1 == Array.IndexOf(args, sourceFile))
                    {
                        ChangeSoundIds = false;
                    }
                    else if (args[i + 1].Length != 4 || !args[i + 1].All(char.IsDigit))
                    {
                        throw new ArgumentException($"Character id after flag '-s' must be a 4 digit number.");
                    }

                    SoundChrId = args[i + 1];
                }
                else if (!args[i].Equals("-x"))
                {
                    throw new ArgumentException($"Unknown flag: {args[i]}");
                }
            }
        }
    }
}