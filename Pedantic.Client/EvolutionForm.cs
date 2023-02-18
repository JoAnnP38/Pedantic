using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using LiteDB;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Client
{
    public partial class EvolutionForm : Form
    {
        public enum State
        {
            Stopped,
            Starting,
            Tournament,
            SuddenDeath,
            Stopping,
            Cancelling
        }

        public enum TournamentType
        {
            Normal,
            SuddenDeath
        }

        public class BackgroundArgs
        {
            public string Command { get; set; } = string.Empty;
            public string Args { get; set; } = string.Empty;
            public string WorkingDir { get; set; } = string.Empty;
        }

        public class MatchKey : IEquatable<MatchKey>, IComparable<MatchKey>
        {
            public int Round { get; set; }
            public ObjectId MinPlayer { get; set; }
            public ObjectId MaxPlayer { get; set; }

            public MatchKey(int round, ObjectId player1, ObjectId player2)
            {
                Round = round;
                MinPlayer = player1.CompareTo(player2) <= 0 ? player1 : player2;
                MaxPlayer = player1.CompareTo(player2) <= 0 ? player2 : player1;
            }

            public bool Equals(MatchKey? other)
            {
                if (other == null)
                {
                    return false;
                }

                return Round == other.Round && MinPlayer.Equals(other.MinPlayer) && MaxPlayer.Equals(other.MaxPlayer);
            }

            public override bool Equals(object? obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                return Equals(obj as MatchKey);
            }

            public int CompareTo(MatchKey? other)
            {
                if (other == null)
                {
                    return 1;
                }
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                int result = Round - other.Round;
                if (result == 0)
                {
                    result = MinPlayer.CompareTo(other.MinPlayer);
                    if (result == 0)
                    {
                        return MaxPlayer.CompareTo(other.MaxPlayer);
                    }
                    return result;
                }
                return result;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Round, MinPlayer, MaxPlayer);
            }
        }

        public State FormState { get; set; } = State.Stopped;
        public Evolution? Evolution { get; set; }
        public TimeSpan RunTime { get; set; } = TimeSpan.Zero;
        public TimeSpan AvgGenTime { get; set; } = TimeSpan.Zero;

        public EvolutionForm()
        {
            InitializeComponent();
        }

        private void EvolutionForm_Load(object sender, EventArgs e)
        {
            using var rep = new GeneticsRepository();
            Evolution.InitializeWeights(rep);
            Evolution = rep.Evolutions
                .Include(e => e.GrandWinner)
                .FindAll()
                .OrderByDescending(e => e.UpdatedOn)
                .FirstOrDefault();

            if (Evolution == null)
            {
                Evolution = new Evolution()
                {
                    UpdatedOn = DateTime.Now
                };
                rep.Evolutions.Insert(Evolution);
            }

            EvolutionUpdated(false);

            Evolution? converged1 = null;
            if (Evolution.State == Evolution.EvolutionState.Converged && Evolution.GrandWinner != null)
            {
                converged1 = Evolution;
            }
            else
            {
                converged1 = rep.Evolutions
                    .Include(e => e.GrandWinner)
                    .Find(e => e.State == Evolution.EvolutionState.Converged && e.GrandWinner != null)
                    .OrderByDescending(e => e.ConvergedOn)
                    .FirstOrDefault();
            }

            txtConvergePct.Text = @"0.000%";
            if (converged1 != null && converged1.GrandWinner != null)
            {
                Evolution? converged2 = rep.Evolutions
                    .Include(e => e.GrandWinner)
                    .Find(e => e.State == Evolution.EvolutionState.Converged && e.Id != converged1.Id && e.GrandWinner != null)
                    .OrderByDescending(e => e.ConvergedOn)
                    .FirstOrDefault();

                if (converged2 != null && converged2.GrandWinner != null)
                {
                    double pctConverged = 100.0 - converged1.GrandWinner.PercentChanged(converged2.GrandWinner);
                    txtConvergePct.Text = $@"{pctConverged:F3}";
                }
            }

            txtAvgGenTime.Text = $@"{AvgGenTime:hh\:mm\:ss}";
            txtRunTime.Text = $@"{RunTime:d\.hh\:mm\:ss}";
            toolStripOutput.Text = string.Empty;
            UpdateState(State.Stopped, false);
            Text = $@"Pedantic Evolution ({StateToString(FormState)})";
        }

        private void UpdateState(State state, bool forceUpdate = true)
        {
            FormState = state;
            toolStripStatus.Text = StateToString(state);
            Text = @$"Pedantic Evolution ({StateToString(state)})";
            switch (state)
            {
                case State.Stopped:
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    btnCancel.Enabled = true;
                    break;

                case State.Starting:
                case State.Stopping:
                case State.Cancelling:
                    btnStart.Enabled = false;
                    btnStop.Enabled = false;
                    btnCancel.Enabled = false;
                    break;

                case State.Tournament:
                case State.SuddenDeath:
                    btnStart.Enabled = false;
                    btnStop.Enabled = true;
                    btnCancel.Enabled = true;
                    break;
            }

            if (forceUpdate)
            {
                ForceDisplay(statusStrip);
            }
        }

        public static string StateToString(State state)
        {
            return state switch
            {
                State.Stopped => "Stopped",
                State.Starting => "Starting",
                State.Tournament => "Running",
                State.SuddenDeath => "Running",
                State.Stopping => "Stopping",
                State.Cancelling => "Cancelling",
                _ => string.Empty
            };
        }

        public static string EvolutionStateToString(Evolution.EvolutionState state)
        {
            return state switch
            {
                Evolution.EvolutionState.Evolving => "Evolving",
                Evolution.EvolutionState.Initial => "New",
                Evolution.EvolutionState.Converged => "Converged",
                Evolution.EvolutionState.Cancelled => "Cancelled",
                Evolution.EvolutionState.Failed => "Failed",
                _ => string.Empty
            };
        }

        private DateTime runStart = DateTime.MaxValue;

        private void runTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now - runStart;
            txtRunTime.Text = ts.ToString(@"hh\:mm\:ss");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!StartPreconditionsMet())
            {
                return;
            }

            UpdateState(State.Starting);
            StartEvolution();
            UpdateState(State.Tournament);
        }

        private bool StartPreconditionsMet()
        {
            if (FormState != State.Stopped)
            {
                return false;
            }

            if (!File.Exists(Program.AppSettings.EnginePath))
            {
                return false;
            }

            return File.Exists(Program.AppSettings.CuteChessPath);
        }

        private void StartEvolution()
        {
            if (Evolution == null)
            {
                throw new Exception("Unexpected: Evolution object is null.");
            }

            using GeneticsRepository rep = new GeneticsRepository();
            Evolution.EvolutionState[] endStates =
            {
                Evolution.EvolutionState.Cancelled, Evolution.EvolutionState.Converged, Evolution.EvolutionState.Failed
            };
            if (endStates.Contains(Evolution.State))
            {
                Evolution = new Evolution
                {
                    UpdatedOn = DateTime.Now
                };
                rep.Evolutions.Insert(Evolution);
            }

            if (Evolution.State == Evolution.EvolutionState.Evolving)
            {
                // delete any incomplete generations
                var generations = rep.Generations
                    .Find(g => g.Evolution.Id == Evolution.Id &&
                               g.State != Generation.GenerationState.Complete);

                if (generations != null)
                {
                    foreach (Generation? g in generations)
                    {
                        g?.Delete(rep);
                    }
                }
            }

            Evolution.State = Evolution.EvolutionState.Evolving;
            rep.Evolutions.Update(Evolution);
            EvolutionUpdated();

            currentGeneration = new(Evolution);
            rep.Generations.Insert(currentGeneration);
            runStart = DateTime.Now;
            runTimer.Start();

            StartTournament(TournamentType.Normal, currentGeneration, rep);
        }

        private void BuildArguments(TournamentType type, GeneticsRepository rep, ChessWeights[] weights, out string args)
        {
            if (Evolution == null)
            {
                throw new InvalidOperationException("Evolution is null");
            }

            if (currentGeneration == null)
            {
                throw new InvalidOperationException("Current generation is null");
            }

            StringBuilder sbArgs = new();
            List<EngineConfiguration> engConfigs = new();
            AppSettings settings = Program.AppSettings;

            double tcTime = settings.TimeControlsTime / 1000.0;
            double tcIncrement = settings.TimeControlsIncrement / 1000.0;
            StringBuilder sb = new();
            sb.Append($"generation_{currentGeneration.Id}");
            if (type == TournamentType.SuddenDeath)
            {
                sb.Append("_suddendeath");
            }

            sb.Append(".pgn");
            string pgnFile = sb.ToString();

            string engDir = Path.GetDirectoryName(settings.EnginePath) ?? Environment.CurrentDirectory;
            engDir = engDir.Replace('\\', '/');
            foreach (var wt in weights)
            {
                engConfigs.Add(new()
                {
                    command=settings.EnginePath.Replace('\\', '/'),
                    initStrings = new []
                    {
                        $"setoption name Hash value {settings.HashSize}", 
                        $"setoption name Ponder value {settings.Ponder}",
                        $"setoption name Search_Algorithm value {settings.SearchType}"
                    },
                    name=wt.Id.ToString(),
                    ponder=settings.Ponder,
                    protocol="uci",
                    stderrFile=Path.Combine(engDir, $"engine_{wt.Id}_errors.txt"),
                    whitepov=false,
                    workingDirectory=engDir
                });
                sbArgs.Append($"-engine conf={wt.Id} option.Evaluation_ID={wt.Id} ");
            }

            int games = type == TournamentType.Normal ? 4 : 2;
            sbArgs.Append($"-each proto=uci tc=40/{tcTime:F1}+{tcIncrement:F3} restart=on ");
            sbArgs.Append($"option.Search_Algorithm={settings.SearchType} timemargin=20 arg=uci -variant standard ");
            sbArgs.Append($"-concurrency {settings.SimultaneousGames} -draw movenumber=60 movecount=4 score=20 ");
            sbArgs.Append("-resign movecount=4 score=1400 twosided=true ");
            sbArgs.Append($"-maxmoves {settings.MaxMoves} -tournament knockout -event {currentGeneration.Id} -games {games} ");
            sbArgs.Append($"-pgnout {pgnFile} -site \"Clearwater, FL USA\"");
            if (settings.Debug)
            {
                sbArgs.Append(" -debug");
            }
            args = sbArgs.ToString();

            using var json = File.CreateText(Path.Combine(engDir, "engines.json"));
            string jsonString = System.Text.Json.JsonSerializer.Serialize(engConfigs);
            json.WriteLine(jsonString);
            json.Flush();
            json.Close();
        }

        private void StartTournament(TournamentType type, Generation gen, GeneticsRepository rep)
        {
            ChessWeights[] weights = type == TournamentType.Normal
                ? TournamentWeights(rep, true)
                : SuddenDeathWeights(rep, gen);

            if (weights.Length != 64)
            {
                throw new Exception("Incorrect number of available weights.");
            }

            BuildArguments(type, rep, weights, out string args);
            bgWorker.RunWorkerAsync(new BackgroundArgs
            {
                Command = Program.AppSettings.CuteChessPath,
                Args = args,
                WorkingDir = Path.GetDirectoryName(Program.AppSettings.EnginePath) ?? Environment.CurrentDirectory
            });
        }

        private ChessWeights[] TournamentWeights(GeneticsRepository rep, bool ageWeights = false)
        {
            if (ageWeights)
            {
                foreach (var wt in rep.Weights.Find(w => w.IsActive))
                {
                    wt.Age += 1;
                    if (wt.Age > 5 && !wt.IsImmortal)
                    {
                        wt.IsActive = false;
                    }

                    wt.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(wt);
                }
            }

            ChessWeights.CalculateStatistics(rep, out double[] mean, out double[] sigma);
            int activeCount = rep.Weights.Find(w => w.IsActive).Count();
            for (int n = 0; n < 64 - activeCount; n++)
            {
                rep.Weights.Insert(ChessWeights.CreateRandom());
            }

            if (activeCount > 64)
            {
                ChessWeights[] least = rep.Weights
                    .Find(w => w.IsActive && !w.IsImmortal)
                    .OrderBy(w => w.Score)
                    .ThenByDescending(w => w.Age)
                    .ToArray();

                for (int n = 0; n < activeCount - 64; n++)
                {
                    least[n].IsActive = false;
                    rep.Weights.Update(least[n]);
                }
            }

            ChessWeights[] weights = rep.Weights
                .Find(w => w.IsActive)
                .OrderByDescending(w => w.Score)
                .Take(64)
                .ToArray();

            Random.Shared.Shuffle(weights);
            return weights;
        }

        private ChessWeights[] SuddenDeathWeights(GeneticsRepository rep, Generation gen)
        {
            List<ChessWeights> weights = new List<ChessWeights>();
            var losers = rep.Matches
                .Include(m => m.Player1)
                .Find(m => m.Generation.Id == gen.Id && m.RoundNumber == 1 && m.Winner != null &&
                           m.Winner.Id != m.Player1.Id)
                .Select(m => m.Player1)
                .Union(rep.Matches
                    .Include(m => m.Player2)
                    .Find(m => m.Generation.Id == gen.Id && m.RoundNumber == 1 && m.Winner != null && 
                               m.Winner.Id != m.Player2.Id)
                    .Select(m => m.Player2))
                .ToArray();


            for (int roundNumber = 3; roundNumber <= 6; roundNumber++)
            {
                int number = roundNumber;
                var parents = rep.Matches
                    .Include(m => m.Player1)
                    .Include(m => m.Player2)
                    .Find(m => m.Generation.Id == gen.Id && m.RoundNumber == number);

                foreach (var p in parents)
                {
                    var progeny = ChessWeights.CrossOver(p.Player1, p.Player2, true);
                    rep.Weights.Insert(progeny.child1);
                    rep.Weights.Insert(progeny.child2);
                    weights.Add(progeny.child1);
                    weights.Add(progeny.child2);

                    if (roundNumber == 6)
                    {
                        ChessWeights w1, w2;
                        ChessWeights.CalculateStatistics(rep, out double[] mean, out double[] sigma);
                        if (mean.Length == ChessWeights.MAX_WEIGHTS && sigma.Length == ChessWeights.MAX_WEIGHTS)
                        {
                            w1 = ChessWeights.CreateNormal(mean, sigma);
                            w2 = ChessWeights.CreateNormal(mean, sigma);
                        }
                        else
                        {
                            w1 = ChessWeights.CreateRandom();
                            w2 = ChessWeights.CreateRandom();
                        }

                        rep.Weights.Insert(w1);
                        rep.Weights.Insert(w2);
                        weights.Add(w1);
                        weights.Add(w2);
                    }
                }
            }

            int childrenCount = weights.Count;
            weights.AddRange(losers[..(64 - childrenCount)]);

            Random.Shared.Shuffle(weights);
            return weights.ToArray();
        }

        private void EvolutionUpdated(bool forceUpdate = true)
        {
            if (Evolution != null)
            {
                using var rep = new GeneticsRepository();
                txtEvolutionID.Text = Evolution.Id.ToString();
                txtEvolutionState.Text = EvolutionStateToString(Evolution.State);
                txtUpdatedOn.Text = Evolution.UpdatedOn.ToString("G");
                int count = rep.Generations
                    .Find(g => g.Evolution.Id == Evolution.Id && g.State == Generation.GenerationState.Complete)
                    .Count();
                txtCount.Text = count.ToString();
                txtMaxGen.Text = Program.AppSettings.MaxGenerations.ToString();
            }
            else
            {
                txtEvolutionID.Text = string.Empty;
                txtEvolutionState.Text = string.Empty;
                txtUpdatedOn.Text = string.Empty;
                txtCount.Text = string.Empty;
                txtMaxGen.Text = string.Empty;
            }

            if (forceUpdate)
            {
                ForceDisplay(this);
            }
        }

        private static void ForceDisplay(Control control)
        {
            control.Invalidate();
            control.Update();
            control.Refresh();
            Application.DoEvents();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Process? cuteChess = null;
            StreamWriter? errorOut = null;

            try
            {
                BackgroundWorker worker = sender as BackgroundWorker ?? throw new InvalidOperationException();
                BackgroundArgs args = e.Argument as BackgroundArgs ?? throw new InvalidOperationException();
                errorOut = File.AppendText(Path.Combine(args.WorkingDir, "cutechess-errors.txt"));

                cuteChess = new()
                {
                    StartInfo = new ProcessStartInfo(args.Command, args.Args)
                    {
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        WorkingDirectory = args.WorkingDir,
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };

                bool tournamentComplete = false;

                cuteChess.OutputDataReceived += (o, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        statusStrip.Invoke(() => { toolStripOutput.Text = eventArgs.Data; });
                    }

                    if (worker.CancellationPending)
                    {
                        cuteChess.Kill(true);
                    }
                };

                cuteChess.ErrorDataReceived += (o, eventArgs) =>
                {
                    if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        errorOut.WriteLine($"{DateTime.Now:s} - {eventArgs.Data}");
                    }

                    if (worker.CancellationPending)
                    {
                        cuteChess.Kill(true);
                    }
                };

                cuteChess.Exited += (o, eventArgs) =>
                {
                    if (!worker.CancellationPending)
                    {
                        tournamentComplete = true;
                    }
                };

                if (!cuteChess.Start())
                {
                    e.Result = false;
                    return;
                }

                cuteChess.BeginOutputReadLine();
                cuteChess.BeginErrorReadLine();

                while (!tournamentComplete && !worker.CancellationPending && !cuteChess.HasExited)
                {
                    if (cuteChess.WaitForExit(1000))
                    {
                        break;
                    }
                }

                if (worker.CancellationPending)
                {
                    if (!cuteChess.HasExited)
                    {
                        cuteChess.Kill(true);
                        cuteChess.WaitForExit();
                    }

                    e.Result = false;
                    return;
                }

                e.Result = true;
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
                if (cuteChess != null && !cuteChess.HasExited)
                {
                    cuteChess.Kill(true);
                }

                throw;
            }
            finally
            {
                if (errorOut != null)
                {
                    errorOut.Close();
                }

                if (cuteChess != null)
                {
                    cuteChess.Close();
                }
            }
        }

        private void UpdateToolStripOutput(string message)
        {
            if (statusStrip.InvokeRequired)
            {
                BeginInvoke(new UpdateToolStripOutputMethod(UpdateToolStripOutput));
                return;
            }

            toolStripOutput.Text = message;
        }

        private delegate void UpdateToolStripOutputMethod(string message);
        private Generation? currentGeneration;

        private void EvolutionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
            }
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                UpdateState(State.Stopped, false);
                runTimer.Stop();
                return;
            }
            if (e.Cancelled)
            {
                runTimer.Stop();
                Hide();
                Close();
                return;
            }

            if (FormState == State.Stopping)
            {
                UpdateState(State.Stopped, false);
                runTimer.Stop();
            }
            else if (FormState == State.Tournament)
            {
                ProcessTournamentPgn();
                StartTournament(TournamentType.SuddenDeath, currentGeneration!, new GeneticsRepository());
            }
            else
            {
                ProcessSuddenDeathPgn();
                if (FormState == State.Tournament)
                {
                    StartEvolution();
                }
            }
        }

        private void ProcessSuddenDeathPgn()
        {
            using var rep = new GeneticsRepository();
            string pgnPath = Path.GetDirectoryName(Program.AppSettings.EnginePath) ?? Environment.CurrentDirectory;
            pgnPath = Path.Combine(pgnPath, $"generation_{currentGeneration!.Id}_suddendeath.pgn");
            StringBuilder sbGamePgn = new();
            ChessWeights? whitePlayer = null;
            ChessWeights? blackPlayer = null;
            int whiteScore = 0;
            int blackScore = 0;
            int currentRound = 0;
            Dictionary<MatchKey, Match> matchLookup = new();

            using var pgnFile = File.OpenText(pgnPath);
            while (!pgnFile.EndOfStream)
            {
                string? line = pgnFile.ReadLine();
                if (line != null)
                {
                    if (line.StartsWith("[Event") && sbGamePgn.Length > 0 && currentRound > 0 &&
                        whitePlayer != null && blackPlayer != null)
                    {
                        if (currentRound == 1)
                        {
                            MatchKey key = new(currentRound, whitePlayer.Id, blackPlayer.Id);
                            Match match;
                            if (matchLookup.ContainsKey(key))
                            {
                                match = matchLookup[key];
                            }
                            else
                            {
                                match = new(currentGeneration, currentRound, whitePlayer, blackPlayer);
                                matchLookup.Add(key, match);
                                rep.Matches.Insert(match);
                            }

                            Game game = new(match, whitePlayer, blackPlayer, whiteScore, blackScore,
                                sbGamePgn.ToString().Trim());
                            rep.Games.Insert(game);
                        }

                        whitePlayer.Wins += whiteScore == 2 ? 1 : 0;
                        whitePlayer.Draws += whiteScore == 1 ? 1 : 0;
                        whitePlayer.Losses += whiteScore == 0 ? 1 : 0;
                        whitePlayer.UpdatedOn = DateTime.UtcNow;
                        rep.Weights.Update(whitePlayer);

                        blackPlayer.Wins += blackScore == 2 ? 1 : 0;
                        blackPlayer.Draws += blackScore == 1 ? 1 : 0;
                        blackPlayer.Losses += blackScore == 0 ? 1 : 0;
                        blackPlayer.UpdatedOn = DateTime.UtcNow;
                        rep.Weights.Update(blackPlayer);

                        sbGamePgn.Clear();
                        whitePlayer = null;
                        blackPlayer = null;
                        whiteScore = 0;
                        blackScore = 0;
                    }
                    else if (line.StartsWith("[Round"))
                    {
                        currentRound = int.Parse(ParseString(line));
                    }
                    else if (line.StartsWith("[White"))
                    {
                        ObjectId id = new(ParseString(line));
                        whitePlayer = rep.Weights.FindById(id);
                    }
                    else if (line.StartsWith("[Black"))
                    {
                        ObjectId id = new(ParseString(line));
                        blackPlayer = rep.Weights.FindById(id);
                    }
                    else if (line.StartsWith("[Result"))
                    {
                        string result = ParseString(line);
                        if (result == "1-0")
                        {
                            whiteScore = 2;
                            blackScore = 0;
                        }
                        else if (result == "0-1")
                        {
                            blackScore = 2;
                            whiteScore = 0;
                        }
                        else if (result == "1/2-1/2")
                        {
                            whiteScore = 1;
                            blackScore = 1;
                        }
                    }

                    sbGamePgn.AppendLine(line);
                }
            }
            if (sbGamePgn.Length > 0 && currentRound > 0 && whitePlayer != null && blackPlayer != null)
            {
                if (currentRound == 1)
                {
                    MatchKey key = new(currentRound, whitePlayer.Id, blackPlayer.Id);
                    Match match;
                    if (matchLookup.ContainsKey(key))
                    {
                        match = matchLookup[key];
                    }
                    else
                    {
                        match = new(currentGeneration, currentRound, whitePlayer, blackPlayer);
                        matchLookup.Add(key, match);
                        rep.Matches.Insert(match);
                    }

                    Game game = new(match, whitePlayer, blackPlayer, whiteScore, blackScore,
                        sbGamePgn.ToString().Trim());
                    rep.Games.Insert(game);
                }

                whitePlayer.Wins += whiteScore == 2 ? 1 : 0;
                whitePlayer.Draws += whiteScore == 1 ? 1 : 0;
                whitePlayer.Losses += whiteScore == 0 ? 1 : 0;
                whitePlayer.UpdatedOn = DateTime.UtcNow;
                rep.Weights.Update(whitePlayer);

                blackPlayer.Wins += blackScore == 2 ? 1 : 0;
                blackPlayer.Draws += blackScore == 1 ? 1 : 0;
                blackPlayer.Losses += blackScore == 0 ? 1 : 0;
                blackPlayer.UpdatedOn = DateTime.UtcNow;
                rep.Weights.Update(blackPlayer);
            }

            foreach (Match match in matchLookup.Values)
            {
                int player1Score = 0;
                int player2Score = 0;

                foreach (Game game in rep.Games.Find(g => g.Match.Id == match.Id))
                {
                    if (match.Player1.Id == game.WhitePlayer.Id)
                    {
                        player1Score += game.WhiteScore;
                        player2Score += game.BlackScore;
                    }
                    else
                    {
                        player1Score += game.BlackScore;
                        player2Score += game.WhiteScore;
                    }
                }

                match.Winner = player1Score >= player2Score ? match.Player1 : match.Player2;
                match.IsComplete = true;
                rep.Matches.Update(match);

                if (!(match.Loser?.IsImmortal ?? true))
                {
                    match.Loser.IsActive = false;
                    match.Loser.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(match.Loser);
                }
            }

            currentGeneration.State = Generation.GenerationState.Complete;
            rep.Generations.Update(currentGeneration);

            // test convergence
            Evolution!.TotalTime += DateTime.Now - runStart;
            Generation? last = rep.Generations
                .Include(g => g.Winner)
                .Find(g => g.Evolution.Id == Evolution.Id && 
                           g.State == Generation.GenerationState.Complete &&
                           g.CreatedOn < currentGeneration.CreatedOn)
                .MaxBy(g => g.CreatedOn);

            if (last != null && last.Winner != null && !last.Winner.IsImmortal && 
                currentGeneration.Winner != null && !currentGeneration.Winner.IsImmortal)
            {
                double pctConverged = 100.0 - currentGeneration.Winner.PercentChanged(last.Winner);
                txtConvergePct.Text = $@"{pctConverged:F3}";

                if (pctConverged >= 99.0)
                {
                    Evolution.State = Evolution.EvolutionState.Converged;
                    Evolution.GrandWinner = currentGeneration.Winner;
                    Evolution.ConvergedOn = DateTime.UtcNow;
                    UpdateState(State.Stopped, false);
                    runTimer.Stop();
                    rep.Evolutions.Update(Evolution);
                    Evolution.GrandWinner.IsImmortal = true;
                    rep.Weights.Update(Evolution.GrandWinner);
                    EvolutionUpdated(false);
                    runStart = DateTime.Now;
                    return;
                }
            }

            int count = rep.Generations
                .Find(g => g.Evolution.Id == Evolution.Id && g.State == Generation.GenerationState.Complete)
                .Count();

            if (count >= Program.AppSettings.MaxGenerations)
            {
                Evolution.State = Evolution.EvolutionState.Failed;
                UpdateState(State.Stopped, false);
                runTimer.Stop();
                rep.Evolutions.Update(Evolution);
                EvolutionUpdated(false);
                runStart = DateTime.Now;
                return;
            }
            UpdateState(State.Tournament, false);
            rep.Evolutions.Update(Evolution);
            EvolutionUpdated(false);
            runStart = DateTime.Now;
        }

        private void ProcessTournamentPgn()
        {
            try
            {
                using var rep = new GeneticsRepository();
                string pgnPath = Path.GetDirectoryName(Program.AppSettings.EnginePath) ?? Environment.CurrentDirectory;
                pgnPath = Path.Combine(pgnPath, $"generation_{currentGeneration!.Id}.pgn");
                StringBuilder sbGamePgn = new();
                ChessWeights? whitePlayer = null;
                ChessWeights? blackPlayer = null;
                int whiteScore = 0;
                int blackScore = 0;
                int currentRound = 0;
                Dictionary<MatchKey, Match> matchLookup = new();

                using var pgnFile = File.OpenText(pgnPath);
                while (!pgnFile.EndOfStream)
                {
                    string? line = pgnFile.ReadLine();
                    if (line != null)
                    {
                        if (line.StartsWith("[Event") && sbGamePgn.Length > 0 && currentRound > 0 &&
                            whitePlayer != null && blackPlayer != null)
                        {
                            MatchKey key = new(currentRound, whitePlayer.Id, blackPlayer.Id);
                            Match match;
                            if (matchLookup.ContainsKey(key))
                            {
                                match = matchLookup[key];
                            }
                            else
                            {
                                match = new(currentGeneration, currentRound, whitePlayer, blackPlayer);
                                matchLookup.Add(key, match);
                                rep.Matches.Insert(match);
                            }

                            Game game = new(match, whitePlayer, blackPlayer, whiteScore, blackScore,
                                sbGamePgn.ToString().Trim());
                            rep.Games.Insert(game);

                            whitePlayer.Wins += whiteScore == 2 ? 1 : 0;
                            whitePlayer.Draws += whiteScore == 1 ? 1 : 0;
                            whitePlayer.Losses += whiteScore == 0 ? 1 : 0;
                            whitePlayer.UpdatedOn = DateTime.UtcNow;
                            rep.Weights.Update(whitePlayer);

                            blackPlayer.Wins += blackScore == 2 ? 1 : 0;
                            blackPlayer.Draws += blackScore == 1 ? 1 : 0;
                            blackPlayer.Losses += blackScore == 0 ? 1 : 0;
                            blackPlayer.UpdatedOn = DateTime.UtcNow;
                            rep.Weights.Update(blackPlayer);

                            sbGamePgn.Clear();
                            whitePlayer = null;
                            blackPlayer = null;
                            whiteScore = 0;
                            blackScore = 0;
                        }
                        else if (line.StartsWith("[Round"))
                        {
                            currentRound = int.Parse(ParseString(line));
                        }
                        else if (line.StartsWith("[White"))
                        {
                            ObjectId id = new(ParseString(line));
                            whitePlayer = rep.Weights.FindById(id);
                        }
                        else if (line.StartsWith("[Black"))
                        {
                            ObjectId id = new(ParseString(line));
                            blackPlayer = rep.Weights.FindById(id);
                        }
                        else if (line.StartsWith("[Result"))
                        {
                            string result = ParseString(line);
                            if (result == "1-0")
                            {
                                whiteScore = 2;
                                blackScore = 0;
                            }
                            else if (result == "0-1")
                            {
                                blackScore = 2;
                                whiteScore = 0;
                            }
                            else if (result == "1/2-1/2")
                            {
                                whiteScore = 1;
                                blackScore = 1;
                            }
                        }

                        sbGamePgn.AppendLine(line);
                    }
                }

                if (sbGamePgn.Length > 0 && currentRound > 0 && whitePlayer != null && blackPlayer != null)
                {
                    MatchKey key = new(currentRound, whitePlayer.Id, blackPlayer.Id);
                    Match match;
                    if (matchLookup.ContainsKey(key))
                    {
                        match = matchLookup[key];
                    }
                    else
                    {
                        match = new(currentGeneration, currentRound, whitePlayer, blackPlayer);
                        matchLookup.Add(key, match);
                        rep.Matches.Insert(match);
                    }

                    Game game = new(match, whitePlayer, blackPlayer, whiteScore, blackScore,
                        sbGamePgn.ToString().Trim());
                    rep.Games.Insert(game);

                    whitePlayer.Wins += whiteScore == 2 ? 1 : 0;
                    whitePlayer.Draws += whiteScore == 1 ? 1 : 0;
                    whitePlayer.Losses += whiteScore == 0 ? 1 : 0;
                    whitePlayer.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(whitePlayer);

                    blackPlayer.Wins += blackScore == 2 ? 1 : 0;
                    blackPlayer.Draws += blackScore == 1 ? 1 : 0;
                    blackPlayer.Losses += blackScore == 0 ? 1 : 0;
                    blackPlayer.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(blackPlayer);
                }

                Match? finals = null;
                foreach (Match match in matchLookup.Values)
                {
                    if (match.RoundNumber == 6)
                    {
                        finals = match;
                    }

                    int player1Score = 0;
                    int player2Score = 0;

                    foreach (Game game in rep.Games.Find(g => g.Match.Id == match.Id))
                    {
                        if (match.Player1.Id == game.WhitePlayer.Id)
                        {
                            player1Score += game.WhiteScore;
                            player2Score += game.BlackScore;
                        }
                        else
                        {
                            player1Score += game.BlackScore;
                            player2Score += game.WhiteScore;
                        }
                    }

                    match.Winner = player1Score > player2Score ? match.Player1 : match.Player2;
                    match.IsComplete = true;
                    rep.Matches.Update(match);
                }

                if (finals != null && finals.Winner != null)
                {
                    currentGeneration.State = Generation.GenerationState.SuddenDeath;
                    currentGeneration.Winner = finals.Winner;
                    rep.Generations.Update(currentGeneration);

                    /* TODO: replace with extension field to track tournament wins (use double so that half points can
                       be granted to semifinalists */
                    finals.Winner.Age--;
                    rep.Weights.Update(finals.Winner);
                }

                UpdateState(State.SuddenDeath, false);
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
            }
        }

        private static string ParseString(string line)
        {
            int first = line.IndexOf('"');
            int second = line[(first + 1)..].IndexOf('"');
            return line[(first + 1)..(first + second + 1)];
        }

        private void linkConvergence_LinkClicked(object sender, LinkLabelLinkClickedEventArgs args)
        {
            if (Evolution != null)
            {
                using var rep = new GeneticsRepository();
                var gens = rep.Generations
                    .Find(g => g.Evolution.Id == Evolution.Id && g.State == Generation.GenerationState.Complete)
                    .OrderBy(g => g.CreatedOn)
                    .ToArray();

                if (gens.Length > 1)
                {
                    List<double> convergeTrend = new(gens.Length - 1);
                    for (int n = 1; n < gens.Length; n++)
                    {
                        if (gens[n].Winner != null && gens[n - 1].Winner != null && !gens[n].Winner!.IsImmortal)
                        {
                            ChessWeights w1 = gens[n].Winner!;
                            ChessWeights w0 = gens[n - 1].Winner!;

                            convergeTrend.Add(100.0 - w1.PercentChanged(w0));
                        }
                    }

                    if (convergeTrend.Count > 1)
                    {
                        double a = 0, b = 0, c = 0, d, e = 0;
                        double r = convergeTrend.Count - 1;
                        r = (r * r + r) / 2.0;
                        d = r * r;
                        for (int n = 0; n < convergeTrend.Count; n++)
                        {
                            a += n * convergeTrend[n] * convergeTrend.Count;
                            b += r * convergeTrend[n];
                            c += n * n * convergeTrend.Count;
                            e += convergeTrend[n];
                        }

                        double m = (a - b) / (c - d);
                        double f = m * r;
                        double yi = (e - f) / convergeTrend.Count;
                        double cn = (100 - yi) / m;
                    }

                }
            }
        }
    }
}
