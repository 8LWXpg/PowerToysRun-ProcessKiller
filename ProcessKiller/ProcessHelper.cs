using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
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
		"secure system",
		"memory compression",
		"registry",
		"rundll32",
		"csrss",
		"lsass",
		"lsaiso",
		"ngciso",
		"smss",
		"wininit",
		"winlogon",
		"services",
		"spoolsv",
		"wmiprvse",
		"dwm",
		"fontdrvhost",
		"audiodg",
		"dashost",
		"wudfhost",
		"wudfcompanionhost",
		"wlanext",
		"searchindexer",
		"securityhealthservice",
		"msmpeng",
		"nissrv",
	];

	private static bool IsSystemProcess(Process p) => SystemProcessList.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase);

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

	/// <summary>
	/// Command line of a process, or null when it cannot be read.
	/// </summary>
	public static unsafe string? GetCommandLine(Process p)
	{
		var ptr = NativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
		using SafeProcessHandle handle = new(ptr, true);
		if (handle.IsInvalid)
		{
			return null;
		}

		var process = (HANDLE)handle.DangerousGetHandle();
		uint size = 0;

		// The first call only reports how big the answer is.
		if (Windows.Wdk.PInvoke.NtQueryInformationProcess(process, PROCESSINFOCLASS.ProcessCommandLineInformation, null, 0, ref size) != NTSTATUS.STATUS_INFO_LENGTH_MISMATCH || size == 0)
		{
			return null;
		}

		var buffer = new byte[size];
		fixed (byte* info = buffer)
		{
			if (Windows.Wdk.PInvoke.NtQueryInformationProcess(process, PROCESSINFOCLASS.ProcessCommandLineInformation, info, size, ref size).SeverityCode != NTSTATUS.Severity.Success)
			{
				return null;
			}

			// The text follows the UNICODE_STRING header, whose Buffer points back into it.
			var value = (UNICODE_STRING*)info;
			return value->Length == 0 ? null : new string(value->Buffer, 0, value->Length / 2);
		}
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
