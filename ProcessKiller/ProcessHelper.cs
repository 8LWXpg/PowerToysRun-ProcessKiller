using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin.Common.Win32;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

internal partial class ProcessHelper
{
	public static readonly HashSet<string> SystemProcessList =
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
	public static List<ProcessResult> GetMatchingProcesses(string search, bool showCommandLine, bool showShellExplorer)
	{
		var shellWindowId = GetProcessIDFromWindowHandle(NativeMethods.GetShellWindow());
		var processes = Process.GetProcesses().Where(p => !IsSystemProcess(p) && (p.Id != shellWindowId || showShellExplorer)).ToList();
		CommandLineQuery? commandLineQuery = null;
		if (showCommandLine)
		{
			commandLineQuery = new CommandLineQuery();
		}

		if (string.IsNullOrWhiteSpace(search))
		{
			return processes.ConvertAll(p => showCommandLine ?
				new ProcessResult(p, commandLineQuery!) :
				new ProcessResult(p));
		}

		List<ProcessResult> results = [];
		foreach (Process p in processes)
		{
			MatchResult matchResult = StringMatcher.FuzzySearch(search, $"{p.ProcessName} - {p.Id}");
			var score = matchResult.Score;
			if (score > 0)
			{
				results.Add(showCommandLine ?
					new ProcessResult(p, score, matchResult.MatchData, commandLineQuery!) :
					new ProcessResult(p, score, matchResult.MatchData));
			}
		}

		return results;
	}

	public static bool TryKill(Process p)
	{
		try
		{
			if (!p.HasExited)
			{
				p.Kill();
				return p.WaitForExit(50);
			}
		}
		catch (Exception e)
		{
			Log.Exception($"Failed to kill process {p.ProcessName}", e, typeof(ProcessHelper));
		}

		return false;
	}
}
