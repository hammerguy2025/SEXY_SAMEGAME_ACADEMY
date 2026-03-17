using UnityEngine;

namespace SameGame.Runtime
{
    public static class SameGameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAppExists()
        {
            if (Object.FindFirstObjectByType<SameGameApp>() != null)
            {
                return;
            }

            var host = new GameObject("SameGameApp");
            host.AddComponent<SameGameApp>();
        }
    }
}
