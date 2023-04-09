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
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Pedantic.Genetics
{
    public class ChessDb
    {
        public class PedanticDb
        {
            public PedanticDb()
            {
                Weights = new();
                Stats = new();
            }

            public SortedList<Guid, ChessWeights> Weights { get; set; }
            public SortedList<Guid, ChessStats> Stats { get; set; }
        }

        public const string APP_NAME = "Pedantic";

        public ChessDb()
        {
            string jsonFile = GetConnectionString();
            if (File.Exists(jsonFile))
            {
                string json = File.ReadAllText(jsonFile);
                db = JsonSerializer.Deserialize<PedanticDb>(json) ??
                     new PedanticDb()
                     {
                         Weights = new SortedList<Guid, ChessWeights>(),
                         Stats = new SortedList<Guid, ChessStats>()
                     };
            }
            else
            {
                db = new PedanticDb()
                {
                    Weights = new SortedList<Guid, ChessWeights>(),
                    Stats = new SortedList<Guid, ChessStats>()
                };
            }

            weights = new WeightsRepository(db.Weights);
            stats = new StatsRepository(db.Stats);
        }

        public IRepository<ChessWeights> Weights => weights;
        public IRepository<ChessStats> Stats => stats;

        public void Save()
        {
            string jsonFile = GetConnectionString();
            string json = JsonSerializer.Serialize(db);
            File.WriteAllText(jsonFile, json);
        }

        public static string GetConnectionString()
        {
            string? dirFullName = Path.GetDirectoryName(Environment.ProcessPath);
            string jsonPath = dirFullName != null ? 
                Path.Combine(dirFullName, "Pedantic.json") : string.Empty;
            return jsonPath;
        }

        private readonly PedanticDb db;
        private readonly WeightsRepository weights;
        private readonly StatsRepository stats;
    }
}
