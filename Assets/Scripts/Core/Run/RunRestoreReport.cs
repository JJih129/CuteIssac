using System.Collections.Generic;

namespace CuteIssac.Core.Run
{
    public enum RunRestoreEntryResult
    {
        None,
        Restored,
        Skipped,
        Unavailable,
    }

    public enum RunRestoreRoomPolicyResult
    {
        None,
        ResumePosition,
        EncounterReset,
        SafeReturn,
    }

    public sealed class RunRestoreReport
    {
        public int FloorIndex { get; set; }
        public string RoomId { get; set; }
        public string ActiveItemId { get; set; }
        public string ConsumableItemId { get; set; }
        public string TrinketItemId { get; set; }
        public string ActiveTimedEffectSourceItemId { get; set; }
        public string ConsumableTimedEffectSourceItemId { get; set; }
        public RunRestoreEntryResult ActiveTimedEffectResult { get; set; }
        public RunRestoreEntryResult ConsumableTimedEffectResult { get; set; }
        public RunRestoreRoomPolicyResult RoomPolicyResult { get; set; }
        public string Headline { get; set; } = "\uB7F0 \uBCF5\uC6D0";
        public string Detail { get; set; } = "\uBCF5\uC6D0 \uC2DC\uB3C4\uAC00 \uC544\uC9C1 \uC5C6\uC2B5\uB2C8\uB2E4";

        public string BuildSummary()
        {
            List<string> lines = new()
            {
                Headline,
                $"- \uC0C1\uD0DC: {Detail}",
            };

            if (FloorIndex > 0)
            {
                lines.Add($"- \uCE35: {FloorIndex}");
            }

            lines.Add($"- \uBC29: {ResolveLabel(RoomId)}");
            lines.Add($"- \uC561\uD2F0\uBE0C: {ResolveLabel(ActiveItemId)}");
            lines.Add($"- \uC18C\uBE44\uD615: {ResolveLabel(ConsumableItemId)}");
            lines.Add($"- \uC7A5\uC2E0\uAD6C: {ResolveLabel(TrinketItemId)}");
            lines.Add($"- \uC561\uD2F0\uBE0C \uBC84\uD504: {BuildEntryStatus(ActiveTimedEffectResult, ActiveTimedEffectSourceItemId)}");
            lines.Add($"- \uC18C\uBE44\uD615 \uBC84\uD504: {BuildEntryStatus(ConsumableTimedEffectResult, ConsumableTimedEffectSourceItemId)}");
            lines.Add($"- \uBC29 \uC815\uCC45: {BuildRoomPolicyStatus(RoomPolicyResult)}");

            return string.Join("\n", lines);
        }

        private static string ResolveLabel(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "\uC5C6\uC74C" : value;
        }

        private static string BuildEntryStatus(RunRestoreEntryResult result, string sourceItemId)
        {
            return result switch
            {
                RunRestoreEntryResult.Restored => $"\uBCF5\uC6D0\uB428 ({ResolveLabel(sourceItemId)})",
                RunRestoreEntryResult.Skipped => $"\uAC74\uB108\uB700 ({ResolveLabel(sourceItemId)})",
                RunRestoreEntryResult.Unavailable => $"\uC0AC\uC6A9 \uBD88\uAC00 ({ResolveLabel(sourceItemId)})",
                _ => "\uC5C6\uC74C",
            };
        }

        private static string BuildRoomPolicyStatus(RunRestoreRoomPolicyResult result)
        {
            return result switch
            {
                RunRestoreRoomPolicyResult.EncounterReset => "\uC804\uD22C \uC7AC\uC2DC\uC791",
                RunRestoreRoomPolicyResult.SafeReturn => "\uC548\uC804 \uBCF5\uADC0",
                RunRestoreRoomPolicyResult.ResumePosition => "\uD604\uC7AC \uC704\uCE58 \uBCF5\uADC0",
                _ => "\uC5C6\uC74C",
            };
        }
    }
}
