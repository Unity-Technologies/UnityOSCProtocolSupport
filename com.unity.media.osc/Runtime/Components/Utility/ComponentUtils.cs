#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace Unity.Media.Osc
{
    /// <summary>
    /// A class containing utility methods for managing components.
    /// </summary>
    static class ComponentUtils
    {
        /// <summary>
        /// Finds a component from the same Scene as the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject  to get the most relevant component for.</param>
        /// <param name="includeInactive">Allows returning components on inactive objects.</param>
        /// <typeparam name="T">The type of component to get.</typeparam>
        /// <returns>A <typeparamref name="T"/> instance, or <see langword="null"/> if no instance was found.</returns>
        internal static T FindComponentInSameScene<T>(GameObject gameObject, bool includeInactive = false) where T : Component
        {
            var component = gameObject.GetComponentInParent<T>();

            if (component != null)
                return component;

#if UNITY_EDITOR
            // Find a component from the same stage if the gameobject is not in a scene
            var stage = StageUtility.GetStage(gameObject);

            if (stage != StageUtility.GetMainStage())
            {
                foreach (var candidate in stage.FindComponentsOfType<T>())
                {
                    if (candidate.gameObject.activeInHierarchy)
                    {
                        return candidate;
                    }
                }
            }
#endif

            foreach (var candidate in Object.FindObjectsOfType<T>(includeInactive))
            {
                if (candidate.gameObject.scene == gameObject.scene)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
