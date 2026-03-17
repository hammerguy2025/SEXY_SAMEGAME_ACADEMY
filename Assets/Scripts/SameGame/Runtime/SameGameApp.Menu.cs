using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private enum CharacterSelectionMode
        {
            Play,
            Gallery
        }

        private const string SelectedCharacterPrefKey = "SameGame.SelectedCharacter";
        private const string LegacyMasterVolumePrefKey = "SameGame.MasterVolume";
        private const string BgmVolumePrefKey = "SameGame.BgmVolume";
        private const string SeVolumePrefKey = "SameGame.SeVolume";
        private const string LanguagePrefKey = "SameGame.Language";
        private const string UnlockedRewardsPrefKey = "SameGame.UnlockedRewards";

        [SerializeField] private List<CharacterDefinition> _characters = new List<CharacterDefinition>();

        private readonly HashSet<string> _unlockedRewardKeys = new HashSet<string>();

        private int _selectedCharacterIndex;
        private int _galleryCharacterIndex;
        private float _bgmVolume = 0.5f;
        private float _seVolume = 0.5f;
        private string _languageCode = "ja";
        private CharacterSelectionMode _characterSelectionMode;

        private GameObject _characterSelectPanel;
        private RectTransform _characterSelectionGrid;
        private Text _characterSelectTitleText;
        private Text _characterSelectStatusText;
        private GameObject _galleryPanel;
        private RectTransform _galleryContent;
        private Text _galleryTitleText;
        private Text _galleryStatusText;
        private Text _galleryEmptyText;
        private ScrollRect _galleryScrollRect;
        private GameObject _galleryViewerOverlay;
        private RectTransform _galleryViewerViewport;
        private Image _galleryViewerImage;
        private RectTransform _galleryViewerPortraitViewport;
        private Image _galleryViewerPortraitImage;
        private Button _galleryViewerPortraitToggleButton;
        private Text _galleryViewerPortraitToggleText;
        private CharacterStageProfile _galleryViewerStageCharacter;
        private bool _galleryViewerPortraitVisible = true;
        private GameObject _optionsPanel;
        private Text _optionsStatusText;
        private Slider _bgmVolumeSlider;
        private Slider _seVolumeSlider;
        private Text _bgmVolumeValueText;
        private Text _seVolumeValueText;
        private Text _languageValueText;
        private Text _optionsTitleText;
        private Text _bgmLabelText;
        private Text _seLabelText;
        private Text _languageLabelText;

        private void InitializeCharacters()
        {
            if (_characters.Count > 0)
            {
                return;
            }

            _characters.Add(new CharacterDefinition(
                "character_01",
                "初等部",
                "初等部のキャラクター一覧",
                new Color(0.95f, 0.41f, 0.55f, 1f),
                new Color(0.98f, 0.83f, 0.58f, 1f),
                CreateStageProfiles("elementary", "初等部", 5)));

            _characters.Add(new CharacterDefinition(
                "character_02",
                "中等部",
                "中等部のキャラクター一覧",
                new Color(0.34f, 0.73f, 0.96f, 1f),
                new Color(0.75f, 0.9f, 1f, 1f),
                CreateStageProfiles("middle", "中等部", 5)));

            _characters.Add(new CharacterDefinition(
                "character_03",
                "高等部",
                "高等部のキャラクター一覧",
                new Color(0.37f, 0.84f, 0.58f, 1f),
                new Color(0.86f, 0.97f, 0.86f, 1f),
                CreateStageProfiles("high", "高等部", 5)));

            _characters.Add(new CharacterDefinition(
                "character_04",
                "教員",
                "教員キャラクター一覧",
                new Color(0.68f, 0.52f, 0.94f, 1f),
                new Color(0.89f, 0.82f, 0.99f, 1f),
                CreateStageProfiles("faculty", "教員", 5)));
        }

        private static List<CharacterStageProfile> CreateStageProfiles(string groupId, string groupName, int count)
        {
            var profiles = new List<CharacterStageProfile>(count);
            for (var i = 0; i < count; i++)
            {
                profiles.Add(new CharacterStageProfile(
                    groupId + "_character_" + (i + 1).ToString("00"),
                    GetStageCharacterName(groupName, i),
                    groupName + "のキャラクター"));
            }

            return profiles;
        }

        private void SanitizeCharacters()
        {
            if (_characters.Count == 0)
            {
                InitializeCharacters();
            }

            var requiredStageCount = Mathf.Max(1, _stages.Count);
            for (var i = 0; i < _characters.Count; i++)
            {
                if (_characters[i] == null)
                {
                    _characters[i] = new CharacterDefinition();
                }

                if (string.IsNullOrWhiteSpace(_characters[i].id))
                {
                    _characters[i].id = "character_" + (i + 1).ToString("00");
                }

                if (string.IsNullOrWhiteSpace(_characters[i].displayName))
                {
                    _characters[i].displayName = "Category " + (i + 1);
                }

                if (string.IsNullOrWhiteSpace(_characters[i].summary))
                {
                    _characters[i].summary = _characters[i].displayName + "のキャラクター一覧";
                }

                if (_characters[i].stageCharacters == null)
                {
                    _characters[i].stageCharacters = new List<CharacterStageProfile>();
                }

                while (_characters[i].stageCharacters.Count < requiredStageCount)
                {
                    var stageIndex = _characters[i].stageCharacters.Count;
                    _characters[i].stageCharacters.Add(new CharacterStageProfile(
                        _characters[i].id + "_character_" + (stageIndex + 1).ToString("00"),
                        GetStageCharacterName(_characters[i].displayName, stageIndex),
                        _characters[i].displayName + "のキャラクター"));
                }

                for (var stageIndex = 0; stageIndex < _characters[i].stageCharacters.Count; stageIndex++)
                {
                    var profile = _characters[i].stageCharacters[stageIndex];
                    if (profile == null)
                    {
                        profile = new CharacterStageProfile();
                        _characters[i].stageCharacters[stageIndex] = profile;
                    }

                    if (string.IsNullOrWhiteSpace(profile.id))
                    {
                        profile.id = _characters[i].id + "_character_" + (stageIndex + 1).ToString("00");
                    }

                    if (string.IsNullOrWhiteSpace(profile.displayName))
                    {
                        profile.displayName = GetStageCharacterName(_characters[i].displayName, stageIndex);
                    }

                    if (string.IsNullOrWhiteSpace(profile.summary))
                    {
                        profile.summary = _characters[i].displayName + "のキャラクター";
                    }
                }

                ApplyDefaultCharacterSprites(_characters[i]);
            }
        }

        private void LoadProgress()
        {
            _selectedCharacterIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(SelectedCharacterPrefKey, 0),
                0,
                Mathf.Max(0, _characters.Count - 1));
            const float defaultBgmVolume = 0.2f;
            const float defaultSeVolume = 0.5f;
            _bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumePrefKey, defaultBgmVolume));
            _seVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SeVolumePrefKey, defaultSeVolume));
            if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                _languageCode = NormalizeLanguageCode(PlayerPrefs.GetString(LanguagePrefKey, "ja"));
            }
            else
            {
                _languageCode = GetInitialLanguageCode();
                PlayerPrefs.SetString(LanguagePrefKey, _languageCode);
                PlayerPrefs.Save();
            }
            ApplyAudioVolumes();

            _unlockedRewardKeys.Clear();
            var serializedRewards = PlayerPrefs.GetString(UnlockedRewardsPrefKey, string.Empty);
            if (string.IsNullOrEmpty(serializedRewards))
            {
                return;
            }

            var keys = serializedRewards.Split('|');
            for (var i = 0; i < keys.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(keys[i]))
                {
                    _unlockedRewardKeys.Add(keys[i]);
                }
            }
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(SelectedCharacterPrefKey, _selectedCharacterIndex);
            PlayerPrefs.SetFloat(BgmVolumePrefKey, _bgmVolume);
            PlayerPrefs.SetFloat(SeVolumePrefKey, _seVolume);
            PlayerPrefs.DeleteKey(LegacyMasterVolumePrefKey);
            PlayerPrefs.SetString(LanguagePrefKey, _languageCode);
            PlayerPrefs.SetString(UnlockedRewardsPrefKey, string.Join("|", _unlockedRewardKeys));
            PlayerPrefs.Save();
        }

        private void SetMenuPanelVisibility(bool showTitle, bool showCharacterSelect, bool showGallery, bool showOptions)
        {
            if (_titlePanel != null)
            {
                _titlePanel.SetActive(showTitle);
            }

            if (_characterSelectPanel != null)
            {
                _characterSelectPanel.SetActive(showCharacterSelect);
            }

            if (_galleryPanel != null)
            {
                _galleryPanel.SetActive(showGallery);
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.SetActive(showOptions);
            }
        }

        private void OpenCharacterSelect()
        {
            ShowCharacterSelect(CharacterSelectionMode.Play, false);
        }

        private void OpenGallery()
        {
            LoadProgress();
            _galleryCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
            ShowCharacterSelect(CharacterSelectionMode.Gallery, true);
        }

        private void ReturnToGalleryCharacterSelect()
        {
            HideGalleryRewardViewer();
            ShowCharacterSelect(CharacterSelectionMode.Gallery, true);
        }

        private void ShowCharacterSelect(CharacterSelectionMode mode, bool preserveGalleryCharacter)
        {
            _state = AppState.CharacterSelect;
            _characterSelectionMode = mode;
            if (mode == CharacterSelectionMode.Gallery)
            {
                var fallbackIndex = Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
                _galleryCharacterIndex = preserveGalleryCharacter
                    ? Mathf.Clamp(_galleryCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1))
                    : fallbackIndex;
            }

            HideConfirmationDialog();
            SetMenuPanelVisibility(false, true, false, false);
            _hudRoot.SetActive(false);
            _rewardOverlay.SetActive(false);
            _resultPopup.SetActive(false);
            ClearScorePopups();
            PlayMenuBgm();
            RefreshCharacterSelectPanel();
            ApplyLocalizedCharacterSelectCopy();
        }

        private void OpenGalleryForCharacter(int characterIndex)
        {
            LoadProgress();
            _galleryCharacterIndex = Mathf.Clamp(characterIndex, 0, Mathf.Max(0, _characters.Count - 1));
            _state = AppState.Gallery;
            HideConfirmationDialog();
            SetMenuPanelVisibility(false, false, true, false);
            _hudRoot.SetActive(false);
            _rewardOverlay.SetActive(false);
            _resultPopup.SetActive(false);
            ClearScorePopups();
            PlayMenuBgm();
            HideGalleryRewardViewer();
            RefreshGalleryPanel();
        }

        private void OpenGalleryRewardViewer(CharacterStageProfile stageCharacter)
        {
            if (stageCharacter == null || _galleryViewerOverlay == null || _galleryViewerImage == null)
            {
                return;
            }

            var sprite = stageCharacter.rewardSprite != null ? stageCharacter.rewardSprite : stageCharacter.portrait;
            if (sprite == null)
            {
                return;
            }

            _galleryViewerStageCharacter = stageCharacter;
            _galleryViewerPortraitVisible = stageCharacter.portrait != null;
            _galleryViewerOverlay.SetActive(true);
            _galleryViewerImage.sprite = sprite;
            _galleryViewerImage.color = Color.white;
            if (_galleryViewerPortraitImage != null)
            {
                _galleryViewerPortraitImage.sprite = stageCharacter.portrait;
                _galleryViewerPortraitImage.color = stageCharacter.portrait != null ? Color.white : Color.clear;
            }

            Canvas.ForceUpdateCanvases();
            LayoutGalleryViewer();
        }

        private void HideGalleryRewardViewer()
        {
            if (_galleryViewerOverlay != null)
            {
                _galleryViewerOverlay.SetActive(false);
            }

            _galleryViewerStageCharacter = null;
        }

        private void ToggleGalleryViewerPortrait()
        {
            if (_galleryViewerStageCharacter == null || _galleryViewerStageCharacter.portrait == null)
            {
                return;
            }

            _galleryViewerPortraitVisible = !_galleryViewerPortraitVisible;
            Canvas.ForceUpdateCanvases();
            LayoutGalleryViewer();
        }

        private void OpenOptions()
        {
            _state = AppState.Options;
            HideConfirmationDialog();
            SetMenuPanelVisibility(false, false, false, true);
            _hudRoot.SetActive(false);
            _rewardOverlay.SetActive(false);
            _resultPopup.SetActive(false);
            ClearScorePopups();
            PlayMenuBgm();
            RefreshOptionsPanel();
        }

        private void StartCampaignForCharacter(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= _characters.Count)
            {
                return;
            }

            _selectedCharacterIndex = characterIndex;
            SaveProgress();
            StartCampaign();
        }

        private void SelectGalleryCharacter(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex >= _characters.Count)
            {
                return;
            }

            _galleryCharacterIndex = characterIndex;
            OpenGalleryForCharacter(characterIndex);
        }

        private CharacterDefinition GetSelectedCharacter()
        {
            if (_characters.Count == 0)
            {
                InitializeCharacters();
                SanitizeCharacters();
            }

            _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
            return _characters[_selectedCharacterIndex];
        }

        private CharacterDefinition GetGalleryCharacter()
        {
            if (_characters.Count == 0)
            {
                InitializeCharacters();
                SanitizeCharacters();
            }

            var fallbackIndex = Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
            _galleryCharacterIndex = Mathf.Clamp(_galleryCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
            if (_galleryCharacterIndex < 0 || _galleryCharacterIndex >= _characters.Count)
            {
                _galleryCharacterIndex = fallbackIndex;
            }

            return _characters[_galleryCharacterIndex];
        }

        private CharacterStageProfile GetCurrentStageCharacter()
        {
            return GetStageCharacter(GetSelectedCharacter(), _currentStageIndex);
        }

        private CharacterStageProfile GetStageCharacter(CharacterDefinition character, int stageIndex)
        {
            if (character == null)
            {
                return new CharacterStageProfile();
            }

            if (character.stageCharacters == null || character.stageCharacters.Count == 0)
            {
                character.stageCharacters = CreateStageProfiles(character.id, character.displayName, Mathf.Max(1, _stages.Count));
            }

            var clampedIndex = Mathf.Clamp(stageIndex, 0, Mathf.Max(0, character.stageCharacters.Count - 1));
            var profile = character.stageCharacters[clampedIndex];
            if (profile == null)
            {
                profile = new CharacterStageProfile(
                    character.id + "_character_" + (clampedIndex + 1).ToString("00"),
                    GetStageCharacterName(character.displayName, clampedIndex),
                    character.displayName + "のキャラクター");
                character.stageCharacters[clampedIndex] = profile;
            }

            return profile;
        }

        private static string GetStageCharacterName(string groupName, int stageIndex)
        {
            const string labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var suffix = stageIndex >= 0 && stageIndex < labels.Length
                ? labels[stageIndex].ToString()
                : (stageIndex + 1).ToString();
            return groupName + "キャラ" + suffix;
        }

        private static string GetRewardLabel(CharacterStageProfile profile)
        {
            return profile == null || string.IsNullOrWhiteSpace(profile.displayName)
                ? "ご褒美CG"
                : profile.displayName + "のCG";
        }

        private int GetCharacterSelectionIndex()
        {
            return _characterSelectionMode == CharacterSelectionMode.Gallery
                ? Mathf.Clamp(_galleryCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1))
                : Mathf.Clamp(_selectedCharacterIndex, 0, Mathf.Max(0, _characters.Count - 1));
        }

        private void HandleCharacterSelection(int characterIndex)
        {
            if (_characterSelectionMode == CharacterSelectionMode.Gallery)
            {
                SelectGalleryCharacter(characterIndex);
                return;
            }

            StartCampaignForCharacter(characterIndex);
        }

        private void ApplyCharacterSelectCopy()
        {
            if (_characterSelectTitleText == null || _characterSelectStatusText == null)
            {
                return;
            }

            _characterSelectTitleText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? "ギャラリー"
                : "どのレベルであそぶ？";
            _characterSelectStatusText.text = string.Empty;
            _characterSelectStatusText.gameObject.SetActive(false);
        }

        private void RefreshMainMenuStatus()
        {
            if (_titleStatusText == null)
            {
                return;
            }

            _titleStatusText.text = string.Empty;
            _titleStatusText.gameObject.SetActive(false);
        }

        private void UnlockCurrentReward()
        {
            var selectedCharacter = GetSelectedCharacter();
            var rewardKey = GetRewardKey(selectedCharacter.id, _currentStageIndex);
            if (_unlockedRewardKeys.Add(rewardKey))
            {
                SaveProgress();
            }
        }

        private bool IsRewardUnlocked(CharacterDefinition character, int stageIndex)
        {
            return _unlockedRewardKeys.Contains(GetRewardKey(character.id, stageIndex));
        }

        private int CountUnlockedRewards(CharacterDefinition character)
        {
            var unlockedCount = 0;
            for (var stageIndex = 0; stageIndex < _stages.Count; stageIndex++)
            {
                if (IsRewardUnlocked(character, stageIndex))
                {
                    unlockedCount++;
                }
            }

            return unlockedCount;
        }

        private static string GetRewardKey(string characterId, int stageIndex)
        {
            return characterId + ":" + stageIndex;
        }

        private void SetBgmVolume(float value)
        {
            _bgmVolume = Mathf.Clamp01(value);
            ApplyAudioVolumes();
            SaveProgress();
            RefreshOptionsPanel();
        }

        private void SetSeVolume(float value)
        {
            _seVolume = Mathf.Clamp01(value);
            ApplyAudioVolumes();
            SaveProgress();
            RefreshOptionsPanel();
        }

        private void SetLanguageCode(string languageCode)
        {
            _languageCode = NormalizeLanguageCode(languageCode);
            SaveProgress();
            RefreshLocalizedUi();
        }

        private string GetLanguageLabel()
        {
            return _languageCode == "en" ? "English" : "日本語";
        }

        private string GetLocalizedLanguageName()
        {
            return _languageCode == "en" ? "English" : "\u65E5\u672C\u8A9E";
        }

        private void RefreshLocalizedUi()
        {
            RefreshLocalizedButtons();
            RefreshTitleLogoForLanguage();
            RefreshTitlePlatformButtons(true);
            RefreshMainMenuStatus();
            RefreshOptionsPanel();
            RefreshCharacterSelectPanel();
            ApplyLocalizedCharacterSelectCopy();
            RefreshGalleryPanel();

            if (_rewardPromptText != null)
            {
                _rewardPromptText.text = _languageCode == "en" ? "Click to continue" : "\u30AF\u30EA\u30C3\u30AF\u3057\u3066\u6B21\u3078";
            }

            if (_galleryViewerOverlay != null && _galleryViewerOverlay.activeSelf)
            {
                LayoutGalleryViewer();
            }

            if (_state == AppState.Playing || _state == AppState.Reward || _state == AppState.Result)
            {
                RefreshHud();
            }

            if (_state == AppState.Result && _resultPopup != null && _resultPopup.activeSelf)
            {
                _resultTitleText.text = _languageCode == "en" ? "Stage Failed" : "\u30B9\u30C6\u30FC\u30B8\u5931\u6557";
                _resultBodyText.text = GetStageFailureMessage(_currentScore, GetTargetScoreForStage(_currentStageIndex));
            }
        }

        private void ApplyLocalizedCharacterSelectCopy()
        {
            if (_characterSelectTitleText == null || _characterSelectStatusText == null)
            {
                return;
            }

            _characterSelectTitleText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? "Gallery"
                : (_languageCode == "en" ? "Which level will you play?" : "\u3069\u306E\u30EC\u30D9\u30EB\u3067\u3042\u305D\u3076\uFF1F");
            _characterSelectStatusText.text = string.Empty;
            _characterSelectStatusText.gameObject.SetActive(false);
        }

        private string GetLocalizedCharacterName(CharacterDefinition character)
        {
            if (character == null)
            {
                return _languageCode == "en" ? "Category" : "\u30AB\u30C6\u30B4\u30EA";
            }

            if (_languageCode != "en")
            {
                return character.displayName;
            }

            switch (character.id)
            {
                case "character_01":
                    return "Elementary";
                case "character_02":
                    return "Middle";
                case "character_03":
                    return "High";
                case "character_04":
                    return "Teacher";
                default:
                    return character.displayName;
            }
        }

        private string GetLocalizedStageCharacterName(CharacterDefinition character, CharacterStageProfile profile)
        {
            if (_languageCode != "en")
            {
                return profile != null ? profile.displayName : string.Empty;
            }

            const string labels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stageIndex = 0;
            if (character != null && profile != null && character.stageCharacters != null)
            {
                stageIndex = character.stageCharacters.IndexOf(profile);
            }

            if (stageIndex < 0 && profile != null && !string.IsNullOrWhiteSpace(profile.id))
            {
                var separator = profile.id.LastIndexOf('_');
                if (separator >= 0 && int.TryParse(profile.id.Substring(separator + 1), out var parsedStageIndex))
                {
                    stageIndex = Mathf.Max(0, parsedStageIndex - 1);
                }
                else
                {
                    stageIndex = 0;
                }
            }

            var suffix = stageIndex >= 0 && stageIndex < labels.Length
                ? labels[stageIndex].ToString()
                : (stageIndex + 1).ToString();
            return GetLocalizedCharacterName(character) + " " + suffix;
        }

        private string GetLocalizedGalleryTitle(CharacterDefinition character)
        {
            return _languageCode == "en"
                ? GetLocalizedCharacterName(character) + " Gallery"
                : GetLocalizedCharacterName(character) + "\u30AE\u30E3\u30E9\u30EA\u30FC";
        }

        private string GetLocalizedGalleryEmptyText(CharacterDefinition character)
        {
            return _languageCode == "en"
                ? "No CG has been unlocked for this level yet."
                : GetLocalizedCharacterName(character) + "\u306E\u89E3\u653E\u6E08\u307FCG\u306F\u307E\u3060\u3042\u308A\u307E\u305B\u3093\u3002";
        }

        private string GetStageFailureMessage(int currentScore, int targetScore)
        {
            if (_languageCode == "en")
            {
                return
                    "You did not reach the target score.\n\n" +
                    "Current Score: " + currentScore + "\n" +
                    "Target Score: " + targetScore + "\n\n" +
                    "Try again, or return to the top menu.";
            }

            return
                "\u76EE\u6A19\u30B9\u30B3\u30A2\u306B\u5C4A\u304D\u307E\u305B\u3093\u3067\u3057\u305F\u3002\n\n" +
                "\u73FE\u5728\u30B9\u30B3\u30A2: " + currentScore + "\n" +
                "\u76EE\u6A19\u30B9\u30B3\u30A2: " + targetScore + "\n\n" +
                "\u3082\u3046\u4E00\u5EA6\u6311\u6226\u3059\u308B\u304B\u3001TOP\u3078\u623B\u3063\u3066\u304F\u3060\u3055\u3044\u3002";
        }

        private void RequestReturnToTitleLocalized()
        {
            if (_state != AppState.Playing)
            {
                ReturnToTitle();
                return;
            }

            ShowConfirmationDialog(
                _languageCode == "en"
                    ? "Return to the main menu?"
                    : "\u30E1\u30CB\u30E5\u30FC\u753B\u9762\u306B\u623B\u308A\u307E\u3059",
                ReturnToTitle);
        }

        private void RequestQuit()
        {
            SaveProgress();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }
    }
}
