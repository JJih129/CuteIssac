using System;
using System.Collections.Generic;

namespace CuteIssac.Core.Meta
{
    /// <summary>
    /// Persistent payload for account-level unlocks that survive between runs.
    /// </summary>
    [Serializable]
    public sealed class UnlockSaveData
    {
        public int Version = 1;
        public List<string> UnlockedKeys = new();
    }
}
