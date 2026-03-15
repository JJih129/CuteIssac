using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CuteIssac.Core.Spawning
{
    public static class GameplaySpawnTelemetry
    {
        private sealed class SpawnStats
        {
            public SpawnStats(string prefabName)
            {
                PrefabName = prefabName;
            }

            public string PrefabName { get; }
            public int Attempts;
            public int Successes;
            public int Failures;
            public int PooledSpawns;
            public int InstantiatedSpawns;
            public int ValidationFailures;
        }

        private static readonly Dictionary<int, SpawnStats> StatsByPrefab = new();

        public static void RecordSpawn(GameObject prefab, SpawnReusePolicy reusePolicy, bool success)
        {
            if (prefab == null)
            {
                return;
            }

            SpawnStats stats = GetOrCreateStats(prefab);
            stats.Attempts++;

            if (reusePolicy == SpawnReusePolicy.Pooled)
            {
                stats.PooledSpawns++;
            }
            else
            {
                stats.InstantiatedSpawns++;
            }

            if (success)
            {
                stats.Successes++;
            }
            else
            {
                stats.Failures++;
            }
        }

        public static void RecordValidationFailure(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            GetOrCreateStats(prefab).ValidationFailures++;
        }

        public static string BuildSummary(int maxEntries = 6)
        {
            if (StatsByPrefab.Count == 0)
            {
                return "SPAWN\nNO DATA";
            }

            List<SpawnStats> snapshot = new(StatsByPrefab.Values);
            snapshot.Sort((left, right) =>
            {
                int attemptCompare = right.Attempts.CompareTo(left.Attempts);
                return attemptCompare != 0 ? attemptCompare : string.CompareOrdinal(left.PrefabName, right.PrefabName);
            });

            StringBuilder builder = new();
            builder.AppendLine("SPAWN");
            int count = Mathf.Min(maxEntries, snapshot.Count);

            for (int i = 0; i < count; i++)
            {
                SpawnStats stats = snapshot[i];
                builder.Append(stats.PrefabName);
                builder.Append(": ok ");
                builder.Append(stats.Successes);
                builder.Append('/');
                builder.Append(stats.Attempts);
                builder.Append("  pooled ");
                builder.Append(stats.PooledSpawns);
                builder.Append("  inst ");
                builder.Append(stats.InstantiatedSpawns);

                if (stats.Failures > 0 || stats.ValidationFailures > 0)
                {
                    builder.Append("  fail ");
                    builder.Append(stats.Failures);
                    builder.Append("  invalid ");
                    builder.Append(stats.ValidationFailures);
                }

                if (i < count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static SpawnStats GetOrCreateStats(GameObject prefab)
        {
            int key = prefab.GetInstanceID();

            if (StatsByPrefab.TryGetValue(key, out SpawnStats stats))
            {
                return stats;
            }

            stats = new SpawnStats(prefab.name);
            StatsByPrefab.Add(key, stats);
            return stats;
        }
    }
}
