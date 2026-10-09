using ShengXi.View;
using ShengXi.UI;
using ShengXi.Simulation;
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
            root.AddComponent<GameLoop>();
            root.AddComponent<GridView>();

            var buildMode = root.AddComponent<BuildModeController>();
            var input = root.AddComponent<TileClickInput>();
            var basePlacement = root.AddComponent<BasePlacementController>();
            var towerRange = root.AddComponent<TowerRangeOverlay>();
            root.AddComponent<ResourceBar>();
            root.AddComponent<BuildMenu>();
            // 工坊面板加在结算面板之前：结算全屏遮罩要盖在它上面
            root.AddComponent<WorkshopPanel>();
            // 升级面板与工坊面板都订阅 BuildingSelected，各自只认自己那类建筑
            root.AddComponent<UpgradePanel>();
            root.AddComponent<EnemyView>();
            root.AddComponent<DayBanner>();
            root.AddComponent<GameOverPanel>();
            // 开始界面最后加：同层级下后创建的排在后面，会盖在所有 HUD 之上
            root.AddComponent<StartMenuPanel>();

            // 清掉场景里可能残留的默认相机（模板场景自带 Main Camera），
            // 只保留我们自己创建的正交相机，避免 Camera.main 取错相机导致坐标换算错误。
            var existingCameras = Object.FindObjectsByType<Camera>();
            foreach (var existingCam in existingCameras)
            {
                Object.Destroy(existingCam.gameObject);
            }

            // 开机时还没有世界（要先进开始界面），相机先按地图默认尺寸摆好；
            // 新开一局用的也是这套默认尺寸，所以不用等地图生成再调整。
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            cam.transform.position = new Vector3(
                (MapGenerator.DefaultWidth - 1) * 0.5f,
                (MapGenerator.DefaultHeight - 1) * 0.5f,
                -10f);
            cam.orthographicSize = MapGenerator.DefaultHeight * 0.5f + 2f;

            input.Initialize(cam, buildMode);
            buildMode.Initialize(cam);
            basePlacement.Initialize(cam);
            towerRange.Initialize(cam, buildMode);
        }
    }
}
