using Community.PowerToys.Run.Plugin.ProcessKiller.Properties;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common.Win32;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

public class ProcessResult : Result
{
	public ProcessResult(Process process, MatchResult matchResult, CommandLineQuery? commandLineQuery, string rawQuery, bool showCommandLine, string fallbackIcon, PluginInitContext context)
	{
		(var iconFallback, var path) = TryGetProcessFilename(process);
		var commandLine = commandLineQuery is null ? null : commandLineQuery.GetCommandLine(process.Id);

		Title = $"{process.ProcessName} - {process.Id}";
		SubTitle = path;
		IcoPath = iconFallback ? fallbackIcon : path;
		Score = matchResult.Score;
		TitleHighlightData = matchResult.MatchData;
		ToolTipData = new ToolTipData($"{process.ProcessName} - {process.Id}", GetToolTipText(process, process.WorkingSet64, path, showCommandLine, commandLine));
		ContextData = process;
		Action = c =>
		{
			_ = ProcessHelper.TryKill(process);
			context.API.ChangeQuery(rawQuery, true);
			return true;
		};
	}

	private static string GetToolTipText(Process process, long memoryUsage, string path, bool showCommandLine, string? commandLine)
	{
		var textBuilder = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_main_window}:\n  {process.MainWindowTitle}");
		}

		_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_memory}:\n  {FormatMemorySize(memoryUsage)}");
		_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_path}:\n  {path}");

		if (showCommandLine && !string.IsNullOrWhiteSpace(commandLine))
		{
			_ = textBuilder.AppendLine($"{Resources.plugin_tool_tip_command_line}:\n  {commandLine}");
		}

		textBuilder.Length -= 2; // Length of "\r\n"
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
