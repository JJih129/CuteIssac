using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace CuteIssac.Core.Debug
{
    public static class DevelopmentLogService
    {
        private static readonly bool[] CategoryEnabled =
        {
            true,
            true,
            true,
            true,
            true,
            true
        };

        private static readonly StringBuilder SummaryBuilder = new(192);

        public static bool IsEnabled(DevelopmentLogCategory category)
        {
            int index = (int)category;
            return index >= 0 && index < CategoryEnabled.Length && CategoryEnabled[index];
        }

        public static void SetEnabled(DevelopmentLogCategory category, bool enabled)
        {
            int index = (int)category;
            if (index < 0 || index >= CategoryEnabled.Length)
            {
                return;
            }

            CategoryEnabled[index] = enabled;
        }

        public static bool Toggle(DevelopmentLogCategory category)
        {
            bool nextEnabled = !IsEnabled(category);
            SetEnabled(category, nextEnabled);
            return nextEnabled;
        }

        public static string BuildCategorySummary()
        {
            SummaryBuilder.Clear();
            SummaryBuilder.AppendLine("LOG CATEGORIES");

            for (int index = 0; index < CategoryEnabled.Length; index++)
            {
                DevelopmentLogCategory category = (DevelopmentLogCategory)index;
                SummaryBuilder
                    .Append("- ")
                    .Append(category)
                    .Append(": ")
                    .Append(CategoryEnabled[index] ? "ON" : "OFF");

                if (index < CategoryEnabled.Length - 1)
                {
                    SummaryBuilder.AppendLine();
                }
            }

            return SummaryBuilder.ToString();
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(DevelopmentLogCategory category, string message, Object context = null)
        {
            if (!IsEnabled(category))
            {
                return;
            }

            UnityEngine.Debug.Log(Format(category, message), context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(DevelopmentLogCategory category, string message, Object context = null)
        {
            if (!IsEnabled(category))
            {
                return;
            }

            UnityEngine.Debug.LogWarning(Format(category, message), context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(DevelopmentLogCategory category, string message, Object context = null)
        {
            if (!IsEnabled(category))
            {
                return;
            }

            UnityEngine.Debug.LogError(Format(category, message), context);
        }

        private static string Format(DevelopmentLogCategory category, string message)
        {
            return $"[{category}] {message}";
        }
    }
}
