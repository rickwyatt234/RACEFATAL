namespace RaceFatal.Shared
{
    public readonly struct GameId
    {
        public string Value { get; }

        public GameId(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }
}
