using System;

namespace SameGame.Runtime
{
    [Serializable]
    public sealed class BoardConfig
    {
        public int width = 15;
        public int height = 10;
        public int colorCount = 4;
        public int targetScore = 250;

        public BoardConfig()
        {
        }

        public BoardConfig(int width, int height, int colorCount, int targetScore)
        {
            this.width = width;
            this.height = height;
            this.colorCount = colorCount;
            this.targetScore = targetScore;
        }

        public BoardConfig Clone()
        {
            return new BoardConfig(width, height, colorCount, targetScore);
        }
    }
}
