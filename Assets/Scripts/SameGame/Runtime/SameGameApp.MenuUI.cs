using UnityEngine;
using UnityEngine.UI;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private void BuildCharacterSelectPanel(Transform root)
        {
            _characterSelectPanel = CreatePanel("CharacterSelectPanel", root, new Color(0.04f, 0.05f, 0.09f, 0.95f)).GameObject;
            Stretch(_characterSelectPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplySharedMenuBackground(_characterSelectPanel, "CharacterSelectBackground");

            var card = CreatePanel("CharacterSelectCard", _characterSelectPanel.transform, new Color(0.12f, 0.14f, 0.21f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.975f), Vector2.zero, Vector2.zero);

            _characterSelectTitleText = CreateText("CharacterSelectTitle", card.GameObject.transform, "部門選択", 44, FontStyle.Bold);
            _characterSelectTitleText.alignment = TextAnchor.MiddleCenter;
            _characterSelectTitleText.color = Color.white;
            Stretch(_characterSelectTitleText.rectTransform, new Vector2(0.08f, 0.895f), new Vector2(0.92f, 0.965f), Vector2.zero, Vector2.zero);

            _characterSelectStatusText = CreateText("CharacterSelectStatus", card.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            _characterSelectStatusText.alignment = TextAnchor.MiddleCenter;
            _characterSelectStatusText.color = new Color(0.85f, 0.89f, 0.95f, 1f);
            Stretch(_characterSelectStatusText.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);
            _characterSelectStatusText.gameObject.SetActive(false);

            var gridObject = new GameObject("CharacterGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(card.GameObject.transform, false);
            _characterSelectionGrid = gridObject.GetComponent<RectTransform>();
            Stretch(_characterSelectionGrid, new Vector2(0.01f, 0.12f), new Vector2(0.99f, 0.885f), Vector2.zero, Vector2.zero);

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.spacing = new Vector2(6f, 6f);
            grid.cellSize = new Vector2(410f, 670f);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var backButton = CreateButton(card.GameObject.transform, "メインメニューへ戻る", new Color(0.31f, 0.36f, 0.47f, 1f));
            backButton.onClick.AddListener(ShowTitle);
            Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.34f, 0.025f), new Vector2(0.66f, 0.085f), Vector2.zero, Vector2.zero);
        }

        private void RefreshCharacterSelectPanel()
        {
            if (_characterSelectionGrid == null || _characterSelectStatusText == null || _characterSelectTitleText == null)
            {
                return;
            }

            ClearChildren(_characterSelectionGrid);

            var selectionIndex = GetCharacterSelectionIndex();
            var selectedCharacter = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? GetGalleryCharacter()
                : GetSelectedCharacter();

            _characterSelectTitleText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? "ギャラリー"
                : "部門選択";
            _characterSelectStatusText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? "閲覧する部門: " + selectedCharacter.displayName
                : "遊ぶ部門: " + selectedCharacter.displayName;

            for (var i = 0; i < _characters.Count; i++)
            {
                CreateCharacterThumbnailButton(_characterSelectionGrid, _characters[i], i == selectionIndex, i);
            }

            _characterSelectTitleText.text = _characterSelectionMode == CharacterSelectionMode.Gallery
                ? "Gallery"
                : (_languageCode == "en" ? "Which level will you play?" : "どのレベルであそぶ？");
            _characterSelectStatusText.text = string.Empty;
            _characterSelectStatusText.gameObject.SetActive(false);
        }

        private void CreateCharacterThumbnailButton(Transform parent, CharacterDefinition character, bool isSelected, int characterIndex)
        {
            var firstStageCharacter = GetStageCharacter(character, 0);

            var buttonObject = new GameObject("CharacterButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.15f, 1f);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = Color.Lerp(image.color, Color.white, 0.1f);
            colors.pressedColor = Color.Lerp(image.color, Color.black, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            if (isSelected)
            {
                var outline = buttonObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(3f, -3f);
                outline.effectColor = new Color(1f, 0.92f, 0.6f, 1f);
            }

            var stageOne = _stages.Count > 0 ? _stages[0] : null;
            var thumbnail = CreatePanel(
                "Thumbnail",
                buttonObject.transform,
                Color.Lerp(
                    stageOne != null ? stageOne.secondaryColor : character.secondaryColor,
                    character.secondaryColor,
                    0.55f));
            Stretch(thumbnail.RectTransform, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.985f), Vector2.zero, Vector2.zero);

            var frameOutline = thumbnail.GameObject.AddComponent<Outline>();
            frameOutline.effectDistance = new Vector2(3f, -3f);
            frameOutline.effectColor = Color.Lerp(
                stageOne != null ? stageOne.accentColor : character.accentColor,
                character.accentColor,
                0.5f);

            if (firstStageCharacter.portrait != null)
            {
                var portraitObject = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
                portraitObject.transform.SetParent(thumbnail.GameObject.transform, false);
                var portraitImage = portraitObject.GetComponent<Image>();
                portraitImage.sprite = firstStageCharacter.portrait;
                portraitImage.preserveAspect = true;
                portraitImage.color = Color.white;
                Stretch((RectTransform)portraitObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                var silhouette = CreatePanel(
                    "Silhouette",
                    thumbnail.GameObject.transform,
                    Color.Lerp(
                        stageOne != null ? stageOne.accentColor : character.accentColor,
                        character.accentColor,
                        0.6f));
                Stretch(silhouette.RectTransform, new Vector2(0.24f, 0.08f), new Vector2(0.76f, 0.74f), Vector2.zero, Vector2.zero);

                var shoulders = CreatePanel("Shoulders", thumbnail.GameObject.transform, character.secondaryColor);
                Stretch(shoulders.RectTransform, new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.48f), Vector2.zero, Vector2.zero);

                var head = CreatePanel("Head", thumbnail.GameObject.transform, Color.white);
                Stretch(head.RectTransform, new Vector2(0.34f, 0.64f), new Vector2(0.66f, 0.86f), Vector2.zero, Vector2.zero);
            }

            var namePlate = CreatePanel("NamePlate", buttonObject.transform, new Color(0.08f, 0.1f, 0.15f, 0.92f));
            Stretch(namePlate.RectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.105f), Vector2.zero, Vector2.zero);

            var name = CreateText("Name", namePlate.GameObject.transform, GetLocalizedCharacterName(character), 26, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = Color.white;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 20;
            name.resizeTextMaxSize = 26;
            Stretch(name.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            button.onClick.AddListener(delegate { HandleCharacterSelection(characterIndex); });
        }

        #if false
        private void BuildGalleryPanel(Transform root)
        {
            _galleryPanel = CreatePanel("GalleryPanel", root, new Color(0.04f, 0.05f, 0.09f, 0.95f)).GameObject;
            Stretch(_galleryPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = CreatePanel("GalleryCard", _galleryPanel.transform, new Color(0.12f, 0.14f, 0.21f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.14f, 0.08f), new Vector2(0.86f, 0.92f), Vector2.zero, Vector2.zero);

            _galleryTitleText = CreateText("GalleryTitle", card.GameObject.transform, "ギャラリー", 44, FontStyle.Bold);
            _galleryTitleText.alignment = TextAnchor.MiddleCenter;
            _galleryTitleText.color = Color.white;
            Stretch(_galleryTitleText.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            _galleryStatusText = CreateText("GalleryStatus", card.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            _galleryStatusText.alignment = TextAnchor.MiddleCenter;
            _galleryStatusText.color = new Color(0.85f, 0.89f, 0.95f, 1f);
            Stretch(_galleryStatusText.rectTransform, new Vector2(0.08f, 0.8f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero);

            var scrollRoot = new GameObject("GalleryScrollRoot", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollRoot.transform.SetParent(card.GameObject.transform, false);
            Stretch((RectTransform)scrollRoot.transform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);
            scrollRoot.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.92f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            Stretch(viewportRect, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            _galleryContent = (RectTransform)content.transform;
            _galleryContent.anchorMin = new Vector2(0f, 1f);
            _galleryContent.anchorMax = new Vector2(1f, 1f);
            _galleryContent.pivot = new Vector2(0.5f, 1f);
            _galleryContent.anchoredPosition = Vector2.zero;
            _galleryContent.sizeDelta = new Vector2(0f, 0f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _galleryScrollRect = scrollRoot.GetComponent<ScrollRect>();
            _galleryScrollRect.viewport = viewportRect;
            _galleryScrollRect.content = _galleryContent;
            _galleryScrollRect.horizontal = false;
            _galleryScrollRect.vertical = true;
            _galleryScrollRect.movementType = ScrollRect.MovementType.Clamped;

            _galleryEmptyText = CreateText("GalleryEmpty", card.GameObject.transform, "この部門で解放されたCGはまだありません。", 24, FontStyle.Bold);
            _galleryEmptyText.alignment = TextAnchor.MiddleCenter;
            _galleryEmptyText.color = new Color(1f, 0.92f, 0.72f, 1f);
            Stretch(_galleryEmptyText.rectTransform, new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.56f), Vector2.zero, Vector2.zero);

            var backButton = CreateButton(card.GameObject.transform, "部門選択へ戻る", new Color(0.31f, 0.36f, 0.47f, 1f));
            backButton.onClick.AddListener(ReturnToGalleryCharacterSelect);
            Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.33f, 0.05f), new Vector2(0.67f, 0.12f), Vector2.zero, Vector2.zero);
        }

        private void RefreshGalleryPanel()
        {
            if (_galleryContent == null || _galleryEmptyText == null || _galleryTitleText == null || _galleryStatusText == null)
            {
                return;
            }

            LoadProgress();
            ClearChildren(_galleryContent);

            var group = GetGalleryCharacter();
            var unlockedCount = 0;
            _galleryTitleText.text = group.displayName + " ギャラリー";

            for (var stageIndex = 0; stageIndex < _stages.Count; stageIndex++)
            {
                if (!IsRewardUnlocked(group, stageIndex))
                {
                    continue;
                }

                unlockedCount++;
                CreateGalleryEntryCard(_galleryContent, group, GetStageCharacter(group, stageIndex), _stages[stageIndex], stageIndex);
            }

            _galleryStatusText.text = "解放済みCG: " + unlockedCount + " / " + _stages.Count;
            _galleryEmptyText.text = group.displayName + " の解放済みCGはまだありません。";
            _galleryEmptyText.gameObject.SetActive(unlockedCount == 0);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_galleryContent);
            if (_galleryScrollRect != null)
            {
                _galleryScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void CreateGalleryEntryCard(
            Transform parent,
            CharacterDefinition group,
            CharacterStageProfile stageCharacter,
            StageDefinition stage,
            int stageIndex)
        {
            var entry = new GameObject("GalleryEntry", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            entry.transform.SetParent(parent, false);

            var image = entry.GetComponent<Image>();
            image.color = new Color(0.15f, 0.17f, 0.23f, 0.98f);

            var layout = entry.GetComponent<LayoutElement>();
            layout.minHeight = 168f;

            var accent = CreatePanel("Accent", entry.transform, Color.Lerp(group.accentColor, stage.accentColor, 0.45f));
            Stretch(accent.RectTransform, new Vector2(0f, 0f), new Vector2(0.03f, 1f), Vector2.zero, Vector2.zero);

            var preview = CreatePanel("Preview", entry.transform, Color.Lerp(stage.secondaryColor, group.secondaryColor, 0.45f));
            Stretch(preview.RectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.3f, 0.88f), Vector2.zero, Vector2.zero);

            var previewSprite = stageCharacter.rewardSprite != null ? stageCharacter.rewardSprite : stageCharacter.portrait;
            if (previewSprite != null)
            {
                var previewImageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
                previewImageObject.transform.SetParent(preview.GameObject.transform, false);
                var previewImage = previewImageObject.GetComponent<Image>();
                previewImage.sprite = previewSprite;
                previewImage.preserveAspect = true;
                previewImage.color = Color.white;
                Stretch((RectTransform)previewImageObject.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            }
            else
            {
                var previewLabel = CreateText("PreviewLabel", preview.GameObject.transform, "CG " + (stageIndex + 1), 28, FontStyle.Bold);
                previewLabel.alignment = TextAnchor.MiddleCenter;
                previewLabel.color = Color.white;
                Stretch(previewLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var title = CreateText("Title", entry.transform, GetRewardLabel(stageCharacter), 28, FontStyle.Bold);
            title.color = Color.white;
            Stretch(title.rectTransform, new Vector2(0.36f, 0.6f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);

            var body = CreateText("Body", entry.transform, stageCharacter.displayName + "\n" + stage.title, 20, FontStyle.Normal);
            body.color = new Color(0.83f, 0.87f, 0.94f, 1f);
            body.alignment = TextAnchor.UpperLeft;
            Stretch(body.rectTransform, new Vector2(0.36f, 0.22f), new Vector2(0.92f, 0.54f), Vector2.zero, Vector2.zero);
        }

        #endif

        private void BuildGalleryPanel(Transform root)
        {
            _galleryPanel = CreatePanel("GalleryPanel", root, new Color(0.04f, 0.05f, 0.09f, 0.95f)).GameObject;
            Stretch(_galleryPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplySharedMenuBackground(_galleryPanel, "GalleryBackground");

            var card = CreatePanel("GalleryCard", _galleryPanel.transform, new Color(0.12f, 0.14f, 0.21f, 0.76f));
            Stretch(card.RectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            _galleryTitleText = CreateText("GalleryTitle", card.GameObject.transform, "ギャラリー", 44, FontStyle.Bold);
            _galleryTitleText.alignment = TextAnchor.MiddleCenter;
            _galleryTitleText.color = Color.white;
            Stretch(_galleryTitleText.rectTransform, new Vector2(0.14f, 0.9f), new Vector2(0.86f, 0.97f), Vector2.zero, Vector2.zero);

            _galleryStatusText = CreateText("GalleryStatus", card.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            _galleryStatusText.gameObject.SetActive(false);
            Stretch(_galleryStatusText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            _galleryEmptyText = CreateText("GalleryEmpty", card.GameObject.transform, string.Empty, 24, FontStyle.Bold);
            _galleryEmptyText.gameObject.SetActive(false);
            Stretch(_galleryEmptyText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var closeButton = CreateIconButton("GalleryBackButton", card.GameObject.transform, new Color(0.14f, 0.17f, 0.24f, 0.95f));
            closeButton.onClick.AddListener(ReturnToGalleryCharacterSelect);
            Stretch(closeButton.GetComponent<RectTransform>(), new Vector2(0.935f, 0.905f), new Vector2(0.975f, 0.965f), Vector2.zero, Vector2.zero);

            var rowObject = new GameObject("GalleryRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(card.GameObject.transform, false);
            _galleryContent = (RectTransform)rowObject.transform;
            Stretch(_galleryContent, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);

            var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.padding = new RectOffset(0, 0, 0, 0);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            _galleryScrollRect = null;

            var viewerOverlay = CreatePanel("GalleryViewerOverlay", _galleryPanel.transform, new Color(0f, 0f, 0f, 0.96f));
            _galleryViewerOverlay = viewerOverlay.GameObject;
            Stretch(viewerOverlay.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dismissButton = _galleryViewerOverlay.AddComponent<Button>();
            dismissButton.onClick.AddListener(HideGalleryRewardViewer);

            var viewerViewport = CreatePanel("GalleryViewerViewport", _galleryViewerOverlay.transform, Color.black);
            viewerViewport.Image.raycastTarget = false;
            Stretch(viewerViewport.RectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            viewerViewport.GameObject.AddComponent<RectMask2D>();
            _galleryViewerViewport = viewerViewport.RectTransform;

            var viewerImageObject = new GameObject("GalleryViewerImage", typeof(RectTransform), typeof(Image));
            viewerImageObject.transform.SetParent(viewerViewport.GameObject.transform, false);
            _galleryViewerImage = viewerImageObject.GetComponent<Image>();
            _galleryViewerImage.raycastTarget = false;
            _galleryViewerImage.color = Color.white;
            _galleryViewerImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _galleryViewerImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _galleryViewerImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var viewerPortraitViewport = CreatePanel("GalleryViewerPortraitViewport", _galleryViewerOverlay.transform, new Color(0.95f, 0.96f, 0.98f, 1f));
            viewerPortraitViewport.Image.raycastTarget = false;
            Stretch(viewerPortraitViewport.RectTransform, new Vector2(0.74f, 0.02f), new Vector2(0.99f, 0.9f), Vector2.zero, Vector2.zero);
            _galleryViewerPortraitViewport = viewerPortraitViewport.RectTransform;

            var viewerPortraitImageObject = new GameObject("GalleryViewerPortraitImage", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            viewerPortraitImageObject.transform.SetParent(viewerPortraitViewport.GameObject.transform, false);
            _galleryViewerPortraitImage = viewerPortraitImageObject.GetComponent<Image>();
            _galleryViewerPortraitImage.raycastTarget = false;
            _galleryViewerPortraitImage.color = Color.clear;
            _galleryViewerPortraitImage.preserveAspect = true;
            Stretch(_galleryViewerPortraitImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var portraitFitter = viewerPortraitImageObject.GetComponent<AspectRatioFitter>();
            portraitFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            portraitFitter.aspectRatio = 0.56f;

            _galleryViewerPortraitToggleButton = CreateButton(_galleryViewerOverlay.transform, "立ち絵: ON", new Color(0.12f, 0.16f, 0.24f, 0.9f));
            _galleryViewerPortraitToggleButton.onClick.AddListener(ToggleGalleryViewerPortrait);
            Stretch(_galleryViewerPortraitToggleButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.91f), new Vector2(0.88f, 0.97f), Vector2.zero, Vector2.zero);
            _galleryViewerPortraitToggleText = _galleryViewerPortraitToggleButton.GetComponentInChildren<Text>(true);

            var viewerCloseButton = CreateIconButton("GalleryViewerCloseButton", _galleryViewerOverlay.transform, new Color(0f, 0f, 0f, 0.54f));
            viewerCloseButton.onClick.AddListener(HideGalleryRewardViewer);
            Stretch(viewerCloseButton.GetComponent<RectTransform>(), new Vector2(0.93f, 0.91f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);

            _galleryViewerOverlay.SetActive(false);
        }

        private void RefreshGalleryPanel()
        {
            if (_galleryContent == null || _galleryTitleText == null)
            {
                return;
            }

            LoadProgress();
            ClearChildren(_galleryContent);

            var group = GetGalleryCharacter();
            _galleryTitleText.text = group.displayName + "ギャラリー";

            if (_galleryStatusText != null)
            {
                _galleryStatusText.gameObject.SetActive(false);
            }

            if (_galleryEmptyText != null)
            {
                _galleryEmptyText.gameObject.SetActive(false);
            }

            var portraitCount = Mathf.Min(group.stageCharacters != null ? group.stageCharacters.Count : 0, _stages.Count);
            for (var stageIndex = 0; stageIndex < portraitCount; stageIndex++)
            {
                CreateGalleryPortraitButton(
                    _galleryContent,
                    group,
                    GetStageCharacter(group, stageIndex),
                    IsRewardUnlocked(group, stageIndex));
            }

            _galleryTitleText.text = GetLocalizedGalleryTitle(group);
            if (_galleryEmptyText != null)
            {
                _galleryEmptyText.text = GetLocalizedGalleryEmptyText(group);
            }

            HideGalleryRewardViewer();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_galleryContent);
        }

        private void CreateGalleryPortraitButton(
            Transform parent,
            CharacterDefinition group,
            CharacterStageProfile stageCharacter,
            bool unlocked)
        {
            var portraitButtonObject = new GameObject("GalleryPortrait", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            portraitButtonObject.transform.SetParent(parent, false);

            var layout = portraitButtonObject.GetComponent<LayoutElement>();
            layout.minWidth = 220f;
            layout.minHeight = 640f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;

            var background = portraitButtonObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);

            var button = portraitButtonObject.GetComponent<Button>();
            var buttonColors = button.colors;
            buttonColors.normalColor = background.color;
            buttonColors.highlightedColor = Color.Lerp(background.color, Color.white, 0.12f);
            buttonColors.pressedColor = Color.Lerp(background.color, Color.black, 0.14f);
            buttonColors.selectedColor = background.color;
            buttonColors.disabledColor = Color.Lerp(background.color, Color.black, 0.26f);
            button.colors = buttonColors;
            button.interactable = unlocked;
            if (unlocked)
            {
                button.onClick.AddListener(delegate { OpenGalleryRewardViewer(stageCharacter); });
            }

            var frame = CreatePanel("Frame", portraitButtonObject.transform, Color.Lerp(group.accentColor, group.secondaryColor, 0.4f));
            frame.Image.raycastTarget = false;
            Stretch(frame.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var portraitArea = CreatePanel("PortraitArea", portraitButtonObject.transform, new Color(0.97f, 0.97f, 0.98f, 1f));
            portraitArea.Image.raycastTarget = false;
            Stretch(portraitArea.RectTransform, new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f), Vector2.zero, Vector2.zero);

            if (stageCharacter != null && stageCharacter.portrait != null)
            {
                var portraitImageObject = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
                portraitImageObject.transform.SetParent(portraitArea.GameObject.transform, false);
                var portraitImage = portraitImageObject.GetComponent<Image>();
                portraitImage.sprite = stageCharacter.portrait;
                portraitImage.preserveAspect = true;
                portraitImage.color = Color.white;
                portraitImage.raycastTarget = false;
                Stretch((RectTransform)portraitImageObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            if (!unlocked)
            {
                var lockTint = CreatePanel("LockedTint", portraitButtonObject.transform, new Color(0f, 0f, 0f, 0.58f));
                lockTint.Image.raycastTarget = false;
                Stretch(lockTint.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        private Button CreateIconButton(string name, Transform parent, Color backgroundColor)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = backgroundColor;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.16f);
            colors.selectedColor = backgroundColor;
            colors.disabledColor = Color.Lerp(backgroundColor, Color.black, 0.3f);
            button.colors = colors;

            CreateIconBar(buttonObject.transform, 45f);
            CreateIconBar(buttonObject.transform, -45f);
            return button;
        }

        private void CreateIconBar(Transform parent, float angle)
        {
            var barObject = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barObject.transform.SetParent(parent, false);

            var barImage = barObject.GetComponent<Image>();
            barImage.color = Color.white;
            barImage.raycastTarget = false;

            var rectTransform = (RectTransform)barObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(26f, 4f);
            rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void LayoutGalleryViewer()
        {
            if (_galleryViewerImage == null || _galleryViewerViewport == null)
            {
                return;
            }

            var rewardSprite = _galleryViewerImage.sprite;
            var portraitSprite = _galleryViewerStageCharacter != null ? _galleryViewerStageCharacter.portrait : null;
            var hasPortrait = portraitSprite != null;
            var showPortrait = hasPortrait && _galleryViewerPortraitVisible;

            if (_galleryViewerPortraitToggleButton != null)
            {
                _galleryViewerPortraitToggleButton.gameObject.SetActive(hasPortrait);
            }

            if (_galleryViewerPortraitToggleText != null)
            {
                _galleryViewerPortraitToggleText.text = _languageCode == "en"
                    ? (_galleryViewerPortraitVisible ? "Portrait: ON" : "Portrait: OFF")
                    : (_galleryViewerPortraitVisible ? "立ち絵: ON" : "立ち絵: OFF");
            }

            if (_galleryViewerPortraitViewport != null)
            {
                _galleryViewerPortraitViewport.gameObject.SetActive(showPortrait);
            }

            if (_galleryViewerPortraitImage != null)
            {
                _galleryViewerPortraitImage.sprite = portraitSprite;
                _galleryViewerPortraitImage.color = showPortrait ? Color.white : Color.clear;

                var portraitFitter = _galleryViewerPortraitImage.GetComponent<AspectRatioFitter>();
                if (portraitFitter != null && portraitSprite != null && portraitSprite.rect.height > 0.5f)
                {
                    portraitFitter.aspectRatio = portraitSprite.rect.width / portraitSprite.rect.height;
                }
            }

            if (showPortrait)
            {
                Stretch(_galleryViewerViewport, new Vector2(0.01f, 0.02f), new Vector2(0.73f, 0.9f), Vector2.zero, Vector2.zero);
                if (_galleryViewerPortraitViewport != null)
                {
                    Stretch(_galleryViewerPortraitViewport, new Vector2(0.74f, 0.02f), new Vector2(0.99f, 0.9f), Vector2.zero, Vector2.zero);
                }
            }
            else
            {
                Stretch(_galleryViewerViewport, new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.9f), Vector2.zero, Vector2.zero);
            }

            var viewportSize = _galleryViewerViewport.rect.size;
            if (viewportSize.x <= 0.5f || viewportSize.y <= 0.5f)
            {
                return;
            }

            var imageRect = _galleryViewerImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;

            if (rewardSprite == null)
            {
                imageRect.sizeDelta = viewportSize;
                return;
            }

            var spriteAspect = rewardSprite.rect.width / Mathf.Max(1f, rewardSprite.rect.height);
            var viewportAspect = viewportSize.x / Mathf.Max(1f, viewportSize.y);
            float width;
            float height;

            if (!showPortrait)
            {
                if (spriteAspect > viewportAspect)
                {
                    width = viewportSize.x;
                    height = width / Mathf.Max(0.01f, spriteAspect);
                }
                else
                {
                    height = viewportSize.y;
                    width = height * spriteAspect;
                }
            }
            else
            {
                if (spriteAspect > viewportAspect)
                {
                    height = viewportSize.y;
                    width = height * spriteAspect;
                }
                else
                {
                    width = viewportSize.x;
                    height = width / Mathf.Max(0.01f, spriteAspect);
                }
            }

            imageRect.sizeDelta = new Vector2(width, height);
        }

        private void BuildOptionsPanel(Transform root)
        {
            _optionsPanel = CreatePanel("OptionsPanel", root, new Color(0.04f, 0.05f, 0.09f, 0.95f)).GameObject;
            Stretch(_optionsPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplySharedMenuBackground(_optionsPanel, "OptionsBackground");

            var card = CreatePanel("OptionsCard", _optionsPanel.transform, new Color(0.12f, 0.14f, 0.21f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.22f, 0.14f), new Vector2(0.78f, 0.86f), Vector2.zero, Vector2.zero);

            var title = CreateText("OptionsTitle", card.GameObject.transform, "オプション", 42, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            Stretch(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            _optionsTitleText = title;

            _optionsStatusText = CreateText("OptionsStatus", card.GameObject.transform, string.Empty, 20, FontStyle.Normal);
            _optionsStatusText.gameObject.SetActive(false);

            var bgmLabel = CreateText("BgmLabel", card.GameObject.transform, "BGM", 28, FontStyle.Bold);
            bgmLabel.color = Color.white;
            Stretch(bgmLabel.rectTransform, new Vector2(0.12f, 0.71f), new Vector2(0.3f, 0.79f), Vector2.zero, Vector2.zero);
            _bgmLabelText = bgmLabel;

            _bgmVolumeValueText = CreateText("BgmVolumeValue", card.GameObject.transform, string.Empty, 24, FontStyle.Bold);
            _bgmVolumeValueText.alignment = TextAnchor.MiddleRight;
            _bgmVolumeValueText.color = new Color(1f, 0.93f, 0.72f, 1f);
            Stretch(_bgmVolumeValueText.rectTransform, new Vector2(0.72f, 0.71f), new Vector2(0.88f, 0.79f), Vector2.zero, Vector2.zero);

            _bgmVolumeSlider = CreateSlider(card.GameObject.transform, "BgmVolumeSlider");
            Stretch(_bgmVolumeSlider.GetComponent<RectTransform>(), new Vector2(0.12f, 0.59f), new Vector2(0.88f, 0.67f), Vector2.zero, Vector2.zero);
            _bgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);

            var seLabel = CreateText("SeLabel", card.GameObject.transform, "SE", 28, FontStyle.Bold);
            seLabel.color = Color.white;
            Stretch(seLabel.rectTransform, new Vector2(0.12f, 0.47f), new Vector2(0.3f, 0.55f), Vector2.zero, Vector2.zero);
            _seLabelText = seLabel;

            _seVolumeValueText = CreateText("SeVolumeValue", card.GameObject.transform, string.Empty, 24, FontStyle.Bold);
            _seVolumeValueText.alignment = TextAnchor.MiddleRight;
            _seVolumeValueText.color = new Color(1f, 0.93f, 0.72f, 1f);
            Stretch(_seVolumeValueText.rectTransform, new Vector2(0.72f, 0.47f), new Vector2(0.88f, 0.55f), Vector2.zero, Vector2.zero);

            _seVolumeSlider = CreateSlider(card.GameObject.transform, "SeVolumeSlider");
            Stretch(_seVolumeSlider.GetComponent<RectTransform>(), new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.43f), Vector2.zero, Vector2.zero);
            _seVolumeSlider.onValueChanged.AddListener(SetSeVolume);

            var languageLabel = CreateText("LanguageLabel", card.GameObject.transform, "言語", 28, FontStyle.Bold);
            languageLabel.color = Color.white;
            Stretch(languageLabel.rectTransform, new Vector2(0.12f, 0.23f), new Vector2(0.3f, 0.31f), Vector2.zero, Vector2.zero);
            _languageLabelText = languageLabel;

            _languageValueText = CreateText("LanguageValue", card.GameObject.transform, string.Empty, 24, FontStyle.Bold);
            _languageValueText.alignment = TextAnchor.MiddleRight;
            _languageValueText.color = new Color(1f, 0.93f, 0.72f, 1f);
            Stretch(_languageValueText.rectTransform, new Vector2(0.5f, 0.23f), new Vector2(0.88f, 0.31f), Vector2.zero, Vector2.zero);

            var languageJaButton = CreateButton(card.GameObject.transform, "日本語", new Color(0.31f, 0.36f, 0.47f, 1f));
            languageJaButton.onClick.AddListener(delegate { SetLanguageCode("ja"); });
            Stretch(languageJaButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.14f), new Vector2(0.44f, 0.215f), Vector2.zero, Vector2.zero);

            var languageEnButton = CreateButton(card.GameObject.transform, "English", new Color(0.31f, 0.36f, 0.47f, 1f));
            languageEnButton.onClick.AddListener(delegate { SetLanguageCode("en"); });
            Stretch(languageEnButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.14f), new Vector2(0.88f, 0.215f), Vector2.zero, Vector2.zero);

            var backButton = CreateButton(card.GameObject.transform, "メインメニューへ戻る", new Color(0.84f, 0.31f, 0.39f, 1f));
            backButton.onClick.AddListener(ShowTitle);
            Stretch(backButton.GetComponent<RectTransform>(), new Vector2(0.24f, 0.015f), new Vector2(0.76f, 0.075f), Vector2.zero, Vector2.zero);
        }

        private void RefreshOptionsPanel()
        {
            if (_optionsStatusText == null || _bgmVolumeValueText == null || _seVolumeValueText == null || _languageValueText == null)
            {
                return;
            }

            _optionsStatusText.text = string.Empty;
            _bgmVolumeValueText.text = Mathf.RoundToInt(_bgmVolume * 100f) + "%";
            _seVolumeValueText.text = Mathf.RoundToInt(_seVolume * 100f) + "%";
            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.SetValueWithoutNotify(_bgmVolume);
            }

            if (_seVolumeSlider != null)
            {
                _seVolumeSlider.SetValueWithoutNotify(_seVolume);
            }

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

            _languageValueText.text = GetLocalizedLanguageName();
        }

        private Slider CreateSlider(Transform parent, string name)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            var sliderRect = sliderObject.GetComponent<RectTransform>();

            var background = CreatePanel("Background", sliderObject.transform, new Color(0.19f, 0.22f, 0.29f, 1f));
            Stretch(background.RectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), Vector2.zero, Vector2.zero);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            var fill = CreatePanel("Fill", fillArea.transform, new Color(0.95f, 0.67f, 0.29f, 1f));
            Stretch(fill.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObject.transform, false);
            var handleSlideAreaRect = handleSlideArea.GetComponent<RectTransform>();
            Stretch(handleSlideAreaRect, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            var handle = CreatePanel("Handle", handleSlideArea.transform, new Color(1f, 0.93f, 0.72f, 1f));
            handle.RectTransform.sizeDelta = new Vector2(26f, 26f);
            handle.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handle.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            handle.RectTransform.pivot = new Vector2(0.5f, 0.5f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fill.RectTransform;
            slider.handleRect = handle.RectTransform;
            slider.targetGraphic = handle.Image;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;

            return slider;
        }

        private void ClearChildren(Transform parent)
        {
            while (parent.childCount > 0)
            {
                var child = parent.GetChild(0);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
