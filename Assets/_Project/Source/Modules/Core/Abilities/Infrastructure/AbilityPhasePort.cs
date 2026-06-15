using System;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using UniRx;

namespace AvantajPrim.Abilities.Infrastructure
{
    public sealed class AbilityPhasePort : IAbilityPhaseNotifier
    {
        private readonly Subject<AbilityPhaseChangedEvent> _phaseChanged = new Subject<AbilityPhaseChangedEvent>();

        public IObservable<AbilityPhaseChangedEvent> PhaseChanged => _phaseChanged;

        public void NotifyPhaseChanged(AbilityPhaseChangedEvent evt) => _phaseChanged.OnNext(evt);
    }
}
