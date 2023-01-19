using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public readonly struct SearchResult
    {
        public readonly int Score;
        public readonly ulong[] Pv;

        public SearchResult()
        {
            Score = 0;
            Pv = emptyPv;
        }

        public SearchResult(int score, ulong[] pv)
        {
            Score = score;
            Pv = pv;
        }

        public SearchResult(int score)
        {
            Score = score;
            Pv = emptyPv;
        }

        public SearchResult(int score, ulong move)
        {
            Score = score;
            Pv = move != 0 ? new[] { move } : emptyPv;
        }

        public static SearchResult operator -(SearchResult op)
        {
            return new SearchResult(-op.Score, op.Pv);
        }

        private static readonly ulong[] emptyPv = Array.Empty<ulong>();
    }
}
