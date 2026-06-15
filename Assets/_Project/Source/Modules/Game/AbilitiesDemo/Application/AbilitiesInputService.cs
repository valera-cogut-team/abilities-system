using System.Collections.Generic;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Domain;
using Cysharp.Threading.Tasks;
using Input.Facade;
using LifeCycle.Facade;
using Logger.Facade;
using UnityEngine;

namespace AvantajPrim.AbilitiesDemo.Application
{
    public sealed class AbilitiesInputService : IUpdateHandler
    {
        private readonly IInputFacade _input;
        private readonly AbilityCastingService _casting;
        private readonly AbilityHotkeyBindingService _hotkeys;
        private readonly PlayerInputRouter _inputRouter;
        private readonly DemoGameplaySession _session;
        private readonly ILoggerFacade _logger;

        public AbilitiesInputService(
            IInputFacade input,
            AbilityCastingService casting,
            AbilityHotkeyBindingService hotkeys,
            PlayerInputRouter inputRouter,
            DemoGameplaySession session,
            ILoggerFacade logger)
        {
            _input = input;
            _casting = casting;
            _hotkeys = hotkeys;
            _inputRouter = inputRouter;
            _session = session;
            _logger = logger;
        }

        private string _lastFailedCastId;
        private float _lastFailedCastLogTime;
        public void OnUpdate(float deltaTime)
        {
            if (!_session.IsActive)
                return;

            _inputRouter.TryHandleInteract(_input);

            if (_hotkeys.TryGetPressedAbility(_input, out AbilityDefinition definition))
                CastSlot(definition.Id.Value).Forget();
        }

        public UniTask CastSlot(string abilityId) => CastAbilityAsync(abilityId);

        public UniTask CastAbilityAsync(string abilityId) => CastInternalAsync(abilityId);

        private async UniTask CastInternalAsync(string abilityId)
        {
            CastAbilityResult result = await _casting.CastAsync(abilityId);
            if (result.Success)
                return;

            float now = Time.unscaledTime;
            if (_lastFailedCastId == abilityId &&
                now - _lastFailedCastLogTime < DemoConstants.Input.FailedCastLogCooldownSeconds)
                return;

            _lastFailedCastId = abilityId;
            _lastFailedCastLogTime = now;
            _logger?.LogWarning($"[AbilitiesDemo] Cast '{abilityId}' failed: {result.ErrorCode}");
        }
    }

    public sealed class AbilityHotkeyBindingService
    {
        private readonly AbilityCatalog _catalog;
        private readonly Dictionary<string, int> _runtimeOverrides = new Dictionary<string, int>();

        public AbilityHotkeyBindingService(AbilityCatalog catalog)
        {
            _catalog = catalog;
        }

        public void SetRuntimeOverride(string abilityId, int keyCode)
        {
            if (string.IsNullOrWhiteSpace(abilityId) || keyCode == 0)
                return;

            _runtimeOverrides[abilityId] = keyCode;
        }

        public void ClearRuntimeOverride(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return;

            _runtimeOverrides.Remove(abilityId);
        }

        public void ClearAllRuntimeOverrides() => _runtimeOverrides.Clear();

        public bool TryGetRuntimeOverride(string abilityId, out int keyCode) =>
            _runtimeOverrides.TryGetValue(abilityId, out keyCode);

        public int GetEffectiveKeyCode(AbilityDefinition definition)
        {
            if (definition == null)
                return 0;

            if (_runtimeOverrides.TryGetValue(definition.Id.Value, out int overrideKey) && overrideKey != 0)
                return overrideKey;

            if (definition.HotkeyKeyCode != 0)
                return definition.HotkeyKeyCode;

            if (definition.HotkeySlot >= 1 && definition.HotkeySlot <= DemoConstants.Input.MaxHotkeySlot)
                return (int)KeyCode.Alpha0 + definition.HotkeySlot;

            return 0;
        }

        public IReadOnlyList<AbilityHotkeyBinding> GetBindings()
        {
            var result = new List<AbilityHotkeyBinding>();
            foreach (AbilityDefinition definition in _catalog.Enumerate())
            {
                int keyCode = GetEffectiveKeyCode(definition);
                if (keyCode == 0)
                    continue;

                result.Add(new AbilityHotkeyBinding(definition, keyCode));
            }

            return result;
        }

        public bool TryGetPressedAbility(IInputFacade input, out AbilityDefinition definition)
        {
            definition = null;
            foreach (AbilityHotkeyBinding binding in GetBindings())
            {
                if (input == null || !input.GetKeyDown(binding.KeyCode))
                    continue;

                definition = binding.Definition;
                return true;
            }

            return false;
        }
    }

    public readonly struct AbilityHotkeyBinding
    {
        public AbilityDefinition Definition { get; }
        public int KeyCode { get; }

        public AbilityHotkeyBinding(AbilityDefinition definition, int keyCode)
        {
            Definition = definition;
            KeyCode = keyCode;
        }
    }
}
