
using Pedantic.Chess;


#nullable enable
namespace Pedantic
{
    public readonly struct PosRecord
    {
        public readonly ulong Hash;
        public readonly short Ply;
        public readonly short GamePly;
        public readonly short Eval;
        public readonly string Fen;
        public readonly float Result;

        public PosRecord(ulong hash, short ply, short gamePly, short eval, string fen, float result)
        {
            Hash = hash;
            Ply = ply;
            GamePly = gamePly;
            Eval = eval;
            Fen = fen;
            Result = result;
            Features = new EvalFeatures(new Board(fen));
        }

        public EvalFeatures Features { get; init; }
    }
}
