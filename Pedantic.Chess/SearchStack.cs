namespace Pedantic.Chess
{
    public class SearchStack
    {
        public const int OFFSET = 4;
        private readonly SearchItem[] searchStack = new SearchItem[Constants.MAX_PLY + OFFSET];

        public SearchStack()
        {
            for (int n = 0; n < Constants.MAX_PLY + OFFSET; n++)
            {
                searchStack[n] = new SearchItem();
            }
        }

        public ref SearchItem this[int index]
        {
            get
            {
                return ref searchStack[index + OFFSET];
            }
        }

        public void Initialize(Board board, History history)
        {
            searchStack[0].Continuation = history.NullMoveContinuation;
            searchStack[1].Continuation = history.NullMoveContinuation;
            searchStack[2].Move = (uint)board.PrevLastMove;
            searchStack[2].Continuation = history.GetContinuation(board.PrevLastMove);
            searchStack[3].Move = (uint)board.LastMove;
            searchStack[3].Continuation = history.GetContinuation(board.LastMove);
            searchStack[3].IsCheckingMove = board.IsChecked();
            searchStack[3].IsPromotionThreat = board.IsPromotionThreat(board.LastMove);
        }

        public void Clear()
        {
            Array.Clear(searchStack);
        }
    }
}
