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
        public const int AVG_MOVES_PER_PLY = 36;
        public const short CHECKMATE_SCORE = 20000;
        public const short CHECKMATE_BASE = 19500;
        public const short TOTAL_STARTING_MATERIAL = 7800;
        public const int MINOR_PIECE_COUNT = 4;
        public const int MAJOR_MINOR_PIECE_COUNT = 6;
        public const short PV_SCORE = 20000;
        public const short BAD_CAPTURE = 11400;
        public const short CAPTURE_SCORE = 10000;
        public const short PROMOTE_SCORE = 9000;
        public const short KILLER_SCORE = 7000;
        public const short HISTORY_SCORE = 5000;
        public const short INFINITE_WINDOW = short.MaxValue;
        public const ulong LINEUP_K = 0ul;
        public const ulong LINEUP_KQ = 0x0005ul;
        public const ulong LINEUP_KR = 0x0004ul;
        public const ulong LINEUP_KB = 0x0003ul;
        public const ulong LINEUP_KN = 0x0002ul;
        public const ulong LINEUP_KP = 0x0001ul;
        public const ulong LINEUP_KNN = 0x0012ul;
        public const ulong LINEUP_KBN = 0x001Aul;
        public const ulong LINEUP_KBB = 0x001Bul;
        public const int LAZY_EVAL_MARGIN = 500;
        public const int STATIC_NULL_MOVE_MARGIN = 150;
        public const int INVALID_PROBE = int.MinValue;

        public const string REGEX_FEN = @"^\s*([rnbqkpRNBQKP1-8]+/){7}[rnbqkpRNBQKP1-8]+\s[bw]\s(-|K?Q?k?q?)\s(-|[a-h][36])\s\d+\s\d+\s*$";
        public const string REGEX_MOVE = @"^[a-h][1-8][a-h][1-8](n|b|r|q)?$";
        public const string REGEX_INDEX = @"^[a-h][1-8]$";
        public const string FEN_EMPTY = @"8/8/8/8/8/8/8/8 w - - 0 0";
        public const string FEN_START_POS = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public const string APP_NAME = "Pedantic";
    }
}
