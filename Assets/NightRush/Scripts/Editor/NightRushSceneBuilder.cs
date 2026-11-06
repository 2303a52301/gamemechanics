using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NightRush.EditorTools
{
    public static class NightRushSceneBuilder
    {
        [MenuItem("Night Rush/Create Game Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();

            new GameObject("NightRushGame").AddComponent<GameManager>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            const string path = "Assets/Scenes/NightRush.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            Debug.Log("Night Rush scene created at " + path + ". Press Play.");
        }
    }
}
