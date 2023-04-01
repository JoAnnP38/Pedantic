// ***********************************************************************
// Assembly         : Pedantic.Genetics
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="GeneticsRepository.cs" company="Pedantic.Genetics">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement repository patter over LiteDB database.
// </summary>
// ***********************************************************************
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
            Weights.EnsureIndex(w => w.UpdatedOn);
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
