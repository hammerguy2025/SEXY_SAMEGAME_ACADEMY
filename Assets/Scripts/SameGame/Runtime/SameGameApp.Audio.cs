using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const string MenuBgmResourcePath = "Audio/Bgm/menu_main";
        private const string ElementaryStageBgmResourcePath = "Audio/Bgm/stage_elementary";
        private const string MiddleStageBgmResourcePath = "Audio/Bgm/stage_middle";
        private const string HighStageBgmResourcePath = "Audio/Bgm/stage_high";
        private const string TeacherStageBgmResourcePath = "Audio/Bgm/stage_teacher";
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
        private string _currentBgmResourcePath = string.Empty;

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
            PlayBgmResource(MenuBgmResourcePath);
        }

        private void PlayStageBgm(StageDefinition stage)
        {
            EnsureAudio();
            var bgmPath = GetStageBgmResourcePath();
            if (string.IsNullOrWhiteSpace(bgmPath))
            {
                bgmPath = stage != null ? stage.bgmResourcePath : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(bgmPath))
            {
                StopStageBgm();
                return;
            }

            PlayBgmResource(bgmPath);
        }

        private string GetStageBgmResourcePath()
        {
            var character = GetSelectedCharacter();
            if (character == null)
            {
                return string.Empty;
            }

            switch (character.id)
            {
                case "character_01":
                    return ElementaryStageBgmResourcePath;
                case "character_02":
                    return MiddleStageBgmResourcePath;
                case "character_03":
                    return HighStageBgmResourcePath;
                case "character_04":
                    return TeacherStageBgmResourcePath;
                default:
                    return string.Empty;
            }
        }

        private void PlayBgmResource(string resourcePath)
        {
            EnsureAudio();
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                StopStageBgm();
                return;
            }

            var clip = LoadBgmClip(resourcePath);
            if (clip == null)
            {
                StopStageBgm();
                return;
            }

            if (_currentBgmResourcePath == resourcePath &&
                _activeBgmSource != null &&
                _activeBgmSource.isPlaying &&
                _activeBgmSource.clip == clip)
            {
                return;
            }

            StopBgmCoroutines();
            _currentBgmResourcePath = resourcePath;

            if (_activeBgmSource == null || !_activeBgmSource.isPlaying || _activeBgmSource.clip == null)
            {
                StartClipOnSource(_activeBgmSource ?? _bgmPrimarySource, clip, _bgmVolume);
                _activeBgmSource = _activeBgmSource ?? _bgmPrimarySource;
                _inactiveBgmSource = GetOtherBgmSource(_activeBgmSource);
                _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, resourcePath));
                return;
            }

            _bgmSwitchRoutine = StartCoroutine(CrossfadeToClip(clip, resourcePath, BgmSwitchFadeDuration));
        }

        private AudioClip LoadBgmClip(string resourcePath)
        {
            if (_bgmClipCache.TryGetValue(resourcePath, out var clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>(resourcePath);
            _bgmClipCache[resourcePath] = clip;
            return clip;
        }

        private IEnumerator CrossfadeToClip(AudioClip clip, string resourcePath, float fadeDuration)
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
                _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, resourcePath));
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
            _bgmLoopRoutine = StartCoroutine(BgmLoopRoutine(_activeBgmSource, resourcePath));
        }

        private IEnumerator BgmLoopRoutine(AudioSource source, string resourcePath)
        {
            while (source != null &&
                   source == _activeBgmSource &&
                   _currentBgmResourcePath == resourcePath &&
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
                    if (source != _activeBgmSource || _currentBgmResourcePath != resourcePath || source.clip != clip)
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
                    if (source != _activeBgmSource || _currentBgmResourcePath != resourcePath || source.clip != clip || nextSource.clip != clip)
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
        }

        private void StopStageBgm()
        {
            StopBgmCoroutines();
            _currentBgmResourcePath = string.Empty;

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
