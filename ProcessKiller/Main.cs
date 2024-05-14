using Community.PowerToys.Run.Plugin.ProcessKiller.Properties;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ProcessKiller
{
	public class Main : IPlugin, IPluginI18n, ISettingProvider, IReloadable, IDisposable
	{
		private PluginInitContext? _context;

		private bool _disposed;

		public string Name => Resources.plugin_name;
		public string Description => Resources.plugin_description;
		public static string PluginID => "78844AE082E24C0C8AC9DB222FF67317";

		private const string KillAllCount = nameof(KillAllCount);
		private int? _killAllCount;

		public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
		[
			new()
			{
				PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
				Key = KillAllCount,
				DisplayLabel = Resources.plugin_setting_kill_all_count,
				NumberValue = 5,
				NumberBoxMin = 2,
			}
		];

		public void UpdateSettings(PowerLauncherPluginSettings settings) => _killAllCount = (int?)(settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == KillAllCount)?.NumberValue) ?? 5;

		public List<Result> Query(Query query)
		{
			var search = query.Search;
			List<ProcessResult> processes = ProcessHelper.GetMatchingProcesses(search);

			if (processes.Count == 0)
			{
				return [];
			}

			var sortedResults = processes.ConvertAll(pr =>
				{
					System.Diagnostics.Process p = pr.Process;
					var path = ProcessHelper.TryGetProcessFilename(p);
					return new Result()
					{
						IcoPath = path,
						Title = $"{p.ProcessName} - {p.Id}",
						SubTitle = path,
						TitleHighlightData = StringMatcher.FuzzySearch(search, p.ProcessName).MatchData,
						Score = pr.Score,
						ContextData = p.ProcessName,
						Action = c =>
						{
							ProcessHelper.TryKill(p);
							// Re-query to refresh process list
							_context!.API.ChangeQuery(query.RawQuery, true);
							return true;
						}
					};
				}).OrderBy(x => x.Title).ToList();

			// When there are multiple results AND all of them are instances of the same executable
			// add a quick option to kill them all at the top of the results.
			Result? firstResult = sortedResults.FirstOrDefault(x => !string.IsNullOrEmpty(x.SubTitle));
			if (processes.Count > 1 && !string.IsNullOrEmpty(search) && sortedResults.Count(r => r.SubTitle == firstResult?.SubTitle) >= _killAllCount)
			{
				sortedResults.Insert(1, new Result()
				{
					IcoPath = firstResult?.IcoPath,
					Title = string.Format(Resources.plugin_kill_all, firstResult?.ContextData),
					SubTitle = string.Format(Resources.plugin_kill_all_count, processes.Count),
					Score = 200,
					Action = c =>
					{
						processes.ForEach(p => ProcessHelper.TryKill(p.Process));
						// Re-query to refresh process list
						_context!.API.ChangeQuery(query.RawQuery, true);
						return true;
					}
				});
			}

			return sortedResults;
		}

		public void Init(PluginInitContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

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
}
