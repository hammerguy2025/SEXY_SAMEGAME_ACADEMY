#if UNITY_EDITOR
using NUnit.Framework;
using SameGame.Runtime;

namespace SameGame.Tests.EditMode
{
    public class SameGameBoardTests
    {
        [Test]
        public void RemovingVerticalPair_ShiftsColumnsLeft_WhenColumnBecomesEmpty()
        {
            var config = new BoardConfig(3, 2, 4, 0);
            var board = SameGameBoard.FromCells(config, new[]
            {
                0, 1, 2,
                0, 2, 1
            });

            var move = board.TryRemoveGroup(0, 0);

            Assert.That(move.RemovedCount, Is.EqualTo(2));
            Assert.That(board.GetCell(0, 0), Is.EqualTo(1));
            Assert.That(board.GetCell(0, 1), Is.EqualTo(2));
            Assert.That(board.GetCell(1, 0), Is.EqualTo(2));
            Assert.That(board.GetCell(1, 1), Is.EqualTo(1));
            Assert.That(board.GetCell(2, 0), Is.EqualTo(-1));
            Assert.That(board.GetCell(2, 1), Is.EqualTo(-1));
        }

        [Test]
        public void BoardWithNoAdjacentMatches_HasNoMoves()
        {
            var config = new BoardConfig(3, 3, 4, 0);
            var board = SameGameBoard.FromCells(config, new[]
            {
                0, 1, 2,
                3, 0, 1,
                2, 3, 0
            });

            Assert.That(board.HasMoves(), Is.False);
        }

        [Test]
        public void RandomBoardGeneration_ProducesPlayableBoard()
        {
            var config = new BoardConfig(15, 10, 4, 0);
            var board = SameGameBoard.CreateRandom(config, 12345);

            Assert.That(board.Width, Is.EqualTo(15));
            Assert.That(board.Height, Is.EqualTo(10));
            Assert.That(board.CountOccupiedCells(), Is.EqualTo(150));
            Assert.That(board.HasMoves(), Is.True);
        }

        [Test]
        public void ScoreFormula_MatchesSpecification()
        {
            Assert.That(SameGameBoard.CalculateScore(1), Is.EqualTo(0));
            Assert.That(SameGameBoard.CalculateScore(2), Is.EqualTo(0));
            Assert.That(SameGameBoard.CalculateScore(3), Is.EqualTo(1));
            Assert.That(SameGameBoard.CalculateScore(4), Is.EqualTo(4));
            Assert.That(SameGameBoard.CalculateScore(5), Is.EqualTo(9));
        }

        [Test]
        public void TurnScore_AddsAllClearBonus_WhenBoardIsCleared()
        {
            Assert.That(SameGameBoard.CalculateTurnScore(2, false), Is.EqualTo(0));
            Assert.That(SameGameBoard.CalculateTurnScore(2, true), Is.EqualTo(300));
            Assert.That(SameGameBoard.CalculateTurnScore(4, true), Is.EqualTo(304));
        }

        [Test]
        public void TinyRandomBoard_StillContainsMove_WhenTwoCellsExist()
        {
            var config = new BoardConfig(2, 1, 4, 0);
            var board = SameGameBoard.CreateRandom(config, 7);

            Assert.That(board.CountOccupiedCells(), Is.EqualTo(2));
            Assert.That(board.HasMoves(), Is.True);
        }
    }
}
#endif
