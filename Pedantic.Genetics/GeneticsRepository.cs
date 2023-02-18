using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Pedantic.Genetics
{
    public class GeneticsRepository : IDisposable
    {
        public const string APP_NAME = "Pedantic";

        private readonly LiteDatabase db;

        static GeneticsRepository()
        {
            BsonMapper.Global.RegisterType<TimeSpan>
            (
                serialize: (ts) => new BsonValue(ts.Ticks),
                deserialize: (bson) => new TimeSpan(bson.AsInt64)
            );
        }

        public GeneticsRepository(bool readOnly = false)
        {
            db = new LiteDatabase(GetConnectionString(readOnly));
            if (!readOnly)
            {
                EnsureIndices();
            }
        }

        public ILiteCollection<ChessWeights> Weights => db.GetCollection<ChessWeights>("weights");
        public ILiteCollection<Evolution> Evolutions => db.GetCollection<Evolution>("evolutions");
        public ILiteCollection<Generation> Generations => db.GetCollection<Generation>("generations");
        public ILiteCollection<Match> Matches => db.GetCollection<Match>("matches");
        public ILiteCollection<Game> Games => db.GetCollection<Game>("games");

        public bool BeginTransaction()
        {
            return db.BeginTrans();
        }

        public bool CommitTransaction()
        {
            return db.Commit();
        }

        public bool RollbackTransaction()
        {
            return db.Rollback();
        }

        public void DeleteAll()
        {
            bool commit = false;
            try
            {
                commit = BeginTransaction();
                Evolutions.DeleteAll();
                Generations.DeleteAll();
                Matches.DeleteAll();
                Games.DeleteAll();
                Weights.DeleteAll();
                if (commit)
                {
                    CommitTransaction();
                }
            }
            catch
            {
                if (commit)
                {
                    RollbackTransaction();
                }

                throw;
            }
        }

        public void EnsureIndices()
        {
            Evolutions.EnsureIndex(e => e.ConvergedOn);
            Evolutions.EnsureIndex(e => e.UpdatedOn);
            Evolutions.EnsureIndex(e => e.State);
            Generations.EnsureIndex(g => g.Evolution.Id);
            Generations.EnsureIndex(g => g.State);
            Generations.EnsureIndex(g => g.CreatedOn);
            Matches.EnsureIndex(m => m.Generation.Id);
            Matches.EnsureIndex(m => m.RoundNumber);
            Matches.EnsureIndex(m => m.Player1.Id);
            Matches.EnsureIndex(m => m.Player2.Id);
            Games.EnsureIndex(g => g.Match.Id);
            Games.EnsureIndex(g => g.WhitePlayer.Id);
            Games.EnsureIndex(g => g.BlackPlayer.Id);
        }
          
        public void Dispose() => db.Dispose();

        public static string GetConnectionString(bool readOnly)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            folder = Path.Combine(folder, APP_NAME);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            folder = Path.Combine(folder, $"{APP_NAME}.db");
            string connection = $"Filename={folder}; Connection=Shared";
            if (readOnly)
            {
                connection = $"{connection}; ReadOnly=true;";
            }

            return connection;
        }
    }
}
