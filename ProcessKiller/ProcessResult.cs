using Community.PowerToys.Run.Plugin.ProcessKiller.Properties;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wox.Infrastructure;
using Wox.Plugin;
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

	public bool IconFallback { get; }

	/// <summary>
	/// Memory usage of the process
	/// </summary>
	public long MemoryUsage { get; }

	public string? CommandLine { get; }

	public ProcessResult(Process process, MatchResult matchResult, CommandLineQuery? commandLineQuery)
	{
		Process = process;
		Score = matchResult.Score;
		MatchData = matchResult.MatchData;
		(IconFallback, Path) = TryGetProcessFilename(process);
		MemoryUsage = process.WorkingSet64;
		if (commandLineQuery is not null)
		{
			CommandLine = commandLineQuery.GetCommandLine(process.Id);
		}
	}

	public Result ToResult(string rawQuery, bool showCommandLine, string fallbackIcon, PluginInitContext context)
	{
		return new Result()
		{
			Title = $"{Process.ProcessName} - {Process.Id}",
			SubTitle = Path,
			IcoPath = IconFallback ? fallbackIcon : Path,
			Score = Score,
			TitleHighlightData = MatchData,
			ToolTipData = new ToolTipData($"{Process.ProcessName} - {Process.Id}", GetToolTipText(showCommandLine)),
			ContextData = Process,
			Action = c =>
			{
				_ = ProcessHelper.TryKill(Process);
				context.API.ChangeQuery(rawQuery, true);
				return true;
			}
		};
	}

	public string GetToolTipText(bool showCommandLine)
	{
		var textBuilder = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(Process.MainWindowTitle))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_main_window}:\n  {Process.MainWindowTitle}");
		}

		_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_memory}:\n  {FormatMemorySize(MemoryUsage)}");

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

	/// <summary>
	/// Try to get path of the process. If not, returns process name.
	/// </summary>
	/// <param name="p"></param>
	/// <returns></returns>
	private static (bool, string) TryGetProcessFilename(Process p)
	{
		try
		{
			var capacity = 2000;
			StringBuilder builder = new(capacity);
			var ptr = NativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
			return QueryFullProcessImageName(ptr, 0, builder, ref capacity) ?
				(false, builder.ToString()) :
				(true, p.ProcessName);
		}
		catch
		{
			return (true, p.ProcessName);
		}
	}

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool QueryFullProcessImageName(
		[In] IntPtr hProcess,
		[In] int dwFlags,
		[Out] StringBuilder lpExeName,
		ref int lpdwSize);

	private const double KB = 1024;
	private const double MB = KB * 1024;
	private const double GB = MB * 1024;
	public static string FormatMemorySize(long mem) => (double)mem switch
	{
		< KB => $"{mem:0.##} B",
		< MB => $"{mem / KB:0.##} KB",
		< GB => $"{mem / MB:0.##} MB",
		_ => $"{mem / GB:0.##} GB"
	};
}
