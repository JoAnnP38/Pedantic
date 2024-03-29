﻿// ***********************************************************************
// Assembly         : Pedantic.Genetics
// Author           : JoAnn D. Peeler
// Created          : 04-03-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="ChessStats.cs" company="Pedantic.Genetics">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Pedantic.Genetics
{
    public sealed class ChessStats
    {
        public const string CURRENT_VERSION = "0.3";

        public ChessStats(Guid id, string phase, int depth, long nodesVisited, string version)
        {
            Id = id;
            Phase = phase;
            Depth = depth;
            NodesVisited = nodesVisited;
            Version = version;
        }

        public ChessStats()
        {
            Id = Guid.NewGuid();
            Phase = string.Empty;
            Depth = 0;
            NodesVisited = 0;
            Version = CURRENT_VERSION;
        }

        public Guid Id { get; set; }
        public string Phase { get; set; }
        public int Depth { get; set; }
        public long NodesVisited { get; set; }
        public string Version { get; set; }
    }
}