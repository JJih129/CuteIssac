using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    internal static class LocalizedUiFontProvider
    {
        private static Font _cachedFont;
        private static Font _cachedNumericWorldFont;

        public static Font GetFont()
        {
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            string[] preferredFonts =
            {
                "Malgun Gothic",
                "Noto Sans KR",
                "Noto Sans CJK KR",
                "NanumGothic",
                "Segoe UI",
                "Arial",
            };

            _cachedFont = Font.CreateDynamicFontFromOSFont(preferredFonts, 32);

            if (_cachedFont == null)
            {
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _cachedFont;
        }

        public static Font GetNumericWorldFont()
        {
            if (_cachedNumericWorldFont != null)
            {
                return _cachedNumericWorldFont;
            }

            // Damage numbers only need ASCII digits/signs. The built-in runtime font
            // is more stable for pooled world-space TextMesh than dynamic OS fonts.
            _cachedNumericWorldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (_cachedNumericWorldFont == null)
            {
                _cachedNumericWorldFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "Segoe UI", "Malgun Gothic" },
                    32);
            }

            return _cachedNumericWorldFont;
        }

        public static void Apply(Text text)
        {
            if (text != null)
            {
                text.font = GetFont();
            }
        }

        public static void ApplyReadableDefaults(
            Text text,
            int fontSize,
            TextAnchor alignment,
            FontStyle fontStyle = FontStyle.Normal,
            bool supportRichText = false,
            HorizontalWrapMode horizontalOverflow = HorizontalWrapMode.Wrap,
            VerticalWrapMode verticalOverflow = VerticalWrapMode.Truncate,
            float lineSpacing = 1f,
            bool bestFit = false,
            int minBestFitSize = 0,
            int maxBestFitSize = 0)
        {
            if (text == null)
            {
                return;
            }

            Apply(text);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.supportRichText = supportRichText;
            text.horizontalOverflow = horizontalOverflow;
            text.verticalOverflow = verticalOverflow;
            text.lineSpacing = lineSpacing;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = bestFit;
            text.raycastTarget = false;

            if (!bestFit)
            {
                return;
            }

            text.resizeTextMinSize = minBestFitSize > 0 ? minBestFitSize : Mathf.Max(14, fontSize - 8);
            text.resizeTextMaxSize = maxBestFitSize > 0 ? maxBestFitSize : fontSize;
        }

        public static void Apply(TextMesh textMesh)
        {
            if (textMesh != null)
            {
                textMesh.font = GetFont();
            }
        }

        public static void ApplyNumericWorld(TextMesh textMesh)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.font = GetNumericWorldFont();
            textMesh.richText = false;

            if (!textMesh.gameObject.activeSelf)
            {
                textMesh.gameObject.SetActive(true);
            }

            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.sharedMaterial = textMesh.font != null ? textMesh.font.material : renderer.sharedMaterial;
            }
        }

        public static void ApplyReadableDefaults(
            TextMesh textMesh,
            int fontSize,
            TextAnchor anchor,
            TextAlignment alignment,
            float characterSize,
            FontStyle fontStyle = FontStyle.Normal,
            bool richText = false,
            float lineSpacing = 1f)
        {
            if (textMesh == null)
            {
                return;
            }

            Apply(textMesh);
            textMesh.fontSize = fontSize;
            textMesh.anchor = anchor;
            textMesh.alignment = alignment;
            textMesh.characterSize = characterSize;
            textMesh.fontStyle = fontStyle;
            textMesh.richText = richText;
            textMesh.lineSpacing = lineSpacing;
        }
    }
}
