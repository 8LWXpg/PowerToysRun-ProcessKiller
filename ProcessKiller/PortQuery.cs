using System.Diagnostics;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;
internal class PortQuery
{
	public readonly Dictionary<int, string> Query;

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
		Query = process.StandardOutput.ReadToEnd().Split('\n').Skip(4)
			.Select(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			.ToDictionary(e => int.Parse(e.Length > 4 ? e[4] : e[3]), e => e[1]);
	}
}
