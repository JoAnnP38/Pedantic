using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public readonly struct GameResult
    {
        public GameToPlay Game { get; init; }
        public int WhiteScore { get; init; }
        public int BlackScore { get; init; }
        public string Pgn { get; init; }

        public GameResult(GameToPlay game, int whiteScore, int blackScore, string pgn)
        {
            Game = game;
            WhiteScore = whiteScore;
            BlackScore = blackScore;
            Pgn = pgn;
        }
    }
}
