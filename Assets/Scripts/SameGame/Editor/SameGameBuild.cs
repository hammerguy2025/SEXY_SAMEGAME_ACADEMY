using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SameGame.Editor
{
    public static class SameGameBuild
    {
        private const string WebGlOutputPath = "Builds/WebGL";

        public static void BuildWebGL()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes were found in Build Settings.");
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), WebGlOutputPath);
            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.WebGL,
                locationPathName = outputPath,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
            }
        }
    }
}
