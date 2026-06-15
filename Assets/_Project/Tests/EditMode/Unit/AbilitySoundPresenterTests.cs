using AvantajPrim.Abilities.Domain;
using AvantajPrim.AbilitiesDemo.Application;
using AvantajPrim.AbilitiesDemo.Presentation;
using AvantajPrim.Tests.EditMode;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilitySoundPresenterTests
    {
        [Test]
        public void HandleSoundIntent_DoesNothing_WhenClipKeyEmpty()
        {
            var audio = new RecordingAudioFacade();
            var presenter = new AbilitySoundPresenter(audio, new StubAddressablesFacade(), logger: null);

            presenter.HandleSoundIntent(new PresentationSoundIntent(string.Empty, 1f)).GetAwaiter().GetResult();

            Assert.AreEqual(0, audio.Played.Count);
        }

        [Test]
        public void HandleSoundIntent_LoadsClipAndPlays2D()
        {
            var audio = new RecordingAudioFacade();
            var addressables = new StubAddressablesFacade();
            var clip = AudioClip.Create("test_clip", 44100, 1, 44100, false);
            addressables.Register("sfx_test", clip);
            var presenter = new AbilitySoundPresenter(audio, addressables, logger: null);

            presenter.HandleSoundIntent(new PresentationSoundIntent("sfx_test", 0.75f)).GetAwaiter().GetResult();

            Assert.AreEqual(1, audio.Played.Count);
            Assert.AreSame(clip, audio.Played[0].Clip);
            Assert.AreEqual(0.75f, audio.Played[0].Volume);
        }

        [Test]
        public void HandleSoundIntent_UsesCache_OnSecondPlay()
        {
            var audio = new RecordingAudioFacade();
            var addressables = new StubAddressablesFacade();
            var clip = AudioClip.Create("cached_clip", 44100, 1, 44100, false);
            addressables.Register("sfx_cached", clip);
            var presenter = new AbilitySoundPresenter(audio, addressables, logger: null);
            var intent = new PresentationSoundIntent("sfx_cached", 1f);

            presenter.HandleSoundIntent(intent).GetAwaiter().GetResult();
            addressables.Register("sfx_cached", null);
            presenter.HandleSoundIntent(intent).GetAwaiter().GetResult();

            Assert.AreEqual(2, audio.Played.Count);
            Assert.AreSame(clip, audio.Played[1].Clip);
        }

        [Test]
        public void PreloadClipsAsync_SkipsNullCatalog()
        {
            var audio = new RecordingAudioFacade();
            var addressables = new StubAddressablesFacade();
            var presenter = new AbilitySoundPresenter(audio, addressables, logger: null);

            Assert.DoesNotThrow(() =>
                presenter.PreloadClipsAsync(null).GetAwaiter().GetResult());
        }
    }
}
