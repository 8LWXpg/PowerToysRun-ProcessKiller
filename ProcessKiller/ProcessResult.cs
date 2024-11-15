using Community.PowerToys.Run.Plugin.ProcessKiller.Properties;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wox.Plugin.Common.Win32;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

internal class ProcessResult
{
	public Process Process { get; }

	/// <summary>
	/// Fuzzy search score
	/// </summary>
	public int Score { get; }

	/// <summary>
	/// Fuzzy search match data
	/// </summary>
	public List<int>? MatchData { get; }

	/// <summary>
	/// Full path to the process executable
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// Memory usage of the process
	/// </summary>
	public long MemoryUsage { get; }

	public string? CommandLine { get; }

	public ProcessResult(Process process, int score, List<int> matchData, CommandLineQuery commandLineQuery)
	{
		Process = process;
		Score = score;
		MatchData = matchData;
		Path = TryGetProcessFilename(process);
		CommandLine = commandLineQuery.GetCommandLine(process.Id);
		MemoryUsage = process.WorkingSet64;
	}

	public ProcessResult(Process process, int score, List<int> matchData)
	{
		Process = process;
		Score = score;
		MatchData = matchData;
		Path = TryGetProcessFilename(process);
		MemoryUsage = process.WorkingSet64;
	}

	public ProcessResult(Process process, CommandLineQuery commandLineQuery)
	{
		Process = process;
		Score = 0;
		Path = TryGetProcessFilename(process);
		CommandLine = commandLineQuery.GetCommandLine(process.Id);
		MemoryUsage = process.WorkingSet64;
	}

	public ProcessResult(Process process)
	{
		Process = process;
		Score = 0;
		Path = TryGetProcessFilename(process);
		MemoryUsage = process.WorkingSet64;
	}

	public string GetToolTipText(bool showCommandLine)
	{
		var textBuilder = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(Process.MainWindowTitle))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_main_window}:\n  {Process.MainWindowTitle}");
		}

		_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_memory}: {FormatMemorySize(MemoryUsage)}");

		if (!string.IsNullOrWhiteSpace(Path))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_path}:\n  {Path}");
		}

		if (showCommandLine && !string.IsNullOrWhiteSpace(CommandLine))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_command_line}:\n  {CommandLine}");
		}

		if (textBuilder.Length > 0)
		{
			textBuilder.Length -= 2; // Length of "\r\n"
		}

		return textBuilder.ToString();
	}

	private static string TryGetProcessFilename(Process p)
	{
		try
		{
			var capacity = 2000;
			StringBuilder builder = new(capacity);
			var ptr = NativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
			return QueryFullProcessImageName(ptr, 0, builder, ref capacity) ? builder.ToString() : string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool QueryFullProcessImageName(
		[In] IntPtr hProcess,
		[In] int dwFlags,
		[Out] StringBuilder lpExeName,
		ref int lpdwSize);

	private static string FormatMemorySize(long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB" };
		int order = 0;
		double mem = bytes;
		while (mem >= 1024 && order < sizes.Length - 1)
		{
			order++;
			mem /= 1024;
		}
		return $"{mem:0.##} {sizes[order]}";
	}
}
