using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Unity.Media.Osc
{
    static class OscManager
    {
        /// <summary>
        /// An event invoked before Update in the player loop.
        /// </summary>
        internal static event Action PreUpdate;

        /// <summary>
        /// An event invoked when messages derived from scene data should be sent.
        /// </summary>
        internal static event Action MessageOutputUpdate;

        /// <summary>
        /// An event invoked when auto bundles for the frame should be sent.
        /// </summary>
        internal static event Action AutoBundlesUpdate;

        /// <summary>
        /// An event invoked when the network streams should be processed.
        /// </summary>
        internal static event Action NetworkUpdate;

        struct OscPreUpdate
        {
        }

        struct OscPostLateUpdate
        {
        }

#if UNITY_EDITOR
        static readonly EditorApplication.CallbackFunction s_EditModeUpdate = () =>
        {
            OnPreUpdate();
            OnPostLateUpdate();
        };

        [InitializeOnLoadMethod]
        static void Init()
        {
            // To get regular updates in edit mode we must use the EditorApplication.update callback.
            // However, to optimize latency in play mode we need to switch to the player loop
            // callbacks instead since it avoids the need to wait for the next frame when sending updated
            // OSC Messages.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update -= s_EditModeUpdate;
                EditorApplication.update += s_EditModeUpdate;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            switch (mode)
            {
                case PlayModeStateChange.EnteredEditMode:
                {
                    EditorApplication.update -= s_EditModeUpdate;
                    EditorApplication.update += s_EditModeUpdate;
                    break;
                }
                case PlayModeStateChange.ExitingEditMode:
                {
                    EditorApplication.update -= s_EditModeUpdate;
                    break;
                }
                case PlayModeStateChange.EnteredPlayMode:
                {
                    PlayerLoopExtensions.RegisterUpdate<PreUpdate, OscPreUpdate>(OnPreUpdate);
                    PlayerLoopExtensions.RegisterUpdate<PostLateUpdate, OscPostLateUpdate>(OnPostLateUpdate);
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    PlayerLoopExtensions.DeregisterUpdate<OscPreUpdate>(OnPreUpdate);
                    PlayerLoopExtensions.DeregisterUpdate<OscPostLateUpdate>(OnPostLateUpdate);
                    break;
                }
            }
        }
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            PlayerLoopExtensions.RegisterUpdate<PreUpdate, OscPreUpdate>(OnPreUpdate);
            PlayerLoopExtensions.RegisterUpdate<PostLateUpdate, OscPostLateUpdate>(OnPostLateUpdate);
        }
#endif

        static void OnPreUpdate()
        {
            PreUpdate?.Invoke();
        }

        static void OnPostLateUpdate()
        {
            MessageOutputUpdate?.Invoke();
            AutoBundlesUpdate?.Invoke();
            NetworkUpdate?.Invoke();
        }
    }
}
