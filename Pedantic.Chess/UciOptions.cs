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
        public const bool DEFAULT_ANALYSE_MODE = false;
        public const int DEFAULT_THREADS = 1;
        public const int DEFAULT_CONTEMPT = 0;

        static UciOptions()
        {
            CollectStatistics = DEFAULT_COLLECT_STATISTICS;
            Hash = DEFAULT_HASH;
            OwnBook = DEFAULT_OWN_BOOK;
            Ponder = DEFAULT_PONDER;
            RandomSearch = DEFAULT_RANDOM_SEARCH;
            SyzygyPath = DEFAULT_SYZYGY_PATH;
            SyzygyProbeRoot = DEFAULT_SYZYGY_PROBE_ROOT;
            SyzygyProbeDepth = DEFAULT_SYZYGY_PROBE_DEPTH;
            AnalyseMode = DEFAULT_ANALYSE_MODE;
            Threads = DEFAULT_THREADS;
            Contempt = DEFAULT_CONTEMPT;
        }

        public static bool CollectStatistics { get; set; }
        public static int Hash 
        { 
            get => hash; 
            set
            {
                hash = Math.Clamp(value, 16, 2048);
            }
        }
        public static bool OwnBook { get; set; }
        public static bool Ponder { get; set; }
        public static bool RandomSearch { get; set; }
        public static string SyzygyPath { get; set; }
        public static bool SyzygyProbeRoot { get; set; }
        public static int SyzygyProbeDepth 
        { 
            get => syzygyProbeDepth;
            set
            {
                syzygyProbeDepth = Math.Clamp(value, 0, Constants.MAX_PLY - 1);
            }
        }
        public static bool AnalyseMode { get; set; }
        public static int Threads 
        { 
            get => threads;
            set
            {
                threads = Math.Clamp(value, 1, Environment.ProcessorCount);
            }
        }

        public static int Contempt { get; set; }

        private static int hash;
        private static int syzygyProbeDepth;
        private static int threads;
    }
}
