using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp : MonoBehaviour
    {
        private const string DefaultStageBackgroundVideoPath = "Background/Stage/kirakira_heart_BG.mp4";
        private static readonly int[][] DepartmentTargetScoreTable =
        {
            new[] { 560, 620, 680, 740, 800 },
            new[] { 860, 920, 980, 1040, 1100 },
            new[] { 1160, 1220, 1270, 1310, 1350 },
            new[] { 1390, 1430, 1460, 1485, 1500 }
        };

        private enum AppState
        {
            Title,
            CharacterSelect,
            Gallery,
            Options,
            Playing,
            Reward,
            Result
        }

        [SerializeField] private List<StageDefinition> _stages = new List<StageDefinition>();

        private readonly HashSet<Vector2Int> _highlightedCells = new HashSet<Vector2Int>();

        private SameGameBoard _board;
        private AppState _state;
        private Font _font;
        private Coroutine _hintRoutine;
        private bool _campaignCompleted;
        private int _currentStageIndex;
        private int _currentStageSeed;
        private int _currentScore;
        private int _seedCounter;

        private Canvas _canvas;
        private RectTransform _layoutRoot;
        private GameObject _titlePanel;
        private Text _titleStatusText;
        private GameObject _hudRoot;
        private Text _scoreText;
        private Text _stageText;
        private Text _boardSummaryText;
        private Text _characterHeaderText;
        private Text _characterBodyText;
        private Image _characterCardImage;
        private RectTransform _boardShell;
        private RectTransform _boardFrame;
        private GridLayoutGroup _boardGrid;
        private RectTransform _scorePopupLayer;
        private GameObject _rewardOverlay;
        private Text _rewardTitleText;
        private Text _rewardBodyText;
        private Text _rewardPromptText;
        private Image _rewardCardImage;
        private RectTransform _rewardImageViewport;
        private GameObject _resultPopup;
        private Text _resultTitleText;
        private Text _resultBodyText;
        private GameObject _confirmationDialog;
        private Text _confirmationMessageText;
        private Action _confirmationAcceptAction;
        private Vector2 _lastBoardFrameSize;
        private int _lastScreenHeight;
        private int _lastScreenWidth;

        private void Awake()
        {
            var allApps = FindObjectsByType<SameGameApp>(FindObjectsSortMode.None);
            if (allApps.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            InitializeCharacters();
            InitializeStages();
            SanitizeCharacters();
            SanitizeStages();
            LoadProgress();
            InitializeWindowAspect();
            EnsureAudio();
            EnsureUi();
            ShowTitle();
        }

        private void Start()
        {
            ShowTitle();
        }

        private void Reset()
        {
            _characters.Clear();
            _stages.Clear();
            InitializeCharacters();
            InitializeStages();
        }

        private void Update()
        {
            EnforceWindowAspectRatio();

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsConfirmationDialogOpen())
                {
                    HideConfirmationDialog();
                    return;
                }

                if (_state == AppState.Playing)
                {
                    RequestReturnToTitleLocalized();
                    return;
                }

                if (_state != AppState.Title)
                {
                    ReturnToTitle();
                    return;
                }
            }

            if (_state == AppState.Playing && !IsConfirmationDialogOpen())
            {
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    RestartCurrentStage();
                    return;
                }

                if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
                {
                    ShowHint();
                    return;
                }
            }

            if (_state == AppState.Title || _state == AppState.CharacterSelect || _state == AppState.Gallery || _state == AppState.Options)
            {
                if (_hudRoot != null)
                {
                    _hudRoot.SetActive(false);
                }

                if (_rewardOverlay != null)
                {
                    _rewardOverlay.SetActive(false);
                }

                if (_resultPopup != null)
                {
                    _resultPopup.SetActive(false);
                }
            }

            RefreshBoardForLayoutChanges();
        }

        private void OnDestroy()
        {
            StopHintRoutine();
            ClearScorePopups();
            DisposeStageBackground();

            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }
        }

        private void InitializeStages()
        {
            if (_stages.Count > 0)
            {
                return;
            }

            _stages.Add(new StageDefinition(
                "Stage 1",
                new BoardConfig(15, 10, 4, 560),
                "Stage Character 1",
                "Reward CG 01",
                DefaultStageBgmClipKey,
                DefaultStageBackgroundVideoPath,
                new Color(0.96f, 0.48f, 0.61f, 1f),
                new Color(1f, 0.83f, 0.59f, 1f)));

            _stages.Add(new StageDefinition(
                "Stage 2",
                new BoardConfig(15, 10, 4, 620),
                "Stage Character 2",
                "Reward CG 02",
                DefaultStageBgmClipKey,
                DefaultStageBackgroundVideoPath,
                new Color(0.79f, 0.37f, 0.87f, 1f),
                new Color(0.45f, 0.74f, 1f, 1f)));

            _stages.Add(new StageDefinition(
                "Stage 3",
                new BoardConfig(15, 10, 4, 680),
                "Stage Character 3",
                "Reward CG 03",
                DefaultStageBgmClipKey,
                DefaultStageBackgroundVideoPath,
                new Color(0.31f, 0.72f, 0.82f, 1f),
                new Color(0.18f, 0.27f, 0.55f, 1f)));

            _stages.Add(new StageDefinition(
                "Stage 4",
                new BoardConfig(15, 10, 4, 740),
                "Stage Character 4",
                "Reward CG 04",
                DefaultStageBgmClipKey,
                DefaultStageBackgroundVideoPath,
                new Color(0.97f, 0.59f, 0.33f, 1f),
                new Color(0.87f, 0.25f, 0.33f, 1f)));

            _stages.Add(new StageDefinition(
                "Final Stage",
                new BoardConfig(15, 10, 4, 800),
                "Stage Character 5",
                "Reward CG Final",
                DefaultStageBgmClipKey,
                DefaultStageBackgroundVideoPath,
                new Color(0.95f, 0.35f, 0.52f, 1f),
                new Color(0.26f, 0.12f, 0.41f, 1f)));
        }

        private void StartCampaign()
        {
            _campaignCompleted = false;
            StartStage(0, false);
        }

        private void StartStage(int stageIndex, bool reuseSeed)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Count)
            {
                return;
            }

            _currentStageIndex = stageIndex;
            if (!reuseSeed)
            {
                _currentStageSeed = GenerateStageSeed(stageIndex);
            }

            _currentScore = 0;
            _highlightedCells.Clear();
            StopHintRoutine();
            HideConfirmationDialog();
            ClearScorePopups();
            _board = SameGameBoard.CreateRandom(_stages[stageIndex].board, _currentStageSeed);

            _state = AppState.Playing;
            SetMenuPanelVisibility(false, false, false, false);
            _hudRoot.SetActive(true);
            _rewardOverlay.SetActive(false);
            _resultPopup.SetActive(false);
            PlayStageBgm(_stages[stageIndex]);
            PlayStageBackground(_stages[stageIndex]);
            Canvas.ForceUpdateCanvases();
            LayoutBoardFrame(_stages[stageIndex].board);
            CacheLayoutState();
            RefreshAll();
        }

        private void RestartCurrentStage()
        {
            if (_state == AppState.Title)
            {
                StartCampaign();
                return;
            }

            StartStage(_currentStageIndex, false);
        }

        private void ReturnToTitle()
        {
            StopHintRoutine();
            HideConfirmationDialog();
            ShowTitle();
        }

        private void ShowTitle()
        {
            _state = AppState.Title;
            HideConfirmationDialog();
            SetMenuPanelVisibility(true, false, false, false);
            _hudRoot.SetActive(false);
            _rewardOverlay.SetActive(false);
            _resultPopup.SetActive(false);
            ClearScorePopups();
            StopStageBackground();
            PlayMenuBgm();
            RefreshMainMenuStatus();
            CacheLayoutState();
        }

        private void ShowHint()
        {
            if (_state != AppState.Playing || _board == null)
            {
                return;
            }

            StopHintRoutine();
            _hintRoutine = StartCoroutine(HintRoutine());
        }

        private IEnumerator HintRoutine()
        {
            _highlightedCells.Clear();
            var groups = _board.GetRemovableGroups();
            if (groups.Count == 0)
            {
                _hintRoutine = null;
                yield break;
            }

            var bestGroup = groups[0];
            for (var i = 1; i < groups.Count; i++)
            {
                if (groups[i].Count > bestGroup.Count)
                {
                    bestGroup = groups[i];
                }
            }

            for (var j = 0; j < bestGroup.Count; j++)
            {
                _highlightedCells.Add(bestGroup[j]);
            }

            RefreshBoard();
            yield return new WaitForSeconds(1f);
            _highlightedCells.Clear();
            RefreshBoard();
            _hintRoutine = null;
        }

        private void StopHintRoutine()
        {
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }

            _highlightedCells.Clear();
        }

        private void OnBlockPressed(int x, int y)
        {
            if (_state != AppState.Playing || _board == null)
            {
                return;
            }

            var move = _board.TryRemoveGroup(x, y);
            if (!move.RemovedAny)
            {
                return;
            }

            var clearedBoard = _board.CountOccupiedCells() == 0;
            var scoreDelta = SameGameBoard.CalculateTurnScore(move.RemovedCount, clearedBoard);
            _currentScore += scoreDelta;
            StopHintRoutine();
            PlayBlockClearSe(scoreDelta);
            ShowScorePopup(x, y, scoreDelta, clearedBoard);
            RefreshAll();

            if (!_board.HasMoves())
            {
                EvaluateStageEndLocalized();
            }
        }

        private void EvaluateStageEnd()
        {
            var stage = GetCurrentStage();
            var targetScore = GetTargetScoreForStage(_currentStageIndex);
            if (_currentScore >= targetScore)
            {
                UnlockCurrentReward();
                ShowReward();
                return;
            }

            _state = AppState.Result;
            _resultPopup.SetActive(true);
            PlayStageFailSe();
            _resultTitleText.text = "ステージ失敗";
            _resultBodyText.text =
                "目標スコアに届きませんでした。\n\n" +
                "現在スコア: " + _currentScore + "\n" +
                "目標スコア: " + targetScore + "\n\n" +
                "もう一度挑戦するか、TOPへ戻ってください。";
        }

        private void ShowReward()
        {
            var stage = GetCurrentStage();
            var group = GetSelectedCharacter();
            var stageCharacter = GetCurrentStageCharacter();
            var rewardSprite = stageCharacter.rewardSprite != null ? stageCharacter.rewardSprite : stageCharacter.portrait;
            _state = AppState.Reward;
            HideConfirmationDialog();
            _rewardOverlay.SetActive(true);
            _rewardCardImage.sprite = rewardSprite;
            _rewardCardImage.color = rewardSprite != null
                ? Color.white
                : Color.Lerp(stage.accentColor, group.accentColor, 0.5f);
            Canvas.ForceUpdateCanvases();
            LayoutRewardImage(rewardSprite);
            PlayStageClearSe();
            _rewardTitleText.text = "STAGE CLEAR";
            _rewardBodyText.text = string.Empty;
#if false
                "現在スコア: " + _currentScore;
        #endif
        }

        private void EvaluateStageEndLocalized()
        {
            var targetScore = GetTargetScoreForStage(_currentStageIndex);
            if (_currentScore >= targetScore)
            {
                UnlockCurrentReward();
                ShowReward();
                return;
            }

            _state = AppState.Result;
            _resultPopup.SetActive(true);
            PlayStageFailSe();
            ShowLocalizedStageFailure(targetScore);
        }

        private void ShowLocalizedStageFailure(int targetScore)
        {
            if (_resultTitleText == null || _resultBodyText == null)
            {
                return;
            }

            _resultTitleText.text = _languageCode == "en" ? "Stage Failed" : "ステージ失敗";
            _resultBodyText.text = GetStageFailureMessage(_currentScore, targetScore);
            _resultTitleText.text = _languageCode == "en" ? "Stage Failed" : "\u30B9\u30C6\u30FC\u30B8\u5931\u6557";
        }

        private void DismissRewardOverlay()
        {
            if (_state != AppState.Reward)
            {
                return;
            }

            _rewardOverlay.SetActive(false);
            var nextStage = _currentStageIndex + 1;
            if (nextStage >= _stages.Count)
            {
                _campaignCompleted = true;
                ShowTitle();
                return;
            }

            StartStage(nextStage, false);
        }

        private void RefreshAll()
        {
            RefreshHud();
            RefreshBoard();
        }

        private void RefreshHud()
        {
            var stage = GetCurrentStage();
            var group = GetSelectedCharacter();
            var stageCharacter = GetCurrentStageCharacter();
            _scoreText.text = _currentScore + " / " + GetTargetScoreForStage(_currentStageIndex);
            _stageText.text = stage.title;
            if (_boardSummaryText != null)
            {
                _boardSummaryText.text = string.Empty;
            }

            var boardStillSprite = stageCharacter.rewardSprite != null ? stageCharacter.rewardSprite : stageCharacter.portrait;
            if (_boardStillImage != null)
            {
                _boardStillImage.sprite = boardStillSprite;
                _boardStillImage.color = boardStillSprite != null ? Color.white : Color.clear;

                var boardStillFitter = _boardStillImage.GetComponent<AspectRatioFitter>();
                if (boardStillFitter != null && boardStillSprite != null && boardStillSprite.rect.height > 0.5f)
                {
                    boardStillFitter.aspectRatio = boardStillSprite.rect.width / boardStillSprite.rect.height;
                }
            }

            _characterCardImage.sprite = stageCharacter.portrait;
            _characterCardImage.preserveAspect = true;
            var portraitFitter = _characterCardImage.GetComponent<AspectRatioFitter>();
            if (portraitFitter != null && stageCharacter.portrait != null && stageCharacter.portrait.rect.height > 0.5f)
            {
                portraitFitter.aspectRatio = stageCharacter.portrait.rect.width / stageCharacter.portrait.rect.height;
            }
            _characterCardImage.color = stageCharacter.portrait != null
                ? Color.white
                : Color.Lerp(stage.secondaryColor, group.secondaryColor, 0.45f);
            _characterHeaderText.text = stageCharacter.displayName;
            _characterBodyText.text = group.displayName;
            _characterHeaderText.text = GetLocalizedStageCharacterName(group, stageCharacter);
            _characterBodyText.text = GetLocalizedCharacterName(group);
        }

        private StageDefinition GetCurrentStage()
        {
            return _stages[_currentStageIndex];
        }

        private void SanitizeStages()
        {
            if (_stages.Count == 0)
            {
                InitializeStages();
            }

            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] == null)
                {
                    _stages[i] = new StageDefinition();
                }

                if (_stages[i].board == null)
                {
                    _stages[i].board = new BoardConfig();
                }

                if (string.IsNullOrWhiteSpace(_stages[i].backgroundVideoPath))
                {
                    _stages[i].backgroundVideoPath = DefaultStageBackgroundVideoPath;
                }
            }
        }

        private void RefreshBoardForLayoutChanges()
        {
            if (_board == null || _boardShell == null || _boardFrame == null || !_hudRoot.activeInHierarchy)
            {
                return;
            }

            var currentSize = _boardShell.rect.size;
            var sizeChanged = Mathf.Abs(currentSize.x - _lastBoardFrameSize.x) > 0.5f ||
                Mathf.Abs(currentSize.y - _lastBoardFrameSize.y) > 0.5f;
            var resolutionChanged = _lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height;
            if (!sizeChanged && !resolutionChanged)
            {
                return;
            }

            var stage = GetCurrentStage();
            if (stage == null || stage.board == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutBoardFrame(stage.board);
            CacheLayoutState();
            if (_state == AppState.Reward && _rewardOverlay != null && _rewardOverlay.activeInHierarchy)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRewardImage(_rewardCardImage != null ? _rewardCardImage.sprite : null);
            }
        }

        private void CacheLayoutState()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastBoardFrameSize = _boardShell != null ? _boardShell.rect.size : Vector2.zero;
        }

        private int GenerateStageSeed(int stageIndex)
        {
            _seedCounter++;
            return unchecked(Environment.TickCount ^ (stageIndex * 7919) ^ (_seedCounter * 104729));
        }

        private int GetTargetScoreForStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Count)
            {
                return 0;
            }

            var departmentIndex = Mathf.Clamp(_selectedCharacterIndex, 0, DepartmentTargetScoreTable.Length - 1);
            var departmentScores = DepartmentTargetScoreTable[departmentIndex];
            if (stageIndex >= 0 && stageIndex < departmentScores.Length)
            {
                return departmentScores[stageIndex];
            }

            return _stages[stageIndex].board != null ? _stages[stageIndex].board.targetScore : 0;
        }

        private Color GetBlockColor(int colorIndex)
        {
            switch (colorIndex % 4)
            {
                case 0:
                    return new Color(0.29f, 0.67f, 0.95f, 1f);
                case 1:
                    return new Color(0.35f, 0.84f, 0.56f, 1f);
                case 2:
                    return new Color(0.96f, 0.77f, 0.28f, 1f);
                default:
                    return new Color(0.71f, 0.54f, 0.95f, 1f);
            }
        }

        private void RequestReturnToTitle()
        {
            if (_state != AppState.Playing)
            {
                ReturnToTitle();
                return;
            }

            ShowConfirmationDialog(_languageCode == "en" ? "Return to the main menu?" : "メニュー画面に戻ります", ReturnToTitle);
        }

        private bool IsConfirmationDialogOpen()
        {
            return _confirmationDialog != null && _confirmationDialog.activeSelf;
        }

        private void ShowConfirmationDialog(string message, Action onConfirm)
        {
            if (_confirmationDialog == null || _confirmationMessageText == null)
            {
                onConfirm?.Invoke();
                return;
            }

            _confirmationAcceptAction = onConfirm;
            _confirmationMessageText.text = message;
            _confirmationDialog.SetActive(true);
        }

        private void ConfirmConfirmationDialog()
        {
            var acceptAction = _confirmationAcceptAction;
            HideConfirmationDialog();
            acceptAction?.Invoke();
        }

        private void HideConfirmationDialog()
        {
            _confirmationAcceptAction = null;
            if (_confirmationDialog != null)
            {
                _confirmationDialog.SetActive(false);
            }
        }
    }
}
