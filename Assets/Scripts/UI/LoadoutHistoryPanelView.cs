using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CuteIssac.UI
{
    [DisallowMultipleComponent]
    public sealed class LoadoutHistoryPanelView : MonoBehaviour
    {
        public readonly struct EntryPresentation
        {
            public EntryPresentation(
                string headline,
                string detail,
                Color accentColor,
                bool emphasize,
                float freshness)
            {
                Headline = headline ?? string.Empty;
                Detail = detail ?? string.Empty;
                AccentColor = accentColor.a > 0.01f
                    ? accentColor
                    : Color.white;
                Emphasize = emphasize;
                Freshness = Mathf.Clamp01(freshness);
            }

            public string Headline { get; }
            public string Detail { get; }
            public Color AccentColor { get; }
            public bool Emphasize { get; }
            public float Freshness { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(Headline);
        }

        [Header("Optional Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Skinnable Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text firstEntryText;
        [SerializeField] private Text secondEntryText;
        [SerializeField] private Text thirdEntryText;

        [Header("Labels")]
        [SerializeField] private string titleLabel = "RUN TRACE";
        [SerializeField] private string emptyHeadline = "NO RECENT SWING";
        [SerializeField] private string emptyDetail = "Recent build, supply, and shop changes will stack here.";

        [Header("Styling")]
        [SerializeField] private Color panelTint = new(0.06f, 0.09f, 0.14f, 0.3f);
        [SerializeField] private Color titleColor = new(0.94f, 0.98f, 1f, 0.92f);
        [SerializeField] private Color placeholderAccentColor = new(0.7f, 0.82f, 0.92f, 1f);
        [SerializeField] [Range(0f, 1f)] private float accentTintStrength = 0.24f;
        [SerializeField] [Range(0f, 1f)] private float newestEntryAlpha = 0.96f;
        [SerializeField] [Range(0f, 1f)] private float oldestEntryAlpha = 0.34f;
        [SerializeField] [Range(0.6f, 1.2f)] private float newestEntryScale = 1f;
        [SerializeField] [Range(0.6f, 1.2f)] private float oldestEntryScale = 0.88f;

        public void ConfigureRuntimeView(
            GameObject root,
            Image background,
            Text title,
            Text firstEntry,
            Text secondEntry,
            Text thirdEntry)
        {
            panelRoot = root;
            backgroundImage = background;
            titleText = title;
            firstEntryText = firstEntry;
            secondEntryText = secondEntry;
            thirdEntryText = thirdEntry;
            EnsureTextSetup();
        }

        public void ConfigureLabels(string title, string emptyHeadlineText, string emptyDetailText)
        {
            titleLabel = string.IsNullOrWhiteSpace(title)
                ? "RUN TRACE"
                : title;
            emptyHeadline = string.IsNullOrWhiteSpace(emptyHeadlineText)
                ? "NO RECENT SWING"
                : emptyHeadlineText;
            emptyDetail = string.IsNullOrWhiteSpace(emptyDetailText)
                ? "Recent build, supply, and shop changes will stack here."
                : emptyDetailText;
        }

        public void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void ShowPlaceholder()
        {
            EnsureTextSetup();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = ResolveBackgroundImageColor(panelTint);
            }

            if (titleText != null)
            {
                titleText.text = titleLabel;
                titleText.color = titleColor;
            }

            ApplyEntry(firstEntryText, emptyHeadline, emptyDetail, placeholderAccentColor, false, 0.42f, 1f);
            ApplyEntry(secondEntryText, string.Empty, string.Empty, placeholderAccentColor, false, 0f, 0.9f);
            ApplyEntry(thirdEntryText, string.Empty, string.Empty, placeholderAccentColor, false, 0f, 0.84f);
        }

        public void SetEntries(IReadOnlyList<EntryPresentation> entries)
        {
            EnsureTextSetup();

            if (entries == null || entries.Count == 0)
            {
                ShowPlaceholder();
                return;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            EntryPresentation newestEntry = entries[0];

            if (backgroundImage != null)
            {
                Color basePanelColor = ResolveBackgroundImageColor(panelTint);
                backgroundImage.color = Color.Lerp(
                    basePanelColor,
                    Color.Lerp(basePanelColor, newestEntry.AccentColor, 0.18f),
                    accentTintStrength * Mathf.Lerp(0.7f, 1f, newestEntry.Freshness));
            }

            if (titleText != null)
            {
                titleText.text = titleLabel;
                titleText.color = Color.Lerp(titleColor, newestEntry.AccentColor, 0.24f);
            }

            ApplyEntry(firstEntryText, entries.Count > 0 ? entries[0].Headline : string.Empty, entries.Count > 0 ? entries[0].Detail : string.Empty, entries.Count > 0 ? entries[0].AccentColor : placeholderAccentColor, entries.Count > 0 && entries[0].Emphasize, entries.Count > 0 ? entries[0].Freshness : 0f, 1f);
            ApplyEntry(secondEntryText, entries.Count > 1 ? entries[1].Headline : string.Empty, entries.Count > 1 ? entries[1].Detail : string.Empty, entries.Count > 1 ? entries[1].AccentColor : placeholderAccentColor, entries.Count > 1 && entries[1].Emphasize, entries.Count > 1 ? entries[1].Freshness : 0f, 0.92f);
            ApplyEntry(thirdEntryText, entries.Count > 2 ? entries[2].Headline : string.Empty, entries.Count > 2 ? entries[2].Detail : string.Empty, entries.Count > 2 ? entries[2].AccentColor : placeholderAccentColor, entries.Count > 2 && entries[2].Emphasize, entries.Count > 2 ? entries[2].Freshness : 0f, 0.86f);
        }

        private Color ResolveBackgroundImageColor(Color fallbackColor)
        {
            return backgroundImage != null && backgroundImage.sprite != null ? Color.white : fallbackColor;
        }

        private void EnsureTextSetup()
        {
            ConfigureText(titleText, 15, FontStyle.Bold, TextAnchor.UpperLeft, false);
            ConfigureText(firstEntryText, 16, FontStyle.Normal, TextAnchor.UpperLeft, true);
            ConfigureText(secondEntryText, 15, FontStyle.Normal, TextAnchor.UpperLeft, true);
            ConfigureText(thirdEntryText, 15, FontStyle.Normal, TextAnchor.UpperLeft, true);
        }

        private static void ConfigureText(Text text, int fontSize, FontStyle fontStyle, TextAnchor anchor, bool richText)
        {
            if (text == null)
            {
                return;
            }

            LocalizedUiFontProvider.ApplyReadableDefaults(
                text,
                fontSize,
                anchor,
                fontStyle,
                supportRichText: richText,
                horizontalOverflow: HorizontalWrapMode.Wrap,
                verticalOverflow: VerticalWrapMode.Truncate,
                lineSpacing: 0.92f);
        }

        private void ApplyEntry(
            Text text,
            string headline,
            string detail,
            Color accentColor,
            bool emphasize,
            float freshness,
            float alphaScale)
        {
            if (text == null)
            {
                return;
            }

            bool hasHeadline = !string.IsNullOrWhiteSpace(headline);
            text.gameObject.SetActive(hasHeadline);

            if (!hasHeadline)
            {
                text.text = string.Empty;
                return;
            }

            float alpha = Mathf.Lerp(oldestEntryAlpha, newestEntryAlpha, Mathf.Clamp01(freshness)) * alphaScale;
            float scale = Mathf.Lerp(oldestEntryScale, newestEntryScale, Mathf.Clamp01(freshness));
            text.rectTransform.localScale = new Vector3(scale, scale, 1f);
            text.color = new Color(1f, 1f, 1f, alpha);

            Color headlineColor = Color.Lerp(accentColor, Color.white, emphasize ? 0.08f : 0.18f);
            Color detailColor = Color.Lerp(accentColor, Color.white, 0.54f);
            string headlineHex = ColorUtility.ToHtmlStringRGB(headlineColor);
            string detailHex = ColorUtility.ToHtmlStringRGB(detailColor);

            text.text = string.IsNullOrWhiteSpace(detail)
                ? $"<color=#{headlineHex}><b>{headline}</b></color>"
                : $"<color=#{headlineHex}><b>{headline}</b></color>\n<size=13><color=#{detailHex}>{detail}</color></size>";
        }
    }
}
