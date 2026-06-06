// Loads card sprites directly from the 2D Cards Game Art Pack.
// Uses AssetDatabase in Editor; falls back to white texture in builds.
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rami
{
    /// <summary>
    /// Maps CardSuit+CardRank → Sprite using the Rect Cards from the 2D Cards Game Art Pack.
    /// Both copies of a duplicated card share the same sprite (looked up by suit+rank only).
    /// No ScriptableObject dependency.
    /// </summary>
    public static class CardSpriteLibrary
    {
        private static Dictionary<string, Sprite> _cache;
        private static Sprite _backSprite;
        private static Sprite _jokerSprite;
        private static Sprite _fallbackSprite;
        private static Sprite _jokerFallbackSprite;

        // ──────────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the face sprite for a card.
        /// Both copies of a duplicated card return the same sprite — identity is tracked
        /// by <see cref="PlayingCard.InstanceId"/>, not by sprite reference.
        /// </summary>
        public static Sprite GetFaceSprite(PlayingCard card)
        {
            EnsureCache();

            if (card.IsJoker)
                return _jokerSprite != null ? _jokerSprite : GetJokerFallback();

            if (_cache.TryGetValue(GetKey(card.Suit, card.Rank), out var spr) && spr != null)
                return spr;

            Debug.LogWarning($"[Rami] Sprite missing for {card}. Using back sprite.");
            return GetBackSprite();
        }

        public static Sprite GetBackSprite()
        {
            EnsureCache();
            return _backSprite != null ? _backSprite : GetFallback();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Initialisation
        // ──────────────────────────────────────────────────────────────────────────

        private static void EnsureCache()
        {
            if (_cache != null) return;
            _cache = new Dictionary<string, Sprite>();
            BuildCache();
        }

        private static void BuildCache()
        {
            var suitMeta = new (CardSuit suit, string folder, string suffix)[]
            {
                (CardSuit.Clubs,    "Clubs",    "club"),
                (CardSuit.Diamonds, "Diamonds", "diamond"),
                (CardSuit.Hearts,   "Hearts",   "heart"),
                (CardSuit.Spades,   "Spades",   "spade"),
            };

            var rankPrefix = new Dictionary<CardRank, string>
            {
                { CardRank.Ace,   "A"  },
                { CardRank.Two,   "2"  },
                { CardRank.Three, "3"  },
                { CardRank.Four,  "4"  },
                { CardRank.Five,  "5"  },
                { CardRank.Six,   "6"  },
                { CardRank.Seven, "7"  },
                { CardRank.Eight, "8"  },
                { CardRank.Nine,  "9"  },
                { CardRank.Ten,   "10" },
                { CardRank.Jack,  "J"  },
                { CardRank.Queen, "Q"  },
                { CardRank.King,  "K"  }
            };

            string basePath = "Assets/2D Cards Game Art Pack/Sprites/Standard 52 Cards/Rect Cards";

            foreach (var (suit, folder, suffix) in suitMeta)
            {
                foreach (var (rank, prefix) in rankPrefix)
                {
                    string fileName = $"{prefix}{suffix}.png";
                    string path     = $"{basePath}/{folder}/{fileName}";
                    var sprite      = LoadSprite(path);
                    if (sprite != null)
                        _cache[GetKey(suit, rank)] = sprite;
                    else
                        Debug.LogWarning($"[Rami] Card sprite not found: {path}");
                }
            }

            _backSprite = LoadSprite($"{basePath}/Card Back/card_back_rect_1.png");
            if (_backSprite == null)
                Debug.LogWarning("[Rami] Card back sprite not found.");

            // Try common joker file names from the art pack.
            string[] jokerPaths =
            {
                $"{basePath}/Joker/joker.png",
                $"{basePath}/Joker/joker_rect.png",
                $"{basePath}/Joker/Joker.png",
                "Assets/2D Cards Game Art Pack/Sprites/Standard 52 Cards/Joker/joker.png",
            };

            foreach (var jp in jokerPaths)
            {
                _jokerSprite = LoadSprite(jp);
                if (_jokerSprite != null) break;
            }

            if (_jokerSprite == null)
                Debug.LogWarning("[Rami] Joker sprite not found — using coloured fallback.");

            Debug.Log($"[Rami] CardSpriteLibrary loaded {_cache.Count} face sprites " +
                      $"(joker sprite {(_jokerSprite != null ? "found" : "missing — fallback active")}).");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private static Sprite LoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        private static Sprite GetFallback()
        {
            if (_fallbackSprite != null) return _fallbackSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            return _fallbackSprite;
        }

        /// <summary>Distinct purple card so jokers are visually identifiable when no sprite exists.</summary>
        private static Sprite GetJokerFallback()
        {
            if (_jokerFallbackSprite != null) return _jokerFallbackSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var purple = new Color(0.6f, 0.1f, 0.8f, 1f);
            tex.SetPixels(new Color[] { purple, purple, purple, purple });
            tex.Apply();
            _jokerFallbackSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            return _jokerFallbackSprite;
        }

        private static string GetKey(CardSuit suit, CardRank rank) => $"{suit}_{rank}";
    }
}
