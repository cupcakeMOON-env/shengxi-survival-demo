using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShengXi.EditorTools
{
    /// <summary>
    /// 创建并保存 Main 场景，同时加入 Build Settings。
    /// 首次打开工程后执行一次：Tools → ShengXi → Setup Main Scene。
    /// 场景本身是空的，运行时由 GameBootstrap 自动搭好一切。
    /// </summary>
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        /// <summary>
        /// 首次打开工程时自动创建 Main 场景（无需手动点菜单）。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void EnsureMainSceneOnLoad()
        {
            if (File.Exists(ScenePath))
            {
                return;
            }

            EditorApplication.delayCall += CreateMainScene;
        }

        [MenuItem("Tools/ShengXi/Setup Main Scene")]
        public static void CreateMainScene()
        {
            var scenesDir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(scenesDir) && !Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[ShengXi] Main scene created at {ScenePath}");
        }
    }
}
