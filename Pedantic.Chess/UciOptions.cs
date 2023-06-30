﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public static class UciOptions
    {
        public const bool DEFAULT_COLLECT_STATISTICS = false;
        public const int DEFAULT_HASH = 64;
        public const bool DEFAULT_OWN_BOOK = true;
        public const bool DEFAULT_PONDER = false;
        public const bool DEFAULT_RANDOM_SEARCH = false;
        public const string DEFAULT_SYZYGY_PATH = "";
        public const bool DEFAULT_SYZYGY_PROBE_ROOT = true;
        public const int DEFAULT_SYZYGY_PROBE_DEPTH = 2;

        static UciOptions()
        {
            CollectStatistics = DEFAULT_COLLECT_STATISTICS;
            EvaluationID = null;
            Hash = DEFAULT_HASH;
            OwnBook = DEFAULT_OWN_BOOK;
            Ponder = DEFAULT_PONDER;
            RandomSearch = DEFAULT_RANDOM_SEARCH;
            SyzygyPath = DEFAULT_SYZYGY_PATH;
            SyzygyProbeRoot = DEFAULT_SYZYGY_PROBE_ROOT;
            SyzygyProbeDepth = DEFAULT_SYZYGY_PROBE_DEPTH;
        }

        public static bool CollectStatistics { get; set; }
        public static Guid? EvaluationID { get; set; }
        public static int Hash { get; set;}
        public static bool OwnBook { get; set; }
        public static bool Ponder { get; set; }
        public static bool RandomSearch { get; set; }
        public static string SyzygyPath { get; set; }
        public static bool SyzygyProbeRoot { get; set; }
        public static int SyzygyProbeDepth { get; set; }
    }
}