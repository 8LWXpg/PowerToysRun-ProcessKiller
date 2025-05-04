using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
internal class PortQuery
{
	public readonly Dictionary<string, Process> Query;

	/// <summary>
	/// parse output from <c>netstat.exe</c>
	/// </summary>
	public PortQuery()
	{
		var process = new Process
		{
			StartInfo = new()
			{
				FileName = "netstat.exe",
				Arguments = "-a -n -o",
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			}
		};
		_ = process.Start();

		var processes = Process.GetProcesses().Where(p => !ProcessHelper.IsSystemProcess(p)).ToList();
		Query = [];
		foreach (var row in process.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
		{
			var elements = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var localAddress = elements[1];
			var pid = int.Parse(elements.Length > 4 ? elements[4] : elements[3]);
			Process? pr = processes.FirstOrDefault(e => e.Id == pid);
			if (pr == null)
			{
				continue;
			}

			// There should be only one process using that address and port
			Query[localAddress] = pr;
		}
	}

	public List<Result> GetMatchingResults(string search, string rawQuery, bool showCommandLine, string fallbackIcon, PluginInitContext context)
	{
		CommandLineQuery? commandLineQuery = showCommandLine ? new() : null;

		List<Result> results = [.. Query
			.Select(e =>
			{
				MatchResult matchResult = StringMatcher.FuzzySearch(search, e.Key);
				Process process = e.Value;
				var result = new ProcessResult(process, matchResult, commandLineQuery, rawQuery, showCommandLine, fallbackIcon, context)
				{
					Title = e.Key,
					QueryTextDisplay = $":{search}"
				};
				return (Result)result;
			})];

		if (!string.IsNullOrWhiteSpace(search))
		{
			_ = results.RemoveAll(r => r.Score <= 0);
		}

		return results;
	}
}
