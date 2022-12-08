using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Pedantic.Collections
{
    public sealed class BitBoardArray2D : ICloneable
    {
        private readonly ulong[] array;
        private readonly int length1;
        private readonly int length2;

        public BitBoardArray2D(int length1, int length2)
        {
            array = new ulong[length1 * length2];
            this.length1 = length1;
            this.length2 = length2;
        }

        public bool IsFixedSize => true;
        public bool IsReadOnly => false;
        public int Length1 => length1;
        public int Length2 => length2;
        public int Length => array.Length;
        public int Rank => 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ulong bb)
        {
            Array.Fill(array, bb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int i, int j)
        {
            return i * length2 + j;
        }

        public ref ulong this[int i, int j] => ref array[i * length2 + j];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetItem(int i, int j)
        {
            return array[GetIndex(i, j)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetItem(int i, int j, ulong value)
        {
            array[GetIndex(i, j)] = value;
        }

        public Span<ulong> this[int i] => new(array, i * length2, length2);

        public void Copy(BitBoardArray2D other)
        {
            Debug.Assert(length1 == other.length1 && length2 == other.length2);
            Array.Copy(other.array, array, array.Length);
        }
        public BitBoardArray2D Clone()
        {
            BitBoardArray2D clone = new(length1, length2);
            Array.Copy(array, clone.array, clone.array.Length);
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
