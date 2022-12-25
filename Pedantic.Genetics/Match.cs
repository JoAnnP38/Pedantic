using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public class Match
    {
        public Match()
        {
            Id = ObjectId.Empty;
            Generation = new Generation();
            RoundNumber = 0;
            Player1 = ChessWeights.Empty;
            Player2 = ChessWeights.Empty;
            Winner = null;
            IsComplete = false;
        }

        public Match(Generation generation, int roundNumber, ChessWeights player1, ChessWeights player2)
        {
            Id = ObjectId.NewObjectId();
            Generation = generation;
            RoundNumber = roundNumber;
            Player1 = player1;
            Player2 = player2;
            Winner = null;
            IsComplete = false;
        }

        public Match(ObjectId id, Generation generation, int roundNumber, ChessWeights player1, ChessWeights player2,
            ChessWeights? winner, bool isComplete)
        {
            Id = id;
            Generation = generation;
            RoundNumber = roundNumber;
            Player1 = player1;
            Player2 = player2;
            Winner = winner;
            IsComplete = isComplete;
        }

        public ObjectId Id { get; set; }

        [BsonRef("generations")]
        public Generation Generation { get; set; }
        public int RoundNumber { get; set; }

        [BsonRef("weights")]
        public ChessWeights Player1 { get; set; }

        [BsonRef("weights")]
        public ChessWeights Player2 { get; set; }

        [BsonRef("weights")]
        public ChessWeights? Winner { get; set; }
        public bool IsComplete { get; set; }

        [BsonIgnore]
        public ChessWeights? Loser
        {
            get
            {
                ChessWeights? loser = null;
                if (Winner != null)
                {
                    loser = Winner.Id == Player1.Id ? Player2 : Player1;
                }

                return loser;
            }
        }

        [BsonIgnore]
        public List<Game> Games
        {
            get
            {
                if (games == null)
                {
                    using var rep = new GeneticsRepository();
                    IEnumerable<Game> gc = rep.Games
                        .Include(g => g.Match)
                        .Include(g => g.WhitePlayer)
                        .Include(g => g.BlackPlayer)
                        .Find(g => g.Match.Id == Id)
                        .OrderBy(g => g.Id);

                    games = gc.ToList();
                }

                return games;
            }
        }

        public bool PostResult(Game game)
        {
            if (!IsComplete)
            {
                using var rep = new GeneticsRepository();
                if (Games.Count == 0)
                {
                    if (game.WhitePlayer.Id != Player1.Id || game.BlackPlayer.Id != Player2.Id)
                    {
                        throw new InvalidOperationException($"Results posted out of sequence ({Id}).");
                    }
                    UpdatePlayerScore(Player1, game.WhiteScore);
                    UpdatePlayerScore(Player2, game.BlackScore);
                }
                else if (Games.Count == 1)
                {
                    if (game.WhitePlayer.Id != Player2.Id || game.BlackPlayer.Id != Player1.Id)
                    {
                        throw new InvalidOperationException($"Results posted out of sequence ({Id}).");
                    }
                    UpdatePlayerScore(Player1, game.BlackScore);
                    UpdatePlayerScore(Player2, game.WhiteScore);
                    int player1Score = game.BlackScore + Games[0].WhiteScore;
                    int player2Score = game.WhiteScore + Games[0].BlackScore;

                    if (player1Score > player2Score)
                    {
                        Winner = Player1;
                    }
                    else if (player2Score > player1Score)
                    {
                        Winner = Player2;
                    }
                    else if (Random.Shared.NextDouble() < 0.5d)
                    {
                        Winner = Player1;
                    }
                    else
                    {
                        Winner = Player2;
                    }

                    IsComplete = true;
                }

                rep.Games.Insert(game);
                rep.Matches.Update(this);
                rep.Weights.Update(Player1);
                rep.Weights.Update(Player2);
            }

            return IsComplete;
        }

        private void UpdatePlayerScore(ChessWeights weights, int score)
        {
            switch (score)
            {
                case 0:
                    weights.Losses += 1;
                    break;

                case 1:
                    weights.Draws += 1;
                    break;

                case 2:
                    weights.Wins += 1;
                    break;
            }
        }

        private List<Game>? games = null;
    }

}
