using System.Diagnostics;

namespace Community.PowerToys.Run.Plugin.ProcessKiller
{
	internal class ProcessResult(Process process, int score)
	{
		public Process Process { get; } = process;

		public int Score { get; } = score;
	}
}
