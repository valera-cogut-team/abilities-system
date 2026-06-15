using System;
using System.Threading;
using Addressables.Facade;
using AvantajPrim.Abilities.Domain;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Domain;
using Cysharp.Threading.Tasks;
using Effects.Facade;
using Logger.Facade;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class AbilityVfxPresenter
    {
        private readonly DemoEntityRegistry _registry;
        private readonly IEffectsFacade _effects;
        private readonly IAddressablesFacade _addressables;
        private readonly ILoggerFacade _logger;
        private readonly EntityAttachedVfxRegistry _entityVfxRegistry;

        public AbilityVfxPresenter(
            DemoEntityRegistry registry,
            IEffectsFacade effects,
            IAddressablesFacade addressables,
            ILoggerFacade logger,
            EntityAttachedVfxRegistry entityVfxRegistry)
        {
            _registry = registry;
            _effects = effects;
            _addressables = addressables;
            _logger = logger;
            _entityVfxRegistry = entityVfxRegistry;
        }

        public async UniTask HandleVfxIntent(PresentationVfxIntent intent)
        {
            if (_effects == null || string.IsNullOrEmpty(intent.PrefabKey))
                return;

            if (!_registry.TryGetView(intent.TargetId, out EntityView view))
                return;

            bool isHealingVfx = intent.PresentationStyle == VfxPresentationStyle.Healing;
            if (!TryCaptureSpawnPose(view, intent, isHealingVfx, out SpawnPose pose))
                return;

            try
            {
                if (intent.DurationType == AbilityDurationType.Instant && !isHealingVfx)
                {
                    await SpawnInstantWorldVfxAsync(intent, pose);
                    return;
                }

                Transform attachParent = pose.AttachParent;
                if (!IsTransformAlive(attachParent))
                    attachParent = null;

                Vector3 position = attachParent != null ? pose.LocalPosition : pose.WorldPosition;
                Quaternion rotation = attachParent != null ? pose.LocalRotation : pose.WorldRotation;

                GameObject spawned = await _effects.SpawnAsync(intent.PrefabKey, position, rotation, attachParent);
                FinalizeSpawnedVfx(intent, spawned, pose.Scale, pose.Lifetime, isHealingVfx);
            }
            catch (Exception ex) when (IsDestroyedUnityObjectException(ex))
            {
                // Target or attach parent was destroyed while the async spawn was in flight.
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"[AbilitiesDemo] VFX '{intent.PrefabKey}' failed: {ex.Message}");
            }
        }

        public async UniTask PreloadVfxAsync(
            DemoAddressableCatalog addressCatalog,
            CancellationToken cancellationToken = default)
        {
            if (_addressables == null || addressCatalog == null)
                return;

            AssetReferenceGameObject[] vfxPrefabs = addressCatalog.VfxPrefabs ?? System.Array.Empty<AssetReferenceGameObject>();
            var vfxLoads = new UniTask[vfxPrefabs.Length];
            for (int i = 0; i < vfxPrefabs.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                vfxLoads[i] = _addressables.TryLoadAssetAsync<GameObject>(vfxPrefabs[i]);
            }

            await UniTask.WhenAll(vfxLoads);

            if (_effects == null)
                return;

            for (int i = 0; i < vfxPrefabs.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string key = vfxPrefabs[i]?.TryGetRuntimeKey();
                if (string.IsNullOrEmpty(key))
                    continue;

                await _effects.PrewarmAsync(
                    key,
                    DemoConstants.Presentation.VfxPrewarmCountPerKey,
                    cancellationToken);
            }
        }

        private async UniTask SpawnInstantWorldVfxAsync(PresentationVfxIntent intent, SpawnPose pose)
        {
            GameObject instance = await _effects.SpawnAsync(intent.PrefabKey, pose.WorldPosition, pose.WorldRotation);
            if (!IsGameObjectAlive(instance))
            {
                await _effects.PlayOneShotAsync(intent.PrefabKey, pose.WorldPosition, pose.WorldRotation);
                return;
            }

            instance.transform.localScale *= pose.Scale;
            TrackEntityVfx(intent.TargetId, instance);
            _effects.ScheduleDespawn(
                instance,
                Mathf.Max(DemoConstants.Presentation.InstantVfxMinDespawnSeconds, intent.Duration));
        }

        private void FinalizeSpawnedVfx(
            PresentationVfxIntent intent,
            GameObject spawned,
            float scale,
            float lifetime,
            bool isHealingVfx)
        {
            if (!IsGameObjectAlive(spawned))
                return;

            if (isHealingVfx)
                VfxPresentationStyles.ApplyHealingStyle(
                    spawned.transform,
                    scale,
                    DemoConstants.Presentation.HealingVfxAlphaMultiplier);
            else
                spawned.transform.localScale *= scale;

            TrackEntityVfx(intent.TargetId, spawned);
            if (lifetime > 0f)
                _effects.ScheduleDespawn(spawned, lifetime);
        }

        private static bool TryCaptureSpawnPose(
            EntityView view,
            PresentationVfxIntent intent,
            bool isHealingVfx,
            out SpawnPose pose)
        {
            pose = default;
            if (!IsViewAlive(view))
                return false;

            try
            {
                pose.Scale = isHealingVfx
                    ? DemoConstants.Presentation.HealingVfxScale
                    : DemoConstants.Presentation.DefaultVfxScale;
                pose.Lifetime = isHealingVfx
                    ? Mathf.Max(intent.Duration, DemoConstants.Presentation.HealingVfxMinLifetimeSeconds)
                    : intent.Duration;

                if (isHealingVfx)
                {
                    Transform parent = view.transform;
                    if (!IsTransformAlive(parent))
                        return false;

                    float bodyCenterY = view.GetBodyCenterLocalY();
                    pose.AttachParent = parent;
                    pose.LocalPosition = new Vector3(
                        intent.OffsetX,
                        intent.OffsetY > 0f ? intent.OffsetY : bodyCenterY,
                        intent.OffsetZ);
                    pose.LocalRotation = Quaternion.identity;
                    pose.WorldPosition = parent.TransformPoint(pose.LocalPosition);
                    pose.WorldRotation = Quaternion.identity;
                    return true;
                }

                pose.AttachParent = null;
                pose.WorldPosition = view.GetVfxWorldPosition(intent.OffsetX, intent.OffsetY, intent.OffsetZ);
                pose.WorldRotation = view.transform.rotation;
                pose.LocalPosition = pose.WorldPosition;
                pose.LocalRotation = pose.WorldRotation;
                return true;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }

        private void TrackEntityVfx(EntityId targetId, GameObject instance) =>
            _entityVfxRegistry?.Track(targetId, instance);

        private static bool IsViewAlive(EntityView view) => view != null;

        private static bool IsTransformAlive(Transform transform) => transform != null;

        private static bool IsGameObjectAlive(GameObject gameObject) => gameObject != null;

        private static bool IsDestroyedUnityObjectException(Exception ex) =>
            ex is MissingReferenceException ||
            (ex.Message != null && ex.Message.IndexOf("destroyed", StringComparison.OrdinalIgnoreCase) >= 0);

        private struct SpawnPose
        {
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Transform AttachParent;
            public float Scale;
            public float Lifetime;
        }
    }
}
