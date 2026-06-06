// Editor utility: adds MainMenu and Game scenes to Build Settings automatically.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rami.Editor
{
    [InitializeOnLoad]
    public static class RamiSceneSetup
    {
        static RamiSceneSetup()
        {
            EnsureScenesInBuildSettings();
        }

        [MenuItem("Rami/Add Scenes to Build Settings")]
        public static void EnsureScenesInBuildSettings()
        {
            var requiredScenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Game.unity"
            };

            var existing = EditorBuildSettings.scenes.ToList();
            bool changed = false;

            foreach (var scenePath in requiredScenes)
            {
                bool alreadyIn = existing.Any(s => s.path == scenePath);
                if (!alreadyIn)
                {
                    existing.Add(new EditorBuildSettingsScene(scenePath, enabled: true));
                    changed = true;
                    Debug.Log($"[Rami] Added scene to Build Settings: {scenePath}");
                }
            }

            if (changed)
                EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
#endif
