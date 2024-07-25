using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
internal class PortQuery
{
	public readonly Dictionary<string, List<int>> Query;

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
		Query = [];
		foreach (var row in process.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Skip(2))
		{
			var elements = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var localAdderss = elements[1];
			var port = int.Parse(elements.Length > 4 ? elements[4] : elements[3]);
			if (Query.TryGetValue(localAdderss, out List<int>? value))
			{
				value.Add(port);
			}
			else
			{
				Query[localAdderss] = [port];
			}
		}
	}

	public List<Result> GetMatchingResults(string search)
	{
		if (string.IsNullOrWhiteSpace(search))
		{
			return Query.ToList().ConvertAll(e => new Result
			{
				Title = $"{e.Key}",
				SubTitle = "Kill all processes using this pid",
				Score = 0,
				Action = _ =>
				{
					e.Value.ForEach(pid => ProcessHelper.TryKill(Process.GetProcessById(pid)));
					return true;
				}
			});
		}

		List<Result> results = [];
		foreach ((var localAddress, List<int> pids) in Query)
		{
			MatchResult match = StringMatcher.FuzzySearch(search, localAddress);
			if (match.Score <= 0)
			{
				continue;
			}

			results.Add(new Result
			{
				Title = $"{localAddress}",
				SubTitle = "Kill all processes using this pid",
				Score = match.Score,
				TitleHighlightData = match.MatchData.ConvertAll(e => e + 1),
				Action = _ =>
				{
					pids.ForEach(pid => ProcessHelper.TryKill(Process.GetProcessById(pid)));
					return true;
				}
			});
		}

		return results;
	}
}
