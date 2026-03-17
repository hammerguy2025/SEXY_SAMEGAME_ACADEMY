using System.Runtime.InteropServices;
using UnityEngine;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SameGame_PlayStreamingBgm(string url, float volume);

        [DllImport("__Internal")]
        private static extern void SameGame_SetStreamingBgmVolume(float volume);

        [DllImport("__Internal")]
        private static extern void SameGame_StopStreamingBgm();

        [DllImport("__Internal")]
        private static extern void SameGame_ToggleFullscreen();

        [DllImport("__Internal")]
        private static extern int SameGame_IsFullscreen();

        [DllImport("__Internal")]
        private static extern int SameGame_CanUseFullscreen();
#endif

        private static bool UseBrowserBgmPlayback()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        private static bool SupportsBrowserFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return SameGame_CanUseFullscreen() != 0;
#else
            return false;
#endif
        }

        private static bool IsBrowserFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return SameGame_IsFullscreen() != 0;
#else
            return false;
#endif
        }

        private static void ToggleBrowserFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SameGame_ToggleFullscreen();
#endif
        }

        private static void PlayBrowserBgm(string clipUrl, float volume)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SameGame_PlayStreamingBgm(clipUrl, volume);
#endif
        }

        private static void SetBrowserBgmVolume(float volume)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SameGame_SetStreamingBgmVolume(volume);
#endif
        }

        private static void StopBrowserBgm()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SameGame_StopStreamingBgm();
#endif
        }
    }
}
