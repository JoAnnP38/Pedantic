using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public readonly struct GameToPlay
    {
        public int Round { get; init; }
        public string MatchId { get; init; }
        public string WhiteId { get; init; }
        public string BlackId { get; init; }

        public GameToPlay(int round, Match match, ChessWeights white, ChessWeights black)
        {
            Round = round;
            MatchId = match.Id.ToString();
            WhiteId = white.Id.ToString();
            BlackId = black.Id.ToString();
        }
    }
}
