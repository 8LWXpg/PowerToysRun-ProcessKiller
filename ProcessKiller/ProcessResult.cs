// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Community.PowerToys.Run.Plugin.ProcessKiller
{
    internal class ProcessResult(Process process, int score)
    {
        public Process Process { get; } = process;

        public int Score { get; } = score;
    }
}
