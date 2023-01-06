using System;
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
        public static void Log(string message)
        {
            Console.WriteLine($@"info string {message}");
            Util.WriteLine($@"info string {message}");
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
            Console.WriteLine(output);
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
            Console.WriteLine(output);
            Util.WriteLine(output);
        }

        public static void BestMove(ulong bestmove)
        {
            Console.WriteLine(@$"bestmove {Move.ToString(bestmove)}");
            Util.WriteLine(@$"bestmove {Move.ToString(bestmove)}");
        }

        public static void BestMove(string bestmove)
        {
            Console.WriteLine(@$"bestmove {bestmove}");
            Util.WriteLine(@$"bestmove {bestmove}");
        }
    }
}
