using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARR.EditorTools
{
    /// Batch-mode helpers:  -executeMethod ARR.EditorTools.ArrSetup.Setup | BuildMac
    public static class ArrSetup
    {
        const string ScenePath = "Assets/Scenes/BeyondTheMist.unity";

        [MenuItem("ARR/Setup Scene")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ARR_Bootstrap").AddComponent<ArrBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Beyond the Mist";
            PlayerSettings.companyName = "ARR Hackathon";
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SplashScreen.show = false;
            // shared field also used by the macOS player (optional microphone breath input)
            PlayerSettings.iOS.microphoneUsageDescription = "Optionally uses the microphone to sense breathing sounds so the mist can respond.";
            AssetDatabase.SaveAssets();
            Debug.Log("[ARR] scene + settings ready");
        }

        [MenuItem("ARR/Build Windows App")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) Setup();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/BeyondTheMist.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[ARR] windows build result: " + report.summary.result + " size=" + report.summary.totalSize);
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        [MenuItem("ARR/Build macOS App")]
        public static void BuildMac()
        {
            if (!File.Exists(ScenePath)) Setup();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/BeyondTheMist.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[ARR] build result: " + report.summary.result + " size=" + report.summary.totalSize);
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
