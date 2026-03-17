using System;
using UnityEngine;

namespace SameGame.Runtime
{
    [Serializable]
    public sealed class StageDefinition
    {
        public string title = "Stage 1";
        public BoardConfig board = new BoardConfig();
        public string characterLabel = "Default Pose";
        public string rewardLabel = "Reward CG";
        public string bgmResourcePath = string.Empty;
        public string backgroundVideoPath = string.Empty;
        public Color accentColor = new Color(0.95f, 0.45f, 0.55f, 1f);
        public Color secondaryColor = new Color(1f, 0.85f, 0.55f, 1f);

        public StageDefinition()
        {
        }

        public StageDefinition(
            string title,
            BoardConfig board,
            string characterLabel,
            string rewardLabel,
            string bgmResourcePath,
            string backgroundVideoPath,
            Color accentColor,
            Color secondaryColor)
        {
            this.title = title;
            this.board = board;
            this.characterLabel = characterLabel;
            this.rewardLabel = rewardLabel;
            this.bgmResourcePath = bgmResourcePath;
            this.backgroundVideoPath = backgroundVideoPath;
            this.accentColor = accentColor;
            this.secondaryColor = secondaryColor;
        }
    }
}
