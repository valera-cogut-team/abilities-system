using System.Collections.Generic;
using AvantajPrim.Abilities.Data;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using AvantajPrim.AbilitiesDemo.Domain;
using Cysharp.Threading.Tasks;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityActivationReplayTests
    {
        [Test]
        public void ActivationLog_RecordsComponentOrder()
        {
            var log = new AbilityActivationLog();
            var abilityId = new AbilityId(DemoConstants.AbilityIds.Firewall);
            var caster = new EntityId(1);
            var target = new EntityId(2);

            log.Record(abilityId, caster, target, nameof(LockInputComponentData));
            log.Record(abilityId, caster, target, nameof(DamageComponentData));

            Assert.AreEqual(2, log.Frames.Count);
            Assert.AreEqual(nameof(LockInputComponentData), log.Frames[0].ComponentTypeName);
            Assert.AreEqual(nameof(DamageComponentData), log.Frames[1].ComponentTypeName);
        }

        [Test]
        public void ActivationLog_RecordsCastSessions()
        {
            var log = new AbilityActivationLog();
            var abilityId = new AbilityId(DemoConstants.AbilityIds.Firewall);
            var caster = new EntityId(1);
            EntityId[] targets = new[] { new EntityId(2), new EntityId(3) };

            log.RecordCast(abilityId, caster, targets);

            Assert.AreEqual(1, log.Casts.Count);
            Assert.AreEqual(2, log.Casts[0].TargetIds.Length);
            Assert.AreEqual(targets[0], log.Casts[0].TargetIds[0]);
            Assert.AreEqual(targets[1], log.Casts[0].TargetIds[1]);
        }

        [Test]
        public void ReplayService_ReplaysLastCastOnce()
        {
            var log = new AbilityActivationLog();
            var abilityId = new AbilityId(DemoConstants.AbilityIds.Dash);
            var caster = new EntityId(1);
            var target = new EntityId(2);

            log.RecordCast(abilityId, caster, new[] { target });
            log.Record(abilityId, caster, target, "AnimationComponentData");
            log.Record(abilityId, caster, target, "DamageComponentData");
            log.RecordCast(new AbilityId(DemoConstants.AbilityIds.Healing), caster, new[] { caster });
            log.Record(new AbilityId(DemoConstants.AbilityIds.Healing), caster, caster, "AnimationComponentData");

            var replayCalls = new List<(AbilityId, EntityId, EntityId)>();
            var replay = new AbilityActivationReplayService(
                log,
                (id, c, t) =>
                {
                    replayCalls.Add((id, c, t));
                    return UniTask.CompletedTask;
                });

            replay.ReplayLastAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, replayCalls.Count);
            Assert.AreEqual(DemoConstants.AbilityIds.Healing, replayCalls[0].Item1.Value);
            Assert.AreEqual(caster, replayCalls[0].Item2);
            Assert.AreEqual(caster, replayCalls[0].Item3);
        }

        [Test]
        public void ReplayService_ReplaysLastMultiTargetCast()
        {
            var log = new AbilityActivationLog();
            var abilityId = new AbilityId(DemoConstants.AbilityIds.Firewall);
            var caster = new EntityId(1);
            EntityId[] targets = new[] { new EntityId(2), new EntityId(3) };

            log.RecordCast(abilityId, caster, targets);

            var singleCalls = new List<(AbilityId, EntityId, EntityId)>();
            var multiCalls = new List<(AbilityId, EntityId, IReadOnlyList<EntityId>)>();
            var replay = new AbilityActivationReplayService(
                log,
                (id, c, t) =>
                {
                    singleCalls.Add((id, c, t));
                    return UniTask.CompletedTask;
                },
                (id, c, list) =>
                {
                    multiCalls.Add((id, c, list));
                    return UniTask.CompletedTask;
                });

            replay.ReplayLastAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, singleCalls.Count);
            Assert.AreEqual(1, multiCalls.Count);
            Assert.AreEqual(2, multiCalls[0].Item3.Count);
        }

        [Test]
        public void ReplayService_ReplaysByAbilityId()
        {
            var log = new AbilityActivationLog();
            var dashId = new AbilityId(DemoConstants.AbilityIds.Dash);
            var firewallId = new AbilityId(DemoConstants.AbilityIds.Firewall);
            var caster = new EntityId(1);
            var target = new EntityId(2);

            log.RecordCast(dashId, caster, new[] { target });
            log.RecordCast(firewallId, caster, new[] { target });

            var replayCalls = new List<(AbilityId, EntityId, EntityId)>();
            var replay = new AbilityActivationReplayService(
                log,
                (id, c, t) =>
                {
                    replayCalls.Add((id, c, t));
                    return UniTask.CompletedTask;
                });

            replay.ReplayByAbilityIdAsync(dashId).GetAwaiter().GetResult();

            Assert.AreEqual(1, replayCalls.Count);
            Assert.AreEqual(DemoConstants.AbilityIds.Dash, replayCalls[0].Item1.Value);
        }
    }
}
