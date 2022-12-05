﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public static class Constants
    {
        public const int MAX_SQUARES = 64;
        public const int MAX_COLORS = 2;
        public const int MAX_PIECES = 6;
        public const int MAX_COORDS = 8;
        public const int MAX_GAME_LENGTH = 512;
        public const int MAX_PLY = 32;
        public const int AVG_MOVES_PER_PLY = 36;

        public const string REGEX_FEN = @"^\s*([rnbqkpRNBQKP1-8]+/){7}[rnbqkpRNBQKP1-8]+\s[bw]\s(-|K?Q?k?q?)\s(-|[a-h][36])\s\d+\s\d+\s*$";
        public const string REGEX_MOVE = @"^[a-h][1-8][a-h][1-8](n|b|r|q)?$";
        public const string REGEX_INDEX = @"^[a-h][1-8]$";
        public const string FEN_EMPTY = @"8/8/8/8/8/8/8/8 w - - 0 0";
        public const string FEN_START_POS = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    }
}
