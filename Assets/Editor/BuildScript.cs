using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShengXi.EditorTools
{
    /// <summary>
    /// 命令行打 Windows 独立包：
    /// Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod ShengXi.EditorTools.BuildScript.BuildWindows
    /// 产物在 D:/codex/ShengXiSurvivalDemo-Win64/，运行 exe 不再需要 Unity 编辑器。
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string OutputDir = "D:/codex/ShengXiSurvivalDemo-Win64";
        private const string ExeName = "ShengXiSurvivalDemo.exe";

        [MenuItem("Tools/ShengXi/Build Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
            {
                ProjectSetup.CreateMainScene();
            }

            Directory.CreateDirectory(OutputDir);
            var outputPath = Path.Combine(OutputDir, ExeName);

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath },
                outputPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Build] Windows 构建失败：{summary.result}，输出 {outputPath}");
                throw new System.Exception($"Build failed: {summary.result}");
            }

            Debug.Log($"[Build] Windows 构建成功：{outputPath}（{summary.totalSize / (1024 * 1024)} MB）");
        }
    }
}
