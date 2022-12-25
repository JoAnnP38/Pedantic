using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Pedantic.Genetics
{
    public class Generation : IMessageHandler
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
        }

        public Generation(Evolution evolution)
        {
            Id = ObjectId.NewObjectId();
            Evolution = evolution;
            State = GenerationState.Initial;
        }

        [BsonCtor]
        public Generation(ObjectId id, Evolution evolution, GenerationState state, ChessWeights? winner)
        {
            Id = id;
            Evolution = evolution;
            State = state;
            Winner = winner;
        }

        public ObjectId Id { get; set; }

        [BsonRef("evolutions")]
        public Evolution Evolution { get; set; }
        public GenerationState State { get; set; }

        [BsonRef("weights")]
        public ChessWeights? Winner { get; set; }

        [BsonIgnore] 
        public DateTime CreatedOn => Id.CreationTime;

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
                    case GenerationState.Initial:
                        InitialState(msg, rep);
                        break;

                    case GenerationState.Round0:
                        Round0State(msg, rep);
                        break;

                    case GenerationState.Round1:
                        Round1State(msg, rep);
                        break;

                    case GenerationState.Round2:
                        Round2State(msg, rep);
                        break;

                    case GenerationState.Round3:
                        Round3State(msg, rep);
                        break;

                    case GenerationState.Round4:
                        Round4State(msg, rep);
                        break;

                    case GenerationState.Round5:
                        Round5State(msg, rep);
                        break;

                    case GenerationState.SuddenDeath:
                        SuddenDeathState(msg, rep);
                        break;

                    case GenerationState.Complete:
                        CompleteState(msg, rep);
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

        public bool PostResult(Game game)
        {
            if (game.Match.PostResult(game))
            {
                bool roundComplete = true;

                foreach (Match match in GetRound(game.Match.RoundNumber))
                {
                    if (!match.IsComplete)
                    {
                        roundComplete = false;
                        break;
                    }
                }

                if (roundComplete)
                {
                    MessageHandler(Message.RoundComplete);
                }
            }
            return State == GenerationState.Complete;
        }

        public IEnumerable<GameToPlay> GetGamesToPlay(int maxGames)
        {
            if (State < GenerationState.Round0 || State > GenerationState.SuddenDeath)
            {
                return new List<GameToPlay>();
            }

            int roundIndex = (int)State - 1;
            List<Match> round = GetRound(roundIndex);
            List<GameToPlay> gamesToPlay = new();

            for (int n = 0; n < round.Count && maxGames > 0; ++n)
            {
                Match match = round[n];
                if (!match.IsComplete)
                {
                    if (match.Games.Count == 0)
                    {
                        gamesToPlay.Add(new GameToPlay(match, match.Player1, match.Player2));
                        --maxGames;
                    }

                    if (match.Games.Count < 2 && maxGames > 0)
                    {
                        gamesToPlay.Add(new GameToPlay(match, match.Player2, match.Player1));
                        --maxGames;
                    }
                }
            }

            return gamesToPlay;
        }

        private List<Match> CreateNewRound(GeneticsRepository rep, List<Match> prior, int roundNumber)
        {
            List<Match> round = new();
            for (int n = 0; n < prior.Count; n += 2)
            {
                ChessWeights? player1 = prior[n].Winner;
                ChessWeights? player2 = prior[n + 1].Winner;
                if (player1 == null || player2 == null)
                {
                    throw new Exception($"State of round {roundNumber - 1} is corrupt ({Id}).");
                }
                Match match = new Match(this, roundNumber, player1, player2);
                rep.Matches.Insert(match);
                round.Add(match);
            }

            return round;
        }

        private void InitialState(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.Start)
            {
                InitializeWeights(rep);
                ChessWeights[] weights = rep.Weights
                    .Find(w => w.IsActive)
                    .OrderByDescending(w => w.Score)
                    .ToArray();

                List<Match> round = new();
                for (int n = 0; n < 32; n++)
                {
                    Match match = new Match(this, 0, weights[n], weights[63 - n]);
                    rep.Matches.Insert(match);
                    round.Add(match);
                }

                rounds[0] = round;
                State = GenerationState.Round0;
                rep.Generations.Update(this);
            }
        }

        private void Round0State(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                rounds[1] = CreateNewRound(rep, GetRound(0), 1);
                State = GenerationState.Round1;
                rep.Generations.Update(this);
            }
        }

        private void Round1State(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                rounds[2] = CreateNewRound(rep, GetRound(1), 2);
                State = GenerationState.Round2;
                rep.Generations.Update(this);
            }
        }

        private void Round2State(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                rounds[3] = CreateNewRound(rep, GetRound(2), 3);
                State = GenerationState.Round3;
                rep.Generations.Update(this);
            }
        }

        private void Round3State(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                rounds[4] = CreateNewRound(rep, GetRound(3), 4);
                State = GenerationState.Round4;
                rep.Generations.Update(this);
            }
        }

        private void Round4State(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                rounds[5] = CreateNewRound(rep, GetRound(4), 5);
                State = GenerationState.Round5;
                rep.Generations.Update(this);
            }
        }

        private void Round5State(Message msg, GeneticsRepository rep)
        { 
            if (msg.MessageType == MessageType.RoundComplete)
            {
                foreach (ChessWeights w in rep.Weights.Find(w => w.IsActive))
                {
                    w.Age += 1;
                    rep.Weights.Update(w);
                }
                List<ChessWeights> children = new();
                foreach (Match match in GetRound(2))
                {
                    var progeny = ChessWeights.CrossOver(match.Player1, match.Player2, true);
                    rep.Weights.Insert(progeny.child1);
                    rep.Weights.Insert(progeny.child2);
                    children.Add(progeny.child1);
                    children.Add(progeny.child2);
                }

                List<Match> round0 = GetRound(0);
                List<Match> suddenDeath = new();

                for (int n = 0; n < 32; n++)
                {
                    if (round0[n].Loser == null)
                    {
                        throw new Exception($"Match data is corrupted -- winner not set ({Id}).");
                    }
                    Match sdMatch = new Match(this, 6, round0[n].Loser, children[n]);
                    rep.Matches.Insert(sdMatch);
                    suddenDeath.Add(sdMatch);
                }

                rounds[6] = suddenDeath;
                Winner = GetRound(5)[0].Winner;
                State = GenerationState.SuddenDeath;
                rep.Generations.Update(this);
            }
        }

        private void SuddenDeathState(Message msg, GeneticsRepository rep)
        {
            if (msg.MessageType == MessageType.RoundComplete)
            {
                foreach (Match match in GetRound(6))
                {
                    if (match.Loser == null)
                    {
                        throw new Exception($"Match data is corrupted -- winner not set ({Id}).");
                    }

                    match.Loser.IsActive = false;
                    rep.Weights.Update(match.Loser);
                }
                State = GenerationState.Complete;
                rep.Generations.Update(this);
                Evolution.MessageHandler(Message.GenerationComplete);
            }
        }

        private void CompleteState(Message msg, GeneticsRepository rep)
        {

        }

        public static void InitializeWeights(GeneticsRepository rep)
        {
            IEnumerable<ChessWeights> oldWeights =
                rep.Weights.Find(w => w.IsActive && !w.IsImmortal && w.Age >= 5);

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

        private readonly List<Match>?[] rounds = new List<Match>[7];
    }
}
