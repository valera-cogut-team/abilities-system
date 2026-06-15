using System.Collections.Generic;
using System.Threading;
using Addressables.Facade;
using Audio.Facade;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using Cysharp.Threading.Tasks;
using Logger.Facade;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvantajPrim.AbilitiesDemo.Presentation
{
    public sealed class AbilitySoundPresenter
    {
        private readonly IAudioFacade _audio;
        private readonly IAddressablesFacade _addressables;
        private readonly ILoggerFacade _logger;
        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        public AbilitySoundPresenter(IAudioFacade audio, IAddressablesFacade addressables, ILoggerFacade logger)
        {
            _audio = audio;
            _addressables = addressables;
            _logger = logger;
        }

        public async UniTask HandleSoundIntent(PresentationSoundIntent intent)
        {
            if (_audio == null || string.IsNullOrEmpty(intent.ClipKey))
                return;

            AudioClip clip = await ResolveClipAsync(intent.ClipKey);
            if (clip == null)
            {
                _logger?.LogWarning($"[AbilitiesDemo] Sound clip not found: '{intent.ClipKey}'");
                return;
            }

            _audio.PlaySound2D(clip, intent.Volume);
        }

        public async UniTask PreloadClipsAsync(
            DemoAddressableCatalog addressCatalog,
            CancellationToken cancellationToken = default)
        {
            if (_addressables == null || addressCatalog == null)
                return;

            AssetReferenceT<AudioClip>[] soundClips = addressCatalog.SoundClips ?? System.Array.Empty<AssetReferenceT<AudioClip>>();
            for (int i = 0; i < soundClips.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AssetReferenceT<AudioClip> reference = soundClips[i];
                if (reference == null || !reference.RuntimeKeyIsValid())
                    continue;

                string key = reference.RuntimeKey as string;
                if (string.IsNullOrEmpty(key) || _clipCache.ContainsKey(key))
                    continue;

                AudioClip clip = await _addressables.TryLoadAssetAsync<AudioClip>(reference);
                if (clip != null)
                    _clipCache[key] = clip;
            }
        }

        private async UniTask<AudioClip> ResolveClipAsync(string key)
        {
            if (_clipCache.TryGetValue(key, out AudioClip cached))
                return cached;

            if (_addressables == null)
                return null;

            AudioClip clip = await _addressables.TryLoadAssetAsync<AudioClip>(key);
            if (clip != null)
                _clipCache[key] = clip;

            return clip;
        }
    }
}
