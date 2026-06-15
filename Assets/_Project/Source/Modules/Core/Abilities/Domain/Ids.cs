using System;

namespace AvantajPrim.Abilities.Domain
{
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Value;
        public EntityId(int value) => Value = value;
        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;
        public override string ToString() => $"Entity({Value})";
    }

    public readonly struct AbilityId : IEquatable<AbilityId>
    {
        public readonly string Value;
        public AbilityId(string value) => Value = value ?? string.Empty;
        public bool Equals(AbilityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AbilityId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public static bool operator ==(AbilityId a, AbilityId b) => a.Value == b.Value;
        public static bool operator !=(AbilityId a, AbilityId b) => a.Value != b.Value;
        public override string ToString() => Value;
    }
}
