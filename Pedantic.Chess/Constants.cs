// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Constants.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Global constants.
// </summary>
// ***********************************************************************
namespace Pedantic.Chess
{
    public static class Constants
    {
        public const int MAX_SQUARES = 64;
        public const int MAX_COLORS = 2;
        public const int MAX_PIECES = 6;
        public const int MAX_COORDS = 8;
        public const int MAX_PHASES = 3;
        public const int MAX_GAME_LENGTH = 1024;
        public const int MAX_PLY = 64;
        public const int MAX_KING_PLACEMENTS = 4;
        public const int AVG_MOVES_PER_PLY = 36;
        public const short CHECKMATE_SCORE = 20000;
        public const short TABLEBASE_WIN = 19500;
        public const short TABLEBASE_LOSS = -19500;
        public const short TOTAL_STARTING_MATERIAL = 7800;
        public const int MINOR_PIECE_COUNT = 4;
        public const int MAJOR_MINOR_PIECE_COUNT = 6;
        public const int PV_SCORE = int.MaxValue;
        public const int CAPTURE_SCORE = 166000;
        public const int PROMOTE_SCORE = 132500;
        public const int KILLER_SCORE = 132000;
        public const int BAD_CAPTURE = 66000;
        public const int HISTORY_SCORE_MIN = -16384;
        public const int HISTORY_SCORE_MAX = 16384;
        public const short INFINITE_WINDOW = short.MaxValue;
        public const int WINDOW_MIN_DEPTH = 6;
        public const int LAZY_EVAL_MARGIN = 500;
        public const int INVALID_PROBE = int.MinValue;
        public const short MAX_PHASE = 64;

        public const string REGEX_FEN = @"^\s*([rnbqkpRNBQKP1-8]+/){7}[rnbqkpRNBQKP1-8]+\s[bw]\s(-|K?Q?k?q?)\s(-|[a-h][36])\s\d+\s\d+\s*$";
        public const string REGEX_MOVE = @"^[a-h][1-8][a-h][1-8](n|b|r|q)?$";
        public const string REGEX_INDEX = @"^[a-h][1-8]$";
        public const string FEN_EMPTY = @"8/8/8/8/8/8/8/8 w - - 0 0";
        public const string FEN_START_POS = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public const string APP_NAME = "Pedantic";
        public const string APP_VERSION = "0.4.1";
    }
}
