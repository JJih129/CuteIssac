using System.Globalization;
using System.Text;

namespace CuteIssac.Core.Feedback
{
    internal static class FloatingFeedbackLabelUtility
    {
        private const int MaxEventLabelLength = 30;

        public static string NormalizeEventLabel(string source, string fallback)
        {
            string normalized = NormalizeCore(source);

            if (LooksCorrupted(normalized))
            {
                normalized = NormalizeCore(fallback);
            }

            return string.IsNullOrWhiteSpace(normalized) ? "EVENT" : normalized;
        }

        private static string NormalizeCore(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length);
            bool lastWasSpace = false;
            bool insideTag = false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];

                if (character == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (insideTag)
                {
                    if (character == '>')
                    {
                        insideTag = false;
                    }

                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    if (!lastWasSpace)
                    {
                        builder.Append(' ');
                        lastWasSpace = true;
                    }

                    continue;
                }

                UnicodeCategory category = char.GetUnicodeCategory(character);
                if (category == UnicodeCategory.Control
                    || category == UnicodeCategory.Format
                    || category == UnicodeCategory.PrivateUse
                    || category == UnicodeCategory.Surrogate
                    || category == UnicodeCategory.OtherNotAssigned
                    || character == '\uFFFD')
                {
                    continue;
                }

                builder.Append(character);
                lastWasSpace = false;
            }

            string normalized = builder.ToString().Trim();
            if (normalized.Length > MaxEventLabelLength)
            {
                normalized = normalized.Substring(0, MaxEventLabelLength).TrimEnd();
            }

            return normalized;
        }

        private static bool LooksCorrupted(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            int alphanumericCount = 0;
            int suspiciousCount = 0;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];

                if (char.IsLetterOrDigit(character))
                {
                    alphanumericCount++;
                }

                if ((character >= '\uF900' && character <= '\uFAFF') || character == '?')
                {
                    suspiciousCount++;
                }
            }

            return alphanumericCount == 0 || suspiciousCount >= 2;
        }
    }
}
