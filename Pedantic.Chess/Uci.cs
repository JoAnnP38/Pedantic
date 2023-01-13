using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Utilities;
using static System.Formats.Asn1.AsnWriter;

namespace Pedantic.Chess
{
    public static class Uci
    {
        private static bool disableOutput = false;
        private static object lockObject = new();

        public static bool DisableOutput
        {
            get
            {
                lock (lockObject)
                {
                    return disableOutput;
                }
            }
            set
            {
                lock (lockObject)
                {
                    disableOutput = value;
                }
            }
        }

        public static void Log(string message)
        {
            Console.Out.WriteLineAsync($@"info string {message}");
        }

        public static void Info(int depth, int score, long nodes, long timeMs, ulong[] pv)
        {
            StringBuilder sb = new();
            int nps = (int)(nodes * 1000 / Math.Max(1, timeMs));
            sb.Append(@$"info depth {depth} score cp {score} nodes {nodes} nps {nps} time {timeMs} pv");
            for (int n = 0; n < pv.Length; n++)
            {
                sb.Append($@" {Move.ToString(pv[n])}");
            }

            string output = sb.ToString();

            if (!DisableOutput)
            {
                Console.Out.WriteLineAsync(output);
            }
            Util.WriteLine(output);
        }

        public static void InfoMate(int depth, int mateIn, long nodes, long timeMs, ulong[] pv)
        {
            StringBuilder sb = new();
            int nps = (int)(nodes * 1000 / Math.Max(1, timeMs));
            sb.Append(@$"info depth {depth} score mate {mateIn} nodes {nodes} nps {nps} time {timeMs} pv");
            for (int n = 0; n < pv.Length; n++)
            {
                sb.Append($@" {Move.ToString(pv[n])}");
            }

            string output = sb.ToString();
            if (!DisableOutput)
            {
                Console.Out.WriteLineAsync(output);
            }
            Util.WriteLine(output);
        }

        public static void BestMove(ulong bestmove, ulong? suggestedPonder = null)
        {
            Console.Write(@$"bestmove {Move.ToString(bestmove)}");
            Util.Write(@$"bestmove {Move.ToString(bestmove)}");
            if (suggestedPonder.HasValue)
            {
                Console.WriteLine(@$" ponder {Move.ToString(suggestedPonder.Value)}");
                Util.WriteLine(@$" ponder {Move.ToString(suggestedPonder.Value)}");
            }
            else
            {
                Console.WriteLine();
                Util.WriteLine();
            }
        }

        public static void BestMove(string bestmove, string? suggestedPonder = null)
        {
            Console.Write(@$"bestmove {bestmove}");
            Util.Write(@$"bestmove {bestmove}");

            if (suggestedPonder != null)
            {
                Console.Out.WriteLineAsync(@$" ponder {suggestedPonder}");
                Util.WriteLine(@$" ponder {suggestedPonder}");
            }
            else
            {
                Console.Out.WriteLineAsync();
                Util.WriteLine();
            }
        }

        public static void CurrentMove(int depth, ulong move, int moveNumber)
        {
            Console.Out.WriteLineAsync($@"info depth {depth} currmove {Move.ToString(move)} currmovenumber {moveNumber}");
        }
    }
}
