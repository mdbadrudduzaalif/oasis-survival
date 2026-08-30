#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[InitializeOnLoad]
public class CleanSceneZombies
{
    static CleanSceneZombies()
    {
        EditorApplication.delayCall += CleanAllSceneZombies;
    }

    [MenuItem("Tools/Clean Pre-placed Scene Zombies")]
    public static void CleanAllSceneZombies()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            return;

        var zombiesParent = GameObject.Find("Zombies");
        if (zombiesParent != null && zombiesParent.transform.childCount > 0)
        {
            var children = new List<GameObject>();
            for (int i = 0; i < zombiesParent.transform.childCount; i++)
            {
                var child = zombiesParent.transform.GetChild(i).gameObject;
                if (child != null)
                {
                    children.Add(child);
                }
            }

            foreach (var child in children)
            {
                Undo.DestroyObjectImmediate(child);
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            Debug.Log($"[CleanSceneZombies] Successfully removed {children.Count} pre-placed zombie models from the scene hierarchy.");
        }
    }
}
#endif

