using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
internal class PortQuery
{
	public readonly Dictionary<string, HashSet<Process>> Query;

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

		IEnumerable<Process> processes = Process.GetProcesses().Where(p => !ProcessHelper.SystemProcessList.Contains(p.ProcessName.ToLower()));
		Query = [];
		foreach (var row in process.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
		{
			var elements = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var localAdderss = elements[1];
			var pid = int.Parse(elements.Length > 4 ? elements[4] : elements[3]);
			Process? pr = processes.FirstOrDefault(e => e.Id == pid);
			if (pr == null)
			{
				continue;
			}

			if (Query.TryGetValue(localAdderss, out HashSet<Process>? value))
			{
				_ = value.Add(pr);
			}
			else
			{
				Query[localAdderss] = [pr];
			}
		}
	}

	public List<Result> GetMatchingResults(string search, string iconPath)
	{
		if (string.IsNullOrWhiteSpace(search))
		{
			return Query.ToList().ConvertAll(e =>
			{
				var values = e.Value.ToList();
				var result = new Result
				{
					Title = $"{e.Key}",
					SubTitle = $"{string.Join(", ", values.ConvertAll(e => e.ProcessName))}",
					Score = 0,
					IcoPath = iconPath,
					QueryTextDisplay = ":",
					Action = _ =>
					{
						values.ForEach(e => ProcessHelper.TryKill(e));
						return true;
					}
				};
				return result;
			});
		}

		List<Result> results = [];
		foreach ((var localAddress, HashSet<Process>? pr) in Query)
		{
			MatchResult match = StringMatcher.FuzzySearch(search, localAddress);
			if (match.Score <= 0)
			{
				continue;
			}

			var values = pr.ToList();
			results.Add(new Result
			{
				Title = $"{localAddress}",
				SubTitle = $"{string.Join(", ", values.ConvertAll(e => e.ProcessName))}",
				Score = match.Score,
				IcoPath = iconPath,
				QueryTextDisplay = $": {search}",
				TitleHighlightData = match.MatchData,
				Action = _ =>
				{
					values.ForEach(e => ProcessHelper.TryKill(e));
					return true;
				}
			});
		}

		return results;
	}
}
