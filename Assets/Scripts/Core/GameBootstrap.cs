using ShengXi.View;
using ShengXi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShengXi.Core
{
    /// <summary>
    /// 运行时自举：任何场景按下 Play 都会自动搭好相机、GameRoot 与网格视图。
    /// 这样 M0 无需手写复杂场景，天然满足「能运行、能看到网格」的验收条件。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // 重载场景（重新开始）后自动重建运行时世界
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BootCore();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootCore();
        }

        private static void BootCore()
        {
            if (Object.FindAnyObjectByType<GameLoop>() != null)
            {
                return;
            }

            var root = new GameObject("GameRoot");
            var loop = root.AddComponent<GameLoop>();
            root.AddComponent<GridView>();

            var buildMode = root.AddComponent<BuildModeController>();
            var input = root.AddComponent<TileClickInput>();
            root.AddComponent<ResourceBar>();
            root.AddComponent<BuildMenu>();
            root.AddComponent<EnemyView>();
            root.AddComponent<DayBanner>();
            root.AddComponent<GameOverPanel>();

            // 清掉场景里可能残留的默认相机（模板场景自带 Main Camera），
            // 只保留我们自己创建的正交相机，避免 Camera.main 取错相机导致坐标换算错误。
            var existingCameras = Object.FindObjectsByType<Camera>();
            foreach (var existingCam in existingCameras)
            {
                Object.Destroy(existingCam.gameObject);
            }

            var map = loop.Map;
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            cam.transform.position = new Vector3((map.Width - 1) * 0.5f, (map.Height - 1) * 0.5f, -10f);
            cam.orthographicSize = map.Height * 0.5f + 2f;

            input.Initialize(cam, buildMode);
            buildMode.Initialize(cam);
        }
    }
}
