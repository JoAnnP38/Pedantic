using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Genetics;

namespace Pedantic.Chess
{
    public static class PgnFormatter
    {
        public enum GameResult
        {
            Unknown,
            WhiteWin,
            BlackWin,
            Draw
        }
        public enum Termination
        {
            Abandoned,
            Adjudication,
            Death,
            Emergency,
            Normal,
            RulesInfraction,
            TimeForfeit,
            Unterminated
        }

        public static string ToPgn(this Board board, string eventName, string location, int round, string white, 
            string black, DateTime startDateTime, GameResult result, Termination termination)
        {
            StringBuilder sb = new();

            sb.AppendLine($"[Event \"Pedantic Evolution Match {eventName}\"]");
            sb.AppendLine($"[Site \"{location}\"]");
            sb.AppendLine($"[Date \"{startDateTime:yyyy.mm.dd}\"]");
            sb.AppendLine($"[Round \"{round}\"]");
            sb.AppendLine($"[White \"{white}\"]");
            sb.AppendLine($"[Black \"{black}\"]");
            sb.AppendLine($"[Result \"{result.ToPgnResult()}\"]");
            sb.AppendLine($"[Termination \"{termination.ToPgnTermination()}\"]");
            sb.AppendLine($"[Time \"{startDateTime:hh:mm:ss}\"]");
            sb.AppendLine();


            ulong[] moves = board.GameMoves();
            Board bd = new(Constants.FEN_START_POS);

            MoveList moveList = new();
            int lineLength = 0;
            string s;

            for (int n = 0; n < moves.Length; n++)
            {
                if (bd.SideToMove == Color.White)
                {
                    s = $"{board.FullMoveCounter}. ";
                    lineLength += s.Length;
                    if (lineLength > 80)
                    {
                        sb.AppendLine();
                        lineLength = 0;
                    }
                    sb.Append(s);

                }

                if (++lineLength > 80)
                {
                    sb.AppendLine();
                    lineLength = 0;
                }
                else
                {
                    sb.Append(' ');
                }

                s = Move.ToSanString(moves[n], bd);
                lineLength += s.Length;
                if (lineLength > 80)
                {
                    sb.AppendLine();
                    lineLength = 0;
                }
                sb.Append(s);

                bd.MakeMove(moves[n]);
            }

            s = $" {result.ToPgnResult()}";
            lineLength += s.Length;
            if (lineLength > 80)
            {
                sb.AppendLine();
            }

            sb.AppendLine(s);
            return sb.ToString();
        }

        public static string ToPgn(this Board board, GameToPlay game, DateTime startDateTime,
            GameResult result, Termination termination)
        {
            return board.ToPgn(game.MatchId, "Clearwater, FL USA", game.Round, game.WhiteId, game.BlackId, 
                startDateTime, result, termination);       
        }

        private static string ToPgnResult(this GameResult result)
        {
            return result switch
            {
                GameResult.WhiteWin => "1-0",
                GameResult.BlackWin => "0-1",
                GameResult.Draw => "1/2-1/2",
                GameResult.Unknown => "*",
                _ => throw new InvalidOperationException("Invalid Game Result")
            };
        }

        private static string ToPgnTermination(this Termination termination)
        {
            return termination switch
            {
                Termination.Abandoned => "abandoned",
                Termination.Adjudication => "adjudication",
                Termination.Death => "death",
                Termination.Emergency => "emergency",
                Termination.Normal => "normal",
                Termination.RulesInfraction => "rules infraction",
                Termination.TimeForfeit => "time forfeit",
                Termination.Unterminated => "unterminated",
                _ => throw new InvalidOperationException("Invalid Termination State")
            };
        }
    }
}
