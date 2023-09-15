using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Media.Osc.Editor
{
    static class ComponentFinder<T> where T : Component
    {
        static readonly HashSet<string> s_VisitedScenes = new HashSet<string>();
        static readonly List<T> s_TempComponents = new List<T>();
        static Action<T> s_OnComponentFound;

        public static void Search(Action<T> onComponentFound)
        {
            s_OnComponentFound = onComponentFound ?? throw new ArgumentNullException(nameof(onComponentFound));
            s_VisitedScenes.Clear();

            SearchBuildScenes();
            SearchPrefabs();
        }

        static void SearchBuildScenes()
        {
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled)
                {
                    continue;
                }

                SearchScene(buildScene.path);
            }
        }

        static void SearchScene(string path)
        {
            if (s_VisitedScenes.Contains(path))
            {
                return;
            }

            s_VisitedScenes.Add(path);

            var scene = SceneManager.GetSceneByPath(path);
            var sceneOpen = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    return;
                }
            }
            else
            {
                sceneOpen = true;
            }

            foreach (var gameObject in scene.GetRootGameObjects())
            {
                SearchGameObject(gameObject);
            }

            if (!sceneOpen)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void SearchPrefabs()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                SearchGameObject(go);
            }
        }

        static void SearchGameObject(GameObject gameObject)
        {
            gameObject.GetComponents(s_TempComponents);

            foreach (var component in s_TempComponents)
            {
                s_OnComponentFound.Invoke(component);
            }

            var transform = gameObject.transform;
            var count = transform.childCount;

            for (var i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                SearchGameObject(child.gameObject);
            }
        }
    }
}
