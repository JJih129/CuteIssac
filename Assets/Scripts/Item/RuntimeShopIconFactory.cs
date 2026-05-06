using UnityEngine;

namespace CuteIssac.Item
{
    /// <summary>
    /// Lightweight cached runtime icons for shop offers that do not have authored sprites.
    /// </summary>
    public static class RuntimeShopIconFactory
    {
        private static Sprite _heartSprite;
        private static Sprite _ammoSprite;

        public static Sprite GetHeartSprite()
        {
            if (_heartSprite != null)
            {
                return _heartSprite;
            }

            const int size = 16;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeShopHeartIcon"
            };

            Color clear = new(0f, 0f, 0f, 0f);
            Color fill = new(1f, 0.56f, 0.64f, 1f);
            string[] mask =
            {
                "0000000000000000",
                "0000110011000000",
                "0001111111110000",
                "0011111111111000",
                "0111111111111100",
                "0111111111111100",
                "0011111111111000",
                "0001111111110000",
                "0000111111100000",
                "0000011111000000",
                "0000001110000000",
                "0000000100000000",
                "0000000000000000",
                "0000000000000000",
                "0000000000000000",
                "0000000000000000"
            };

            for (int y = 0; y < size; y++)
            {
                string row = mask[size - 1 - y];
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, row[x] == '1' ? fill : clear);
                }
            }

            texture.Apply();
            _heartSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _heartSprite.name = "RuntimeShopHeartIcon";
            return _heartSprite;
        }

        public static Sprite GetAmmoSprite()
        {
            if (_ammoSprite != null)
            {
                return _ammoSprite;
            }

            const int width = 16;
            const int height = 22;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "RuntimeShopAmmoIcon"
            };

            Color32[] pixels = new Color32[width * height];
            Color32 solid = new(255, 255, 255, 255);

            FillRect(pixels, width, 5, 2, 6, 15, solid);
            FillRect(pixels, width, 4, 16, 8, 2, solid);
            FillRect(pixels, width, 6, 18, 4, 2, solid);
            FillRect(pixels, width, 5, 0, 6, 2, solid);

            texture.SetPixels32(pixels);
            texture.Apply();
            _ammoSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f);
            _ammoSprite.name = "RuntimeShopAmmoIcon";
            return _ammoSprite;
        }

        private static void FillRect(Color32[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color32 color)
        {
            int maxX = Mathf.Min(width, x + rectWidth);
            int maxY = Mathf.Min(pixels.Length / width, y + rectHeight);

            for (int py = Mathf.Max(0, y); py < maxY; py++)
            {
                int rowOffset = py * width;
                for (int px = Mathf.Max(0, x); px < maxX; px++)
                {
                    pixels[rowOffset + px] = color;
                }
            }
        }
    }
}
