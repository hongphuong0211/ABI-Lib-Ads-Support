#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    /// <summary>
    /// Copy config vào Gradle <c>assets/1.txt</c> (placements) và <c>assets/2.txt</c> (global phẳng, đã mã hóa).
    /// </summary>
    internal sealed class ABIAdsAndroidStreamingAssetsCopy : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 9999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var placementsSrc = ResolveSource(ABIAdsEditorPaths.PlacementsPath(), ABIAdsEditorPaths.PackagePlacementsPath());
            var globalSrc = ResolveSource(ABIAdsEditorPaths.GlobalConfigPath(), ABIAdsEditorPaths.PackageGlobalConfigPath());
            var placementsEncryptedSrc = ResolveSource(ABIAdsEditorPaths.PlacementsAssetPath(), ABIAdsEditorPaths.PackagePlacementsAssetPath());
            var globalEncryptedSrc = ResolveSource(ABIAdsEditorPaths.GlobalAssetPath(), ABIAdsEditorPaths.PackageGlobalAssetPath());

            if (!File.Exists(placementsSrc) && !File.Exists(placementsEncryptedSrc))
            {
                Debug.LogWarning("ABI Ads: thiếu placements config — bỏ qua copy assets/1.txt.");
                return;
            }

            if (!File.Exists(globalSrc) && !File.Exists(globalEncryptedSrc))
            {
                Debug.LogWarning("ABI Ads: thiếu global config — bỏ qua copy assets/2.txt.");
                return;
            }

            var assetsDir = Path.Combine(path, "src", "main", "assets");
            Directory.CreateDirectory(assetsDir);

            var placementsDest = Path.Combine(assetsDir, ABIAdsEditorPaths.PlacementsAssetFileName);
            var globalDest = Path.Combine(assetsDir, ABIAdsEditorPaths.GlobalAssetFileName);

            CopyAsset(placementsEncryptedSrc, placementsSrc, placementsDest, "1.txt");
            CopyAsset(globalEncryptedSrc, globalSrc, globalDest, "2.txt");

            Debug.Log($"ABI Ads: copied config → `{placementsDest}` and `{globalDest}`.");
        }

        private static string ResolveSource(string gamePath, string packageFallbackPath)
        {
            return File.Exists(gamePath) ? gamePath : packageFallbackPath;
        }

        private static void CopyAsset(string encryptedSrc, string jsonSrc, string dest, string label)
        {
            if (File.Exists(encryptedSrc))
            {
                File.Copy(encryptedSrc, dest, true);
                return;
            }

            if (!File.Exists(jsonSrc))
            {
                return;
            }

            File.Copy(jsonSrc, dest, true);
            var raw = File.ReadAllText(jsonSrc);
            if (!ABIAdsConfigFileProbe.LooksLikeEncryptedPayload(raw))
            {
                Debug.LogWarning(
                    $"ABI Ads: assets/{label} đang là JSON thuần (dev). Release: đặt `Resources/Configs/{label}` từ admin-web export.");
            }
        }
    }
}
#endif
