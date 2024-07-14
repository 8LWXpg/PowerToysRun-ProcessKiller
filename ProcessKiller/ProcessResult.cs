using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wox.Plugin.Common.Win32;

namespace Community.PowerToys.Run.Plugin.ProcessKiller;

internal class ProcessResult(Process process, int score)
{
	public Process Process { get; } = process;

	/// <summary>
	/// Fuzzy search score
	/// </summary>
	public int Score { get; } = score;

	/// <summary>
	/// Full path to the process executable
	/// </summary>
	public string Path { get; } = TryGetProcessFilename(process);

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
}
