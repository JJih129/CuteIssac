using System;
using CuteIssac.Core.Meta;
using CuteIssac.Core.Settings;

namespace CuteIssac.Core.Save
{
    /// <summary>
    /// Persistent account-level save payload.
    /// Run snapshots stay separate so temporary state can be deleted independently.
    /// </summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        public int Version = 1;
        public string LastSavedUtc;
        public UnlockSaveData Unlocks = new();
        public GameOptionsData Options = new();
    }
}
