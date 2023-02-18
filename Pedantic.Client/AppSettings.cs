using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pedantic.Chess;

namespace Pedantic.Client
{
    public class AppSettings
    {
        public string CuteChessPath { get; set; } = string.Empty;
        public bool Debug { get; set; } = false;
        public string EnginePath { get; set; } = string.Empty;
        public int HashSize { get; set; } = 128;
        public string LogFilePath { get; set; } = string.Empty;
        public int MaxGenerations { get; set; } = 100;
        public int MaxMoves { get; set; } = 250;
        public bool Ponder { get; set; } = false;
        public string SearchType { get; set; } = "Pv";
        public int SimultaneousGames { get; set; } = 1;
        public int TimeControlsIncrement { get; set; } = 100;
        public int TimeControlsTime { get; set; } = 40000;
    }
}
