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
				Arguments = "-a -n -o",
				FileName = "netstat.exe",
				RedirectStandardOutput = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			}
		};
		_ = process.Start();

		var processes = Process.GetProcesses().Where(p => !ProcessHelper.SystemProcessList.Contains(p.ProcessName.ToLower())).ToList();
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
		List<Result> results = Query.ToList().ConvertAll(e =>
		{
			MatchResult matchResult = StringMatcher.FuzzySearch(search, e.Key);
			Process process = e.Value;
			var result = (showCommandLine ?
				new ProcessResult(process, matchResult, new CommandLineQuery()) :
				new ProcessResult(process, matchResult))
				.ToResult(rawQuery, showCommandLine, fallbackIcon, context);
			result.Title = e.Key;
			result.QueryTextDisplay = $":{search}";
			return result;
		});

		if (!string.IsNullOrWhiteSpace(search))
		{
			_ = results.RemoveAll(r => r.Score <= 0);
		}

		return results;
	}
}
