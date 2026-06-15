using System;
using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Domain;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class EntityCombatState
    {
        public EntityId EntityId { get; }
        public string DisplayName { get; }
        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }

        public event Action<float, float, float> HealthChanged;
        public event Action<float, CombatNumberType> NumberRequested;

        public EntityCombatState(EntityId id, string displayName, float maxHealth)
        {
            EntityId = id;
            DisplayName = displayName;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(float amount, float tweenDuration = DemoConstants.Combat.DefaultHealthTweenSeconds)
        {
            if (amount <= 0f)
                return;

            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth, tweenDuration);
            NumberRequested?.Invoke(amount, CombatNumberType.Damage);
        }

        public void ApplyHeal(float amount, float tweenDuration = DemoConstants.Combat.DefaultHealthTweenSeconds)
        {
            if (amount <= 0f)
                return;

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth, tweenDuration);
            NumberRequested?.Invoke(amount, CombatNumberType.Heal);
        }

        public void ApplyDot(float amount, float tweenDuration = DemoConstants.Combat.DefaultHealthTweenSeconds)
        {
            if (amount <= 0f)
                return;

            CurrentHealth = Math.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth, tweenDuration);
            NumberRequested?.Invoke(amount, CombatNumberType.Dot);
        }
    }

    public enum CombatNumberType
    {
        Damage,
        Heal,
        Dot
    }
}
