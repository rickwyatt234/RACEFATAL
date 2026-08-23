/* 
    SMALL VALUE TYPE FOR IDs
    AVOIDS PASSING RAW STRINGS EVEREYWHERE
*/

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
