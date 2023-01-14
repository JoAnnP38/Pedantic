namespace Pedantic.Chess
{
    public interface IHistory
    {
        public int this[int from, int to] { get; }
    }

    public interface ISearch
    {
        public void Search();
        public void ScoutSearch();
    }
}