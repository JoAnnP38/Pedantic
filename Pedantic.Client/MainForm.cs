using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Pedantic.Genetics;

namespace Pedantic.Client
{
    public partial class MainForm : Form
    {

        public class TaskArgs
        {
            public CancellationTokenSource Source { get; init; }
            public GameForm GameForm { get; init; }

            public TaskArgs(CancellationTokenSource source, GameForm gameForm)
            {
                Source = source;
                GameForm = gameForm;
            }
        }
        public MainForm()
        {
            GameQueue = new ConcurrentQueue<GameToPlay>();
            ResultsQueue = new ConcurrentQueue<GameResult>();
            Source = new();
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            label1.Text = $"Number of simultaneous games: {Program.AppSettings.SimultaneousGames}";
            label2.Text = $"Engine path: {Program.AppSettings.EnginePath}";
        }

        public ConcurrentQueue<GameToPlay> GameQueue { get; }
        public ConcurrentQueue<GameResult> ResultsQueue { get; }

        public CancellationTokenSource Source { get; set; }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Source = new();
            CancellationToken token = Source.Token;

            GameForm frmGame = new();
            frmGame.Show();

            Task.Factory.StartNew(GameForm.PlayGame, new TaskArgs(Source, frmGame), token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Source.Cancel();
        }

        private void btnEngineBat_Click(object sender, EventArgs e)
        {
            using GeneticsRepository repository = new GeneticsRepository();
            var ev = repository.Evolutions.FindOne(e => e.State == Evolution.EvolutionState.Evolving);
            if (ev != null)
            {
                var gen = repository.Generations.FindOne(g =>
                    g.Evolution.Id == ev.Id && g.State != Generation.GenerationState.Complete);

                if (gen != null)
                {
                    TournamentBatch batch = new(gen.Id.ToString());
                    batch.Create();
                }
            }
        }

    }
}
