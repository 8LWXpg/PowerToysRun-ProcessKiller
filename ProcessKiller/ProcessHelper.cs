using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin.Common.Win32;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

internal partial class ProcessHelper
{
	private static readonly HashSet<string> SystemProcessList =
	[
		"conhost",
		"svchost",
		"idle",
		"system",
		"rundll32",
		"csrss",
		"lsass",
		"lsm",
		"smss",
		"wininit",
		"winlogon",
		"services",
		"spoolsv",
		// Used by this Plugin
		"wmiprvse",
	];

	private static bool IsSystemProcess(Process p) => SystemProcessList.Contains(p.ProcessName.ToLower());

	private static uint GetProcessIDFromWindowHandle(IntPtr hwnd)
	{
		_ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
		return processId;
	}

	/// <summary>
	/// Returns a ProcessResult for every running non-system process whose name matches the given search
	/// </summary>
	public static List<ProcessResult> GetMatchingProcesses(string search, bool showCommandLine)
	{
		var shellWindowId = GetProcessIDFromWindowHandle(NativeMethods.GetShellWindow());
		var processes = Process.GetProcesses().Where(p => !IsSystemProcess(p) && p.Id != shellWindowId).ToList();
		CommandLineQuery? commandLineQuery = null;
		if (showCommandLine)
		{
			commandLineQuery = new CommandLineQuery();
		}

		var portQuery = new PortQuery();

		if (string.IsNullOrWhiteSpace(search))
		{
			return processes.ConvertAll(p => showCommandLine ?
				new ProcessResult(p, commandLineQuery!, portQuery) :
				new ProcessResult(p, portQuery));
		}

		List<ProcessResult> results = [];
		foreach (Process p in processes)
		{
			MatchResult matchResult = StringMatcher.FuzzySearch(search, $"{p.ProcessName} - {p.Id}");
			var score = matchResult.Score;
			if (score > 0)
			{
				results.Add(showCommandLine ?
					new ProcessResult(p, score, matchResult.MatchData, commandLineQuery!, portQuery) :
					new ProcessResult(p, score, matchResult.MatchData, portQuery));
			}
		}

		return results;
	}

	public static void TryKill(Process p)
	{
		try
		{
			if (!p.HasExited)
			{
				p.Kill();
				_ = p.WaitForExit(50);
			}
		}
		catch (Exception e)
		{
			Log.Exception($"Failed to kill process {p.ProcessName}", e, typeof(ProcessHelper));
		}
	}
}
