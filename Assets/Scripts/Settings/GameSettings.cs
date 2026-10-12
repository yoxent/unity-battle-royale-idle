using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyale.Settings
{
    public enum SettingsWindowMode
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2
    }

    public enum SettingsBinding
    {
        CameraShake,
        WindowMode,
        Resolution,
        VSync,
        MasterVolume,
        MusicVolume,
        EffectsVolume
    }

    public enum SettingsTab
    {
        Game,
        Display,
        Audio
    }

    public readonly struct ResolutionSize
    {
        public int Width { get; }
        public int Height { get; }

        public ResolutionSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override bool Equals(object obj)
        {
            return obj is ResolutionSize other
                && Width == other.Width
                && Height == other.Height;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }
    }

    public static class GameSettingsKeys
    {
        public const string CameraShake = "settings.cameraShake";
        public const string WindowMode = "settings.windowMode";
        public const string Width = "settings.width";
        public const string Height = "settings.height";
        public const string VSync = "settings.vSync";
        public const string Master = "settings.master";
        public const string Music = "settings.music";
        public const string Effects = "settings.effects";
    }

    public sealed class GameSettings
    {
        public bool cameraShake;
        public SettingsWindowMode windowMode;
        public int width;
        public int height;
        public bool vSync;
        public float masterVolume;
        public float musicVolume;
        public float effectsVolume;

        public static GameSettings CreateDefault(int desktopWidth, int desktopHeight)
        {
            return new GameSettings
            {
                cameraShake = true,
                windowMode = SettingsWindowMode.Borderless,
                width = desktopWidth,
                height = desktopHeight,
                vSync = true,
                masterVolume = 1f,
                musicVolume = 1f,
                effectsVolume = 1f
            };
        }

        public void Clamp(int desktopWidth, int desktopHeight)
        {
            masterVolume = Mathf.Clamp(masterVolume, 0f, 1f);
            musicVolume = Mathf.Clamp(musicVolume, 0f, 1f);
            effectsVolume = Mathf.Clamp(effectsVolume, 0f, 1f);

            if (!Enum.IsDefined(typeof(SettingsWindowMode), windowMode))
            {
                windowMode = SettingsWindowMode.Borderless;
            }

            if (width < 1)
            {
                width = desktopWidth;
            }

            if (height < 1)
            {
                height = desktopHeight;
            }
        }

        public static GameSettings Load(IGameSettingsStore store, int desktopWidth, int desktopHeight)
        {
            var settings = CreateDefault(desktopWidth, desktopHeight);

            if (store.Has(GameSettingsKeys.CameraShake))
            {
                settings.cameraShake = store.GetInt(GameSettingsKeys.CameraShake, 0) != 0;
            }

            if (store.Has(GameSettingsKeys.WindowMode))
            {
                settings.windowMode = (SettingsWindowMode)store.GetInt(GameSettingsKeys.WindowMode, 0);
            }

            if (store.Has(GameSettingsKeys.Width))
            {
                settings.width = store.GetInt(GameSettingsKeys.Width, desktopWidth);
            }

            if (store.Has(GameSettingsKeys.Height))
            {
                settings.height = store.GetInt(GameSettingsKeys.Height, desktopHeight);
            }

            if (store.Has(GameSettingsKeys.VSync))
            {
                settings.vSync = store.GetInt(GameSettingsKeys.VSync, 0) != 0;
            }

            if (store.Has(GameSettingsKeys.Master))
            {
                settings.masterVolume = store.GetFloat(GameSettingsKeys.Master, 1f);
            }

            if (store.Has(GameSettingsKeys.Music))
            {
                settings.musicVolume = store.GetFloat(GameSettingsKeys.Music, 1f);
            }

            if (store.Has(GameSettingsKeys.Effects))
            {
                settings.effectsVolume = store.GetFloat(GameSettingsKeys.Effects, 1f);
            }

            settings.Clamp(desktopWidth, desktopHeight);
            return settings;
        }

        public void Save(IGameSettingsStore store)
        {
            store.SetInt(GameSettingsKeys.CameraShake, cameraShake ? 1 : 0);
            store.SetInt(GameSettingsKeys.WindowMode, (int)windowMode);
            store.SetInt(GameSettingsKeys.Width, width);
            store.SetInt(GameSettingsKeys.Height, height);
            store.SetInt(GameSettingsKeys.VSync, vSync ? 1 : 0);
            store.SetFloat(GameSettingsKeys.Master, masterVolume);
            store.SetFloat(GameSettingsKeys.Music, musicVolume);
            store.SetFloat(GameSettingsKeys.Effects, effectsVolume);
            store.Flush();
        }

        public static List<ResolutionSize> CollectResolutions(IReadOnlyList<ResolutionSize> raw)
        {
            var unique = new List<ResolutionSize>();
            if (raw == null)
            {
                return unique;
            }

            for (var i = 0; i < raw.Count; i++)
            {
                var candidate = raw[i];
                var alreadyPresent = false;
                for (var j = 0; j < unique.Count; j++)
                {
                    var existing = unique[j];
                    if (existing.Width == candidate.Width && existing.Height == candidate.Height)
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    unique.Add(candidate);
                }
            }

            SortResolutions(unique);
            return unique;
        }

        public static ResolutionSize SelectResolution(
            IReadOnlyList<ResolutionSize> options,
            int savedWidth,
            int savedHeight,
            int desktopWidth,
            int desktopHeight)
        {
            if (options == null || options.Count == 0)
            {
                return new ResolutionSize(desktopWidth, desktopHeight);
            }

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option.Width == savedWidth && option.Height == savedHeight)
                {
                    return option;
                }
            }

            var savedArea = (long)savedWidth * savedHeight;
            var bestIndex = 0;
            var bestAreaDiff = long.MaxValue;
            var bestWidthDiff = int.MaxValue;
            var bestHeightDiff = int.MaxValue;

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var area = (long)option.Width * option.Height;
                var areaDiff = Math.Abs(area - savedArea);
                var widthDiff = Math.Abs(option.Width - savedWidth);
                var heightDiff = Math.Abs(option.Height - savedHeight);

                if (areaDiff < bestAreaDiff)
                {
                    bestIndex = i;
                    bestAreaDiff = areaDiff;
                    bestWidthDiff = widthDiff;
                    bestHeightDiff = heightDiff;
                    continue;
                }

                if (areaDiff > bestAreaDiff)
                {
                    continue;
                }

                if (widthDiff < bestWidthDiff)
                {
                    bestIndex = i;
                    bestWidthDiff = widthDiff;
                    bestHeightDiff = heightDiff;
                    continue;
                }

                if (widthDiff > bestWidthDiff)
                {
                    continue;
                }

                if (heightDiff < bestHeightDiff)
                {
                    bestIndex = i;
                    bestHeightDiff = heightDiff;
                }
            }

            return options[bestIndex];
        }

        public static float LinearToDecibels(float volume)
        {
            if (volume <= 0f)
            {
                return -80f;
            }

            return (float)(Math.Log10(volume) * 20d);
        }

        public static FullScreenMode ToFullScreenMode(SettingsWindowMode mode)
        {
            switch (mode)
            {
                case SettingsWindowMode.Fullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                case SettingsWindowMode.Borderless:
                    return FullScreenMode.FullScreenWindow;
                case SettingsWindowMode.Windowed:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        public static int ToVSyncCount(bool vSync)
        {
            return vSync ? 1 : 0;
        }

        public static string FormatPercent(float value, float minimum, float maximum)
        {
            if (maximum <= minimum)
            {
                return "0%";
            }

            var percent = Mathf.RoundToInt((value - minimum) / (maximum - minimum) * 100f);
            return percent + "%";
        }

        static void SortResolutions(List<ResolutionSize> resolutions)
        {
            for (var i = 1; i < resolutions.Count; i++)
            {
                var current = resolutions[i];
                var j = i - 1;
                while (j >= 0 && CompareResolutions(resolutions[j], current) > 0)
                {
                    resolutions[j + 1] = resolutions[j];
                    j--;
                }

                resolutions[j + 1] = current;
            }
        }

        static int CompareResolutions(ResolutionSize left, ResolutionSize right)
        {
            if (left.Width != right.Width)
            {
                return left.Width.CompareTo(right.Width);
            }

            return left.Height.CompareTo(right.Height);
        }
    }
}
