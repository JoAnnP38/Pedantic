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
using System.Collections.Concurrent;

namespace Pedantic
{
    public sealed class PgnPositionReader
    {
        public const int MOVE_OFFSET = 10;
        private readonly bool skipOpening;
        private readonly int openingCount;

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

            public Position(Position other, short eval)
            {
                Hash = other.Hash;
                Ply = other.Ply;
                GamePly = other.GamePly;
                Fen = other.Fen;
                HasCastled = other.HasCastled;
                Eval = eval;
                Result = other.Result;
            }
        }
        public PgnPositionReader(bool skipOpening = true, int openingCount = MOVE_OFFSET)
        {
            this.skipOpening = skipOpening;
            this.openingCount = openingCount;
            labelers = new Labeler[MAX_PARALLELISM];
            for (int n = 0; n < MAX_PARALLELISM; n++)
            {
                labelers[n] = new Labeler();
            }
        }

        public IEnumerable<Position> Positions(TextReader reader)
        {
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
                        IEnumerable<Position>? labeledPositions = null;
                        try
                        {
                            IList<Position> positions = CollectPositions(moves, result);
                            labeledPositions = EvaluatePositions(positions);
                        }
                        catch (Exception ex)
                        {
                            Util.TraceError($"Unexpected exception: '{ex.Message}'.");
                        }

                        if (labeledPositions != null)
                        {
                            foreach (var p in labeledPositions)
                            {
                                yield return p;
                            }
                        }
                        state = PositionState.SeekHeader;
                        break;
                    }
                }
            }
        }

        private IList<Position> CollectPositions(IList<string> moves, float result)
        {
            int ply = 0;
            int gamePly = moves.Count;
            List<Position> output = new(moves.Count);
            Board bd = new(Constants.FEN_START_POS);

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

                    if ((skipOpening && ply <= openingCount) || (ply + MOVE_OFFSET >= gamePly))
                    {
                        continue;
                    }

                    output.Add(new Position(bd.Hash, ply, gamePly, bd.ToFenString(), bd.HasCastled, 0, result));
                }
                else
                {
                    throw new FormatException($"Illegal move or format: '{mv}'.");
                }
            }

            return output;
        }

        private IEnumerable<Position> EvaluatePositions(IList<Position> positions)
        {
            BlockingCollection<Labeler> labelerPool = new(new ConcurrentQueue<Labeler>(labelers));
            ConcurrentQueue<Position> queue = new();
            Parallel.ForEach(positions, new ParallelOptions { MaxDegreeOfParallelism = MAX_PARALLELISM }, p =>
            {
                Labeler labeler = labelerPool.Take();
                if (labeler.Label(p, out Position labeled))
                {
                    queue.Enqueue(labeled);
                }
                labelerPool.Add(labeler);
            });
            return queue;
        }

        private Labeler[] labelers;
        public static readonly int MAX_PARALLELISM = Math.Max(Environment.ProcessorCount - 2, 1);
    }
}
