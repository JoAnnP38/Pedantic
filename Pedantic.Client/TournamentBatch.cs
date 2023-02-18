using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Pedantic.Genetics;

namespace Pedantic.Client
{
    public class TournamentBatch
    {
        private const string @batch_header = "@echo off\nset path=%path%;d:\\program files (x86)\\cute chess\\\ncutechess-cli ^";

        public TournamentBatch(string generationId)
        {
            this.generationId = generationId;
        }

        public void Create()
        {
            try
            {
                using GeneticsRepository repository = new GeneticsRepository();
                string enginePath = Path.GetDirectoryName(Program.AppSettings.EnginePath) ?? Environment.CurrentDirectory;
                using var output =
                    File.CreateText(Path.Combine(enginePath, $"generation_{generationId}.bat"));

                output.WriteLine(batch_header);
                foreach (var wt in repository.Weights.Find(w => w.IsActive)
                             .OrderByDescending(w => w.Wins * 2 + w.Draws)) 
                {
                    configurations.Add(new()
                    {
                        command=Program.AppSettings.EnginePath.Replace('\\', '/'),
                        initStrings= new [] { "setoption name Ponder value false", "setoption name Hash value 128" },
                        name=wt.Id.ToString(),
                        ponder=false,
                        protocol="uci",
                        stderrFile=Path.Combine(enginePath, $"engine_{wt.Id}_errors.txt").Replace('\\', '/'),
                        whitepov=false,
                        workingDirectory=enginePath.Replace('\\', '/')
                    });
                    output.WriteLine($"-engine conf={wt.Id} option.Evaluation_ID={wt.Id} arg=uci ^");
                }
                output.WriteLine("-each proto=uci tc=40/40+0.25 restart=on option.Search_Algorithm=PV timemargin=20 -variant standard ^");
                output.WriteLine("-concurrency 10 -draw movenumber=150 movecount=3 score=0 ^");
                output.WriteLine($"-maxmoves 250 -tournament knockout -event {generationId} -games 2 ^");
                output.WriteLine($"-pgnout generation_{generationId}.pgn -site \"Clearwater, FL USA\" ^");
                output.WriteLine($"-resultformat small > generation_{generationId}_output.txt 2> generation_{generationId}_errors.txt");
                output.Flush();
                output.Close();

                using var json = File.CreateText(Path.Combine(enginePath, "engines.json"));
                string jsonString = JsonSerializer.Serialize(configurations);
                json.WriteLine(jsonString);
                json.Flush();
                json.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private List<EngineConfiguration> configurations = new();
        private readonly string generationId;
    }
}
