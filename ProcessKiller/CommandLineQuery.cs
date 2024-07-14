using System.Management;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
/// <summary>
/// Query all running processes and their command lines using WMI.
/// A lot faster then querying each process individually
/// </summary>
internal class CommandLineQuery
{
	public readonly Dictionary<int, string?> query = [];

	public CommandLineQuery()
	{
		var query = "SELECT ProcessId, CommandLine FROM Win32_Process";
		var searcher = new ManagementObjectSearcher(query);
		foreach (ManagementBaseObject? obj in searcher.Get())
		{
			var processId = Convert.ToInt32(obj["ProcessId"]);
			var commandLine = obj["CommandLine"]?.ToString();
			this.query[processId] = commandLine;
		}
	}

	public string? GetCommandLine(int processId) => query.GetValueOrDefault(processId);
}
