using System.Collections.Generic;
using UnityEngine;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const string CharacterSpriteResourceRoot = "Characters/Profiles/";

        private readonly Dictionary<string, Sprite> _characterSpriteCache = new Dictionary<string, Sprite>();

        private void ApplyDefaultCharacterSprites(CharacterDefinition character)
        {
            if (character == null || character.stageCharacters == null)
            {
                return;
            }

            for (var stageIndex = 0; stageIndex < character.stageCharacters.Count; stageIndex++)
            {
                var profile = character.stageCharacters[stageIndex];
                if (profile == null || string.IsNullOrWhiteSpace(profile.id))
                {
                    continue;
                }

                profile.portrait ??= LoadCharacterSprite(profile.id + "_portrait");
                profile.rewardSprite ??= LoadCharacterSprite(profile.id + "_reward");
            }
        }

        private Sprite LoadCharacterSprite(string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return null;
            }

            if (_characterSpriteCache.TryGetValue(resourceKey, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var sprite = Resources.Load<Sprite>(CharacterSpriteResourceRoot + resourceKey);
            if (sprite != null)
            {
                _characterSpriteCache[resourceKey] = sprite;
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(CharacterSpriteResourceRoot + resourceKey);
            if (sprites != null && sprites.Length > 0)
            {
                _characterSpriteCache[resourceKey] = sprites[0];
                return sprites[0];
            }

            var texture = Resources.Load<Texture2D>(CharacterSpriteResourceRoot + resourceKey);
            if (texture == null)
            {
                return null;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = resourceKey;
            _characterSpriteCache[resourceKey] = sprite;
            return sprite;
        }
    }
}
