
namespace Pedantic.Chess
{
    public sealed class History : IHistory
    {
        private Color sideToMove = Color.White;
        private readonly int[] history = new int[Constants.MAX_COLORS * Constants.MAX_SQUARES * Constants.MAX_SQUARES];

        public Color SideToMove
        {
            get => sideToMove;
            set => sideToMove = value;
        }

        public int this[int from, int to] => history[GetIndex(from, to)];

        public int this[Color color, int from, int to] => history[GetIndex(color, from, to)];

        public void Update(Color color, int from, int to, short value)
        {
            int i = GetIndex(color, from, to);
            history[i] += value;
            if (history[i] > 20000)
            {
                Rescale();
            }
        }

        private int GetIndex(int from, int to)
        {
            return GetIndex(sideToMove, from, to);
        }

        private int GetIndex(Color color, int from, int to)
        {
            return ((int)color << 12) + (from << 6) + to;
        }

        private void Rescale()
        {
            for (int i = 0; i < history.Length; ++i)
            {
                history[i] >>= 1;
            }
        }
    }
}
