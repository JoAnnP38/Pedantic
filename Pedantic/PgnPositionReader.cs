// ***********************************************************************
// Assembly         : Pedantic
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="PgnPositionReader.cs" company="Pedantic">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Read and enumerate positions represented in a PGN file. 
// </summary>
// ***********************************************************************
using Pedantic.Chess;
using Pedantic.Utilities;

namespace Pedantic
{
    public sealed class PgnPositionReader
    {
        private readonly bool skipOpening;
        private readonly int openingCount;
        public const int SEARCH_DEPTH = 6;

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

        public readonly struct Position
        {
            public readonly ulong Hash;
            public readonly int Ply;
            public readonly int GamePly;
            public readonly string Fen;
            public readonly byte HasCastled;
            public readonly short Eval;
            public readonly float Result;

            public Position()
            {
                Hash = 0;
                Ply = 0;
                GamePly = 0;
                Fen = string.Empty;
                HasCastled = 0;
                Eval = 0;
                Result = 0;
            }

            public Position(ulong hash, int ply, int gamePly, string fen, bool[] hasCastled, short eval, float result)
            {
                Hash = hash;
                Ply = ply;
                GamePly = gamePly;
                Fen = fen;
                if (hasCastled[0])
                {
                    HasCastled |= 0x01;
                }

                if (hasCastled[1])
                {
                    HasCastled |= 0x02;
                }
                Eval = eval;
                Result = result;
            }
        }
        public PgnPositionReader(bool skipOpening = true, int openingCount = 8)
        {
            this.skipOpening = skipOpening;
            this.openingCount = openingCount;
        }


        public IEnumerable<Position> Positions(TextReader reader)
        {
            GameClock tc = new()
            {
                Infinite = true
            };
            Board bd = new();
            SearchStack searchStack = new();
            EvalCache cache = new();
            History history = new();
            ObjectPool<MoveList> listPool = new(Constants.MAX_PLY);
            BasicSearch search = new(searchStack, bd, tc, cache, history, listPool, TtTran.Default, Constants.MAX_PLY - 1)
            {
                Uci = new Uci(false, false)
            };
            PositionState state = PositionState.SeekHeader;
            List<string> moves = new();
            float result = 0;
            long lineNumber = 0;

            for(;;)
            {
                string? line = reader.ReadLine();

                if (line == null) break;
                lineNumber++;

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
                                    result = 1.0f;
                                    state = PositionState.ReturnMoves;
                                    break;

                                case draw_token:
                                    result = 0.5f;
                                    state = PositionState.ReturnMoves;
                                    break;

                                case black_win_token:
                                    result = 0.0f;
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

                                if (bd.IsChecked())
                                {
                                    continue;
                                }

                                if (skipOpening && ply < openingCount)
                                {
                                    continue;
                                }
                                history.Clear();
                                searchStack.Initialize(bd);
                                int score = search.SearchRoot(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, SEARCH_DEPTH);
                                ulong[] pv = search.GetPv();
                                if (Math.Abs(score) < Constants.TABLEBASE_WIN && pv.Length > 0)
                                {
                                    ulong bestmove = pv[0];
                                    if (!Move.IsCapture(bestmove))
                                    {
                                        yield return new Position(bd.Hash, ply, gamePly, bd.ToFenString(), bd.HasCastled, 
                                            (short)score, result);
                                    }
                                }
                            }
                            else
                            {
                                Util.WriteLine($"Illegal token encountered: {mv}... skipping to next game.");
                                Console.Error.WriteLine($@"{lineNumber}: Illegal token encountered - '{line}'");
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
