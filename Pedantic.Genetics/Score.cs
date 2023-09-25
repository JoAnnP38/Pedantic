namespace Pedantic.Genetics
{
    public readonly struct Score
    {
        public readonly int Value;

        public Score(short mg, short eg)
        {
            Value = (int)(((uint)eg << 16) + mg);
        }

        public Score(int value)
        {
            Value = value;
        }

        public Score((short mg, short eg) s)
            : this(s.mg, s.eg)
        { }

        public readonly short MG => (short)(uint)Value;
        public readonly short EG => (short)((uint)(Value + 0x8000) >> 16);

        public static Score operator+(Score lhs, Score rhs) => new (lhs.Value + rhs.Value);
        public static Score operator-(Score lhs, Score rhs) => new (lhs.Value - rhs.Value);
        public static Score operator*(Score lhs, Score rhs) => new (lhs.Value * rhs.Value);

        public static implicit operator int(Score s) => s.Value;
        public static explicit operator Score(int value) => new (value);

        public override string ToString()
        {
            return $"({MG}, {EG})";
        }
    }
}
