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

        public GeneticsRepository(bool readOnly = false)
        {
            db = new LiteDatabase(GetConnectionString(readOnly));
            EnsureIndices();
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
            Generations.EnsureIndex(g => g.Evolution.Id);
            Matches.EnsureIndex(m => m.Generation.Id);
            Matches.EnsureIndex(m => m.RoundNumber);
            Games.EnsureIndex(g => g.Match.Id);
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
            string connection = $"Filename={folder}; Connection=shared";
            if (readOnly)
            {
                connection = $"{connection}; ReadOnly=true;";
            }

            return connection;
        }
    }
}
