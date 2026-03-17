using System;
using System.Collections.Generic;
using UnityEngine;

namespace SameGame.Runtime
{
    [Serializable]
    public sealed class CharacterDefinition
    {
        public string id = "character_01";
        public string displayName = "Category 1";
        public string summary = "Default summary";
        public List<CharacterStageProfile> stageCharacters = new List<CharacterStageProfile>();
        public Color accentColor = new Color(0.93f, 0.48f, 0.61f, 1f);
        public Color secondaryColor = new Color(0.98f, 0.84f, 0.58f, 1f);

        public CharacterDefinition()
        {
        }

        public CharacterDefinition(
            string id,
            string displayName,
            string summary,
            Color accentColor,
            Color secondaryColor,
            List<CharacterStageProfile> stageCharacters = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.summary = summary;
            this.accentColor = accentColor;
            this.secondaryColor = secondaryColor;
            this.stageCharacters = stageCharacters ?? new List<CharacterStageProfile>();
        }
    }
}
