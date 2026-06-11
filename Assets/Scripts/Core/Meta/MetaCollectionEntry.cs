using System;

namespace CuteIssac.Core.Meta
{
    [Serializable]
    public sealed class MetaCollectionEntry
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public MetaCollectionEntryKind Kind;
        public string UnlockKey;
        public bool UnlockedByDefault = true;
    }
}
