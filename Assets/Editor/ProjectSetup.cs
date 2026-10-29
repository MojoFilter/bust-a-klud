using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BustAKlud.Editor
{
    /// <summary>
    /// One-shot project setup and build entry points. Everything here is also reachable from the
    /// "Bust-A-Klud" menu, and the Build* methods can be run headless with -executeMethod.
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string WebOutput = "WebBuild";
        const string WindowsOutput = "WinBuild/Bust-A-Klud.exe";

        [MenuItem("Bust-A-Klud/Setup Project (scene + settings)")]
        public static void SetupProject()
        {
            CreateMainScene();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Bust-A-Klud: scene and player settings configured.");
        }

        public static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.02f, 0.08f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CameraRig>();

            new GameObject("Bootstrap").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Bust-A-Klud";
            PlayerSettings.productName = "Bust-A-Klud";
            PlayerSettings.colorSpace = ColorSpace.Gamma;   // flat 2D art; keeps additive glows and fades looking as authored
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;

            // WebGL: our own full-window template, uncompressed so any static file host works unmodified.
            PlayerSettings.WebGL.template = "PROJECT:BustAKlud";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
        }

        [MenuItem("Bust-A-Klud/Build/WebGL")]
        public static void BuildWebGL()
        {
            Build(BuildTarget.WebGL, WebOutput);
        }

        [MenuItem("Bust-A-Klud/Build/Windows 64-bit")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, WindowsOutput);
        }

        // Windows build with the dev harness (-bak-autoplay) compiled in, used for automated playthroughs.
        public static void BuildWindowsDevelopment()
        {
            Build(BuildTarget.StandaloneWindows64, "DevBuild/Bust-A-Klud.exe", BuildOptions.Development);
        }

        static void Build(BuildTarget target, string output, BuildOptions options = BuildOptions.None)
        {
            // Only generate the scene if it is missing: regenerating gives it fresh object IDs every time, which
            // would show up as noise in version control after every build.
            if (!File.Exists(ScenePath))
                CreateMainScene();
            ConfigurePlayerSettings();

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = options
            });

            BuildSummary summary = report.summary;
            Debug.Log($"Bust-A-Klud build {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0}s -> {output}");
            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
