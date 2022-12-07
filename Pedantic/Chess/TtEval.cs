using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class TtEval
    {
        public enum ScoreType : byte
        {
            Exact,
            GreaterOrEqual,
            LessOrEqual
        }

        public const int DEFAULT_SIZE_MB = 50;
        public const int MAX_SIZE_MB = 2047;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;
        public const int HISTORY_DEPTH = 255;

        public readonly struct TtEvalItem
        {
            private readonly ulong hash;
            private readonly ulong data;

            public TtEvalItem(ulong hash, short score, byte depth, ScoreType scoreType, ulong bestMove)
            {
                data = bestMove;
                data = BitOps.BitFieldSet(data, score, 24, 16);
                data = BitOps.BitFieldSet(data, (int)scoreType, 40, 2);
                data = BitOps.BitFieldSet(data, depth, 42, 8);

                this.hash = hash ^ data;
            }

            public TtEvalItem(ulong hash, ulong data)
            {
                this.hash = hash;
                this.data = data;
            }

            public ulong Hash => hash ^ data;
            public ulong Data => data;
            public ulong Move => (ulong)BitOps.BitFieldExtract(data, 0, 24);
            public short Score => (short)BitOps.BitFieldExtract(data, 24, 16);
            public ScoreType ScoreType => (ScoreType)BitOps.BitFieldExtract(data, 40, 2);
            public byte Depth => (byte)BitOps.BitFieldExtract(data, 42, 8);

            public bool IsValid(ulong hash)
            {
                return Hash == hash;
            }
        }


        private static TtEvalItem[] table;
        private static int capacity;

        static TtEval()
        {
            capacity = (DEFAULT_SIZE_MB * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        public static bool TryLookup(ulong hash, out TtEvalItem item)
        {
            int index = GetIndex(hash);
            item = table[index];
            return item.IsValid(hash);
        }

        public static void Add(ulong hash, int depth, short alpha, short beta, short score, ulong move)
        {
            int index = GetIndex(hash);
            TtEvalItem item = table[(int)(hash % (ulong)capacity)];

            ulong bestMove = !item.IsValid(hash) || move != 0ul ? move : item.Move;
            byte itemDepth = depth < 0 ? (byte)0 : (byte)depth;
            ScoreType scoreType = ScoreType.Exact;
            short itemScore = score;

            if (score >= beta)
            {
                scoreType = ScoreType.GreaterOrEqual;
                itemScore = beta;
            }
            else if (score <= alpha)
            {
                scoreType = ScoreType.LessOrEqual;
                itemScore = alpha;
            }

            table[index] = new TtEvalItem(hash, itemScore, itemDepth, scoreType, bestMove);
        }

        public static bool GetBestMove(ulong hash, out ulong bestMove)
        {
            TtEvalItem item = table[GetIndex(hash)];
            if (item.IsValid(hash))
            {
                bestMove = item.Move;
                return true;
            }
            bestMove = 0ul;
            return false;
        }

        public static bool GetScore(ulong hash, int depth, short alpha, short beta, out short score)
        {
            score = 0;

            TtEvalItem item = table[GetIndex(hash)];
            if (item.IsValid(hash))
            {
                score = item.Score;
                ScoreType type = item.ScoreType;
                return (type == ScoreType.Exact) ||
                    (type == ScoreType.LessOrEqual && score <= alpha) ||
                    (type == ScoreType.GreaterOrEqual && score >= beta);
            }
            return false;
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (sizeMb * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        private static int GetIndex(ulong hash)
        {
            return (int)(hash % (ulong)capacity);
        }

        public static int Capacity => capacity;
    }
}
