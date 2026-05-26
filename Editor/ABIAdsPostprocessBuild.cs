#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsPostprocessBuild : IPostprocessBuildWithReport
    {
        private const string GoogleMobileAdsAppIdKey = "GADApplicationIdentifier";
        private const string AppLovinSdkKey = "AppLovinSdkKey";
        private const string EnvironmentGoogleMobileAdsAppIdKey = "ABI_IOS_GOOGLE_AD_APP_ID";
        private const string EnvironmentMaxSdkKey = "ABI_IOS_MAX_SDK_KEY";

        private const string BblModuleAdsFrameworkDirName = "BBLModuleAds.framework";
        private const string PodfileTemplateFileName = "Podfile.template";
        private const string EnvironmentPodfileOverwriteKey = "ABI_IOS_PODFILE_OVERWRITE";
        private const string EnvironmentSkipPodInstallKey = "ABI_IOS_SKIP_POD_INSTALL";
        private const string EnvironmentPodNoRepoUpdateKey = "ABI_IOS_POD_NO_REPO_UPDATE";

        private static readonly Regex PodfileProjectLineRegex = new Regex(
            @"^\s*project\s+['""]([^'""]+)['""]\s*$",
            RegexOptions.Multiline);

        public int callbackOrder => 10000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            RunPipelineOnExportFolder(report.summary.outputPath);
        }

        private static void RunPipelineOnExportFolder(string buildPath)
        {
            if (string.IsNullOrWhiteSpace(buildPath))
            {
                Debug.LogWarning("ABI Ads: đường dẫn export iOS trống — bỏ qua pipeline.");
                return;
            }

            buildPath = Path.GetFullPath(buildPath.Trim());
            if (!Directory.Exists(buildPath))
            {
                Debug.LogWarning($"ABI Ads: không có thư mục `{buildPath}` — bỏ qua pipeline.");
                return;
            }

            PatchInfoPlist(buildPath);
            EmitPodfileFromTemplate(buildPath);
            SyncPodfileProjectLineToExport(buildPath);
            RunPodInstallInExport(buildPath);
            EnsureBblModuleAdsEmbeddedOnce(buildPath);
        }

        [MenuItem("ABI Ads/iOS/Chạy Podfile + pod install + embed (chọn thư mục export…)")]
        private static void MenuRunPipelineOnExportFolder()
        {
            var startDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = EditorUtility.OpenFolderPanel(
                "Chọn thư mục sau Build iOS (ví dụ Unity-build — có Unity-iPhone.xcodeproj)",
                startDir,
                "");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            RunPipelineOnExportFolder(path);
        }

        private static void PatchInfoPlist(string buildPath)
        {
            var plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogWarning($"ABI Ads could not find iOS Info.plist at `{plistPath}`.");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var settings = ABIAdsBuildSettings.Load();
            var appId = ABIAdsBuildSettings.ResolveEnvironmentString(EnvironmentGoogleMobileAdsAppIdKey, settings.admob_app_id);
            plist.root.SetString(GoogleMobileAdsAppIdKey, appId);

            var maxSdkKey = ABIAdsBuildSettings.ResolveEnvironmentString(EnvironmentMaxSdkKey, settings.max_sdk_key);
            if (!string.IsNullOrWhiteSpace(maxSdkKey))
            {
                plist.root.SetString(AppLovinSdkKey, maxSdkKey);
            }

            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log(
                $"ABI Ads wrote `{GoogleMobileAdsAppIdKey}`" +
                (string.IsNullOrWhiteSpace(maxSdkKey) ? string.Empty : $" and `{AppLovinSdkKey}`") +
                " to iOS Info.plist.");
        }

        private static void EmitPodfileFromTemplate(string buildPath)
        {
            if (!TryResolvePodfileTemplate(out var templatePath))
            {
                Debug.LogWarning(
                    $"ABI Ads: không tìm thấy `{PodfileTemplateFileName}` (package `{ABIAdsEditorPaths.PackageName}`). " +
                    "Kiểm tra Package Manager đã cài package; file mẫu phải có tại `Plugins/iOS/Podfile.template` trong package. " +
                    "Không tạo Podfile — `pod install` sẽ bị bỏ qua.");
                return;
            }

            var destPath = Path.Combine(buildPath, "Podfile");
            var overwrite = ABIAdsBuildSettings.ResolveEnvironmentBool(EnvironmentPodfileOverwriteKey);
            if (File.Exists(destPath) && !overwrite)
            {
                Debug.Log(
                    $"ABI Ads: đã có `{destPath}` — giữ nguyên nội dung. (Ghi đè từ template: xóa Podfile hoặc `{EnvironmentPodfileOverwriteKey}=1`.) " +
                    $"Trên macOS, bước tiếp theo vẫn sẽ chạy `pod install` nếu không tắt bằng `{EnvironmentSkipPodInstallKey}`.");
                return;
            }

            var existedBefore = File.Exists(destPath);
            var body = File.ReadAllText(templatePath);
            body = SubstitutePodfileProjectLineForExport(body, buildPath);
            File.WriteAllText(destPath, body);
            if (overwrite && existedBefore)
            {
                Debug.Log(
                    $"ABI Ads: đã ghi đè Podfile tại `{destPath}` từ template (do `{EnvironmentPodfileOverwriteKey}`).");
            }
            else
            {
                Debug.Log($"ABI Ads: đã tạo Podfile tại `{destPath}`.");
            }
        }

        private static void SyncPodfileProjectLineToExport(string buildPath)
        {
            var podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath))
            {
                return;
            }

            if (!TryFindMainUserXcodeProject(buildPath, out var projectFileName))
            {
                Debug.LogWarning(
                    "ABI Ads: không thấy .xcodeproj nào (ngoài Pods) trong thư mục export — không chỉnh dòng `project` trong Podfile. " +
                    "Hãy build iOS vào đúng thư mục này trước khi chạy pipeline.");
                return;
            }

            var text = File.ReadAllText(podfilePath);
            var newText = SubstitutePodfileProjectLine(text, projectFileName);
            if (newText == text)
            {
                return;
            }

            File.WriteAllText(podfilePath, newText);
            Debug.Log($"ABI Ads: đã cập nhật Podfile → `project '{projectFileName}'` (khớp bản export Unity).");
        }

        private static bool TryFindMainUserXcodeProject(string buildPath, out string projectFileName)
        {
            projectFileName = null;
            if (!Directory.Exists(buildPath))
            {
                return false;
            }

            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(buildPath, "*.xcodeproj", SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                return false;
            }

            var names = dirs
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n) &&
                            !n.Equals("Pods.xcodeproj", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (names.Count == 0)
            {
                return false;
            }

            projectFileName = names.FirstOrDefault(n =>
                n.Equals("Unity-iPhone.xcodeproj", StringComparison.OrdinalIgnoreCase)) ?? names[0];
            return true;
        }

        private static string SubstitutePodfileProjectLineForExport(string podfileBody, string buildPath)
        {
            if (!TryFindMainUserXcodeProject(buildPath, out var projectFileName))
            {
                return podfileBody;
            }

            return SubstitutePodfileProjectLine(podfileBody, projectFileName);
        }

        private static string SubstitutePodfileProjectLine(string podfileBody, string projectFileName)
        {
            return PodfileProjectLineRegex.Replace(podfileBody, $"project '{projectFileName}'");
        }

        private static void RunPodInstallInExport(string buildPath)
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                Debug.Log(
                    "ABI Ads: không chạy `pod install` tự động (chỉ hỗ trợ Unity Editor trên macOS). " +
                    "Trên máy build iOS, mở thư mục export và chạy `pod install --repo-update`, rồi mở `.xcworkspace`.");
                return;
            }

            if (ABIAdsBuildSettings.ResolveEnvironmentBool(EnvironmentSkipPodInstallKey))
            {
                Debug.Log(
                    $"ABI Ads: bỏ qua `pod install` (biến môi trường `{EnvironmentSkipPodInstallKey}` được bật). " +
                    "Mở thư mục export và chạy tay nếu cần CocoaPods.");
                return;
            }

            var podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath))
            {
                Debug.Log("ABI Ads: không có Podfile trong thư mục export — bỏ qua `pod install`.");
                return;
            }

            try
            {
                var forceNoRepo = ABIAdsBuildSettings.ResolveEnvironmentBool(EnvironmentPodNoRepoUpdateKey);

                if (!forceNoRepo)
                {
                    var code = RunPodShell(buildPath, "pod install --repo-update", out var out1, out var err1);
                    if (code == 0)
                    {
                        LogPodSuccess("pod install --repo-update", out1, err1);
                        return;
                    }

                    LogPodFailure("pod install --repo-update", code, out1, err1);
                    Debug.LogWarning(
                        "ABI Ads: `pod install --repo-update` thất bại — thử lại `pod install` (không cập nhật repo specs). " +
                        $"Hoặc set `{EnvironmentPodNoRepoUpdateKey}=1` rồi chạy lại; hoặc trong Terminal: " +
                        $"`export LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 && cd \\\"{buildPath}\\\" && pod install`.");
                }

                var code2 = RunPodShell(buildPath, "pod install", out var out2, out var err2);
                if (code2 == 0)
                {
                    LogPodSuccess("pod install", out2, err2);
                    return;
                }

                LogPodFailure("pod install", code2, out2, err2);
                Debug.LogError(
                    "ABI Ads: `pod install` vẫn thất bại. Kiểm tra: `pod --version`, `xcode-select -p`, quyền ghi thư mục export, " +
                    "và log chi tiết phía trên (stdout/stderr từ CocoaPods).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ABI Ads: không chạy được `pod install`: {ex.Message}");
            }
        }

        private static int RunPodShell(string buildPath, string podCommand, out StringBuilder stdout, out StringBuilder stderr)
        {
            var outBuf = new StringBuilder();
            var errBuf = new StringBuilder();
            var buildPathForShell = buildPath.Replace("\"", "\\\"");
            var bashArgs =
                $"-l -c \"export LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8; cd \\\"{buildPathForShell}\\\" && {podCommand}\"";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = bashArgs,
                WorkingDirectory = buildPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outBuf.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errBuf.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            stdout = outBuf;
            stderr = errBuf;
            return process.ExitCode;
        }

        private static void LogPodSuccess(string command, StringBuilder stdout, StringBuilder stderr)
        {
            if (stdout.Length > 0)
            {
                Debug.Log($"ABI Ads [{command} stdout]\n{stdout}");
            }

            if (stderr.Length > 0)
            {
                Debug.Log($"ABI Ads [{command} stderr]\n{stderr}");
            }

            Debug.Log(
                $"ABI Ads: `{command}` hoàn tất. Mở `Unity-iPhone.xcworkspace` trong thư mục export để build Xcode.");
        }

        private static void LogPodFailure(string command, int exitCode, StringBuilder stdout, StringBuilder stderr)
        {
            var detail = new StringBuilder();
            detail.AppendLine($"ABI Ads: `{command}` thoát với mã {exitCode}. Chi tiết CocoaPods:");
            if (stdout.Length > 0)
            {
                detail.AppendLine("--- stdout ---");
                detail.Append(stdout);
            }

            if (stderr.Length > 0)
            {
                detail.AppendLine("--- stderr ---");
                detail.Append(stderr);
            }

            Debug.LogError(detail.Length > 0 ? detail.ToString() : $"ABI Ads: `{command}` thoát với mã {exitCode} (không có stdout/stderr).");
        }

        private static bool TryResolvePodfileTemplate(out string absolutePath)
        {
            absolutePath = null;
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{ABIAdsEditorPaths.PackageName}/package.json")
                      ?? UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                          $"Packages/{ABIAdsEditorPaths.PackageName}/Editor/ABIAdsPostprocessBuild.cs");
            if (pkg != null)
            {
                var fromResolved = Path.Combine(pkg.resolvedPath, "Plugins", "iOS", PodfileTemplateFileName);
                if (File.Exists(fromResolved))
                {
                    absolutePath = Path.GetFullPath(fromResolved);
                    return true;
                }
            }

            var directUnderPackages = Path.Combine(
                projectRoot, "Packages", ABIAdsEditorPaths.PackageName, "Plugins", "iOS", PodfileTemplateFileName);
            if (File.Exists(directUnderPackages))
            {
                absolutePath = Path.GetFullPath(directUnderPackages);
                return true;
            }

            var cacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(cacheRoot))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(cacheRoot, $"{ABIAdsEditorPaths.PackageName}@*", SearchOption.TopDirectoryOnly))
                    {
                        var hit = Path.Combine(dir, "Plugins", "iOS", PodfileTemplateFileName);
                        if (File.Exists(hit))
                        {
                            absolutePath = Path.GetFullPath(hit);
                            return true;
                        }
                    }

                    foreach (var dir in Directory.GetDirectories(cacheRoot, ABIAdsEditorPaths.PackageName, SearchOption.TopDirectoryOnly))
                    {
                        var hit = Path.Combine(dir, "Plugins", "iOS", PodfileTemplateFileName);
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
            }

            var candidates = new List<string>();
            foreach (var subDir in new[] { "Packages", "Assets" })
            {
                var root = Path.Combine(projectRoot, subDir);
                if (!Directory.Exists(root))
                {
                    continue;
                }

                try
                {
                    foreach (var hit in Directory.GetFiles(root, PodfileTemplateFileName, SearchOption.AllDirectories))
                    {
                        var norm = hit.Replace('\\', '/');
                        if (norm.IndexOf("/Library/", StringComparison.Ordinal) >= 0 ||
                            norm.IndexOf("/Temp/", StringComparison.Ordinal) >= 0)
                        {
                            continue;
                        }

                        candidates.Add(hit);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            absolutePath = Path.GetFullPath(candidates
                .OrderByDescending(p => p.IndexOf(ABIAdsEditorPaths.PackageName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenBy(p => p.Length)
                .First());
            return true;
        }

        private static void EnsureBblModuleAdsEmbeddedOnce(string buildPath)
        {
            if (!TryResolveBblModuleAdsProjectRelativePath(buildPath, out var relativePath))
            {
                Debug.Log("ABI Ads: không thấy BBLModuleAds.framework trong bản export iOS — bỏ qua chỉnh Xcode (có thể chưa import plugin iOS).");
                return;
            }

            var projPath = PBXProject.GetPBXProjectPath(buildPath);
            if (!File.Exists(projPath))
            {
                Debug.LogWarning($"ABI Ads: không tìm thấy Xcode project tại `{projPath}`.");
                return;
            }

            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            var frameworkTarget = proj.GetUnityFrameworkTargetGuid();
            if (string.IsNullOrEmpty(frameworkTarget))
            {
                Debug.LogWarning("ABI Ads: export không có target UnityFramework — bỏ qua chỉnh embed BBLModuleAds (Unity quá cũ hoặc bản export đặc biệt).");
                return;
            }

            var mainTarget = proj.GetUnityMainTargetGuid();

            var fileGuid = proj.FindFileGuidByProjectPath(relativePath);
            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = proj.FindFileGuidByRealPath(relativePath);
            }

            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = proj.AddFile(relativePath, relativePath, PBXSourceTree.Source);
            }

            proj.AddFileToEmbedFrameworks(frameworkTarget, fileGuid);
            if (!string.IsNullOrEmpty(mainTarget))
            {
                proj.AddFileToEmbedFrameworks(mainTarget, fileGuid);
            }

            proj.WriteToFile(projPath);
            Debug.Log(
                $"ABI Ads: đã nhúng (Embed & Sign) {BblModuleAdsFrameworkDirName} trên UnityFramework" +
                (string.IsNullOrEmpty(mainTarget) ? "" : " và Unity-iPhone") +
                $" — đường dẫn project: {relativePath}. Nếu trước đó từng gỡ embed tay, build lại Xcode/Archive.");
        }

        private static bool TryResolveBblModuleAdsProjectRelativePath(string buildPath, out string relativePath)
        {
            relativePath = null;
            var root = Path.GetFullPath(buildPath);
            var matches = Directory.GetDirectories(root, BblModuleAdsFrameworkDirName, SearchOption.AllDirectories);
            if (matches.Length == 0)
            {
                return false;
            }

            var normalized = matches.Select(Path.GetFullPath).ToArray();
            var preferred = normalized.FirstOrDefault(p =>
                p.IndexOf($"{Path.DirectorySeparatorChar}Libraries{Path.DirectorySeparatorChar}", StringComparison.Ordinal) >= 0)
                ?? normalized[0];

            relativePath = ProjectRelativePath(root, preferred);
            return true;
        }

        private static string ProjectRelativePath(string projectRootFull, string pathFull)
        {
            var root = projectRootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(pathFull);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return full.Replace('\\', '/');
            }

            return full.Substring(root.Length).Replace('\\', '/');
        }

    }
}
#endif
