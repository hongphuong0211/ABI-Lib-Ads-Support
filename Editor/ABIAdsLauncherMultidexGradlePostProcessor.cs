#if UNITY_ANDROID
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsLauncherMultidexGradlePostProcessor : IPostGenerateGradleAndroidProject
    {
        private const string MultidexKeepFileName = "abi-multidex-keep.pro";
        private const string LogPrefix = "[ABI Ads]";
        private const string Jdk11DexPinBlock = @"
configurations.configureEach {
    resolutionStrategy {
        force 'com.google.errorprone:error_prone_annotations:2.20.0'
        force 'androidx.webkit:webkit:1.11.0'
    }
}
";

        public int callbackOrder => 10001;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!TryResolveLauncherGradlePath(path, out var launcherGradlePath))
            {
                Debug.LogWarning($"{LogPrefix} Could not find launcher/build.gradle under `{path}`.");
                return;
            }

            if (!TryResolveMultidexKeepSource(out var keepSourcePath))
            {
                Debug.LogWarning(
                    $"{LogPrefix} Could not find `{MultidexKeepFileName}`. " +
                    "Copy it to Assets/Plugins/Android or reinstall com.abi.ads.unity.");
                return;
            }

            var launcherDir = Path.GetDirectoryName(launcherGradlePath);
            var keepDestPath = Path.Combine(launcherDir, MultidexKeepFileName);
            try
            {
                File.Copy(keepSourcePath, keepDestPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} Failed to copy {MultidexKeepFileName}: {ex.Message}");
                return;
            }

            var gradle = File.ReadAllText(launcherGradlePath);
            var patched = PatchLauncherGradle(gradle, MultidexKeepFileName);
            if (patched == gradle)
            {
                Debug.Log($"{LogPrefix} launcher/build.gradle already configured (MultiDex + JDK 11 dex pins).");
                return;
            }

            File.WriteAllText(launcherGradlePath, patched);
            Debug.Log($"{LogPrefix} Patched launcher/build.gradle (MultiDex keep + Unity 2022.3 JDK 11 dex pins).");
        }

        private static string PatchLauncherGradle(string gradle, string keepFileName)
        {
            gradle = ApplyUnity2022Jdk11DexPins(gradle);
            if (!gradle.Contains("multiDexEnabled true", StringComparison.Ordinal))
            {
                gradle = Regex.Replace(
                    gradle,
                    @"(defaultConfig\s*\{)",
                    "$1\n        multiDexEnabled true",
                    RegexOptions.Multiline);
            }

            if (!gradle.Contains("androidx.multidex:multidex", StringComparison.Ordinal))
            {
                gradle = Regex.Replace(
                    gradle,
                    @"(dependencies\s*\{)",
                    "$1\n    implementation 'androidx.multidex:multidex:2.0.1'",
                    RegexOptions.Multiline);
            }

            var keepLine = $"multiDexKeepProguard file('{keepFileName}')";
            if (gradle.Contains(keepLine, StringComparison.Ordinal))
            {
                return gradle;
            }

            if (gradle.Contains("multiDexKeepProguard", StringComparison.Ordinal))
            {
                return gradle;
            }

            if (Regex.IsMatch(gradle, @"defaultConfig\s*\{[^}]*multiDexEnabled\s+true", RegexOptions.Singleline))
            {
                return Regex.Replace(
                    gradle,
                    @"(multiDexEnabled\s+true)",
                    $"$1\n        {keepLine}",
                    RegexOptions.Multiline);
            }

            return Regex.Replace(
                gradle,
                @"(defaultConfig\s*\{)",
                $"$1\n        multiDexEnabled true\n        {keepLine}",
                RegexOptions.Multiline);
        }

        /// <summary>
        /// Unity 2022.3 + JDK 11: pin dependencies that break D8 in AGP 7.4; keep Java 11 on launcher.
        /// See docs/android-build-unity-2022-jdk11.md
        /// </summary>
        private static string ApplyUnity2022Jdk11DexPins(string gradle)
        {
            gradle = gradle.Replace("JavaVersion.VERSION_17", "JavaVersion.VERSION_11");

            if (gradle.Contains("error_prone_annotations:2.20.0", StringComparison.Ordinal))
            {
                return gradle;
            }

            return Regex.Replace(
                gradle,
                @"(apply plugin:[^\n]+\n(?:apply plugin:[^\n]+\n)*)",
                "$1" + Jdk11DexPinBlock,
                RegexOptions.Multiline);
        }

        private static bool TryResolveLauncherGradlePath(string generatedModulePath, out string launcherGradlePath)
        {
            launcherGradlePath = null;
            if (string.IsNullOrWhiteSpace(generatedModulePath))
            {
                return false;
            }

            var modulePath = Path.GetFullPath(generatedModulePath.Trim());
            var candidates = new[]
            {
                Path.Combine(modulePath, "..", "launcher", "build.gradle"),
                Path.Combine(modulePath, "launcher", "build.gradle"),
                Path.Combine(Directory.GetParent(modulePath)?.FullName ?? modulePath, "launcher", "build.gradle"),
            };

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    launcherGradlePath = full;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveMultidexKeepSource(out string absolutePath)
        {
            absolutePath = null;

            var hostPlugins = Path.Combine(Application.dataPath, "Plugins", "Android", MultidexKeepFileName);
            if (File.Exists(hostPlugins))
            {
                absolutePath = Path.GetFullPath(hostPlugins);
                return true;
            }

            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                          $"Packages/{ABIAdsEditorPaths.PackageName}/package.json")
                      ?? UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                          $"Packages/{ABIAdsEditorPaths.PackageName}/Editor/ABIAdsLauncherMultidexGradlePostProcessor.cs");
            if (pkg != null)
            {
                var fromPackage = Path.Combine(pkg.resolvedPath, "Plugins", "Android", MultidexKeepFileName);
                if (File.Exists(fromPackage))
                {
                    absolutePath = Path.GetFullPath(fromPackage);
                    return true;
                }
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var direct = Path.Combine(projectRoot, "Packages", ABIAdsEditorPaths.PackageName, "Plugins", "Android", MultidexKeepFileName);
            if (File.Exists(direct))
            {
                absolutePath = Path.GetFullPath(direct);
                return true;
            }

            var cacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot))
            {
                return false;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(cacheRoot, $"{ABIAdsEditorPaths.PackageName}@*", SearchOption.TopDirectoryOnly))
                {
                    var hit = Path.Combine(dir, "Plugins", "Android", MultidexKeepFileName);
                    if (File.Exists(hit))
                    {
                        absolutePath = Path.GetFullPath(hit);
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }
    }
}
#endif
