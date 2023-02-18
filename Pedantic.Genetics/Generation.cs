using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Pedantic.Utilities;

namespace Pedantic.Genetics
{
    public class Generation
    {
        public enum GenerationState
        {
            Initial,
            Round0,
            Round1,
            Round2,
            Round3,
            Round4,
            Round5,
            SuddenDeath,
            Complete
        }

        public Generation()
        {
            Id = ObjectId.Empty;
            Evolution = new Evolution();
            State = GenerationState.Initial;
            CreatedOn = DateTime.UtcNow;
        }

        public Generation(Evolution evolution)
        {
            Id = ObjectId.NewObjectId();
            Evolution = evolution;
            State = GenerationState.Initial;
            CreatedOn = Id.CreationTime;
        }

        [BsonCtor]
        public Generation(ObjectId id, Evolution evolution, GenerationState state, ChessWeights? winner, DateTime createdOn)
        {
            Id = id;
            Evolution = evolution;
            State = state;
            Winner = winner;
            CreatedOn = createdOn;
        }

        public ObjectId Id { get; set; }

        [BsonRef("evolutions")]
        public Evolution Evolution { get; set; }
        public GenerationState State { get; set; }

        [BsonRef("weights")]
        public ChessWeights? Winner { get; set; }

        public DateTime CreatedOn { get; private set; }

        public List<Match> GetRound(int round)
        {
            if (rounds[round] == null)
            {
                using var rep = new GeneticsRepository();
                IEnumerable<Match> mc = rep.Matches
                    .Include(m => m.Generation)
                    .Include(m => m.Generation.Winner)
                    .Include(m => m.Generation.Evolution)
                    .Include(m => m.Generation.Evolution.GrandWinner)
                    .Include(m => m.Player1)
                    .Include(m => m.Player2)
                    .Include(m => m.Winner)
                    .Find(m => m.Generation.Id == Id && m.RoundNumber == round)
                    .OrderBy(m => m.Id);

                rounds[round] = mc.ToList();
            }

            return rounds[round] ?? new List<Match>();
        }

        public bool Delete(GeneticsRepository rep)
        {
            bool deleted = true;

            try
            {
                rep.BeginTransaction();
                var matches = rep.Matches.Find(m => m.Generation.Id == Id);
                foreach (Match match in matches)
                {
                    var games = rep.Games.Find(g => g.Match.Id == match.Id);
                    foreach (Game game in games)
                    {
                        deleted = deleted && rep.Games.Delete(game.Id);
                    }

                    deleted = deleted && rep.Matches.Delete(match.Id);
                }

                deleted = deleted && rep.Generations.Delete(Id);
                if (!deleted)
                {
                    throw new Exception($"Unexpected exception: could not delete generation {Id}");
                }

                rep.CommitTransaction();
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.Message);
                rep.RollbackTransaction();
            }

            return deleted;
        }

        private readonly List<Match>?[] rounds = new List<Match>[7];
    }
}
