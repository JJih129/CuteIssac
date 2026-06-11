using System;
using UnityEngine;

namespace CuteIssac.Core.Meta
{
    [Serializable]
    public sealed class MetaCollectionEntry
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public MetaCollectionEntryKind Kind;
        public string UnlockKey;
        public bool UnlockedByDefault = true;
    }
}
