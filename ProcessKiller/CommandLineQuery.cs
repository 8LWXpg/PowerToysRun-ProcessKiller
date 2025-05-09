using System.Management;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
/// <summary>
/// Query all running processes and their command lines using WMI.
/// A lot faster than querying each process individually
/// </summary>
public class CommandLineQuery
{
	public readonly Dictionary<int, string?> query = [];

	/// <summary>
	/// This class initialization is slow, share the same instance instead of creating a new one.
	/// </summary>
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
