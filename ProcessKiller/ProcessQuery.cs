using System;
using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common.Win32;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

public static class ProcessQuery
{
	public static List<Result> GetMatchingResults(string search, string rawQuery, bool showCommandLine, bool showShellExplorer, string fallbackIcon, PluginInitContext context)
	{
		var excludeId = showShellExplorer ? null : (int?)ProcessHelper.GetProcessIDFromWindowHandle(NativeMethods.GetShellWindow());
		List<Process> processes = ProcessHelper.GetNonSystemProcesses(excludeId);
		CommandLineQuery? commandLineQuery = showCommandLine ? new() : null;

		List<Result> results = processes
			.ConvertAll(p =>
			{
				MatchResult matchResult = StringMatcher.FuzzySearch(search, $"{p.ProcessName} - {p.Id}");
				return (Result)new ProcessResult(p, matchResult, commandLineQuery, rawQuery, showCommandLine, fallbackIcon, context);
			});

		if (!string.IsNullOrWhiteSpace(search))
		{
			foreach (var r in results.Where(r => r.Score <= 0))
			{
				((Process)r.ContextData).Dispose();
			}

			_ = results.RemoveAll(r => r.Score <= 0);
		}

		return results;
	}
}
