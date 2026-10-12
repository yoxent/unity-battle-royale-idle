using System.Collections.Generic;
using BattleRoyale.Settings;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class GameSettingsTests
    {
        [Test]
        public void CreateDefaultUsesTheDesktopSizeAndFullVolumes()
        {
            var settings = GameSettings.CreateDefault(1920, 1080);

            Assert.IsTrue(settings.cameraShake);
            Assert.AreEqual(SettingsWindowMode.Borderless, settings.windowMode);
            Assert.AreEqual(1920, settings.width);
            Assert.AreEqual(1080, settings.height);
            Assert.IsTrue(settings.vSync);
            Assert.AreEqual(1f, settings.masterVolume);
            Assert.AreEqual(1f, settings.musicVolume);
            Assert.AreEqual(1f, settings.effectsVolume);
        }

        [Test]
        public void ClampPullsVolumesAndReplacesABadModeAndSize()
        {
            var settings = GameSettings.CreateDefault(1280, 720);
            settings.masterVolume = -2f;
            settings.musicVolume = 4f;
            settings.effectsVolume = 0.25f;
            settings.windowMode = (SettingsWindowMode)99;
            settings.width = 0;
            settings.height = -5;

            settings.Clamp(1280, 720);

            Assert.AreEqual(0f, settings.masterVolume);
            Assert.AreEqual(1f, settings.musicVolume);
            Assert.AreEqual(0.25f, settings.effectsVolume);
            Assert.AreEqual(SettingsWindowMode.Borderless, settings.windowMode);
            Assert.AreEqual(1280, settings.width);
            Assert.AreEqual(720, settings.height);
        }

        [Test]
        public void LinearToDecibelsMapsSilenceAndHalfVolume()
        {
            Assert.AreEqual(-80f, GameSettings.LinearToDecibels(0f));
            Assert.AreEqual(-80f, GameSettings.LinearToDecibels(-1f));
            Assert.AreEqual(0f, GameSettings.LinearToDecibels(1f));
            Assert.AreEqual(-6.0206f, GameSettings.LinearToDecibels(0.5f), 0.001f);
        }

        [Test]
        public void CollectResolutionsDropsDuplicatesAndSorts()
        {
            var raw = new List<ResolutionSize>
            {
                new ResolutionSize(1920, 1080),
                new ResolutionSize(1280, 720),
                new ResolutionSize(1920, 1080),
                new ResolutionSize(800, 600)
            };

            var unique = GameSettings.CollectResolutions(raw);

            Assert.AreEqual(3, unique.Count);
            Assert.AreEqual(new ResolutionSize(800, 600), unique[0]);
            Assert.AreEqual(new ResolutionSize(1280, 720), unique[1]);
            Assert.AreEqual(new ResolutionSize(1920, 1080), unique[2]);
        }

        [Test]
        public void SelectResolutionPicksAnExactSize()
        {
            var options = new List<ResolutionSize>
            {
                new ResolutionSize(800, 600),
                new ResolutionSize(1920, 1080)
            };

            var picked = GameSettings.SelectResolution(options, 1920, 1080, 1280, 720);

            Assert.AreEqual(new ResolutionSize(1920, 1080), picked);
        }

        [Test]
        public void SelectResolutionPicksTheNearestAreaThenTheCloserSide()
        {
            var options = new List<ResolutionSize>
            {
                new ResolutionSize(180, 100),
                new ResolutionSize(100, 180)
            };

            var picked = GameSettings.SelectResolution(options, 150, 100, 1920, 1080);

            Assert.AreEqual(new ResolutionSize(180, 100), picked);
        }

        [Test]
        public void SelectResolutionKeepsTheFirstFullTie()
        {
            var options = new List<ResolutionSize>
            {
                new ResolutionSize(50, 300),
                new ResolutionSize(150, 100)
            };

            var picked = GameSettings.SelectResolution(options, 100, 200, 1920, 1080);

            Assert.AreEqual(new ResolutionSize(50, 300), picked);
        }

        [Test]
        public void SelectResolutionReturnsTheDesktopSizeWhenTheListIsEmpty()
        {
            var picked = GameSettings.SelectResolution(
                new List<ResolutionSize>(),
                640,
                480,
                1280,
                720);

            Assert.AreEqual(new ResolutionSize(1280, 720), picked);
        }

        [Test]
        public void FullScreenModeAndVSyncCountFollowTheRecord()
        {
            Assert.AreEqual(
                FullScreenMode.ExclusiveFullScreen,
                GameSettings.ToFullScreenMode(SettingsWindowMode.Fullscreen));
            Assert.AreEqual(
                FullScreenMode.FullScreenWindow,
                GameSettings.ToFullScreenMode(SettingsWindowMode.Borderless));
            Assert.AreEqual(
                FullScreenMode.Windowed,
                GameSettings.ToFullScreenMode(SettingsWindowMode.Windowed));
            Assert.AreEqual(1, GameSettings.ToVSyncCount(true));
            Assert.AreEqual(0, GameSettings.ToVSyncCount(false));
        }

        [Test]
        public void FormatPercentShowsAWholePercentOfTheRange()
        {
            Assert.AreEqual("100%", GameSettings.FormatPercent(1f, 0f, 1f));
            Assert.AreEqual("50%", GameSettings.FormatPercent(0.5f, 0f, 1f));
            Assert.AreEqual("0%", GameSettings.FormatPercent(0f, 0f, 1f));
        }

        [Test]
        public void MemoryStoreRoundTripKeepsTheRecord()
        {
            var store = new MemoryGameSettingsStore();
            var saved = GameSettings.CreateDefault(1600, 900);
            saved.cameraShake = false;
            saved.windowMode = SettingsWindowMode.Windowed;
            saved.width = 1280;
            saved.height = 720;
            saved.vSync = false;
            saved.masterVolume = 0.25f;
            saved.musicVolume = 0.5f;
            saved.effectsVolume = 0f;

            saved.Save(store);
            var loaded = GameSettings.Load(store, 1920, 1080);

            Assert.IsFalse(loaded.cameraShake);
            Assert.AreEqual(SettingsWindowMode.Windowed, loaded.windowMode);
            Assert.AreEqual(1280, loaded.width);
            Assert.AreEqual(720, loaded.height);
            Assert.IsFalse(loaded.vSync);
            Assert.AreEqual(0.25f, loaded.masterVolume);
            Assert.AreEqual(0.5f, loaded.musicVolume);
            Assert.AreEqual(0f, loaded.effectsVolume);
        }

        [Test]
        public void MissingKeyKeepsThatDefault()
        {
            var store = new MemoryGameSettingsStore();
            store.SetFloat(GameSettingsKeys.Master, 0.25f);

            var loaded = GameSettings.Load(store, 1920, 1080);

            Assert.AreEqual(0.25f, loaded.masterVolume);
            Assert.IsTrue(loaded.cameraShake);
            Assert.AreEqual(SettingsWindowMode.Borderless, loaded.windowMode);
            Assert.AreEqual(1920, loaded.width);
            Assert.AreEqual(1080, loaded.height);
            Assert.IsTrue(loaded.vSync);
            Assert.AreEqual(1f, loaded.musicVolume);
            Assert.AreEqual(1f, loaded.effectsVolume);
        }

        [Test]
        public void LoadClampsInvalidStoredValuesBeforeReturning()
        {
            const int desktopWidth = 1280;
            const int desktopHeight = 720;
            var store = new MemoryGameSettingsStore();
            store.SetFloat(GameSettingsKeys.Master, -2f);
            store.SetFloat(GameSettingsKeys.Music, 4f);
            store.SetInt(GameSettingsKeys.WindowMode, 99);
            store.SetInt(GameSettingsKeys.Width, 0);
            store.SetInt(GameSettingsKeys.Height, -5);

            var loaded = GameSettings.Load(store, desktopWidth, desktopHeight);

            Assert.AreEqual(0f, loaded.masterVolume);
            Assert.AreEqual(1f, loaded.musicVolume);
            Assert.AreEqual(SettingsWindowMode.Borderless, loaded.windowMode);
            Assert.AreEqual(desktopWidth, loaded.width);
            Assert.AreEqual(desktopHeight, loaded.height);
        }

        [Test]
        public void SaveCallsFlushOnTheStore()
        {
            var store = new FlushCountingGameSettingsStore();
            var settings = GameSettings.CreateDefault(1920, 1080);

            settings.Save(store);

            Assert.AreEqual(1, store.FlushCount);
        }

        sealed class FlushCountingGameSettingsStore : IGameSettingsStore
        {
            readonly Dictionary<string, int> ints = new Dictionary<string, int>();
            readonly Dictionary<string, float> floats = new Dictionary<string, float>();

            public int FlushCount { get; private set; }

            public bool Has(string key)
            {
                return ints.ContainsKey(key) || floats.ContainsKey(key);
            }

            public int GetInt(string key, int fallback)
            {
                return ints.TryGetValue(key, out var value) ? value : fallback;
            }

            public float GetFloat(string key, float fallback)
            {
                return floats.TryGetValue(key, out var value) ? value : fallback;
            }

            public void SetInt(string key, int value)
            {
                ints[key] = value;
            }

            public void SetFloat(string key, float value)
            {
                floats[key] = value;
            }

            public void Flush()
            {
                FlushCount++;
            }
        }
    }
}
