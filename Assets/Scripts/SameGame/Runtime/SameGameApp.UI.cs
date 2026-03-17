using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const string TitleBackgroundResourcePath = "Images/Main_menu/main_bg";
        private const string TitleLogoResourcePath = "Title/Title_Logo";
        private const string EnglishTitleLogoResourcePath = "Title/Title_Logo_EN";
        private const string UiFontResourcePath = "Fonts/MPLUS1p-Regular";
        private const string LocalizedButtonResourceRoot = "Button";

        private readonly Dictionary<string, Sprite> _localizedButtonSpriteCache = new Dictionary<string, Sprite>();
        private readonly List<LocalizedButtonBinding> _localizedButtonBindings = new List<LocalizedButtonBinding>();
        private readonly List<BoardCellView> _boardCellViews = new List<BoardCellView>();
        private RectTransform _boardPlate;
        private Image _boardStillImage;

        private readonly struct UiElement
        {
            public UiElement(GameObject gameObject, RectTransform rectTransform, Image image)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                Image = image;
            }

            public GameObject GameObject { get; }

            public RectTransform RectTransform { get; }

            public Image Image { get; }
        }

        private sealed class LocalizedButtonBinding
        {
            public LocalizedButtonBinding(Button button, Image image, Text label, string assetKey, Color fallbackColor)
            {
                Button = button;
                Image = image;
                Label = label;
                AssetKey = assetKey;
                FallbackColor = fallbackColor;
            }

            public Button Button { get; }

            public Image Image { get; }

            public Text Label { get; }

            public string AssetKey { get; }

            public Color FallbackColor { get; }
        }

        private sealed class BoardCellView
        {
            public BoardCellView(GameObject gameObject, Image image, Button button, Outline outline, Image topHintBorder, Image bottomHintBorder, Image leftHintBorder, Image rightHintBorder)
            {
                GameObject = gameObject;
                Image = image;
                Button = button;
                Outline = outline;
                TopHintBorder = topHintBorder;
                BottomHintBorder = bottomHintBorder;
                LeftHintBorder = leftHintBorder;
                RightHintBorder = rightHintBorder;
            }

            public GameObject GameObject { get; }

            public Image Image { get; }

            public Button Button { get; }

            public Outline Outline { get; }

            public Image TopHintBorder { get; }

            public Image BottomHintBorder { get; }

            public Image LeftHintBorder { get; }

            public Image RightHintBorder { get; }

            public int BoundX { get; set; } = int.MinValue;

            public int BoundY { get; set; } = int.MinValue;

            public int LastRawColor { get; set; } = int.MinValue;

            public bool LastHighlighted { get; set; }

            public bool LastInteractable { get; set; }
        }

        private void EnsureUi()
        {
            EnsureEventSystem();
            _font = Resources.Load<Font>(UiFontResourcePath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("SameGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var backdrop = CreatePanel("Backdrop", canvasObject.transform, new Color(0.09f, 0.11f, 0.16f, 1f));
            Stretch(backdrop.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var layoutRootObject = new GameObject("LayoutRoot", typeof(RectTransform), typeof(AspectRatioFitter));
            layoutRootObject.transform.SetParent(canvasObject.transform, false);
            _layoutRoot = layoutRootObject.GetComponent<RectTransform>();
            Stretch(_layoutRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var aspectFitter = layoutRootObject.GetComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio = 16f / 9f;

            BuildTitleScreen(_layoutRoot);
            BuildCharacterSelectPanel(_layoutRoot);
            BuildGalleryPanel(_layoutRoot);
            BuildOptionsPanel(_layoutRoot);
            BuildHud(_layoutRoot);
            BuildRewardOverlay(_layoutRoot);
            BuildResultPopup(_layoutRoot);
            BuildScorePopupLayer(_layoutRoot);
            BuildConfirmationDialog(_layoutRoot);
            ApplyTitlePlatformOverrides();
            RefreshLocalizedUi();
        }

        private void BuildTitlePanel(Transform root)
        {
            _titlePanel = CreatePanel("TitlePanel", root, new Color(0.05f, 0.06f, 0.11f, 0.94f)).GameObject;
            Stretch(_titlePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TryCreateTitleBackground();

            var contentObject = new GameObject("TitleContent", typeof(RectTransform));
            contentObject.transform.SetParent(_titlePanel.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            var card = new UiElement(contentObject, contentRect, null);
            Stretch(contentRect, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.94f), Vector2.zero, Vector2.zero);

            var layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 28f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var title = CreateText("Title", card.GameObject.transform, "セクシーさめがめ", 54, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.gameObject.SetActive(false);
            TryCreateTitleLogo(contentRect);

            _titleStatusText = CreateText("Status", card.GameObject.transform, string.Empty, 26, FontStyle.Bold);
            _titleStatusText.alignment = TextAnchor.MiddleCenter;
            _titleStatusText.color = new Color(1f, 0.84f, 0.5f, 1f);
            _titleStatusText.gameObject.SetActive(false);

            var playButton = CreateButton(card.GameObject.transform, "あそぶ", new Color(0.94f, 0.36f, 0.53f, 1f));
            playButton.onClick.AddListener(OpenCharacterSelect);

            var galleryButton = CreateButton(card.GameObject.transform, "ギャラリー", new Color(0.95f, 0.67f, 0.29f, 1f));
            galleryButton.onClick.AddListener(OpenGallery);

            var optionsButton = CreateButton(card.GameObject.transform, "オプション", new Color(0.31f, 0.72f, 0.82f, 1f));
            optionsButton.onClick.AddListener(OpenOptions);

            var quitButton = CreateButton(card.GameObject.transform, "やめる", new Color(0.34f, 0.38f, 0.48f, 1f));
            quitButton.onClick.AddListener(RequestQuit);
        }

        private void BuildTitleScreen(Transform root)
        {
            _titlePanel = CreatePanel("TitlePanel", root, new Color(0.05f, 0.06f, 0.11f, 0.94f)).GameObject;
            Stretch(_titlePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TryCreateTitleBackground();

            CreateTitleLogoHero(_titlePanel.transform);

            _titleStatusText = CreateText("Status", _titlePanel.transform, string.Empty, 26, FontStyle.Bold);
            _titleStatusText.alignment = TextAnchor.MiddleCenter;
            _titleStatusText.color = new Color(1f, 0.84f, 0.5f, 1f);
            _titleStatusText.gameObject.SetActive(false);
            Stretch(_titleStatusText.rectTransform, new Vector2(0.25f, 0.78f), new Vector2(0.75f, 0.82f), Vector2.zero, Vector2.zero);

            var playButton = CreateButton(_titlePanel.transform, "あそぶ", new Color(0.94f, 0.36f, 0.53f, 1f));
            playButton.onClick.AddListener(OpenCharacterSelect);
            LayoutTitleButton(playButton, 508f);

            var galleryButton = CreateButton(_titlePanel.transform, "ギャラリー", new Color(0.95f, 0.67f, 0.29f, 1f));
            galleryButton.onClick.AddListener(OpenGallery);
            LayoutTitleButton(galleryButton, 624f);

            var optionsButton = CreateButton(_titlePanel.transform, "オプション", new Color(0.31f, 0.72f, 0.82f, 1f));
            optionsButton.onClick.AddListener(OpenOptions);
            LayoutTitleButton(optionsButton, 740f);

            var quitButton = CreateButton(_titlePanel.transform, "やめる", new Color(0.34f, 0.38f, 0.48f, 1f));
            quitButton.onClick.AddListener(RequestQuit);
            LayoutTitleButton(quitButton, 856f);
        }

        private void CreateTitleLogoHero(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var logoObject = new GameObject("TitleLogoHero", typeof(RectTransform), typeof(Image));
            logoObject.transform.SetParent(parent, false);

            var logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 1f);
            logoRect.anchorMax = new Vector2(0.5f, 1f);
            logoRect.pivot = new Vector2(0.5f, 1f);
            logoRect.anchoredPosition = new Vector2(0f, -58f);
            logoRect.sizeDelta = new Vector2(1080f, 413f);

            var logoImage = logoObject.GetComponent<Image>();
            ApplyTitleLogoSprite(logoImage);
        }

        private void ApplyTitlePlatformOverrides()
        {
            if (ShouldShowQuitButton() || _titlePanel == null)
            {
                return;
            }

            var buttons = _titlePanel.GetComponentsInChildren<Button>(true);
            Button bottomMostButton = null;
            var bottomMostY = float.PositiveInfinity;
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                var rect = button.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                var anchoredY = rect.anchoredPosition.y;
                if (anchoredY < bottomMostY)
                {
                    bottomMostY = anchoredY;
                    bottomMostButton = button;
                }
            }

            if (bottomMostButton != null)
            {
                bottomMostButton.gameObject.SetActive(false);
            }
        }

        private void LayoutTitleButton(Button button, float topOffset)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -topOffset);
            rect.sizeDelta = new Vector2(452f, 96f);
        }

        private static bool ShouldShowQuitButton()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }

        private void TryCreateTitleLogo(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var logoObject = new GameObject("TitleLogo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            logoObject.transform.SetParent(parent, false);

            var logoImage = logoObject.GetComponent<Image>();
            ApplyTitleLogoSprite(logoImage);

            var layout = logoObject.GetComponent<LayoutElement>();
            layout.minHeight = 540f;
            layout.preferredHeight = 540f;
            layout.flexibleWidth = 0f;
        }

        private void RefreshTitleLogoForLanguage()
        {
            if (_titlePanel == null)
            {
                return;
            }

            ApplyTitleLogoSprite(_titlePanel.transform.Find("TitleLogoHero")?.GetComponent<Image>());
            ApplyTitleLogoSprite(_titlePanel.transform.Find("TitleLogo")?.GetComponent<Image>());
        }

        private void ApplyTitleLogoSprite(Image logoImage)
        {
            if (logoImage == null)
            {
                return;
            }

            var texture = LoadTitleLogoTextureForLanguage();
            if (texture == null)
            {
                return;
            }

            logoImage.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
        }

        private Texture2D LoadTitleLogoTextureForLanguage()
        {
            var texture = Resources.Load<Texture2D>(_languageCode == "en" ? EnglishTitleLogoResourcePath : TitleLogoResourcePath);
            if (texture == null && _languageCode == "en")
            {
                texture = Resources.Load<Texture2D>(TitleLogoResourcePath);
            }

            return texture;
        }

        private void TryCreateTitleBackground()
        {
            if (_titlePanel == null)
            {
                return;
            }

            ApplySharedMenuBackground(_titlePanel, "TitleBackground");
        }

        private void ApplySharedMenuBackground(GameObject panel, string backgroundName)
        {
            if (panel == null)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>(TitleBackgroundResourcePath);
            if (texture == null)
            {
                return;
            }

            var overlay = panel.GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0.03f, 0.04f, 0.08f, 0.24f);
            }

            var backgroundObject = new GameObject(backgroundName, typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            backgroundObject.transform.SetParent(panel.transform, false);
            backgroundObject.transform.SetAsFirstSibling();

            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(texture.width, texture.height);

            var background = backgroundObject.GetComponent<RawImage>();
            background.texture = texture;
            background.color = Color.white;
            background.raycastTarget = false;

            var fitter = backgroundObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture.width / (float)texture.height;
        }

        private void BuildHud(Transform root)
        {
            _hudRoot = new GameObject("HudRoot", typeof(RectTransform));
            _hudRoot.transform.SetParent(root, false);
            var hudRect = (RectTransform)_hudRoot.transform;
            Stretch(hudRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildStageBackground(_hudRoot.transform);

            var hudBackdropTint = CreatePanel("HudBackdropTint", _hudRoot.transform, new Color(0.02f, 0.05f, 0.1f, 0.04f));
            hudBackdropTint.Image.raycastTarget = false;
            Stretch(hudBackdropTint.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var header = CreatePanel("Header", _hudRoot.transform, new Color(0.09f, 0.13f, 0.19f, 0.82f));
            Stretch(header.RectTransform, new Vector2(0.012f, 0.935f), new Vector2(0.988f, 0.992f), Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                header.Image,
                new Color(0.09f, 0.13f, 0.19f, 0.82f),
                new Color(0.72f, 0.83f, 0.96f, 0.08f),
                new Color(0f, 0.02f, 0.05f, 0.12f),
                new Vector2(0f, -2f));

            var boardShell = CreatePanel("BoardShell", _hudRoot.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(boardShell.RectTransform, new Vector2(0.008f, 0.015f), new Vector2(0.736f, 0.928f), Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                boardShell.Image,
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero);
            _boardShell = boardShell.RectTransform;

            var boardPlate = CreatePanel("BoardPlate", boardShell.GameObject.transform, new Color(0f, 0f, 0f, 0f));
            boardPlate.RectTransform.anchorMin = new Vector2(0.5f, 1f);
            boardPlate.RectTransform.anchorMax = new Vector2(0.5f, 1f);
            boardPlate.RectTransform.pivot = new Vector2(0.5f, 1f);
            boardPlate.RectTransform.anchoredPosition = new Vector2(0f, -30f);
            boardPlate.RectTransform.sizeDelta = new Vector2(996f, 676f);
            ApplyPanelChrome(
                boardPlate.Image,
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero);
            _boardPlate = boardPlate.RectTransform;

            var boardFrame = CreatePanel("BoardFrame", boardShell.GameObject.transform, new Color(0.063f, 0.094f, 0.125f, 1f));
            boardFrame.RectTransform.anchorMin = new Vector2(0.5f, 1f);
            boardFrame.RectTransform.anchorMax = new Vector2(0.5f, 1f);
            boardFrame.RectTransform.pivot = new Vector2(0.5f, 1f);
            boardFrame.RectTransform.anchoredPosition = new Vector2(0f, -28f);
            boardFrame.RectTransform.sizeDelta = new Vector2(960f, 640f);
            ApplyPanelChrome(
                boardFrame.Image,
                new Color(0.063f, 0.094f, 0.125f, 1f),
                new Color(0.76f, 0.86f, 0.96f, 0.12f),
                new Color(0f, 0.01f, 0.03f, 0.26f),
                new Vector2(0f, -5f));
            _boardFrame = boardFrame.RectTransform;

            var boardStillObject = new GameObject("BoardStillBackground", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            boardStillObject.transform.SetParent(_boardFrame, false);
            _boardStillImage = boardStillObject.GetComponent<Image>();
            _boardStillImage.raycastTarget = false;
            _boardStillImage.color = Color.clear;
            _boardStillImage.preserveAspect = false;
            Stretch(_boardStillImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var boardStillFitter = boardStillObject.GetComponent<AspectRatioFitter>();
            boardStillFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            boardStillFitter.aspectRatio = 1f;

            _boardFrame.gameObject.AddComponent<RectMask2D>();

            var boardStillShade = CreatePanel("BoardStillShade", _boardFrame, new Color(0f, 0f, 0f, 0.4f));
            boardStillShade.Image.raycastTarget = false;
            Stretch(boardStillShade.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var boardLabel = CreateText("BoardHelp", boardShell.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            boardLabel.gameObject.SetActive(false);
            _boardSummaryText = boardLabel;

            var boardGridObject = new GameObject("BoardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            boardGridObject.transform.SetParent(_boardFrame, false);
            var boardGridRect = (RectTransform)boardGridObject.transform;
            Stretch(boardGridRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _boardGrid = boardGridObject.GetComponent<GridLayoutGroup>();
            _boardGrid.spacing = new Vector2(2f, 2f);
            _boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _boardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _boardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _boardGrid.childAlignment = TextAnchor.UpperCenter;

            var characterShell = CreatePanel("CharacterShell", _hudRoot.transform, new Color(0f, 0f, 0f, 0f));
            characterShell.Image.raycastTarget = false;
            Stretch(characterShell.RectTransform, new Vector2(0.752f, 0.015f), new Vector2(0.995f, 0.928f), Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                characterShell.Image,
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero);
            DisableGraphicChrome(characterShell.Image);

            var characterArtFrame = CreatePanel("CharacterArtFrame", characterShell.GameObject.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(characterArtFrame.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                characterArtFrame.Image,
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero);
            DisableGraphicChrome(characterArtFrame.Image);

            var characterBackdrop = CreatePanel("CharacterBackdrop", characterArtFrame.GameObject.transform, new Color(0f, 0f, 0f, 0f));
            characterBackdrop.Image.raycastTarget = false;
            Stretch(characterBackdrop.RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            characterBackdrop.GameObject.SetActive(false);

            var characterImageObject = new GameObject("CharacterCard", typeof(RectTransform), typeof(Image));
            characterImageObject.transform.SetParent(characterArtFrame.GameObject.transform, false);
            _characterCardImage = characterImageObject.GetComponent<Image>();
            _characterCardImage.raycastTarget = false;
            Stretch(_characterCardImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _characterCardImage.preserveAspect = true;

            var portraitFitter = characterImageObject.AddComponent<AspectRatioFitter>();
            portraitFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            portraitFitter.aspectRatio = 0.56f;

            _characterHeaderText = CreateText("CharacterHeader", characterShell.GameObject.transform, "Character", 28, FontStyle.Bold);
            _characterHeaderText.alignment = TextAnchor.MiddleCenter;
            _characterHeaderText.color = Color.white;
            _characterHeaderText.raycastTarget = false;
            _characterHeaderText.gameObject.SetActive(false);
            Stretch(_characterHeaderText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            _characterBodyText = CreateText("CharacterBody", characterShell.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            _characterBodyText.alignment = TextAnchor.MiddleCenter;
            _characterBodyText.color = new Color(0.95f, 0.96f, 0.99f, 1f);
            _characterBodyText.raycastTarget = false;
            _characterBodyText.gameObject.SetActive(false);
            Stretch(_characterBodyText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var scorePanel = CreatePanel("ScorePanel", header.GameObject.transform, new Color(0.13f, 0.23f, 0.41f, 0.97f));
            Stretch(scorePanel.RectTransform, new Vector2(0.01f, 0.07f), new Vector2(0.29f, 0.93f), Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                scorePanel.Image,
                new Color(0.13f, 0.23f, 0.41f, 0.97f),
                new Color(0.85f, 0.93f, 1f, 0.26f),
                new Color(0f, 0.01f, 0.04f, 0.28f),
                new Vector2(0f, -4f));
            DisableGraphicChrome(scorePanel.Image);

            _scoreText = CreateText("Score", scorePanel.GameObject.transform, "0 / 0", 54, FontStyle.Bold);
            _scoreText.alignment = TextAnchor.MiddleLeft;
            _scoreText.color = new Color(0.96f, 0.98f, 1f, 1f);
            ApplyImportantTextChrome(_scoreText);
            Stretch(_scoreText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, -2f), new Vector2(-12f, 2f));

            var stagePanel = CreatePanel("StagePanel", header.GameObject.transform, new Color(0.16f, 0.24f, 0.4f, 0.97f));
            Stretch(stagePanel.RectTransform, new Vector2(0.79f, 0.07f), new Vector2(0.99f, 0.93f), Vector2.zero, Vector2.zero);
            ApplyPanelChrome(
                stagePanel.Image,
                new Color(0.16f, 0.24f, 0.4f, 0.97f),
                new Color(0.85f, 0.93f, 1f, 0.24f),
                new Color(0f, 0.01f, 0.04f, 0.28f),
                new Vector2(0f, -4f));
            DisableGraphicChrome(stagePanel.Image);

            _stageText = CreateText("Stage", stagePanel.GameObject.transform, "Stage 1", 52, FontStyle.Bold);
            _stageText.alignment = TextAnchor.MiddleCenter;
            _stageText.color = new Color(1f, 0.94f, 0.72f, 1f);
            ApplyImportantTextChrome(_stageText);
            Stretch(_stageText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, -2f), new Vector2(-10f, 2f));

            var buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(header.GameObject.transform, false);
            var buttonRowRect = (RectTransform)buttonRow.transform;
            Stretch(buttonRowRect, new Vector2(0.305f, 0.12f), new Vector2(0.775f, 0.88f), Vector2.zero, Vector2.zero);

            var buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 14f;
            buttonLayout.padding = new RectOffset(4, 4, 2, 2);
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childForceExpandHeight = true;

            var restartButton = CreateButton(buttonRow.transform, "ステージのはじめから", new Color(0.3f, 0.55f, 0.95f, 1f));
            restartButton.onClick.AddListener(RestartCurrentStage);
            ApplyHudButtonChrome(restartButton);
            SetButtonPreferredWidth(restartButton, 260f);

            var hintButton = CreateButton(buttonRow.transform, "ヒント", new Color(0.95f, 0.67f, 0.29f, 1f));
            hintButton.onClick.AddListener(ShowHint);
            ApplyHudButtonChrome(hintButton);
            SetButtonPreferredWidth(hintButton, 188f);

            var quitButton = CreateButton(buttonRow.transform, "あきらめる", new Color(0.84f, 0.31f, 0.39f, 1f));
            quitButton.onClick.AddListener(RequestReturnToTitleLocalized);
            ApplyHudButtonChrome(quitButton);
            SetButtonPreferredWidth(quitButton, 205f);
        }

        private void BuildRewardOverlay(Transform root)
        {
            _rewardOverlay = CreatePanel("RewardOverlay", root, Color.black).GameObject;
            Stretch(_rewardOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dismissButton = _rewardOverlay.AddComponent<Button>();
            dismissButton.onClick.AddListener(DismissRewardOverlay);

            var imageViewport = CreatePanel("RewardImageViewport", _rewardOverlay.transform, Color.black);
            imageViewport.Image.raycastTarget = false;
            Stretch(imageViewport.RectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
            imageViewport.GameObject.AddComponent<RectMask2D>();
            _rewardImageViewport = imageViewport.RectTransform;

            var rewardImageObject = new GameObject("RewardCard", typeof(RectTransform), typeof(Image));
            rewardImageObject.transform.SetParent(imageViewport.GameObject.transform, false);
            _rewardCardImage = rewardImageObject.GetComponent<Image>();
            _rewardCardImage.raycastTarget = false;
            _rewardCardImage.color = Color.white;
            _rewardCardImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rewardCardImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rewardCardImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var topBand = CreatePanel("RewardTopBand", _rewardOverlay.transform, new Color(0f, 0f, 0f, 0.58f));
            topBand.Image.raycastTarget = false;
            Stretch(topBand.RectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            _rewardTitleText = CreateText("RewardTitle", topBand.GameObject.transform, "STAGE CLEAR", 54, FontStyle.Bold);
            _rewardTitleText.alignment = TextAnchor.MiddleCenter;
            _rewardTitleText.color = Color.white;
            _rewardTitleText.raycastTarget = false;
            Stretch(_rewardTitleText.rectTransform, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f), Vector2.zero, Vector2.zero);

            _rewardBodyText = CreateText("RewardBody", _rewardOverlay.transform, string.Empty, 24, FontStyle.Normal);
            _rewardBodyText.alignment = TextAnchor.MiddleCenter;
            _rewardBodyText.color = new Color(0.95f, 0.97f, 1f, 1f);
            _rewardBodyText.raycastTarget = false;
            _rewardBodyText.gameObject.SetActive(false);
            Stretch(_rewardBodyText.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            var bottomBand = CreatePanel("RewardBottomBand", _rewardOverlay.transform, new Color(0f, 0f, 0f, 0.58f));
            bottomBand.Image.raycastTarget = false;
            Stretch(bottomBand.RectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);

            var prompt = CreateText("RewardPrompt", bottomBand.GameObject.transform, "クリックして次へ", 28, FontStyle.Bold);
            prompt.alignment = TextAnchor.MiddleRight;
            prompt.color = new Color(1f, 0.95f, 0.82f, 1f);
            prompt.raycastTarget = false;
            Stretch(prompt.rectTransform, new Vector2(0.54f, 0.06f), new Vector2(0.97f, 0.94f), Vector2.zero, Vector2.zero);
            _rewardPromptText = prompt;
            _rewardOverlay.SetActive(false);

#if false

            var card = CreatePanel("RewardCard", _rewardOverlay.transform, new Color(0.16f, 0.18f, 0.25f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.27f, 0.2f), new Vector2(0.73f, 0.8f), Vector2.zero, Vector2.zero);
            _rewardCardImage = card.Image;

            _rewardTitleText = CreateText("RewardTitle", card.GameObject.transform, "STAGE CLEAR", 46, FontStyle.Bold);
            _rewardTitleText.alignment = TextAnchor.UpperCenter;
            _rewardTitleText.color = Color.white;
            Stretch(_rewardTitleText.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            _rewardBodyText = CreateText("RewardBody", card.GameObject.transform, string.Empty, 24, FontStyle.Normal);
            _rewardBodyText.alignment = TextAnchor.MiddleCenter;
            _rewardBodyText.color = new Color(0.95f, 0.97f, 1f, 1f);
            Stretch(_rewardBodyText.rectTransform, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.66f), Vector2.zero, Vector2.zero);

            var prompt = CreateText("RewardPrompt", card.GameObject.transform, "クリックで次ステージへ", 22, FontStyle.Bold);
            prompt.alignment = TextAnchor.LowerCenter;
            prompt.color = new Color(1f, 0.93f, 0.66f, 1f);
            Stretch(prompt.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.16f), Vector2.zero, Vector2.zero);
#endif
        }

        private void LayoutRewardImage(Sprite sprite)
        {
            if (_rewardCardImage == null || _rewardImageViewport == null)
            {
                return;
            }

            var viewportSize = _rewardImageViewport.rect.size;
            if (viewportSize.x <= 0.5f || viewportSize.y <= 0.5f)
            {
                return;
            }

            var imageRect = _rewardCardImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;

            if (sprite == null)
            {
                imageRect.sizeDelta = viewportSize;
                return;
            }

            var spriteAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var viewportAspect = viewportSize.x / Mathf.Max(1f, viewportSize.y);
            float width;
            float height;

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

            imageRect.sizeDelta = new Vector2(width, height);
        }

        private void BuildResultPopup(Transform root)
        {
            _resultPopup = CreatePanel("ResultPopup", root, new Color(0.03f, 0.04f, 0.09f, 0.82f)).GameObject;
            Stretch(_resultPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var blocker = _resultPopup.GetComponent<Image>();
            blocker.color = new Color(0.03f, 0.04f, 0.09f, 0.82f);

            var card = CreatePanel("ResultCard", _resultPopup.transform, new Color(0.16f, 0.18f, 0.25f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.32f, 0.28f), new Vector2(0.68f, 0.72f), Vector2.zero, Vector2.zero);

            _resultTitleText = CreateText("ResultTitle", card.GameObject.transform, "FAILED", 42, FontStyle.Bold);
            _resultTitleText.alignment = TextAnchor.UpperCenter;
            _resultTitleText.color = Color.white;
            Stretch(_resultTitleText.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            _resultBodyText = CreateText("ResultBody", card.GameObject.transform, string.Empty, 22, FontStyle.Normal);
            _resultBodyText.alignment = TextAnchor.MiddleCenter;
            _resultBodyText.color = new Color(0.93f, 0.95f, 0.98f, 1f);
            Stretch(_resultBodyText.rectTransform, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.62f), Vector2.zero, Vector2.zero);

            var buttonRow = new GameObject("ResultButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(card.GameObject.transform, false);
            var buttonRowRect = (RectTransform)buttonRow.transform;
            Stretch(buttonRowRect, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.24f), Vector2.zero, Vector2.zero);

            var layout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var retryButton = CreateButton(buttonRow.transform, "もう一度やる", new Color(0.3f, 0.55f, 0.95f, 1f));
            retryButton.onClick.AddListener(RestartCurrentStage);

            var topButton = CreateButton(buttonRow.transform, "TOPへ戻る", new Color(0.84f, 0.31f, 0.39f, 1f));
            topButton.onClick.AddListener(ReturnToTitle);
            _resultPopup.SetActive(false);
        }

        private void BuildScorePopupLayer(Transform root)
        {
            var popupLayer = new GameObject("ScorePopupLayer", typeof(RectTransform));
            popupLayer.transform.SetParent(root, false);
            _scorePopupLayer = popupLayer.GetComponent<RectTransform>();
            Stretch(_scorePopupLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildConfirmationDialog(Transform root)
        {
            _confirmationDialog = CreatePanel("ConfirmationDialog", root, new Color(0.03f, 0.04f, 0.09f, 0.82f)).GameObject;
            Stretch(_confirmationDialog.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = CreatePanel("ConfirmationCard", _confirmationDialog.transform, new Color(0.14f, 0.16f, 0.23f, 0.98f));
            Stretch(card.RectTransform, new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f), Vector2.zero, Vector2.zero);

            _confirmationMessageText = CreateText("ConfirmationMessage", card.GameObject.transform, string.Empty, 30, FontStyle.Bold);
            _confirmationMessageText.alignment = TextAnchor.MiddleCenter;
            _confirmationMessageText.color = Color.white;
            Stretch(_confirmationMessageText.rectTransform, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.8f), Vector2.zero, Vector2.zero);

            var buttonRow = new GameObject("ConfirmationButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(card.GameObject.transform, false);
            var buttonRowRect = (RectTransform)buttonRow.transform;
            Stretch(buttonRowRect, new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);

            var layout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var okButton = CreateButton(buttonRow.transform, "OK", new Color(0.3f, 0.55f, 0.95f, 1f));
            okButton.onClick.AddListener(ConfirmConfirmationDialog);

            var cancelButton = CreateButton(buttonRow.transform, "キャンセル", new Color(0.34f, 0.38f, 0.48f, 1f));
            cancelButton.onClick.AddListener(HideConfirmationDialog);

            _confirmationDialog.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private void RefreshBoard()
        {
            if (_board == null)
            {
                ClearBoardCellPool();
                return;
            }

            var stage = GetCurrentStage();
            var totalCells = stage.board.width * stage.board.height;
            _boardGrid.constraintCount = stage.board.width;

            if (_boardCellViews.Count != totalCells || _boardGrid.cellSize.x <= 0.5f || _boardGrid.constraintCount != stage.board.width)
            {
                Canvas.ForceUpdateCanvases();
                LayoutBoardFrame(stage.board);
            }

            EnsureBoardCellPool(totalCells);

            var cellIndex = 0;
            for (var visualY = stage.board.height - 1; visualY >= 0; visualY--)
            {
                for (var x = 0; x < stage.board.width; x++)
                {
                    UpdateBoardCell(_boardCellViews[cellIndex], x, visualY);
                    cellIndex++;
                }
            }

            CacheLayoutState();
        }

        private void LayoutBoardFrame(BoardConfig config)
        {
            if (_boardShell == null || _boardFrame == null || _boardGrid == null || config == null)
            {
                return;
            }

            const float horizontalPadding = 2f;
            const float topPadding = 2f;
            const float bottomPadding = 2f;
            const float platePadding = 2f;
            var summaryHeight = _boardSummaryText != null && _boardSummaryText.gameObject.activeSelf ? 72f : 0f;

            var shellRect = _boardShell.rect;
            var availableWidth = Mathf.Max(1f, shellRect.width - (horizontalPadding * 2f));
            var availableHeight = Mathf.Max(1f, shellRect.height - topPadding - bottomPadding - summaryHeight);
            const float spacing = 1f;

            var cellWidth = (availableWidth - (spacing * (config.width - 1f))) / config.width;
            var cellHeight = (availableHeight - (spacing * (config.height - 1f))) / config.height;
            var cellSize = Mathf.Max(1f, Mathf.Floor(Mathf.Min(cellWidth, cellHeight)));

            var gridWidth = (cellSize * config.width) + (spacing * (config.width - 1f));
            var gridHeight = (cellSize * config.height) + (spacing * (config.height - 1f));
            var topOffset = topPadding + Mathf.Floor((availableHeight - gridHeight) * 0.5f);

            _boardGrid.spacing = new Vector2(spacing, spacing);
            _boardGrid.cellSize = new Vector2(cellSize, cellSize);
            _boardFrame.sizeDelta = new Vector2(gridWidth, gridHeight);
            _boardFrame.anchoredPosition = new Vector2(0f, -topOffset);

            if (_boardPlate != null)
            {
                _boardPlate.sizeDelta = new Vector2(gridWidth + (platePadding * 2f), gridHeight + (platePadding * 2f));
                _boardPlate.anchoredPosition = new Vector2(0f, -topOffset + platePadding);
            }
        }

        private void ShowScorePopup(int x, int y, int scoreDelta, bool includesAllClearBonus)
        {
            if (_scorePopupLayer == null || _boardFrame == null || _boardGrid == null || _board == null)
            {
                return;
            }

            var popupObject = new GameObject("ScorePopup", typeof(RectTransform), typeof(CanvasGroup));
            popupObject.transform.SetParent(_scorePopupLayer, false);

            var popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = GetScorePopupCanvasPosition(x, y);
            popupRect.sizeDelta = new Vector2(includesAllClearBonus ? 280f : 180f, 84f);
            popupRect.localScale = Vector3.one * 0.82f;

            var popupText = CreateText(
                "Value",
                popupRect,
                includesAllClearBonus ? "+" + scoreDelta + " BONUS" : "+" + scoreDelta,
                includesAllClearBonus ? 30 : 34,
                FontStyle.Bold);
            popupText.alignment = TextAnchor.MiddleCenter;
            popupText.color = Color.white;
            Stretch(popupText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var outline = popupObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.48f, 0.53f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            var canvasGroup = popupObject.GetComponent<CanvasGroup>();
            StartCoroutine(AnimateScorePopup(popupRect, canvasGroup));
        }

        private Vector2 GetScorePopupCanvasPosition(int x, int y)
        {
            var stage = GetCurrentStage();
            var cellSize = _boardGrid.cellSize;
            var spacing = _boardGrid.spacing;
            var stepX = cellSize.x + spacing.x;
            var stepY = cellSize.y + spacing.y;
            var localPoint = new Vector2(
                (-_boardFrame.rect.width * 0.5f) + (cellSize.x * 0.5f) + (x * stepX),
                (_boardFrame.rect.height * 0.5f) - (cellSize.y * 0.5f) - ((stage.board.height - 1 - y) * stepY));
            var worldPoint = _boardFrame.TransformPoint(localPoint);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPoint);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_scorePopupLayer, screenPoint, null, out var popupPosition);
            return popupPosition;
        }

        private IEnumerator AnimateScorePopup(RectTransform popupRect, CanvasGroup canvasGroup)
        {
            const float duration = 0.5f;
            var startPosition = popupRect.anchoredPosition;
            var endPosition = startPosition + new Vector2(0f, 42f);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (popupRect == null || canvasGroup == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                popupRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
                popupRect.localScale = Vector3.one * (t < 0.25f
                    ? Mathf.Lerp(0.82f, 1.08f, t / 0.25f)
                    : Mathf.Lerp(1.08f, 1f, (t - 0.25f) / 0.75f));
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            if (popupRect != null)
            {
                Destroy(popupRect.gameObject);
            }
        }

        private void ClearScorePopups()
        {
            if (_scorePopupLayer == null)
            {
                return;
            }

            foreach (Transform child in _scorePopupLayer)
            {
                Destroy(child.gameObject);
            }
        }

        private void EnsureBoardCellPool(int requiredCount)
        {
            if (_boardGrid == null)
            {
                return;
            }

            if (_boardCellViews.Count == requiredCount)
            {
                return;
            }

            ClearBoardCellPool();
            for (var i = 0; i < requiredCount; i++)
            {
                _boardCellViews.Add(CreateBoardCellView());
            }
        }

        private void ClearBoardCellPool()
        {
            for (var i = 0; i < _boardCellViews.Count; i++)
            {
                if (_boardCellViews[i].GameObject != null)
                {
                    Destroy(_boardCellViews[i].GameObject);
                }
            }

            _boardCellViews.Clear();
        }

        private BoardCellView CreateBoardCellView()
        {
            var cellObject = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button));
            cellObject.transform.SetParent(_boardGrid.transform, false);

            var buttonImage = cellObject.GetComponent<Image>();
            ApplyRoundedSprite(buttonImage);

            var button = cellObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var outline = GetOrAddComponent<Outline>(cellObject);
            outline.useGraphicAlpha = true;

            var topHintBorder = CreateHintBorder(cellObject.transform, "TopHintBorder", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -7f), Vector2.zero);
            var bottomHintBorder = CreateHintBorder(cellObject.transform, "BottomHintBorder", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 7f));
            var leftHintBorder = CreateHintBorder(cellObject.transform, "LeftHintBorder", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(7f, 0f));
            var rightHintBorder = CreateHintBorder(cellObject.transform, "RightHintBorder", new Vector2(1f, 0f), Vector2.one, new Vector2(-7f, 0f), Vector2.zero);

            return new BoardCellView(cellObject, buttonImage, button, outline, topHintBorder, bottomHintBorder, leftHintBorder, rightHintBorder);
        }

        private void UpdateBoardCell(BoardCellView cellView, int x, int y)
        {
            var rawColor = _board.GetCell(x, y);
            var isHighlighted = _highlightedCells.Contains(new Vector2Int(x, y));
            var isInteractable = rawColor >= 0;

            if (cellView.BoundX != x || cellView.BoundY != y)
            {
                cellView.Button.onClick.RemoveAllListeners();
                var localX = x;
                var localY = y;
                cellView.Button.onClick.AddListener(delegate { OnBlockPressed(localX, localY); });
                cellView.BoundX = x;
                cellView.BoundY = y;
            }

            if (cellView.LastRawColor != rawColor)
            {
                cellView.Image.color = GetBoardCellDisplayColor(rawColor);
                cellView.LastRawColor = rawColor;
            }

            if (cellView.LastInteractable != isInteractable)
            {
                cellView.Button.interactable = isInteractable;
                cellView.LastInteractable = isInteractable;
            }

            if (isInteractable)
            {
                if (isHighlighted)
                {
                    cellView.Outline.enabled = false;
                    UpdateHintBorders(cellView, x, y, true);
                }
                else
                {
                    UpdateHintBorders(cellView, x, y, false);
                    if (!cellView.Outline.enabled || cellView.LastHighlighted)
                    {
                        cellView.Outline.enabled = true;
                        cellView.Outline.effectColor = new Color(0.94f, 0.97f, 1f, 0.08f);
                        cellView.Outline.effectDistance = new Vector2(1f, -1f);
                    }
                }

                cellView.LastHighlighted = isHighlighted;
                return;
            }

            UpdateHintBorders(cellView, x, y, false);
            if (cellView.Outline.enabled)
            {
                cellView.Outline.enabled = false;
            }

            cellView.LastHighlighted = false;
        }

        private Image CreateHintBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var borderObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            borderObject.transform.SetParent(parent, false);

            var borderImage = borderObject.GetComponent<Image>();
            borderImage.color = new Color(1f, 0.18f, 0.22f, 0.95f);
            borderImage.raycastTarget = false;

            var rect = borderObject.GetComponent<RectTransform>();
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            borderObject.SetActive(false);
            return borderImage;
        }

        private void UpdateHintBorders(BoardCellView cellView, int x, int y, bool isHighlighted)
        {
            if (cellView == null)
            {
                return;
            }

            if (!isHighlighted)
            {
                SetHintBorderEnabled(cellView.TopHintBorder, false);
                SetHintBorderEnabled(cellView.BottomHintBorder, false);
                SetHintBorderEnabled(cellView.LeftHintBorder, false);
                SetHintBorderEnabled(cellView.RightHintBorder, false);
                return;
            }

            SetHintBorderEnabled(cellView.TopHintBorder, !_highlightedCells.Contains(new Vector2Int(x, y + 1)));
            SetHintBorderEnabled(cellView.BottomHintBorder, !_highlightedCells.Contains(new Vector2Int(x, y - 1)));
            SetHintBorderEnabled(cellView.LeftHintBorder, !_highlightedCells.Contains(new Vector2Int(x - 1, y)));
            SetHintBorderEnabled(cellView.RightHintBorder, !_highlightedCells.Contains(new Vector2Int(x + 1, y)));
        }

        private static void SetHintBorderEnabled(Image border, bool enabled)
        {
            if (border == null)
            {
                return;
            }

            if (border.gameObject.activeSelf != enabled)
            {
                border.gameObject.SetActive(enabled);
            }
        }

        private Color GetBoardCellDisplayColor(int rawColor)
        {
            var baseColor = rawColor < 0
                ? new Color(0.063f, 0.094f, 0.125f, 0.03f)
                : Color.Lerp(GetBlockColor(rawColor), new Color(0.45f, 0.49f, 0.58f, 1f), 0.12f);
            return baseColor;
        }

        private void ApplyRoundedSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
        }

        private void ApplyPanelChrome(Image image, Color fillColor, Color borderColor, Color shadowColor, Vector2 shadowDistance)
        {
            if (image == null)
            {
                return;
            }

            image.color = fillColor;
            ApplyRoundedSprite(image);

            var outline = GetOrAddComponent<Outline>(image.gameObject);
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            var shadow = GetOrAddComponent<Shadow>(image.gameObject);
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;
            shadow.useGraphicAlpha = true;
        }

        private void DisableGraphicChrome(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = false;

            var outline = image.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }

            var shadow = image.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.enabled = false;
            }
        }

        private void ApplyTextChrome(Text text, Color shadowColor, Vector2 shadowDistance)
        {
            if (text == null)
            {
                return;
            }

            var shadow = GetOrAddComponent<Shadow>(text.gameObject);
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;
            shadow.useGraphicAlpha = true;
        }

        private void ApplyImportantTextChrome(Text text)
        {
            if (text == null)
            {
                return;
            }

            var outline = GetOrAddComponent<Outline>(text.gameObject);
            outline.effectColor = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            ApplyTextChrome(text, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -2f));
        }

        private void ApplyHudButtonChrome(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                ApplyRoundedSprite(image);
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 70f;
                layout.preferredHeight = 70f;
            }

            var outline = GetOrAddComponent<Outline>(button.gameObject);
            outline.effectColor = new Color(0.95f, 0.98f, 1f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            var shadow = GetOrAddComponent<Shadow>(button.gameObject);
            shadow.effectColor = new Color(0f, 0.01f, 0.04f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -3f);
            shadow.useGraphicAlpha = true;

            var label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            ApplyTextChrome(label, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -2f));
        }

        private T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private UiElement CreatePanel(string name, Transform parent, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var image = panelObject.GetComponent<Image>();
            image.color = color;
            return new UiElement(panelObject, panelObject.GetComponent<RectTransform>(), image);
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;

            return text;
        }

        private Button CreateButton(Transform parent, string label, Color backgroundColor)
        {
            var buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = backgroundColor;

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.35f);
            button.colors = colors;

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 72f;
            layout.minWidth = 160f;

            var text = CreateText("Label", buttonObject.transform, label, 22, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));
            RegisterLocalizedButton(button, image, text, label, backgroundColor);

            return button;
        }

        private void RegisterLocalizedButton(Button button, Image image, Text label, string assetKey, Color fallbackColor)
        {
            if (button == null || image == null || label == null || string.IsNullOrWhiteSpace(assetKey))
            {
                return;
            }

            var binding = new LocalizedButtonBinding(button, image, label, assetKey, fallbackColor);
            _localizedButtonBindings.Add(binding);
            ApplyLocalizedButton(binding);
        }

        private void RefreshLocalizedButtons()
        {
            for (var i = 0; i < _localizedButtonBindings.Count; i++)
            {
                ApplyLocalizedButton(_localizedButtonBindings[i]);
            }
        }

        private void ApplyLocalizedButton(LocalizedButtonBinding binding)
        {
            var sprite = LoadLocalizedButtonSprite(binding.AssetKey);
            if (sprite != null)
            {
                binding.Image.sprite = sprite;
                binding.Image.type = Image.Type.Simple;
                binding.Image.preserveAspect = true;
                binding.Image.color = Color.white;
                binding.Image.rectTransform.localScale = Vector3.one * 0.95f;
                binding.Label.gameObject.SetActive(false);

                var colors = binding.Button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
                colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
                binding.Button.colors = colors;
                return;
            }

            binding.Image.sprite = null;
            binding.Image.type = Image.Type.Simple;
            binding.Image.preserveAspect = false;
            binding.Image.color = binding.FallbackColor;
            binding.Image.rectTransform.localScale = Vector3.one;
            binding.Label.text = GetLocalizedButtonFallbackLabel(binding.AssetKey);
            binding.Label.gameObject.SetActive(true);

            var fallbackColors = binding.Button.colors;
            fallbackColors.normalColor = binding.FallbackColor;
            fallbackColors.highlightedColor = Color.Lerp(binding.FallbackColor, Color.white, 0.15f);
            fallbackColors.pressedColor = Color.Lerp(binding.FallbackColor, Color.black, 0.2f);
            fallbackColors.selectedColor = fallbackColors.highlightedColor;
            fallbackColors.disabledColor = new Color(binding.FallbackColor.r, binding.FallbackColor.g, binding.FallbackColor.b, 0.35f);
            binding.Button.colors = fallbackColors;
        }

        private string GetLocalizedButtonFallbackLabel(string assetKey)
        {
            if (_languageCode != "en" || string.IsNullOrWhiteSpace(assetKey))
            {
                return assetKey;
            }

            switch (assetKey)
            {
                case "あそぶ":
                    return "Play";
                case "ギャラリー":
                    return "Gallery";
                case "オプション":
                    return "Options";
                case "やめる":
                    return "Quit";
                case "ステージのはじめから":
                    return "Restart Stage";
                case "ヒント":
                    return "Hint";
                case "あきらめる":
                    return "Give Up";
                case "メインメニューへ戻る":
                    return "Back to Main Menu";
                case "部門選択へ戻る":
                    return "Back to Level Select";
                case "もう一度やる":
                    return "Retry";
                case "TOPへ戻る":
                    return "Back to Top";
                case "キャンセル":
                    return "Cancel";
                case "立ち絵: ON":
                    return "Portrait: ON";
                case "立ち絵: OFF":
                    return "Portrait: OFF";
                case "日本語":
                    return "Japanese";
                default:
                    return assetKey;
            }
        }

        private Sprite LoadLocalizedButtonSprite(string assetKey)
        {
            var languageFolder = _languageCode == "en" ? "EN" : "JP";
            var resourcePath = LocalizedButtonResourceRoot + "/" + languageFolder + "/" + assetKey + "_" + languageFolder;
            if (_localizedButtonSpriteCache.TryGetValue(resourcePath, out var cachedSprite))
            {
                return cachedSprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                _localizedButtonSpriteCache[resourcePath] = null;
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = assetKey + "_" + languageFolder;
            _localizedButtonSpriteCache[resourcePath] = sprite;
            return sprite;
        }

        private void SetButtonPreferredWidth(Button button, float preferredWidth)
        {
            if (button == null)
            {
                return;
            }

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return;
            }

            layout.flexibleWidth = 0f;
            layout.preferredWidth = preferredWidth;
        }

        private void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
