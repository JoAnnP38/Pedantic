using System.Text;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class Uci
    {
        static Uci()
        {
            defaultUci = new Uci(true, false);
        }

        public Uci(bool enable = true, bool debug = false)
        {
            this.enable = enable;
            this.debug = debug;
        }

        public void Log(string message)
        {
            if (!enable)
            {
                return;
            }
            Console.Out.WriteLineAsync($"info string {message}");
        }

        public void Debug(string message)
        {
            if (!enable)
            {
                return;
            }
            if (debug)
            {
                Console.Out.WriteLineAsync($"info string {message}");
            }
        }

        public void Info(int depth, int seldepth, int score, long nodes, long timeMs, ulong[] pv,
            int hashfull, long tbHits, TtFlag flag = TtFlag.Exact)
        {
            if (!enable)
            {
                return;
            }
            StringBuilder sb = new();
            int nps = (int)(nodes * 1000 / Math.Max(1, timeMs));
            sb.Append($"info depth {depth} seldepth {seldepth} score cp {score}");
            if (flag == TtFlag.UpperBound)
            {
                sb.Append(" upperbound");
            }
            else if (flag == TtFlag.LowerBound) 
            {
                sb.Append(" lowerbound");
            }
            sb.Append($" nodes {nodes} hashfull {hashfull} nps {nps} time {timeMs}");
            if (tbHits > 0)
            {
                sb.Append($" tbhits {tbHits}");
            }
            sb.Append(" pv");
            for (int n = 0; n < pv.Length; n++)
            {
                sb.Append($" {Move.ToString(pv[n])}");
            }
            string output = sb.ToString();
            Console.Out.WriteLineAsync(output);
            Util.WriteLine(output);
        }

        public void InfoMate(int depth, int seldepth, int mateIn, long nodes, long timeMs,
            ulong[] pv, int hashfull, long tbHits)
        {
            if (!enable)
            {
                return;
            }
            StringBuilder sb = new();
            int nps = (int)(nodes * 1000 / Math.Max(1, timeMs));
            sb.Append($"info depth {depth} seldepth {seldepth} score mate {mateIn} nodes {nodes} hashfull {hashfull} nps {nps} time {timeMs}");
            if (tbHits > 0)
            {
                sb.Append($" tbhits {tbHits}");
            }
            sb.Append(" pv");
            for (int n = 0; n < pv.Length; n++)
            {
                sb.Append($" {Move.ToString(pv[n])}");
            }
            string output = sb.ToString();
            Console.Out.WriteLineAsync(output);
            Util.WriteLine(output);
        }

        public void BestMove(string bestmove, string? suggestedPonder = null)
        {
            if (!enable)
            {
                return;
            }
            StringBuilder sb = new();
            sb.Append($"bestmove {bestmove}");
            if (suggestedPonder != null)
            {
                sb.Append($" ponder {suggestedPonder}");
            }
            string output = sb.ToString();
            Console.Out.WriteLine(output);
            Util.WriteLine(output);
        }

        public void BestMove(ulong bestmove, ulong? suggestedPonder = null)
        {
            if (!enable)
            {
                return;
            }

            string? ponder = suggestedPonder.HasValue ? Move.ToString(suggestedPonder.Value) : null;
            BestMove(Move.ToString(bestmove), ponder);
        }

        public void CurrentMove(int depth, ulong move, int moveNumber, long nodes, int hashfull)
        {
            if (!enable)
            {
                return;
            }
            string output = $"info depth {depth} currmove {Move.ToString(move)} currmovenumber {moveNumber} nodes {nodes} hashfull {hashfull}";
            Console.Out.WriteLineAsync(output);
        }

        public void Usage(int cpuload)
        {
            if (!enable)
            {
                return;
            }
            Console.Out.WriteLineAsync($"info cpuload {cpuload}");
        }

        public static Uci Default => defaultUci;

        private readonly bool enable;
        private readonly bool debug;
        private readonly static Uci defaultUci;
    }
}
