using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const int StageBackgroundTextureWidth = 1280;
        private const int StageBackgroundTextureHeight = 720;

        private RawImage _stageBackgroundImage;
        private AspectRatioFitter _stageBackgroundFitter;
        private RenderTexture _stageBackgroundTexture;
        private VideoPlayer _stageVideoPlayer;
        private string _currentStageVideoUrl = string.Empty;

        private void BuildStageBackground(Transform parent)
        {
            var backgroundObject = new GameObject("StageBackground", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();

            var rectTransform = backgroundObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(StageBackgroundTextureWidth, StageBackgroundTextureHeight);

            _stageBackgroundImage = backgroundObject.GetComponent<RawImage>();
            _stageBackgroundImage.color = Color.white;
            _stageBackgroundImage.raycastTarget = false;

            _stageBackgroundFitter = backgroundObject.GetComponent<AspectRatioFitter>();
            _stageBackgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _stageBackgroundFitter.aspectRatio = 16f / 9f;
        }

        private void EnsureStageBackgroundPlayer()
        {
            if (_stageVideoPlayer != null)
            {
                return;
            }

            _stageVideoPlayer = gameObject.AddComponent<VideoPlayer>();
            _stageVideoPlayer.playOnAwake = false;
            _stageVideoPlayer.isLooping = true;
            _stageVideoPlayer.skipOnDrop = true;
            _stageVideoPlayer.waitForFirstFrame = true;
            _stageVideoPlayer.source = VideoSource.Url;
            _stageVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _stageVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _stageVideoPlayer.targetTexture = GetStageBackgroundTexture();
            _stageVideoPlayer.prepareCompleted += HandleStageVideoPrepared;
            _stageVideoPlayer.errorReceived += HandleStageVideoError;
        }

        private void PlayStageBackground(StageDefinition stage)
        {
            if (_stageBackgroundImage == null)
            {
                return;
            }

            var videoUrl = ResolveStageVideoUrl(stage);
            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                StopStageBackground();
                return;
            }

            EnsureStageBackgroundPlayer();
            _stageBackgroundImage.texture = GetStageBackgroundTexture();
            if (_currentStageVideoUrl == videoUrl && _stageVideoPlayer.isPrepared)
            {
                if (!_stageVideoPlayer.isPlaying)
                {
                    _stageVideoPlayer.Play();
                }

                return;
            }

            _currentStageVideoUrl = videoUrl;
            _stageVideoPlayer.Stop();
            _stageVideoPlayer.url = videoUrl;
            _stageVideoPlayer.Prepare();
        }

        private void StopStageBackground()
        {
            _currentStageVideoUrl = string.Empty;

            if (_stageVideoPlayer != null)
            {
                _stageVideoPlayer.Stop();
                _stageVideoPlayer.url = string.Empty;
            }

            if (_stageBackgroundImage != null)
            {
                _stageBackgroundImage.texture = null;
            }
        }

        private void DisposeStageBackground()
        {
            StopStageBackground();

            if (_stageVideoPlayer == null)
            {
                return;
            }

            _stageVideoPlayer.prepareCompleted -= HandleStageVideoPrepared;
            _stageVideoPlayer.errorReceived -= HandleStageVideoError;
            Destroy(_stageVideoPlayer);
            _stageVideoPlayer = null;

            if (_stageBackgroundTexture != null)
            {
                _stageBackgroundTexture.Release();
                Destroy(_stageBackgroundTexture);
                _stageBackgroundTexture = null;
            }
        }

        private string ResolveStageVideoUrl(StageDefinition stage)
        {
            var relativePath = stage != null ? stage.backgroundVideoPath : string.Empty;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            return File.Exists(fullPath) ? fullPath : string.Empty;
        }

        private void HandleStageVideoPrepared(VideoPlayer player)
        {
            if (player == null || string.IsNullOrWhiteSpace(_currentStageVideoUrl) || player.url != _currentStageVideoUrl)
            {
                return;
            }

            if (_stageBackgroundImage != null)
            {
                _stageBackgroundImage.texture = GetStageBackgroundTexture();
            }

            UpdateStageBackgroundAspect(player);
            if (_state != AppState.Title)
            {
                player.Play();
            }
        }

        private void HandleStageVideoError(VideoPlayer source, string message)
        {
            if (source == null || source != _stageVideoPlayer || source.url != _currentStageVideoUrl)
            {
                return;
            }

            Debug.LogWarning("Stage background video failed: " + message);
            StopStageBackground();
        }

        private void UpdateStageBackgroundAspect(VideoPlayer player)
        {
            if (_stageBackgroundFitter == null || player == null || player.texture == null || player.texture.height <= 0)
            {
                return;
            }

            _stageBackgroundFitter.aspectRatio = player.texture.width / (float)player.texture.height;
        }

        private RenderTexture GetStageBackgroundTexture()
        {
            if (_stageBackgroundTexture != null)
            {
                return _stageBackgroundTexture;
            }

            _stageBackgroundTexture = new RenderTexture(StageBackgroundTextureWidth, StageBackgroundTextureHeight, 0, RenderTextureFormat.ARGB32);
            _stageBackgroundTexture.name = "StageBackgroundTexture";
            _stageBackgroundTexture.useMipMap = false;
            _stageBackgroundTexture.autoGenerateMips = false;
            _stageBackgroundTexture.Create();
            return _stageBackgroundTexture;
        }
    }
}
