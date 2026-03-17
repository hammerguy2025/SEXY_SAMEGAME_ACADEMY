using UnityEngine;
using UnityEngine.UI;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const string EnglishTitleTextObjectName = "TitleEnglishText";

        private void ApplyLanguageOverrides()
        {
            ApplyTitleLanguageOverrides();
            ApplyCharacterSelectLanguageOverrides();
            ApplyGalleryLanguageOverrides();
            ApplyOptionsLanguageOverrides();
            ApplyHudLanguageOverrides();
            ApplyRewardLanguageOverrides();
            ApplyResultLanguageOverrides();
            ApplyConfirmationLanguageOverrides();
        }

        private void ApplyTitleLanguageOverrides()
        {
            if (_titlePanel == null)
            {
                return;
            }

            var logoHero = _titlePanel.transform.Find("TitleLogoHero");
            var titleLogo = _titlePanel.transform.Find("TitleLogo");
            var englishTitle = EnsureEnglishTitleText();
            var useEnglishText = _languageCode == "en";

            if (logoHero != null)
            {
                logoHero.gameObject.SetActive(!useEnglishText);
            }

            if (titleLogo != null)
            {
                titleLogo.gameObject.SetActive(!useEnglishText);
            }

            if (englishTitle != null)
            {
                englishTitle.gameObject.SetActive(useEnglishText);
                englishTitle.text = "Sexy SameGame Academy";
            }
        }

        private Text EnsureEnglishTitleText()
        {
            if (_titlePanel == null)
            {
                return null;
            }

            var existing = _titlePanel.transform.Find(EnglishTitleTextObjectName);
            if (existing != null)
            {
                return existing.GetComponent<Text>();
            }

            var text = CreateText(EnglishTitleTextObjectName, _titlePanel.transform, "Sexy SameGame Academy", 92, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Stretch(text.rectTransform, new Vector2(0.16f, 0.64f), new Vector2(0.84f, 0.9f), Vector2.zero, Vector2.zero);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.08f, 0.18f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            text.gameObject.SetActive(false);
            return text;
        }

        private void ApplyCharacterSelectLanguageOverrides()
        {
            if (_characterSelectTitleText != null)
            {
                _characterSelectTitleText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                    ? "Gallery"
                    : (_languageCode == "en" ? "Which level will you play?" : "どのレベルであそぶ？");
            }

            if (_characterSelectStatusText != null)
            {
                _characterSelectStatusText.text = string.Empty;
                _characterSelectStatusText.gameObject.SetActive(false);
            }

            if (_characterSelectionGrid == null)
            {
                return;
            }

            for (var i = 0; i < _characterSelectionGrid.childCount && i < _characters.Count; i++)
            {
                var label = _characterSelectionGrid.GetChild(i).Find("NamePlate/Name");
                if (label == null)
                {
                    continue;
                }

                var text = label.GetComponent<Text>();
                if (text == null)
                {
                    continue;
                }

                text.text = GetDepartmentNameForLanguage(_characters[i]);
            }
        }

        private void ApplyGalleryLanguageOverrides()
        {
            if (_galleryTitleText != null)
            {
                _galleryTitleText.text = BuildGalleryTitleForLanguage(GetGalleryCharacter());
            }

            if (_galleryEmptyText != null)
            {
                _galleryEmptyText.text = BuildGalleryEmptyTextForLanguage(GetGalleryCharacter());
            }

            if (_galleryViewerPortraitToggleText != null)
            {
                _galleryViewerPortraitToggleText.text = _languageCode == "en"
                    ? (_galleryViewerPortraitVisible ? "Portrait: ON" : "Portrait: OFF")
                    : (_galleryViewerPortraitVisible ? "立ち絵: ON" : "立ち絵: OFF");
            }
        }

        private void ApplyOptionsLanguageOverrides()
        {
            if (_optionsTitleText != null)
            {
                _optionsTitleText.text = _languageCode == "en" ? "Options" : "オプション";
            }

            if (_bgmLabelText != null)
            {
                _bgmLabelText.text = "BGM";
            }

            if (_seLabelText != null)
            {
                _seLabelText.text = "SE";
            }

            if (_languageLabelText != null)
            {
                _languageLabelText.text = _languageCode == "en" ? "Language" : "言語";
            }

            if (_languageValueText != null)
            {
                _languageValueText.text = _languageCode == "en" ? "English" : "日本語";
            }
        }

        private void ApplyHudLanguageOverrides()
        {
            if (_state != AppState.Playing && _state != AppState.Reward && _state != AppState.Result)
            {
                return;
            }

            var character = GetSelectedCharacter();
            var stageCharacter = GetCurrentStageCharacter();

            if (_characterHeaderText != null)
            {
                _characterHeaderText.text = GetStageCharacterNameForLanguage(character, stageCharacter);
            }

            if (_characterBodyText != null)
            {
                _characterBodyText.text = GetDepartmentNameForLanguage(character);
            }
        }

        private void ApplyRewardLanguageOverrides()
        {
            if (_rewardPromptText != null)
            {
                _rewardPromptText.text = _languageCode == "en" ? "Click to continue" : "クリックして次へ";
            }
        }

        private void ApplyResultLanguageOverrides()
        {
            if (_state != AppState.Result || _resultPopup == null || !_resultPopup.activeSelf || _resultTitleText == null || _resultBodyText == null)
            {
                return;
            }

            _resultTitleText.text = _languageCode == "en" ? "Stage Failed" : "ステージ失敗";
            _resultBodyText.text = BuildFailureMessageForLanguage(_currentScore, GetTargetScoreForStage(_currentStageIndex));
        }

        private void ApplyConfirmationLanguageOverrides()
        {
            if (_confirmationDialog == null || !_confirmationDialog.activeSelf || _confirmationMessageText == null)
            {
                return;
            }

            _confirmationMessageText.text = _languageCode == "en"
                ? "Return to the main menu?"
                : "メニュー画面に戻ります";
        }

        private string GetDepartmentNameForLanguage(CharacterDefinition character)
        {
            if (character == null)
            {
                return _languageCode == "en" ? "Category" : "カテゴリ";
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

        private string GetStageCharacterNameForLanguage(CharacterDefinition character, CharacterStageProfile profile)
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

            if (stageIndex < 0)
            {
                stageIndex = 0;
            }

            var suffix = stageIndex < labels.Length ? labels[stageIndex].ToString() : (stageIndex + 1).ToString();
            return GetDepartmentNameForLanguage(character) + " " + suffix;
        }

        private string BuildGalleryTitleForLanguage(CharacterDefinition character)
        {
            return _languageCode == "en"
                ? GetDepartmentNameForLanguage(character) + " Gallery"
                : GetDepartmentNameForLanguage(character) + "ギャラリー";
        }

        private string BuildGalleryEmptyTextForLanguage(CharacterDefinition character)
        {
            return _languageCode == "en"
                ? "No CG has been unlocked for this level yet."
                : GetDepartmentNameForLanguage(character) + "の解放済みCGはまだありません。";
        }

        private string BuildFailureMessageForLanguage(int currentScore, int targetScore)
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
                "目標スコアに届きませんでした。\n\n" +
                "現在スコア: " + currentScore + "\n" +
                "目標スコア: " + targetScore + "\n\n" +
                "もう一度挑戦するか、TOPへ戻ってください。";
        }
    }
}
