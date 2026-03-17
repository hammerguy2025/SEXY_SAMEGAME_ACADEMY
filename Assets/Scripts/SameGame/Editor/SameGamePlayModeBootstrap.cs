using SameGame.Runtime;
using UnityEditor;
using UnityEngine;

namespace SameGame.Editor
{
    [InitializeOnLoad]
    public static class SameGamePlayModeBootstrap
    {
        static SameGamePlayModeBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            if (Object.FindFirstObjectByType<SameGameApp>() != null)
            {
                return;
            }

            var host = new GameObject("SameGameApp");
            host.AddComponent<SameGameApp>();
        }
    }
}
