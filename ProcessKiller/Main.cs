using Community.PowerToys.Run.Plugin.ProcessKiller.Properties;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Diagnostics;
using System.Windows.Controls;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable
{
	private PluginInitContext? _context;

	private bool _disposed;

	public string Name => Resources.plugin_name;
	public string Description => Resources.plugin_description;
	public static string PluginID => "78844AE082E24C0C8AC9DB222FF67317";

	private const string KillAllCount = nameof(KillAllCount);
	private const string ShowCommandLine = nameof(ShowCommandLine);
	private int? _killAllCount;
	private bool _showCommandLine;
	private string? _portIcon;

	public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
	[
		new()
		{
			PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
			Key = KillAllCount,
			DisplayLabel = Resources.plugin_setting_kill_all_count,
			NumberValue = 5,
			NumberBoxMin = 2,
		},
		new()
		{
			PluginOptionType= PluginAdditionalOption.AdditionalOptionType.Checkbox,
			Key = ShowCommandLine,
			DisplayLabel = Resources.plugin_setting_show_command_line,
			DisplayDescription = Resources.plugin_setting_show_command_line_description,
		}
	];

	public void UpdateSettings(PowerLauncherPluginSettings settings)
	{
		_killAllCount = (int?)(settings?.AdditionalOptions?.FirstOrDefault(static x => x.Key == KillAllCount)?.NumberValue) ?? 5;
		_showCommandLine = settings?.AdditionalOptions?.FirstOrDefault(static x => x.Key == ShowCommandLine)?.Value ?? false;
	}

	public List<Result> Query(Query query)
	{
		var search = query.Search;
		if (search.StartsWith(':'))
		{
			return new PortQuery().GetMatchingResults(search[1..], _portIcon!);
		}

		List<ProcessResult> processes = ProcessHelper.GetMatchingProcesses(search, _showCommandLine);

		if (processes.Count == 0)
		{
			return [];
		}

		List<Result> sortedResults = processes.ConvertAll(pr =>
			{
				Process p = pr.Process;
				var path = pr.Path;
				return new Result()
				{
					IcoPath = path,
					Title = $"{p.ProcessName} - {p.Id}",
					SubTitle = path,
					TitleHighlightData = pr.MatchData,
					Score = pr.Score,
					ContextData = p,
					ToolTipData = new ToolTipData($"{p.ProcessName} - {p.Id}", pr.GetToolTipText(_showCommandLine)),
					Action = c =>
					{
						_ = ProcessHelper.TryKill(p);
						_context!.API.ChangeQuery(query.RawQuery, true);
						return true;
					}
				};
			});
		sortedResults.Reverse();

		// When there are multiple results AND all of them are instances of the same executable
		// add a quick option to kill them all at the top of the results.
		Result? topResult = sortedResults.OrderByDescending(e => e.Score).First();
		IEnumerable<Result> killAll = sortedResults.Where(r => !string.IsNullOrEmpty(r.SubTitle) && r.SubTitle == topResult?.SubTitle);
		if (processes.Count > 1 && !string.IsNullOrEmpty(search) && killAll.Count() >= _killAllCount)
		{
			var name = ((Process)topResult?.ContextData!)?.ProcessName;
			var totalMemory = killAll.Sum(r => ((Process)r.ContextData).WorkingSet64);
			sortedResults.Insert(1, new Result()
			{
				IcoPath = topResult?.IcoPath,
				Title = string.Format(Resources.plugin_kill_all, name),
				SubTitle = string.Format(Resources.plugin_kill_all_count, killAll.Count()),
				ToolTipData = new ToolTipData(name, $"{Resources.plugin_tool_tip_memory}:\n  {ProcessResult.FormatMemorySize(totalMemory)}"),
				Score = 200,
				Action = c =>
				{
					// Kill all processes asynchronously
					IEnumerable<Task<bool>> killTasks = killAll.Select(async r =>
					{
						var p = (Process)r.ContextData;
						return await Task.Run(() => ProcessHelper.TryKill(p));
					});
					_ = Task.WhenAll(killTasks);

					_context!.API.ChangeQuery(query.RawQuery, true);
					return true;
				}
			});
		}

		return sortedResults;
	}

	public void Init(PluginInitContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_context.API.ThemeChanged += OnThemeChanged;
		UpdateIconPath(_context.API.GetCurrentTheme());
	}

	private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

	private void UpdateIconPath(Theme theme) => _portIcon = theme is Theme.Light or Theme.HighContrastWhite ? "Images\\Port.light.png" : "Images\\Port.dark.png";

	public string GetTranslatedPluginTitle() => Resources.plugin_name;

	public string GetTranslatedPluginDescription() => Resources.plugin_description;

	public Control CreateSettingPanel() => throw new NotImplementedException();

	public void ReloadData()
	{
		if (_context is null)
		{
			return;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed && disposing)
		{
			_disposed = true;
		}
	}
}
