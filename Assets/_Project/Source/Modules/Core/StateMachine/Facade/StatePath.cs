using System;

namespace StateMachine.Facade
{
    public readonly struct StatePath : IEquatable<StatePath>
    {
        public string Full { get; }
        public string Region { get; }

        private StatePath(string full)
        {
            Full = full ?? throw new ArgumentNullException(nameof(full));
            int dot = full.IndexOf('.');
            Region = dot >= 0 ? full.Substring(0, dot) : full;
        }

        public static StatePath Parse(string full) => new StatePath(full);

        public bool IsChildOf(StatePath parent)
        {
            if (string.IsNullOrEmpty(parent.Full))
                return false;

            return Full.StartsWith(parent.Full + ".", StringComparison.Ordinal);
        }

        public bool Matches(StatePath pattern)
        {
            if (pattern.Full.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = pattern.Full.Substring(0, pattern.Full.Length - 2);
                return Full == prefix || Full.StartsWith(prefix + ".", StringComparison.Ordinal);
            }

            return Full == pattern.Full;
        }

        public bool Equals(StatePath other) => Full == other.Full;
        public override bool Equals(object obj) => obj is StatePath other && Equals(other);
        public override int GetHashCode() => Full?.GetHashCode() ?? 0;
        public override string ToString() => Full;

        public static bool operator ==(StatePath left, StatePath right) => left.Equals(right);
        public static bool operator !=(StatePath left, StatePath right) => !left.Equals(right);
    }
}
