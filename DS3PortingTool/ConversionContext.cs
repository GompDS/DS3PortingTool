using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using SoulsGameSpecs;
using SoulsFormats;
using ArgumentException = System.ArgumentException;

namespace DS3PortingTool;

public static class ConversionContext
{
    /// <summary>
    /// The current-working directory.
    /// </summary>
    public static string Cwd { get; } = AppDomain.CurrentDomain.BaseDirectory;

    public static string ToolsDir { get; } = Cwd + "havok\\";
    /// <summary>
    /// File handlers for the assets supplied from the SourceGame
    /// </summary>
    public static GameAsset[] SourceAssets { get; private set; }
    /// <summary>
    /// The game that the source binder comes from.
    /// </summary>
    public static GameSpec SourceGame { get; private set; }
    /// <summary>
    /// The game that source assets will be converted to.
    /// </summary>
    public static GameSpec TargetGame { get; private set; }
    /// <summary>
    /// The id of the source binder.
    /// </summary>
    public static string SourceId { get; private set; } = string.Empty;
    /// <summary>
    /// The id of the ported binder.
    /// </summary>
    public static string PortedId { get; private set; } = string.Empty;
    /// <summary>
    /// The id that sound events will use.
    /// </summary>
    public static string SoundId { get; private set; } = string.Empty;
    /// <summary>
    /// The id that lock cam param events will use.
    /// </summary>
    public static string LockCamParamId { get; private set; } = string.Empty;
    /// <summary>
    /// Flag setting which if true means only the tae will be ported when porting an anibnd.
    /// </summary>
    public static bool PortTaeOnly { get; private set; }
    /// <summary>
    /// Flag setting which if true means only the flver will be ported if there is one.
    /// </summary>
    public static bool PortFlverOnly { get; private set; }
    /// <summary>
    /// Flag setting which if true means that sound ids will be changed to match new character id.
    /// </summary>
    public static bool ChangeSoundIds { get; private set; }
    /// <summary>
    /// Flag setting which if true means that lock cam param ids will be changed to match new character id.
    /// </summary>
    public static bool ChangeLockCamParamIds { get; private set; }
    /// <summary>
    /// List of animation offsets which are excluded when porting an anibnd.
    /// </summary>
    public static List<int> ExcludedAnimOffsets { get; } = new List<int>();

    private static readonly Dictionary<string, string> FilterNameLookup = new Dictionary<string, string>
    {
        ["-ni"] = "Change Numeric ID",
        ["-eo"] = "Exclude Animation Offsets",
        ["-si"] = "Use Alternate Sound ID Prefix",
        ["-lci"] = "Use Alternate LockCamParam ID"
    };
    
    private static readonly Dictionary<string, string> FilterUsageLookup = new Dictionary<string, string>
    {
        ["-ni"] = "    usage: --change_num_id|-ni old_id new_id",
        ["-eo"] = "    usage: --exclude-anim-offsets|-eo offset ...",
        ["-si"] = "    usage: --use-alt-sound-id-prefix|-si prefix",
        ["-lci"] = "    usage: --use-alt-lock-cam-param-id|-lci id"
    };

    private static readonly Dictionary<string, string[]> SupportedGamesLookup = new Dictionary<string, string[]>
    {
        ["source_game"] = ["sekiro_sdt__windows", "elden_ring__windows", "elden_ring_nightreign__windows"],
        ["target_game"] = ["dark_souls_3__windows"]
    };
    
    private static readonly Dictionary<string, GameAsset.FSAssetType[]> SupportedAssetTypesLookup =
        new Dictionary<string, GameAsset.FSAssetType[]>
    {
        ["elden_ring_nightreign__windows"] = [GameAsset.FSAssetType.CHRBND, GameAsset.FSAssetType.ANIBND],
    };

    private static readonly Dictionary<string, string[]> SupportedAssetTypesRegexLookup = new Dictionary<string, string[]>
    {
        ["elden_ring_nightreign__windows"] = [@"((\.chrbnd$)|\.chrbnd\.dcx$)", @"((\.anibnd$)|\.anibnd\.dcx$)"],
    };

    public static bool TryReadArguments(string[] args)
    {
        Queue<string> argQueue = new Queue<string>();
        foreach (string arg in args)
        {
            argQueue.Enqueue(arg.ToLower());
        }
        
        if (argQueue.Count == 0 || argQueue.Any(x => x is "--help" or "-h"))
        {
            return false;
        }

        if (!_ProcessFilters(argQueue)) return false;

        if (!_ProcessGame(argQueue, "source_game", out GameSpec? source_game)) return false;
        if (!_ProcessGame(argQueue, "target_game", out GameSpec? target_game)) return false;
        SourceGame = source_game;
        TargetGame = target_game;
        
        SourceAssets = new GameAsset[argQueue.Count];
        int i = 0;
        // ReSharper disable once InlineOutVariableDeclaration
        string nextAsset;
        while (argQueue.TryDequeue(out nextAsset!))
        {
            if (!File.Exists(nextAsset))
            {
                Console.Error.WriteLine($"$ERROR: asset does not exist @ \"{nextAsset}\".");
                return false;
            }

            if (!SupportedAssetTypesRegexLookup.TryGetValue(SourceGame.GameSpecKey, out string[]? assetTypePatterns))
            {
                Console.Error.WriteLine($"$ERROR: \"{SourceGame.GameSpecKey}\" has no supported asset types. Aborting.");
                return false;
            }

            
            for (int j = 0; j < assetTypePatterns.Length; j++)
            {
                if (Regex.IsMatch(nextAsset, assetTypePatterns[j]))
                {
                    SourceAssets[i] = new GameAsset(nextAsset, SourceGame, SupportedAssetTypesLookup[SourceGame.GameSpecKey][j]);
                    break;
                }

                if (j == assetTypePatterns.Length - 1)
                {
                    Console.Error.WriteLine("$ERROR: asset is not of a supported type");
                    return false;
                }
            }

            i++;
        }

        if (SourceAssets.Length == 0)
        {
            Console.Error.WriteLine("ERROR: At least one asset must be specified.");
            return false;
        }
        
        return true;
    }
    
    public static void PrintHelp()
    {
        Console.WriteLine("app usage: DS3PortingTool.exe [filters] <source_game> <target_game> <asset> ...");
        Console.WriteLine("    filters:");
        foreach (string key in FilterNameLookup.Keys)
        {
            Console.WriteLine($"        {FilterNameLookup[key],-30} : {FilterUsageLookup[key]}");
        }
        Console.WriteLine($"    source_game: {_FormatSupportedGames(SupportedGamesLookup["source_game"])}");
        Console.WriteLine($"    target_game: {_FormatSupportedGames(SupportedGamesLookup["target_game"])}");
        Console.WriteLine($"    asset: One or more files from source_game to be converted. Only certain file types are supported.");
        Console.WriteLine();
    }

    public static bool TryMatchAssetByFileName(string regex, out string? matchedAsset)
    {
        matchedAsset = null;
        
        foreach (string asset in SourceAssets.Select(x => x.FileName))
        {
            if (Regex.IsMatch(asset, regex))
            {
                matchedAsset = asset;
                return true;
            }
        }

        return false;
    }

    public static string[] MatchAssetsByFileName(string regex)
    {
        List<string> matches = new List<string>();
        
        foreach (string asset in SourceAssets.Select(x => x.FileName))
        {
            if (Regex.IsMatch(asset, regex))
            {
                matches.Add(asset);
            }
        }
        
        return matches.ToArray();
    }

    private static bool _ProcessGame(Queue<string> argQueue, string gameArg, out GameSpec? gameSpec)
    {
        gameSpec = null;
        
        try
        {
            if (!argQueue.TryDequeue(out string gameKey))
            {
                throw new ArgumentException($"{gameArg} must be specified." +
                                            "\n    Valid options include: " +
                                            $"{_FormatSupportedGames(SupportedGamesLookup[gameArg])}");
            }

            if (!SupportedGamesLookup[gameArg].Contains(gameKey))
            {
                throw new ArgumentException($"Specified {gameArg} is not supported." +
                                            "\n    Valid options include: " +
                                            $"{_FormatSupportedGames(SupportedGamesLookup[gameArg])}");
            }

            if (JsonHelper.ValidSpecsById?.TryGetValue(gameKey, out gameSpec) is null || gameSpec is null)
            {
                throw new ArgumentNullException($"GameSpec with key \"{gameKey}\" does not exist.");
            }
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine($"ERROR: {e.Message}");
            return false;
        }

        return true;
    }

    private static bool _ProcessFilters(Queue<string> argQueue)
    {
        while (argQueue.TryPeek(out string? nextArg) && nextArg.StartsWith("-"))
        {
            argQueue.Dequeue();
            
            switch (nextArg)
            {
                case "--do-tae-only":
                case "-to":
                    PortTaeOnly = true;
                    break;
                case "--do-flver-only":
                case "-fo":
                    PortFlverOnly = true;
                    break;
                case "--change_num_id":
                case "-ni":
                {
                    string oldId;
                    string newId;
                    
                    try
                    {
                        if (!argQueue.TryDequeue(out oldId!) ||
                            !argQueue.TryDequeue(out newId!))
                        {
                            throw new ArgumentException("Missing required arguments.");
                        }

                        if (!oldId.IsDigits() || !newId.IsDigits())
                        {
                            throw new ArgumentException("old_id and new_id must only contain digits.");
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.Error.WriteLine($"ERROR: Filter \"{FilterNameLookup["-ni"]}\": " + e.Message);
                        Console.Error.WriteLine(FilterUsageLookup["-ni"]);
                        return false;
                    }

                    SourceId = oldId;
                    PortedId = newId;
                    if (SoundId.Length == 0)
                    {
                        SoundId = PortedId;
                        ChangeSoundIds = true;
                    }

                    if (LockCamParamId.Length == 0)
                    {
                        LockCamParamId = PortedId;
                        ChangeLockCamParamIds = true;
                    }
                }
                    break;
                case "--exclude-anim-offsets":
                case "-eo":
                {
                    while (argQueue.TryDequeue(out string? nextOffset) &&
                           int.TryParse(nextOffset, out int numOffset))
                    {
                        ExcludedAnimOffsets.Add(numOffset);
                    }
                    
                    try
                    {
                        if (ExcludedAnimOffsets.Count == 0)
                        {
                            throw new ArgumentException("At least one offset (number) must be listed.");
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.Error.WriteLine($"ERROR: Filter \"{FilterNameLookup["-eo"]}\": " + e.Message);
                        Console.Error.WriteLine(FilterUsageLookup["-eo"]);
                        return false;
                    }
                }
                    break;
                case "--use-alt-sound-id-prefix":
                case "-si":
                {
                    string prefix;
                    
                    try
                    {
                        if (!argQueue.TryDequeue(out prefix!))
                        {
                            throw new ArgumentException("Missing required arguments.");
                        }
                        
                        if (!prefix.IsDigits())
                        {
                            throw new ArgumentException("prefix must only contain digits.");
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.Error.WriteLine($"ERROR: Filter \"{FilterNameLookup["-si"]}\": " + e.Message);
                        Console.Error.WriteLine(FilterUsageLookup["-si"]);
                        return false;
                    }

                    SoundId = prefix;
                    ChangeSoundIds = !SoundId.Equals(SourceId);
                }
                    break;
                case "--use-alt-lock-cam-param-id":
                case "-lci":
                {
                    string prefix;
                    
                    try
                    {
                        if (!argQueue.TryDequeue(out prefix!))
                        {
                            throw new ArgumentException("Missing required arguments.");
                        }
                        
                        if (!prefix.IsDigits())
                        {
                            throw new ArgumentException("id must only contain digits.");
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.Error.WriteLine($"ERROR: Filter \"{FilterNameLookup["-lci"]}\": " + e.Message);
                        Console.Error.WriteLine(FilterUsageLookup["-lci"]);
                        return false;
                    }

                    LockCamParamId = prefix;
                    ChangeLockCamParamIds = !LockCamParamId.Equals(SourceId);
                }
                    break;
                default:
                    Console.Error.WriteLine($"ERROR: unknown filter \"{nextArg}\".");
                    return false;
            }
        }

        return true;
    }

    private static string _FormatSupportedGames(string[] gamesList)
    {
        string result = string.Empty;
        
        for (int i = 0; i < gamesList.Length; i++)
        {
            result += gamesList[i];
            if (i < gamesList.Length - 1)
            {
                result += " | ";
            }
        }
        
        return result;
    }
}