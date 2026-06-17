using UnityEngine.UI;

namespace CuteIssac.UI
{
    internal static class HudTextFormatting
    {
        public static string NormalizeSerializedLineBreaks(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\r", "\r");
        }

        public static void NormalizeSerializedLineBreaks(Text text)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return;
            }

            string normalized = NormalizeSerializedLineBreaks(text.text);
            if (!string.Equals(text.text, normalized, System.StringComparison.Ordinal))
            {
                text.text = normalized;
            }
        }
    }
}
