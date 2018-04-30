namespace Donut.Lex.Data
{
    public class TokenPosition
    {
        public uint Position { get; set; }
        public uint Line { get; set; }

        public TokenPosition(uint line , uint pos)
        {
            this.Line = line;
            Position = pos;
        }
        public override string ToString()
        {
            return $"{Line}:{Position}";
        }
    }
}
