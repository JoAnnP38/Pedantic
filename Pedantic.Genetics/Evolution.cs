using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Pedantic.Genetics
{
    public class Evolution : IMessageHandler
    {
        public const int MAX_GENERATIONS = 100;

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
        }

        [BsonCtor]
        public Evolution(ObjectId id, EvolutionState state, DateTime updatedOn, ChessWeights? grandWinner)
        {
            Id = id;
            this.state = state;
            UpdatedOn = updatedOn;
            GrandWinner = grandWinner;
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

        public DateTime UpdatedOn { get; private set; }

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

        public void MessageHandler(Message msg, GeneticsRepository? rep = null)
        {
            bool dispose = false, commit = false;
            
            if (rep == null)
            {
                rep = new GeneticsRepository();
                dispose = true;
            }

            try
            {
                commit = rep.BeginTransaction();
                switch (State)
                {
                    case EvolutionState.Initial:
                        InitialState(msg, rep);
                        break;

                    case EvolutionState.Evolving:
                        EvolvingState(msg, rep);
                        break;

                    case EvolutionState.Converged:
                        ConvergedState(msg, rep);
                        break;

                    case EvolutionState.Failed:
                        FailedState(msg, rep);
                        break;

                    case EvolutionState.Cancelled:
                        CancelledState(msg, rep);
                        break;
                }

                if (commit)
                {
                    rep.CommitTransaction();
                }
            }
            catch
            {
                if (commit)
                {
                    rep.RollbackTransaction();
                }

                throw;
            }
            finally
            {
                if (dispose)
                {
                    rep.Dispose();
                }
            }
        }

        private void InitialState(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.Start)
            {
                Generation gen = new Generation(this);
                State = EvolutionState.Evolving;
                rep.Evolutions.Upsert(this);
                rep.Generations.Insert(gen);
                gen.MessageHandler(msg, rep);
            }
            else if (msg.MessageType == MessageType.Cancel)
            {
                State = EvolutionState.Cancelled;
                rep.Evolutions.Update(this);
            }
        }

        private void EvolvingState(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.GenerationComplete)
            {
                int generationCount = rep.Generations.Count(g => g.Evolution.Id == Id);
                if (generationCount >= MAX_GENERATIONS)
                {
                    MessageHandler(Message.EvolutionComplete, rep);
                }
                else
                {
                    Generation gen = new(this);
                    rep.Generations.Insert(gen);
                    gen.MessageHandler(Message.Start, rep);
                }
            }
            else if (msg.MessageType == MessageType.EvolutionComplete)
            {
                Generation[] gens = rep.Generations
                    .Include(g => g.Winner)
                    .Find(g => g.Evolution.Id == Id && g.State == Generation.GenerationState.Complete)
                    .OrderByDescending(g => g.CreatedOn)
                    .ToArray();

                ChessWeights? potential = gens[0].Winner;
                if (potential == null)
                {
                    throw new Exception($"Generation data corrupted -- winner not set ({Id}).");
                }

                ChessWeights? lastWinner = gens[1].Winner;
                if (lastWinner == null)
                {
                    throw new Exception($"Generation data corrupted -- winner not set ({Id}).");
                }

                if (potential.PercentChanged(lastWinner) <= 1.0d && !potential.IsImmortal)
                {
                    // convergence achieved
                    GrandWinner = potential;
                    State = EvolutionState.Converged;
                }
                else
                {
                    GrandWinner = null;
                    State = EvolutionState.Failed;
                }

                rep.Evolutions.Update(this);
            }
        }

        private void ConvergedState(Message msg, GeneticsRepository rep)
        {

        }

        private void FailedState(Message msg, GeneticsRepository rep)
        {

        }

        private void CancelledState(Message msg, GeneticsRepository rep)
        {

        }

        public static void StartEvolution()
        {
            using var rep = new GeneticsRepository();
            if (rep.Evolutions.Exists(e => e.State == EvolutionState.Evolving))
            {
                return;
            }

            Evolution? ev = rep.Evolutions.FindOne(e => e.State == EvolutionState.Initial);
            if (ev == null)
            {
                ev = new Evolution();
                rep.Evolutions.Insert(ev);
            }

            ev.MessageHandler(Message.Start);
        }

        public static IEnumerable<GameToPlay> GetGamesToPlay(int maxGames = 1)
        {
            using var rep = new GeneticsRepository();
            Generation? gen = rep.Generations
                .Include(g => g.Evolution)
                .Include(g => g.Winner)
                .FindOne(g => g.Evolution.State == EvolutionState.Evolving &&
                              g.State >= Generation.GenerationState.Round0 &&
                              g.State <= Generation.GenerationState.SuddenDeath);

            if (gen == null)
            {
                throw new InvalidOperationException(
                    "Cannot request games until evolution has start or after it has ended.");
            }
            return gen.GetGamesToPlay(maxGames);
        }

        public static bool PostResult(GameToPlay gameToPlay, int whiteScore, int blackScore, string pgn)
        {
            using var rep = new GeneticsRepository();

            Match? match = rep.Matches
                .Include(m => m.Generation)
                .Include(m => m.Generation.Winner)
                .Include(m => m.Generation.Evolution)
                .Include(m => m.Generation.Evolution.GrandWinner)
                .Include(m => m.Winner)
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FindById(new ObjectId(gameToPlay.MatchId));

            if (match == null)
            {
                throw new InvalidOperationException($"Cannot post results to match that doesn't exist ({gameToPlay.MatchId}).");
            }

            ChessWeights? whitePlayer = rep.Weights.FindById(new ObjectId(gameToPlay.WhiteId));
            if (whitePlayer == null)
            {
                throw new InvalidOperationException(
                    $"Cannot post results for invalid white player ({gameToPlay.WhiteId}).");
            }

            ChessWeights? blackPlayer = rep.Weights.FindById(new ObjectId(gameToPlay.BlackId));
            if (blackPlayer == null)
            {
                throw new InvalidOperationException(
                    $"Cannot post results for invalid black player ({gameToPlay.BlackId}).");
            }

            match.Generation.PostResult(new Game(match, whitePlayer, blackPlayer, whiteScore, blackScore, pgn));
            EvolutionState state = match.Generation.Evolution.State;
            return state != EvolutionState.Evolving;
        }

        private EvolutionState state;
        private List<Generation>? generations = null;
    }
}
