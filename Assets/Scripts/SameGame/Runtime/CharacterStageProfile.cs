using System;
using UnityEngine;

namespace SameGame.Runtime
{
    [Serializable]
    public sealed class CharacterStageProfile
    {
        public string id = "profile_01";
        public string displayName = "Character A";
        public string summary = "Default profile";
        public Sprite portrait;
        public Sprite rewardSprite;

        public CharacterStageProfile()
        {
        }

        public CharacterStageProfile(
            string id,
            string displayName,
            string summary,
            Sprite portrait = null,
            Sprite rewardSprite = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.summary = summary;
            this.portrait = portrait;
            this.rewardSprite = rewardSprite;
        }
    }
}
