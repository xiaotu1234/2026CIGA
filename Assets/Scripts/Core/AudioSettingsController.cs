using System;
using UnityEngine;

namespace BrokenAnchor.Core
{
    public static class AudioSettingsController
    {
        private const float DefaultVolume = 0.8f;
        private const string MusicVolumeKey = "BrokenAnchor.Audio.MusicVolume";
        private const string VoiceVolumeKey = "BrokenAnchor.Audio.VoiceVolume";
        private const string SfxVolumeKey = "BrokenAnchor.Audio.SfxVolume";

        public static event Action<float> MusicVolumeChanged;

        public static float MusicVolume => GetVolume(MusicVolumeKey);
        public static float VoiceVolume => GetVolume(VoiceVolumeKey);
        public static float SfxVolume => GetVolume(SfxVolumeKey);

        public static void SetMusicVolume(float value)
        {
            SetVolume(MusicVolumeKey, value);
            MusicVolumeChanged?.Invoke(MusicVolume);
        }

        public static void SetVoiceVolume(float value)
        {
            SetVolume(VoiceVolumeKey, value);
        }

        public static void SetSfxVolume(float value)
        {
            SetVolume(SfxVolumeKey, value);
        }

        public static void ApplyMusicVolume(AudioSource source)
        {
            if (source != null)
            {
                source.volume = MusicVolume;
            }
        }

        private static float GetVolume(string key)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
        }

        private static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
