using System.Management;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
internal class CommandLineQuery
{
	public readonly Dictionary<int, string> query = [];

	public CommandLineQuery()
	{
		var query = "SELECT ProcessId, CommandLine FROM Win32_Process";
		var searcher = new ManagementObjectSearcher(query);
		foreach (ManagementBaseObject? obj in searcher.Get())
		{
			var processId = Convert.ToInt32(obj["ProcessId"]);
			var commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
			this.query[processId] = commandLine;
		}
	}

	public string? GetCommandLine(int processId) => query.GetValueOrDefault(processId);
}
