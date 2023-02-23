using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pedantic.Chess;
using Pedantic.Utilities;

namespace Pedantic
{
    public class PgnPositionReader
    {
        private bool skipOpening;
        private int openingCount;

        enum PositionState
        {
            SeekHeader,
            ReadHeader,
            ReadMove,
            ReturnMoves,
            Stop
        };

        private const string white_win_token = "1-0";
        private const string draw_token = "1/2-1/2";
        private const string black_win_token = "0-1";
        private const int eval_margin = 5;

        public readonly struct Position
        {
            public readonly ulong Hash;
            public readonly int Ply;
            public readonly int GamePly;
            public readonly string Fen;
            public readonly double Score;

            public Position()
            {
                Hash = 0ul;
                Ply = 0;
                GamePly = 0;
                Fen = string.Empty;
                Score = 0.0;
            }

            public Position(ulong hash, int ply, int gamePly, string fen, double score)
            {
                Hash = hash;
                Ply = ply;
                GamePly = gamePly;
                Fen = fen;
                Score = score;
            }
        }
        public PgnPositionReader(bool skipOpening = true, int openingCount = 8)
        {
            this.skipOpening = skipOpening;
            this.openingCount = openingCount;
        }


        public IEnumerable<Position> Positions(TextReader reader)
        {
            TimeControl tc = new()
            {
                Infinite = true
            };
            Board bd = new();
            BasicSearch search = new BasicSearch(bd, tc, 0);
            PositionState state = PositionState.SeekHeader;
            List<string> moves = new();
            double score = 0.0;
            long lines = 0;

            while (state != PositionState.Stop)
            {
                string? line = reader.ReadLine();

                if (line == null) break;
                ++lines;

                string[] tokens = line.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                switch (state)
                {
                    case PositionState.SeekHeader:
                        if (!string.IsNullOrEmpty(tokens[0]) && tokens[0].StartsWith("["))
                        {
                            state = PositionState.ReadHeader;
                        }

                        break;

                    case PositionState.ReadHeader:
                        if (tokens.Length == 0 || string.IsNullOrWhiteSpace(tokens[0]))
                        {
                            state = PositionState.ReadMove;
                            moves.Clear();
                        }

                        break;

                    case PositionState.ReadMove:
                        foreach (string token in tokens)
                        {
                            switch (token)
                            {
                                case white_win_token:
                                    score = 1.0;
                                    state = PositionState.ReturnMoves;
                                    break;

                                case draw_token:
                                    score = 0.5;
                                    state = PositionState.ReturnMoves;
                                    break;

                                case black_win_token:
                                    score = 0.0;
                                    state = PositionState.ReturnMoves;
                                    break;

                                default:
                                    moves.Add(token);
                                    break;
                            }
                        }

                        break;

                    case PositionState.ReturnMoves:
                    {
                        int ply = 0;
                        bd.LoadFenPosition(Constants.FEN_START_POS);
                        int gamePly = moves.Count;
                        foreach (string mv in moves)
                        {
                            if (Move.TryParseMove(bd, mv, out ulong move))
                            {
                                ply++;
                                if (!bd.MakeMove(move))
                                {
                                    throw new Exception($"Illegal move encountered: {Move.ToString(move)}");
                                }

                                if (skipOpening && ply < openingCount)
                                {
                                    continue;
                                }

                                int eval = search.Eval.Compute(bd);
                                int delta = Math.Abs(search.Quiesce(-Constants.INFINITE_WINDOW,
                                    Constants.INFINITE_WINDOW, 0) - eval);
                                if (delta == 0)
                                {
                                    yield return new Position(bd.Hash, ply, gamePly, bd.ToFenString(), score);
                                }
                            }
                            else
                            {
                                Util.WriteLine($"Illegal token encountered: {mv}... skipping to next game.");
                                break;
                            }
                        }

                        state = PositionState.SeekHeader;
                        break;
                    }
                }
            }
            
        }

    }
}
