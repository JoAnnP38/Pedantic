using Pedantic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public sealed class EvalCache
    {
        public const int MB_SIZE = 1024 * 1024;
        public const int DEFAULT_CACHE_SIZE = 16;

        [StructLayout(LayoutKind.Sequential, Pack=4)]
        public struct PawnCacheItem
        {
            public PawnCacheItem(ulong hash, ulong passedPawns, Score eval)
            {
                this.hash = hash;
                this.passedPawns = passedPawns;
                this.eval = eval;
            }

            public readonly ulong Hash => hash;
            public readonly ulong PassedPawns => passedPawns;
            public readonly Score Eval => eval;

            public static void SetValue(ref PawnCacheItem item, ulong hash, ulong passedPawns, Score eval)
            {
                item.hash = hash;
                item.passedPawns = passedPawns;
                item.eval = eval;
            }

            public unsafe static int Size
            {
                get
                {
                    return sizeof(PawnCacheItem);
                }
            }

            private ulong hash;
            private ulong passedPawns;
            private Score eval;
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct EvalCacheItem
        {
            public EvalCacheItem(ulong hash, short evalScore, Color stm)
            {
                this.hash = hash;
                this.evalScore = evalScore;
                this.stm = stm;
            }

            public readonly ulong Hash => hash;
            public readonly short EvalScore => evalScore;
            public readonly Color SideToMove => stm;

            public static void SetValue(ref EvalCacheItem item, ulong hash, short evalScore, Color stm)
            {
                item.hash = hash;
                item.evalScore = evalScore;
                item.stm = stm;
            }

            public unsafe static int Size
            {
                get
                {
                    return sizeof(EvalCacheItem);
                }
            }

            private ulong hash;
            private short evalScore;
            private Color stm;
        }

        public EvalCache(int sizeMb = DEFAULT_CACHE_SIZE)
        {
            CalcCacheSizes(sizeMb, out evalSize, out pawnSize);
            evalCache = new EvalCacheItem[evalSize];
            pawnCache = new PawnCacheItem[pawnSize];
        }

        public bool ProbeEvalCache(ulong hash, Color stm, out EvalCacheItem item)
        {
            int index = (int)(hash % (uint)evalSize);
            item = evalCache[index];
            return item.Hash == hash && item.SideToMove == stm;
        }

        public bool ProbePawnCache(ulong hash, out PawnCacheItem item)
        {
            int index = (int)(hash % (uint)pawnSize);
            item = pawnCache[index];
            return item.Hash == hash;
        }

        public void SaveEval(ulong hash, short score, Color stm)
        {
            int index = (int)(hash % (uint)evalSize);
            ref EvalCacheItem item = ref evalCache[index];
            EvalCacheItem.SetValue(ref item, hash, score, stm);
        }

        public void SavePawnEval(ulong hash, ulong passedPawns, Score eval)
        {
            int index = (int)(hash % (uint)pawnSize);
            ref PawnCacheItem item = ref pawnCache[index];
            PawnCacheItem.SetValue(ref item, hash, passedPawns, eval);
        }

        public void Resize(int sizeMb)
        {
            CalcCacheSizes(sizeMb, out evalSize, out pawnSize);
            evalCache = new EvalCacheItem[evalSize];
            pawnCache = new PawnCacheItem[pawnSize];
        }

        public void Clear()
        {
            Array.Clear(evalCache);
            Array.Clear(pawnCache);
        }

        public static void CalcCacheSizes(int sizeMb, out int evalSize, out int pawnSize)
        {
            sizeMb = Math.Clamp(sizeMb, 4, 512);
            evalSize = sizeMb * MB_SIZE / EvalCacheItem.Size;
            sizeMb /= 4;
            pawnSize = sizeMb * MB_SIZE / PawnCacheItem.Size;
        }

        public int EvalCacheSize => evalSize;
        public int PawnCacheSize => pawnSize;

        private int evalSize;
        private int pawnSize;
        private EvalCacheItem[] evalCache;
        private PawnCacheItem[] pawnCache;
    }
}
