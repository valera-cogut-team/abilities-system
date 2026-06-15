using System;
using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using Audio.Facade;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Domain.Ports;
using AvantajPrim.Abilities.Execution;
using Cysharp.Threading.Tasks;
using Effects.Facade;
using StateMachine.Facade;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityId = AvantajPrim.Abilities.Domain.EntityId;
using Object = UnityEngine.Object;

namespace AvantajPrim.Tests.EditMode
{
    internal sealed class RecordingPresentationPort : IAbilityPresentationPort
    {
        public readonly List<DamageRequestedEvent> DamageEvents = new List<DamageRequestedEvent>();
        public readonly List<PresentationAimIntent> AimIntents = new List<PresentationAimIntent>();
        public readonly List<PresentationAnimationIntent> AnimationIntents = new List<PresentationAnimationIntent>();
        public readonly List<PresentationSoundIntent> SoundIntents = new List<PresentationSoundIntent>();
        public readonly List<PresentationVfxIntent> VfxIntents = new List<PresentationVfxIntent>();
        public readonly List<PresentationMovementIntent> MovementIntents = new List<PresentationMovementIntent>();

        public void PublishAnimation(PresentationAnimationIntent intent) => AnimationIntents.Add(intent);
        public void PublishSound(PresentationSoundIntent intent) => SoundIntents.Add(intent);
        public void PublishVfx(PresentationVfxIntent intent) => VfxIntents.Add(intent);
        public void PublishMovement(PresentationMovementIntent intent) => MovementIntents.Add(intent);
        public void PublishAim(PresentationAimIntent intent) => AimIntents.Add(intent);
        public void PublishDamage(DamageRequestedEvent evt) => DamageEvents.Add(evt);
    }

    internal sealed class RecordingEntityStatePort : IEntityStatePort
    {
        public readonly List<(EntityId Id, StatePath Path)> Transitions = new List<(EntityId, StatePath)>();
        public readonly List<(EntityId Id, StatePath Path)> Deactivations = new List<(EntityId, StatePath)>();
        private readonly HashSet<(EntityId Id, string PathFull)> _activeStates = new HashSet<(EntityId, string)>();

        public bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default)
        {
            Transitions.Add((id, path));
            _activeStates.Add((id, path.Full));
            return true;
        }

        public bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default) => true;

        public bool IsInState(EntityId id, StatePath path) => _activeStates.Contains((id, path.Full));

        public bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default)
        {
            Deactivations.Add((id, path));
            _activeStates.Remove((id, path.Full));
            return true;
        }

        public void AcquireCastInputLock(EntityId id, bool movement, bool rotation)
        {
            if (movement)
                TryTransition(id, AbilityStatePaths.InputMovement);
            if (rotation)
                TryTransition(id, AbilityStatePaths.InputRotation);
        }

        public void ReleaseCastInputLock(EntityId id, bool movement, bool rotation)
        {
            if (movement)
                TryDeactivate(id, AbilityStatePaths.InputMovement);
            if (rotation)
                TryDeactivate(id, AbilityStatePaths.InputRotation);
        }

        public void ReleaseCastInputLockLayer(EntityId id)
        {
            ReleaseCastInputLock(id, true, true);
        }
    }

    internal sealed class SelectiveEntityStatePort : IEntityStatePort
    {
        private readonly RecordingEntityStatePort _inner = new RecordingEntityStatePort();
        private readonly EntityId _blockStatusForTarget;

        public SelectiveEntityStatePort(EntityId blockStatusForTarget) =>
            _blockStatusForTarget = blockStatusForTarget;

        public IReadOnlyList<(EntityId Id, StatePath Path)> Transitions => _inner.Transitions;

        public bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default)
        {
            if (!_blockStatusForTarget.Equals(default) &&
                id.Equals(_blockStatusForTarget) &&
                path.Full.StartsWith("Status.", System.StringComparison.Ordinal))
            {
                return false;
            }

            return _inner.TryTransition(id, path, context);
        }

        public bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default) =>
            _inner.CanTransition(id, path, context);

        public bool IsInState(EntityId id, StatePath path) => _inner.IsInState(id, path);

        public bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default) =>
            _inner.TryDeactivate(id, path, context);

        public void AcquireCastInputLock(EntityId id, bool movement, bool rotation) =>
            _inner.AcquireCastInputLock(id, movement, rotation);

        public void ReleaseCastInputLock(EntityId id, bool movement, bool rotation) =>
            _inner.ReleaseCastInputLock(id, movement, rotation);

        public void ReleaseCastInputLockLayer(EntityId id) =>
            _inner.ReleaseCastInputLockLayer(id);
    }

    internal sealed class FailingEntityStatePort : IEntityStatePort
    {
        public bool TryTransition(EntityId id, StatePath path, in TransitionContext context = default) => false;

        public bool CanTransition(EntityId id, StatePath path, in TransitionContext context = default) => false;

        public bool IsInState(EntityId id, StatePath path) => false;

        public bool TryDeactivate(EntityId id, StatePath path, in TransitionContext context = default) => false;

        public void AcquireCastInputLock(EntityId id, bool movement, bool rotation) { }

        public void ReleaseCastInputLock(EntityId id, bool movement, bool rotation) { }

        public void ReleaseCastInputLockLayer(EntityId id) { }
    }

    internal sealed class RecordingAudioFacade : IAudioFacade
    {
        public readonly List<(AudioClip Clip, float Volume)> Played = new List<(AudioClip, float)>();

        public void PlaySound2D(AudioClip clip, float volumeScale = 1f) => Played.Add((clip, volumeScale));

        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f) { }
        public void StopMusic() { }
        public void PauseMusic() { }
        public void ResumeMusic() { }
        public bool IsMusicPlaying => false;
        public float MasterVolumeLinear { get; set; }
        public float SfxVolumeLinear { get; set; }
        public float MusicVolumeLinear { get; set; }
        public bool Muted { get; set; }
        public void SetMasterVolume(float linear01) { }
        public void SetSfxVolume(float linear01) { }
        public void SetMusicVolume(float linear01) { }
        public void SetMuted(bool muted) { }
    }

    internal class RecordingEffectsFacade : IEffectsFacade
    {
        public readonly List<string> SpawnedKeys = new List<string>();
        public readonly List<(GameObject Instance, float Delay)> ScheduledDespawns = new List<(GameObject, float)>();
        public readonly List<(string Key, int Count)> PrewarmCalls = new List<(string, int)>();
        public GameObject NextSpawnResult = new GameObject("VfxStub");
        public Action BeforeCompleteSpawn;
        public Vector3 LastSpawnWorldPosition;

        public virtual UniTask<GameObject> SpawnAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null)
        {
            LastSpawnWorldPosition = worldPosition;
            BeforeCompleteSpawn?.Invoke();

            SpawnedKeys.Add(address);
            GameObject instance = NextSpawnResult;
            if (parent != null)
                instance.transform.SetParent(parent, worldPositionStays: false);

            return UniTask.FromResult(instance);
        }

        public UniTask PlayOneShotAsync(string address, Vector3 worldPosition, Quaternion worldRotation,
            Transform parent = null, CancellationToken cancellationToken = default) =>
            UniTask.CompletedTask;

        public UniTask PrewarmAsync(string address, int count, CancellationToken cancellationToken = default)
        {
            PrewarmCalls.Add((address, count));
            return UniTask.CompletedTask;
        }

        public void ScheduleDespawn(GameObject instance, float delaySeconds) =>
            ScheduledDespawns.Add((instance, delaySeconds));

        public void Despawn(GameObject instance) { }
    }

    internal sealed class StubAddressablesFacade : IAddressablesFacade
    {
        private readonly Dictionary<string, Object> _byKey = new Dictionary<string, Object>();

        public void Register(string key, Object asset) => _byKey[key] = asset;

        public UniTask EnsureInitializedAsync() => UniTask.CompletedTask;

        public UniTask<T> LoadAssetAsync<T>(string address) where T : Object =>
            UniTask.FromResult(_byKey.TryGetValue(address, out Object asset) && asset is T typed ? typed : null);

        public UniTask<T> TryLoadAssetAsync<T>(string address) where T : Object =>
            LoadAssetAsync<T>(address);

        public UniTask<T> LoadAssetAsync<T>(AssetReference reference) where T : Object =>
            TryLoadAssetAsync<T>(reference?.RuntimeKey as string);

        public UniTask<T> TryLoadAssetAsync<T>(AssetReference reference) where T : Object =>
            LoadAssetAsync<T>(reference?.RuntimeKey as string);

        public UniTask<IReadOnlyList<T>> TryLoadAssetsByLabelAsync<T>(string label) where T : Object =>
            UniTask.FromResult<IReadOnlyList<T>>(System.Array.Empty<T>());

        public UniTask<GameObject> LoadPrefabAsync(string address) =>
            UniTask.FromResult(_byKey.TryGetValue(address, out Object asset) && asset is GameObject go ? go : null);

        public UniTask<GameObject> TryLoadPrefabAsync(string address) => LoadPrefabAsync(address);

        public UniTask<GameObject> LoadPrefabAsync(AssetReferenceGameObject reference) =>
            LoadPrefabAsync(reference?.RuntimeKey as string);

        public UniTask<GameObject> TryLoadPrefabAsync(AssetReferenceGameObject reference) =>
            LoadPrefabAsync(reference);

        public UniTask ReleaseAssetAsync(string address) => UniTask.CompletedTask;

        public bool IsAssetLoaded(string address) => _byKey.ContainsKey(address);
    }
}
