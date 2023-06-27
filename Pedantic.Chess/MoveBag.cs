using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public ref struct MoveBag
    {
        private Span<ulong> _moves;
        private int _count;

        public MoveBag(Span<ulong> moves)
        {
            _moves = moves;
            _count = 0;
        }

        public int Capacity => _moves.Length;
        public int Count => _count;

        public bool IsFull => _count >= _moves.Length;

        public void Add(ulong move)
        {
            _moves[_count++] = move;
        }

        public bool TryTake(out ulong move)
        {
            move = 0;
            if (_count == 0)
            {
                return false;
            }
            move = _moves[--_count];
            return true;
        }

        public ulong this[int n]
        {
            get
            {
                if (n < 0 || n >= _count)
                {
                    throw new IndexOutOfRangeException($"Index ({n}) out of range [0, {_count}].");
                }
                return _moves[n];
            }
        }
    }
}
