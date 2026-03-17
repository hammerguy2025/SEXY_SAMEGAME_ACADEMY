using UnityEngine;

namespace SameGame.Runtime
{
    public sealed partial class SameGameApp
    {
        private const float FixedWindowAspectRatio = 16f / 9f;
        private const int MinimumWindowWidth = 960;
        private const int MinimumWindowHeight = 540;
        private const float AspectTolerance = 0.0025f;

        private int _lastWindowWidth;
        private int _lastWindowHeight;

        private void InitializeWindowAspect()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE
            ApplyWindowAspect(Screen.width, Screen.height, true);
#endif
        }

        private void EnforceWindowAspectRatio()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE
            var width = Mathf.Max(Screen.width, MinimumWindowWidth);
            var height = Mathf.Max(Screen.height, MinimumWindowHeight);
            if (_lastWindowWidth == 0 || _lastWindowHeight == 0)
            {
                _lastWindowWidth = width;
                _lastWindowHeight = height;
            }

            var currentAspect = width / (float)height;
            if (Mathf.Abs(currentAspect - FixedWindowAspectRatio) <= AspectTolerance)
            {
                _lastWindowWidth = width;
                _lastWindowHeight = height;
                if (Screen.fullScreenMode != FullScreenMode.Windowed)
                {
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                }

                return;
            }

            var widthDelta = Mathf.Abs(width - _lastWindowWidth);
            var heightDelta = Mathf.Abs(height - _lastWindowHeight);
            if (widthDelta >= heightDelta)
            {
                height = Mathf.Max(MinimumWindowHeight, Mathf.RoundToInt(width / FixedWindowAspectRatio));
            }
            else
            {
                width = Mathf.Max(MinimumWindowWidth, Mathf.RoundToInt(height * FixedWindowAspectRatio));
            }

            ApplyWindowAspect(width, height, false);
#endif
        }

        private void ApplyWindowAspect(int width, int height, bool force)
        {
#if !UNITY_EDITOR && UNITY_STANDALONE
            var clampedWidth = Mathf.Max(MinimumWindowWidth, width);
            var clampedHeight = Mathf.Max(MinimumWindowHeight, height);
            var adjustedAspect = clampedWidth / (float)clampedHeight;
            if (Mathf.Abs(adjustedAspect - FixedWindowAspectRatio) > AspectTolerance)
            {
                clampedHeight = Mathf.Max(MinimumWindowHeight, Mathf.RoundToInt(clampedWidth / FixedWindowAspectRatio));
            }

            if (!force &&
                Screen.fullScreenMode == FullScreenMode.Windowed &&
                Screen.width == clampedWidth &&
                Screen.height == clampedHeight)
            {
                _lastWindowWidth = clampedWidth;
                _lastWindowHeight = clampedHeight;
                return;
            }

            _lastWindowWidth = clampedWidth;
            _lastWindowHeight = clampedHeight;
            Screen.SetResolution(clampedWidth, clampedHeight, FullScreenMode.Windowed);
#endif
        }
    }
}
