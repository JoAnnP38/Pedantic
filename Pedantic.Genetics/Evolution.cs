using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Pedantic.Genetics
{
    public class Evolution
    {
        public const int MAX_GENERATIONS = 400;

        public enum EvolutionState
        {
            Initial,
            Evolving,
            Converged,
            Failed,
            Cancelled
        }
        public Evolution()
        {
            Id = ObjectId.NewObjectId();
            State = EvolutionState.Initial;
            GrandWinner = null;
            TotalTime = TimeSpan.Zero;
        }

        [BsonCtor]
        public Evolution(ObjectId id, EvolutionState state, DateTime? convergedOn, DateTime updatedOn, TimeSpan totalTime, ChessWeights? grandWinner)
        {
            Id = id;
            this.state = state;
            ConvergedOn = convergedOn;
            UpdatedOn = updatedOn;
            GrandWinner = grandWinner;
            TotalTime = totalTime;
        }


        public ObjectId Id { get; set; }

        public EvolutionState State
        {
            get => state;
            set
            {
                state = value;
                UpdatedOn = DateTime.UtcNow;
            }
        }

        public DateTime? ConvergedOn { get; set; }

        public DateTime UpdatedOn { get; set; }

        public TimeSpan TotalTime { get; set; }

        [BsonRef("weights")]
        public ChessWeights? GrandWinner { get; set; }

        [BsonIgnore]
        public List<Generation> Generations
        {
            get
            {
                if (generations == null)
                {
                    using var rep = new GeneticsRepository();
                    IEnumerable<Generation> gen = rep.Generations
                        .Include(g => g.Evolution)
                        .Include(g => GrandWinner)
                        .Find(g => g.Evolution.Id == Id)
                        .OrderBy(g => g.Id);

                    generations = gen.ToList();
                }

                return generations;
            }
        }


        public static void InitializeWeights(GeneticsRepository rep)
        {
            IEnumerable<ChessWeights> oldWeights =
                rep.Weights.Find(w => w.IsActive && !w.IsImmortal && w.Age > 5);

            foreach (var oldWeight in oldWeights)
            {
                // TODO: Do not inactive last round's Grand Winner
                oldWeight.IsActive = false;
                rep.Weights.Update(oldWeight);
            }

            int count = rep.Weights.Count(w => w.IsActive);
            if (count == 0)
            {
                ChessWeights paragon = ChessWeights.CreateParagon();
                rep.Weights.Insert(paragon);
                count++;
            }

            while (count++ < 64)
            {
                rep.Weights.Insert(ChessWeights.CreateRandom());
            }
        }

        private EvolutionState state;
        private List<Generation>? generations = null;
    }
}
