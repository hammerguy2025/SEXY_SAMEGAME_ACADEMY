#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using SameGame.Runtime;
using UnityEngine;

namespace SameGame.Tests.EditMode
{
    public class SameGameAppProgressTests
    {
        private const string SelectedCharacterPrefKey = "SameGame.SelectedCharacter";
        private const string MasterVolumePrefKey = "SameGame.MasterVolume";
        private const string LanguagePrefKey = "SameGame.Language";
        private const string UnlockedRewardsPrefKey = "SameGame.UnlockedRewards";

        private SavedPlayerPrefs _savedPlayerPrefs;

        [SetUp]
        public void SetUp()
        {
            _savedPlayerPrefs = SavedPlayerPrefs.Capture();
            ClearProgressKeys();
            DestroyApps();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyApps();
            _savedPlayerPrefs.Restore();
        }

        [Test]
        public void UnlockCurrentReward_SavesRewardKeyForSelectedCharacter()
        {
            var app = CreateInitializedApp();
            SetField(app, "_currentStageIndex", 0);

            Invoke(app, "UnlockCurrentReward");

            Assert.That(PlayerPrefs.GetString(UnlockedRewardsPrefKey, string.Empty), Is.EqualTo("character_01:0"));
        }

        [Test]
        public void LoadProgress_RestoresUnlockedRewardsForNewInstance()
        {
            PlayerPrefs.SetString(UnlockedRewardsPrefKey, "character_01:0");
            PlayerPrefs.Save();

            var app = CreateInitializedApp();

            Invoke(app, "LoadProgress");
            var character = (CharacterDefinition)Invoke(app, "GetSelectedCharacter");
            var isUnlocked = (bool)Invoke(app, "IsRewardUnlocked", character, 0);

            Assert.That(isUnlocked, Is.True);
        }

        private static SameGameApp CreateInitializedApp()
        {
            var gameObject = new GameObject("SameGameAppProgressTest");
            var app = gameObject.AddComponent<SameGameApp>();

            Invoke(app, "InitializeCharacters");
            Invoke(app, "InitializeStages");
            Invoke(app, "SanitizeCharacters");
            Invoke(app, "SanitizeStages");

            return app;
        }

        private static void ClearProgressKeys()
        {
            PlayerPrefs.DeleteKey(SelectedCharacterPrefKey);
            PlayerPrefs.DeleteKey(MasterVolumePrefKey);
            PlayerPrefs.DeleteKey(LanguagePrefKey);
            PlayerPrefs.DeleteKey(UnlockedRewardsPrefKey);
            PlayerPrefs.Save();
        }

        private static void DestroyApps()
        {
            var apps = Object.FindObjectsByType<SameGameApp>(FindObjectsSortMode.None);
            for (var i = 0; i < apps.Length; i++)
            {
                Object.DestroyImmediate(apps[i].gameObject);
            }
        }

        private static object Invoke(object instance, string methodName, params object[] parameters)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(instance, parameters);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(instance, value);
        }

        private readonly struct SavedPlayerPrefs
        {
            private SavedPlayerPrefs(
                bool hasSelectedCharacter,
                int selectedCharacter,
                bool hasMasterVolume,
                float masterVolume,
                bool hasLanguage,
                string language,
                bool hasUnlockedRewards,
                string unlockedRewards)
            {
                HasSelectedCharacter = hasSelectedCharacter;
                SelectedCharacter = selectedCharacter;
                HasMasterVolume = hasMasterVolume;
                MasterVolume = masterVolume;
                HasLanguage = hasLanguage;
                Language = language;
                HasUnlockedRewards = hasUnlockedRewards;
                UnlockedRewards = unlockedRewards;
            }

            private bool HasSelectedCharacter { get; }

            private int SelectedCharacter { get; }

            private bool HasMasterVolume { get; }

            private float MasterVolume { get; }

            private bool HasLanguage { get; }

            private string Language { get; }

            private bool HasUnlockedRewards { get; }

            private string UnlockedRewards { get; }

            public static SavedPlayerPrefs Capture()
            {
                return new SavedPlayerPrefs(
                    PlayerPrefs.HasKey(SelectedCharacterPrefKey),
                    PlayerPrefs.GetInt(SelectedCharacterPrefKey, 0),
                    PlayerPrefs.HasKey(MasterVolumePrefKey),
                    PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f),
                    PlayerPrefs.HasKey(LanguagePrefKey),
                    PlayerPrefs.GetString(LanguagePrefKey, "ja"),
                    PlayerPrefs.HasKey(UnlockedRewardsPrefKey),
                    PlayerPrefs.GetString(UnlockedRewardsPrefKey, string.Empty));
            }

            public void Restore()
            {
                RestoreInt(SelectedCharacterPrefKey, HasSelectedCharacter, SelectedCharacter);
                RestoreFloat(MasterVolumePrefKey, HasMasterVolume, MasterVolume);
                RestoreString(LanguagePrefKey, HasLanguage, Language);
                RestoreString(UnlockedRewardsPrefKey, HasUnlockedRewards, UnlockedRewards);
                PlayerPrefs.Save();
            }

            private static void RestoreInt(string key, bool hasValue, int value)
            {
                if (hasValue)
                {
                    PlayerPrefs.SetInt(key, value);
                    return;
                }

                PlayerPrefs.DeleteKey(key);
            }

            private static void RestoreFloat(string key, bool hasValue, float value)
            {
                if (hasValue)
                {
                    PlayerPrefs.SetFloat(key, value);
                    return;
                }

                PlayerPrefs.DeleteKey(key);
            }

            private static void RestoreString(string key, bool hasValue, string value)
            {
                if (hasValue)
                {
                    PlayerPrefs.SetString(key, value);
                    return;
                }

                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
#endif
