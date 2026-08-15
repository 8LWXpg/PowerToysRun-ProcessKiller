using System.Diagnostics;
using Wox.Infrastructure;
using Wox.Plugin.Common.Win32;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

internal static class ProcessHelper
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

	public static uint GetProcessIDFromWindowHandle(IntPtr hwnd)
	{
		_ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
		return processId;
	}

	/// <summary>
	/// Returns all non-system processes. Any process filtered out is disposed immediately
	/// so its handle isn't leaked.
	/// </summary>
	public static List<Process> GetNonSystemProcesses(int? excludeProcessId = null)
	{
		List<Process> result = [];
		foreach (Process p in Process.GetProcesses())
		{
			if (IsSystemProcess(p) || p.Id == excludeProcessId)
			{
				p.Dispose();
				continue;
			}

			result.Add(p);
		}

		return result;
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
