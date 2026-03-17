using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string SameGame_GetBrowserLanguage();
#endif

        private string GetInitialLanguageCode()
        {
            var browserLanguage = GetBrowserLanguage();
            if (IsJapaneseLanguage(browserLanguage))
            {
                return "ja";
            }

            if (!string.IsNullOrWhiteSpace(browserLanguage))
            {
                return "en";
            }

            return Application.systemLanguage == SystemLanguage.Japanese ? "ja" : "en";
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return IsJapaneseLanguage(languageCode) ? "ja" : "en";
        }

        private static bool IsJapaneseLanguage(string languageCode)
        {
            return !string.IsNullOrWhiteSpace(languageCode)
                && languageCode.Trim().StartsWith("ja", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetBrowserLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return SameGame_GetBrowserLanguage();
            }
            catch
            {
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
