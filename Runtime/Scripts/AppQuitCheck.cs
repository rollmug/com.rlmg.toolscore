namespace rlmg.Tools.Core
{
    using UnityEngine;

    /// <summary>
    /// Tracks whether the app (or, in the Editor, Play Mode) is shutting down, so singletons can avoid
    /// creating instances during teardown. Shared by every <see cref="SingletonDoNotDestroy{T}"/>, since a
    /// generic class can't use <see cref="RuntimeInitializeOnLoadMethodAttribute"/>.
    ///
    /// <see cref="Initialize"/> runs at the start of every Play session and resets the state, so this also
    /// works with domain reload disabled in Enter Play Mode Options.
    /// </summary>
    internal static class AppQuitCheck
    {
        private static volatile bool isQuitting;

        public static bool IsQuitting
        {
            get => isQuitting;
            private set => isQuitting = value;
        }

        internal static void MarkQuitting()
        {
            IsQuitting = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            IsQuitting = false;

            // Statics survive between sessions without domain reload, so unsubscribe first to avoid stacking handlers.
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private static void OnApplicationQuitting()
        {
            MarkQuitting();
        }

#if UNITY_EDITOR
        /// <summary>
        /// ExitingPlayMode fires before any OnApplicationQuit, OnDisable or OnDestroy, so the flag is set
        /// before other behaviours' teardown regardless of the order Unity tears them down in.
        /// </summary>
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                MarkQuitting();
        }
#endif
    }
}
