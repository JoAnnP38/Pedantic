using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Pedantic.Genetics
{
    public class Game
    {
        public ObjectId Id { get; set; }

        [BsonRef("matches")]
        public Match Match { get; set; }

        [BsonRef("weights")]
        public ChessWeights WhitePlayer { get; set; }

        [BsonRef("weights")]
        public ChessWeights BlackPlayer { get; set; }
        public int WhiteScore { get; set; }
        public int BlackScore { get; set; }
        public string PGN { get; set; }

        public Game()
        {
            Id = new ObjectId();
            Match = new Match();
            WhitePlayer = ChessWeights.Empty;
            BlackPlayer = ChessWeights.Empty;
            WhiteScore = 0;
            BlackScore = 0;
            PGN = string.Empty;
        }

        public Game(Match match, ChessWeights whitePlayer, ChessWeights blackPlayer, int whiteScore, int blackScore, string pgn)
        {
            Id = ObjectId.NewObjectId();
            Match = match;
            WhitePlayer = whitePlayer;
            BlackPlayer = blackPlayer;
            WhiteScore = whiteScore;
            BlackScore = blackScore;
            PGN = pgn;
        }

        [BsonCtor]
        public Game(ObjectId id, Match match, ChessWeights whitePlayer, ChessWeights blackPlayer, int whiteScore,
            int blackScore, string pgn)
        {
            Id = id;
            Match = match;
            WhitePlayer = whitePlayer;
            BlackPlayer = blackPlayer;
            WhiteScore = whiteScore;
            BlackScore = blackScore;
            PGN = pgn;
        }
    }
}
