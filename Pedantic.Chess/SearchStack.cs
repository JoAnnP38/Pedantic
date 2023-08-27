namespace Pedantic.Chess
{
    public class SearchStack
    {
        private readonly SearchItem[] searchStack = new SearchItem[Constants.MAX_PLY + 2];

        public SearchStack(Board board)
        {
            for (int n = 0; n < Constants.MAX_PLY + 2; n++)
            {
                searchStack[n] = new SearchItem();
            }
            searchStack[0].Move = (uint)board.PrevLastMove;
            searchStack[1].Move = (uint)board.LastMove;
            searchStack[1].IsCheckingMove = board.IsChecked();
            searchStack[1].IsPromotionThreat = board.IsPromotionThreat(board.LastMove);
        }

        public SearchStack()
        { }

        public ref SearchItem this[int index]
        {
            get
            {
                return ref searchStack[index + 2];
            }
        }

        public void Initialize(Board board)
        {
            searchStack[0].Move = (uint)board.PrevLastMove;
            searchStack[1].Move = (uint)board.LastMove;
            searchStack[1].IsCheckingMove = board.IsChecked();
            searchStack[1].IsPromotionThreat = board.IsPromotionThreat(board.LastMove);
        }

        public void Clear()
        {
            Array.Clear(searchStack);
        }
    }
}
