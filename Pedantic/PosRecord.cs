
using Pedantic.Chess;


#nullable enable
namespace Pedantic
{
    public readonly struct PosRecord
    {
        public readonly string Fen;
        public readonly float Result;

        public PosRecord(string fen, float result)
        {
            Fen = fen;
            Result = result;
            Features = new EvalFeatures(new Board(fen));
        }

        public EvalFeatures Features { get; init; }
    }
}
