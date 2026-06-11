using UnityEngine;

namespace CuteIssac.Core.Feedback
{
    public static class FloatingFeedbackTextCache
    {
        private const int MaxCachedWholeNumber = 256;

        private static readonly string[] UnsignedWholeNumbers = BuildWholeNumberCache(string.Empty);
        private static readonly string[] PositiveWholeNumbers = BuildWholeNumberCache("+");
        private static readonly string[] NegativeWholeNumbers = BuildWholeNumberCache("-");

        public static string GetUnsignedCeil(float value)
        {
            int wholeNumber = Mathf.CeilToInt(Mathf.Max(0f, value));
            return wholeNumber <= MaxCachedWholeNumber
                ? UnsignedWholeNumbers[wholeNumber]
                : wholeNumber.ToString();
        }

        public static string GetPositiveCeil(float value)
        {
            int wholeNumber = Mathf.CeilToInt(Mathf.Max(0f, value));
            return wholeNumber <= MaxCachedWholeNumber
                ? PositiveWholeNumbers[wholeNumber]
                : "+" + wholeNumber;
        }

        public static string GetNegativeCeil(float value)
        {
            int wholeNumber = Mathf.CeilToInt(Mathf.Max(0f, value));
            return wholeNumber <= MaxCachedWholeNumber
                ? NegativeWholeNumbers[wholeNumber]
                : "-" + wholeNumber;
        }

        private static string[] BuildWholeNumberCache(string prefix)
        {
            string[] cache = new string[MaxCachedWholeNumber + 1];

            for (int value = 0; value < cache.Length; value++)
            {
                cache[value] = string.Concat(prefix, value.ToString());
            }

            return cache;
        }
    }
}
