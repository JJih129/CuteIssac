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
        private static Sprite _candyCoinSprite;
        private static Sprite _speedCandySprite;
        private static Sprite _appleBombSprite;
        private static Sprite _shopItemBodySprite;
        private static Sprite _shopkeeperSprite;

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

        public static Sprite GetCandyCoinSprite()
        {
            if (_candyCoinSprite != null)
            {
                return _candyCoinSprite;
            }

            _candyCoinSprite = CreatePatternSprite(
                "RuntimeCandyCoinIcon",
                new[]
                {
                    "0000111111000000",
                    "0011112222111100",
                    "0112222222222110",
                    "1122222332222211",
                    "1222233333222221",
                    "1222331113322221",
                    "1222311111322221",
                    "1222311441322221",
                    "1222311111322221",
                    "1222331113322221",
                    "1222233333222221",
                    "1122222332222211",
                    "0112222222222110",
                    "0011112222111100",
                    "0000111111000000",
                    "0000000000000000"
                },
                new Color32(0, 0, 0, 0),
                new Color32(255, 203, 54, 255),
                new Color32(255, 226, 94, 255),
                new Color32(255, 112, 160, 255),
                new Color32(72, 39, 86, 255));
            return _candyCoinSprite;
        }

        public static Sprite GetSpeedCandySprite()
        {
            if (_speedCandySprite != null)
            {
                return _speedCandySprite;
            }

            _speedCandySprite = CreatePatternSprite(
                "RuntimeSpeedCandyIcon",
                new[]
                {
                    "0000000000000000",
                    "0000003300000000",
                    "0000033330000000",
                    "0000332233000000",
                    "0003322223300000",
                    "0033222222330000",
                    "0332222222233000",
                    "3322222222223300",
                    "0332222222233000",
                    "0033222222330000",
                    "0003322223300000",
                    "0000332233000000",
                    "0000033330000000",
                    "0000003300000000",
                    "0000000000000000",
                    "0000000000000000"
                },
                new Color32(0, 0, 0, 0),
                new Color32(255, 255, 255, 255),
                new Color32(80, 210, 255, 255),
                new Color32(255, 116, 190, 255),
                new Color32(58, 92, 130, 255));
            return _speedCandySprite;
        }

        public static Sprite GetAppleBombSprite()
        {
            if (_appleBombSprite != null)
            {
                return _appleBombSprite;
            }

            _appleBombSprite = CreatePatternSprite(
                "RuntimeAppleBombIcon",
                new[]
                {
                    "0000004400000000",
                    "0000044440000000",
                    "0000004400000000",
                    "0000011110000000",
                    "0001111111100000",
                    "0011111111110000",
                    "0111111111111000",
                    "0111111111111000",
                    "0111111111111000",
                    "0011111111110000",
                    "0001111111100000",
                    "0000111111000000",
                    "0000011110000000",
                    "0000001100000000",
                    "0000000000000000",
                    "0000000000000000"
                },
                new Color32(0, 0, 0, 0),
                new Color32(230, 54, 54, 255),
                new Color32(255, 104, 76, 255),
                new Color32(80, 178, 86, 255),
                new Color32(68, 46, 34, 255));
            return _appleBombSprite;
        }

        public static Sprite GetShopItemBodySprite()
        {
            if (_shopItemBodySprite != null)
            {
                return _shopItemBodySprite;
            }

            _shopItemBodySprite = CreatePatternSprite(
                "RuntimeShopItemBody",
                new[]
                {
                    "0000111111000000",
                    "0011111111110000",
                    "0111111111111000",
                    "0111111111111000",
                    "1111111111111100",
                    "1111111111111100",
                    "1111111111111100",
                    "1111111111111100",
                    "1111111111111100",
                    "1111111111111100",
                    "0111111111111000",
                    "0111111111111000",
                    "0011111111110000",
                    "0000111111000000",
                    "0000000000000000",
                    "0000000000000000"
                },
                new Color32(0, 0, 0, 0),
                new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255),
                new Color32(255, 255, 255, 255));
            return _shopItemBodySprite;
        }

        public static Sprite GetShopkeeperSprite()
        {
            if (_shopkeeperSprite != null)
            {
                return _shopkeeperSprite;
            }

            _shopkeeperSprite = CreatePatternSprite(
                "RuntimeShopkeeperNpc",
                new[]
                {
                    "0000001111000000",
                    "0000112222110000",
                    "0001222222221000",
                    "0012232323222100",
                    "0012222222222100",
                    "0001223333221000",
                    "0000112222110000",
                    "0000011111000000",
                    "0000144444100000",
                    "0001444444410000",
                    "0014444444441000",
                    "0014444444441000",
                    "0001444444410000",
                    "0000111111100000",
                    "0000000000000000",
                    "0000000000000000"
                },
                new Color32(0, 0, 0, 0),
                new Color32(82, 48, 36, 255),
                new Color32(255, 206, 148, 255),
                new Color32(72, 40, 70, 255),
                new Color32(116, 72, 188, 255));
            return _shopkeeperSprite;
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

        private static Sprite CreatePatternSprite(
            string spriteName,
            string[] rows,
            Color32 clear,
            Color32 color1,
            Color32 color2,
            Color32 color3,
            Color32 color4)
        {
            int height = rows.Length;
            int width = height > 0 ? rows[0].Length : 0;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = spriteName
            };

            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                string row = rows[height - 1 - y];
                int rowOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    pixels[rowOffset + x] = row[x] switch
                    {
                        '1' => color1,
                        '2' => color2,
                        '3' => color3,
                        '4' => color4,
                        _ => clear
                    };
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = spriteName;
            return sprite;
        }
    }
}
