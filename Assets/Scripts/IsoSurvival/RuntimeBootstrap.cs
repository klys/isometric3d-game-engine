using UnityEngine;

namespace IsoSurvival
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindObjectOfType<GameController>() != null)
            {
                return;
            }

            var root = new GameObject("IsoSurvivalGame");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameController>();
        }
    }
}
