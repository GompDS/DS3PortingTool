using System.Diagnostics;

namespace DS3PortingTool;

public static class ProcessUtil
{
    /// <summary>
    ///	Run an external tool with the given arguments.
    /// </summary>
    public static bool TryRunProcess(string searchDir, string applicationName, string args)
    {
        string[] results = Directory.GetFiles(searchDir, $"{applicationName}",
            SearchOption.AllDirectories);
		
        if (results.Length == 0)
        {
            throw new FileNotFoundException($"The external application \"{applicationName}\" could not be found.");
        }

        string toolPath = results.First();
		
        Process tool = new Process();
        tool.StartInfo.FileName = toolPath;
        tool.StartInfo.Arguments = args;
        tool.StartInfo.WorkingDirectory = Path.GetDirectoryName(toolPath);
        tool.StartInfo.RedirectStandardOutput = true;
        tool.StartInfo.RedirectStandardError = true;
        tool.StartInfo.RedirectStandardInput = true;
        tool.Start();
        while (!tool.HasExited)
        {
            tool.StandardInput.Close();
        }

        return tool.StandardError.ReadToEnd().Length <= 0;
    }
}