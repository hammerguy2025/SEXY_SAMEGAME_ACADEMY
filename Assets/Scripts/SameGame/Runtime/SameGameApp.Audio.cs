using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const string BgmStreamingRoot = "Audio/Bgm";
        private const string MenuBgmClipKey = "menu_main.opus";
        private const string DefaultStageBgmClipKey = "stage_default.opus";
        private const string ElementaryStageBgmClipKey = "stage_elementary.opus";
        private const string MiddleStageBgmClipKey = "stage_middle.opus";
        private const string HighStageBgmClipKey = "stage_high.opus";
        private const string TeacherStageBgmClipKey = "stage_teacher.opus";
        private const string BlockClearSeResourcePath = "Audio/Se/block_clear";
        private const string BigBlockClearSeResourcePath = "Audio/Se/block_clear_big";
        private const string StageClearSeResourcePath = "Audio/Se/stage_clear";
        private const string StageFailSeResourcePath = "Audio/Se/stage_fail";
        private const float BgmSwitchFadeDuration = 0.45f;
        private const float BgmLoopFadeDuration = 1.2f;

        private readonly Dictionary<string, AudioClip> _bgmClipCache = new Dictionary<string, AudioClip>();

        private AudioSource _bgmPrimarySource;
        private AudioSource _bgmSecondarySource;
        private AudioSource _activeBgmSource;
        private AudioSource _inactiveBgmSource;
        private AudioSource _seSource;
        private AudioClip _blockClearClip;
        private AudioClip _bigBlockClearClip;
        private AudioClip _stageClearClip;
        private AudioClip _stageFailClip;
        private Coroutine _bgmLoopRoutine;
        private Coroutine _bgmSwitchRoutine;
        private Coroutine _bgmLoadRoutine;
        private string _currentBgmClipKey = string.Empty;
        private string _requestedBgmClipKey = string.Empty;
        private string _loadingBgmClipKey = string.Empty;

        private void EnsureAudio()
        {
            if (_bgmPrimarySource == null)
            {
                _bgmPrimarySource = CreateBgmSource("BgmPrimarySource");
            }

            if (_bgmSecondarySource == null)
            {
                _bgmSecondarySource = CreateBgmSource("BgmSecondarySource");
            }

            _activeBgmSource ??= _bgmPrimarySource;
            _inactiveBgmSource ??= _bgmSecondarySource;

            if (_seSource == null)
            {
                _seSource = gameObject.AddComponent<AudioSource>();
                _seSource.playOnAwake = false;
                _seSource.loop = false;
                _seSource.spatialBlend = 0f;
            }

            _blockClearClip ??= Resources.Load<AudioClip>(BlockClearSeResourcePath);
            _bigBlockClearClip ??= Resources.Load<AudioClip>(BigBlockClearSeResourcePath);
            _stageClearClip ??= Resources.Load<AudioClip>(StageClearSeResourcePath);
            _stageFailClip ??= Resources.Load<AudioClip>(StageFailSeResourcePath);
            ApplyAudioVolumes();
        }

        private AudioSource CreateBgmSource(string sourceName)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.name = sourceName;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void ApplyAudioVolumes()
        {
            if (UseBrowserBgmPlayback())
            {
                SetBrowserBgmVolume(_bgmVolume);
            }

            if (_bgmPrimarySource != null && _bgmSwitchRoutine == null)
            {
                _bgmPrimarySource.volume = _bgmPrimarySource.isPlaying ? (_bgmPrimarySource == _activeBgmSource ? _bgmVolume : 0f) : 0f;
            }

            if (_bgmSecondarySource != null && _bgmSwitchRoutine == null)
            {
                _bgmSecondarySource.volume = _bgmSecondarySource.isPlaying ? (_bgmSecondarySource == _activeBgmSource ? _bgmVolume : 0f) : 0f;
            }

            if (_seSource != null)
            {
                _seSource.volume = _seVolume;
            }
        }

        private void PlayMenuBgm()
        {
            PlayBgmClip(MenuBgmClipKey);
        }

        private void PlayStageBgm(StageDefinition stage)
        {
            EnsureAudio();
            var bgmClipKey = GetStageBgmClipKey();
            if (string.IsNullOrWhiteSpace(bgmClipKey))
            {
                bgmClipKey = stage != null ? stage.bgmResourcePath : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(bgmClipKey))
            {
                StopStageBgm();
                return;
            }

            PlayBgmClip(bgmClipKey);
        }

        private string GetStageBgmClipKey()
        {
            var character = GetSelectedCharacter();
            if (character == null)
            {
                return string.Empty;
            }

            switch (character.id)
            {
                case "character_01":
                    return ElementaryStageBgmClipKey;
                case "character_02":
                    return MiddleStageBgmClipKey;
                case "character_03":
                    return HighStageBgmClipKey;
                case "character_04":
                    return TeacherStageBgmClipKey;
                default:
                    return string.Empty;
            }
        }

        private void PlayBgmClip(string clipKey)
        {
            EnsureAudio();
            if (string.IsNullOrWhiteSpace(clipKey))
            {
                StopStageBgm();
                return;
            }

            _requestedBgmClipKey = clipKey;

            if (UseBrowserBgmPlayback())
            {
                _currentBgmClipKey = clipKey;
                PlayBrowserBgm(BuildStreamingAssetUrl(BgmStreamingRoot + "/" + clipKey), _bgmVolume);
                return;
            }

            if (TryGetCachedBgmClip(clipKey, out var clip))
            {
                PlayResolvedBgmClip(clip, clipKey);
                return;
            }

            if (_bgmLoadRoutine != null && _loadingBgmClipKey == clipKey)
            {
                return;
            }

            if (_bgmLoadRoutine != null)
            {
                StopCoroutine(_bgmLoadRoutine);
                _bgmLoadRoutine = null;
            }

            _loadingBgmClipKey = clipKey;
            _bgmLoadRoutine = StartCoroutine(LoadBgmClipRoutine(clipKey));
        }

        private bool TryGetCachedBgmClip(string clipKey, out AudioClip clip)
        {
            if (_bgmClipCache.TryGetValue(clipKey, out clip) && clip != null)
            {
                return true;
            }

            clip = null;
            return false;
        }

        private IEnumerator LoadBgmClipRoutine(string clipKey)
        {
            var clipUrl = BuildStreamingAssetUrl(BgmStreamingRoot + "/" + clipKey);
            var request = UnityWebRequestMultimedia.GetAudioClip(clipUrl, GetStreamingAudioType(clipKey));
            yield return request.SendWebRequest();

            _bgmLoadRoutine = null;
            _loadingBgmClipKey = string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Failed to load BGM clip from StreamingAssets: " + clipUrl + "\n" + request.error);
                if (_requestedBgmClipKey == clipKey)
                {
                    StopStageBgm();
                }

                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                if (_requestedBgmClipKey == clipKey)
                {
                    StopStageBgm();
                }

                yield break;
            }

            clip.name = Path.GetFileNameWithoutExtension(clipKey);
            _bgmClipCache[clipKey] = clip;

            if (_requestedBgmClipKey == clipKey)
            {
                PlayResolvedBgmClip(clip, clipKey);
            }
        }

        private void PlayResolvedBgmClip(AudioClip clip, string clipKey)
        {
            if (clip == null)
            {
                StopStageBgm();
                return;
            }

            if (_currentBgmClipKey == clipKey &&
                _activeBgmSource != null &&
                _activeBgmSource.isPlaying &&
                _activeBgmSource.clip == clip)
            {
                return;
            }

            StopBgmCoroutines();
            _currentBgmClipKey = clipKey;

            if (_activeBgmSource == null || !_activeBgmSource.isPlaying || _activeBgmSource.clip == null)
            {
                StartClipOnSource(_activeBgmSource ?? _bgmPrimarySource, clip, _bgmVolume);
                _activeBgmSource = _activeBgmSource ?? _bgmPrimarySource;
                _inactiveBgmSource = GetOtherBgmSource(_activeBgmSource);
                _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, clipKey));
                return;
            }

            _bgmSwitchRoutine = StartCoroutine(CrossfadeToClip(clip, clipKey, BgmSwitchFadeDuration));
        }

        private IEnumerator CrossfadeToClip(AudioClip clip, string clipKey, float fadeDuration)
        {
            var fromSource = _activeBgmSource;
            var toSource = GetOtherBgmSource(fromSource);
            if (toSource == null)
            {
                toSource = fromSource;
            }

            if (fromSource == null || !fromSource.isPlaying || fromSource.clip == null)
            {
                StartClipOnSource(toSource ?? _bgmPrimarySource, clip, _bgmVolume);
                _activeBgmSource = toSource ?? _bgmPrimarySource;
                _inactiveBgmSource = GetOtherBgmSource(_activeBgmSource);
                _bgmSwitchRoutine = null;
                _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, clipKey));
                yield break;
            }

            StartClipOnSource(toSource, clip, 0f);
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / fadeDuration);
                fromSource.volume = (1f - t) * _bgmVolume;
                toSource.volume = t * _bgmVolume;
                yield return null;
            }

            fromSource.Stop();
            fromSource.clip = null;
            fromSource.volume = 0f;
            toSource.volume = _bgmVolume;

            _activeBgmSource = toSource;
            _inactiveBgmSource = fromSource;
            _bgmSwitchRoutine = null;
            _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, clipKey));
        }

        private IEnumerator BgmLoopRoutine(AudioSource source, string clipKey)
        {
            while (source != null &&
                   source == _activeBgmSource &&
                   _currentBgmClipKey == clipKey &&
                   source.clip != null)
            {
                var clip = source.clip;
                var fadeDuration = Mathf.Min(BgmLoopFadeDuration, Mathf.Max(0.15f, clip.length * 0.2f));
                var waitDuration = clip.length - fadeDuration;
                if (waitDuration <= 0.05f)
                {
                    source.loop = true;
                    source.volume = _bgmVolume;
                    _bgmLoopRoutine = null;
                    yield break;
                }

                var endTime = Time.unscaledTime + waitDuration;
                while (Time.unscaledTime < endTime)
                {
                    if (source != _activeBgmSource || _currentBgmClipKey != clipKey || source.clip != clip)
                    {
                        _bgmLoopRoutine = null;
                        yield break;
                    }

                    yield return null;
                }

                var nextSource = GetOtherBgmSource(source);
                StartClipOnSource(nextSource, clip, 0f);

                var elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    if (source != _activeBgmSource || _currentBgmClipKey != clipKey || source.clip != clip || nextSource.clip != clip)
                    {
                        nextSource.Stop();
                        nextSource.clip = null;
                        nextSource.volume = 0f;
                        _bgmLoopRoutine = null;
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / fadeDuration);
                    source.volume = (1f - t) * _bgmVolume;
                    nextSource.volume = t * _bgmVolume;
                    yield return null;
                }

                source.Stop();
                source.clip = null;
                source.volume = 0f;
                nextSource.volume = _bgmVolume;

                _activeBgmSource = nextSource;
                _inactiveBgmSource = source;
                source = nextSource;
            }

            _bgmLoopRoutine = null;
        }

        private void StartClipOnSource(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.Stop();
            source.loop = false;
            source.clip = clip;
            source.time = 0f;
            source.volume = volume;
            source.Play();
        }

        private AudioSource GetOtherBgmSource(AudioSource source)
        {
            if (source == _bgmPrimarySource)
            {
                return _bgmSecondarySource;
            }

            return _bgmPrimarySource;
        }

        private void StopBgmCoroutines()
        {
            if (_bgmLoopRoutine != null)
            {
                StopCoroutine(_bgmLoopRoutine);
                _bgmLoopRoutine = null;
            }

            if (_bgmSwitchRoutine != null)
            {
                StopCoroutine(_bgmSwitchRoutine);
                _bgmSwitchRoutine = null;
            }

            if (_bgmLoadRoutine != null)
            {
                StopCoroutine(_bgmLoadRoutine);
                _bgmLoadRoutine = null;
            }

            _loadingBgmClipKey = string.Empty;
        }

        private void StopStageBgm()
        {
            if (UseBrowserBgmPlayback())
            {
                _requestedBgmClipKey = string.Empty;
                _currentBgmClipKey = string.Empty;
                StopBrowserBgm();
                return;
            }

            StopBgmCoroutines();
            _requestedBgmClipKey = string.Empty;
            _currentBgmClipKey = string.Empty;

            if (_bgmPrimarySource != null)
            {
                _bgmPrimarySource.Stop();
                _bgmPrimarySource.clip = null;
                _bgmPrimarySource.volume = 0f;
            }

            if (_bgmSecondarySource != null)
            {
                _bgmSecondarySource.Stop();
                _bgmSecondarySource.clip = null;
                _bgmSecondarySource.volume = 0f;
            }
        }

        private static string BuildStreamingAssetUrl(string relativePath)
        {
            var normalizedRelativePath = relativePath.Replace("\\", "/");
            var basePath = Application.streamingAssetsPath;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return normalizedRelativePath;
            }

            if (basePath.Contains("://", StringComparison.Ordinal) || basePath.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
            {
                return basePath.TrimEnd('/') + "/" + normalizedRelativePath;
            }

            return new Uri(Path.Combine(basePath, normalizedRelativePath)).AbsoluteUri;
        }

        private static AudioType GetStreamingAudioType(string clipKey)
        {
            var extension = Path.GetExtension(clipKey);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return AudioType.UNKNOWN;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".mp3":
                    return AudioType.MPEG;
                case ".ogg":
                case ".opus":
                    return AudioType.OGGVORBIS;
                case ".wav":
                    return AudioType.WAV;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        private void PlayBlockClearSe(int scoreDelta)
        {
            PlaySe(scoreDelta >= 200 ? _bigBlockClearClip : _blockClearClip);
        }

        private void PlayStageClearSe()
        {
            PlaySe(_stageClearClip);
        }

        private void PlayStageFailSe()
        {
            PlaySe(_stageFailClip);
        }

        private void PlaySe(AudioClip clip)
        {
            EnsureAudio();
            if (clip == null)
            {
                return;
            }

            _seSource.PlayOneShot(clip);
        }
    }
}
