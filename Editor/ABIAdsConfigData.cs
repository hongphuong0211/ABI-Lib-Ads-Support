using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal static class ABIAdsEditorPaths
    {
        internal const string PackageName = "com.abi.ads.unity";
        internal const string ResourceFolderInProject = "Resources/Configs";
        internal const string ResourceFolderInPackage = "Resources/Configs";
        internal const string GlobalConfigFileName = "global_config.json";
        internal const string PlacementsFileName = "placements.json";
        internal const string PlacementsAssetFileName = "1.txt";
        internal const string GlobalAssetFileName = "2.txt";
        internal const string DefaultGoogleMobileAdsAppId = "ca-app-pub-3940256099942544~3347511713";

        private const string AndroidLayoutFolderFromRepoRoot = "ads/src/main/res/layout";
        private const string IosNativeAdsFolderFromRepoRoot = "ios-project/BBL-Module-Ads/BBL-Module-Ads/UI/NativeAds";

        internal static string GlobalConfigPath()
        {
            return Path.Combine(ConfigRoot(), GlobalConfigFileName);
        }

        internal static string PlacementsPath()
        {
            return Path.Combine(ConfigRoot(), PlacementsFileName);
        }

        internal static string PlacementsAssetPath()
        {
            return Path.Combine(ConfigRoot(), PlacementsAssetFileName);
        }

        internal static string GlobalAssetPath()
        {
            return Path.Combine(ConfigRoot(), GlobalAssetFileName);
        }

        internal static string ConfigRoot()
        {
            return Path.Combine(Application.dataPath, ResourceFolderInProject);
        }

        internal static string PackageConfigRoot()
        {
            return Path.Combine(ResolvePackageRoot(), ResourceFolderInPackage);
        }

        internal static string PackageGlobalConfigPath()
        {
            return Path.Combine(PackageConfigRoot(), GlobalConfigFileName);
        }

        internal static string PackagePlacementsPath()
        {
            return Path.Combine(PackageConfigRoot(), PlacementsFileName);
        }

        internal static string PackageGlobalAssetPath()
        {
            return Path.Combine(PackageConfigRoot(), GlobalAssetFileName);
        }

        internal static string PackagePlacementsAssetPath()
        {
            return Path.Combine(PackageConfigRoot(), PlacementsAssetFileName);
        }

        internal static string ResolvePackageRoot()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{PackageName}/package.json");
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return packageInfo.resolvedPath;
            }

            var editorScriptGuids = AssetDatabase.FindAssets("ABIAdsBuildSettings t:Script");
            foreach (var guid in editorScriptGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith("Editor/ABIAdsBuildSettings.cs", StringComparison.Ordinal))
                {
                    return Directory.GetParent(Path.GetDirectoryName(Path.GetFullPath(assetPath))).FullName;
                }
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", PackageName));
        }

        internal static string[] LoadNativeAdLayoutValues()
        {
            var repoRoot = ResolveRepositoryRoot(ResolvePackageRoot());
            var androidLayoutFolder = Path.Combine(repoRoot, AndroidLayoutFolderFromRepoRoot);
            var iosNativeAdsFolder = Path.Combine(repoRoot, IosNativeAdsFolderFromRepoRoot);
            var androidLayoutNames = LoadLayoutFileNames(androidLayoutFolder, "*.xml");
            var iosLayoutNames = LoadLayoutFileNames(iosNativeAdsFolder, "*.xib");

            if (androidLayoutNames.Count == 0)
            {
                return SortedValues(iosLayoutNames);
            }

            if (iosLayoutNames.Count == 0)
            {
                return SortedValues(androidLayoutNames);
            }

            var sharedLayoutNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var androidLayoutName in androidLayoutNames)
            {
                if (iosLayoutNames.Contains(androidLayoutName))
                {
                    sharedLayoutNames.Add(androidLayoutName);
                }
            }

            return SortedValues(sharedLayoutNames);
        }

        private static string ResolveRepositoryRoot(string packageRoot)
        {
            var current = new DirectoryInfo(packageRoot);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, AndroidLayoutFolderFromRepoRoot)) ||
                    Directory.Exists(Path.Combine(current.FullName, IosNativeAdsFolderFromRepoRoot)))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return Path.GetFullPath(Path.Combine(packageRoot, "..", ".."));
        }

        private static HashSet<string> LoadLayoutFileNames(string folderPath, string searchPattern)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (!Directory.Exists(folderPath))
            {
                return names;
            }

            foreach (var filePath in Directory.GetFiles(folderPath, searchPattern, SearchOption.TopDirectoryOnly))
            {
                names.Add(Path.GetFileNameWithoutExtension(filePath));
            }

            return names;
        }

        private static string[] SortedValues(HashSet<string> values)
        {
            var sorted = new List<string>(values);
            sorted.Sort(StringComparer.Ordinal);
            return sorted.ToArray();
        }
    }

    internal static class ABIAdsConfigStore
    {
        internal static ABIAdsGlobalConfigData LoadGlobalConfig()
        {
            var path = ABIAdsEditorPaths.GlobalConfigPath();
            if (!File.Exists(path))
            {
                return ABIAdsGlobalConfigData.CreateDefault();
            }

            try
            {
                var raw = File.ReadAllText(path);
                if (ABIAdsConfigFileProbe.LooksLikeEncryptedPayload(raw))
                {
                    Debug.LogWarning(
                        $"ABI Ads: `{path}` đang là ciphertext. Unity Editor chỉ chỉnh JSON thuần — " +
                        "dùng admin-web (export) hoặc `npm run encrypt:unity-configs -- --decrypt` để tạo bản plaintext, " +
                        "hoặc đặt `2.txt` / `1.txt` đã mã hóa cho build release.");
                    return ABIAdsGlobalConfigData.CreateDefault();
                }

                var root = JsonUtility.FromJson<ABIAdsGlobalConfigRoot>(raw);
                if (root != null && root.global_config != null)
                {
                    root.global_config.EnsureDefaults();
                    return root.global_config;
                }

                var direct = JsonUtility.FromJson<ABIAdsGlobalConfigData>(raw);
                if (direct != null)
                {
                    direct.EnsureDefaults();
                    return direct;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse global config `{path}`: {ex.Message}");
            }

            return ABIAdsGlobalConfigData.CreateDefault();
        }

        internal static ABIAdsPlacementsRoot LoadPlacements()
        {
            var path = ABIAdsEditorPaths.PlacementsPath();
            if (!File.Exists(path))
            {
                return ABIAdsPlacementsRoot.CreateDefault();
            }

            try
            {
                var raw = File.ReadAllText(path);
                if (ABIAdsConfigFileProbe.LooksLikeEncryptedPayload(raw))
                {
                    Debug.LogWarning(
                        $"ABI Ads: `{path}` đang là ciphertext. Unity Editor chỉ chỉnh JSON thuần — " +
                        "dùng admin-web hoặc đặt `1.txt` từ export cho build release.");
                    return ABIAdsPlacementsRoot.CreateDefault();
                }

                var root = JsonUtility.FromJson<ABIAdsPlacementsRoot>(raw);
                if (root != null)
                {
                    root.EnsureDefaults();
                    return root;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse placements config `{path}`: {ex.Message}");
            }

            return ABIAdsPlacementsRoot.CreateDefault();
        }

        internal static void SaveGlobalConfig(ABIAdsGlobalConfigData config)
        {
            var path = ABIAdsEditorPaths.GlobalConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var plaintext = WriteGlobalJson(config);
            File.WriteAllText(path, plaintext);
            AssetDatabase.Refresh();
            Debug.Log($"ABI Ads global config saved (JSON thuần) → `{path}`. Mã hóa: admin-web export / `1.txt` `2.txt`.");
        }

        internal static void SavePlacements(ABIAdsPlacementsRoot root)
        {
            var path = ABIAdsEditorPaths.PlacementsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var plaintext = WritePlacementsJson(root);
            File.WriteAllText(path, plaintext);
            AssetDatabase.Refresh();
            Debug.Log($"ABI Ads placement config saved (JSON thuần) → `{path}`. Mã hóa: admin-web export / `1.txt`.");
        }

        private static string WriteGlobalJson(ABIAdsGlobalConfigData config)
        {
            config.EnsureDefaults();
            return JsonUtility.ToJson(config, true) + Environment.NewLine;
        }

        private static string WritePlacementsJson(ABIAdsPlacementsRoot root)
        {
            root.EnsureDefaults();
            var saveRoot = new ABIAdsPlacementsRoot { placements = new List<ABIAdsPlacementConfig>() };
            foreach (var placement in root.placements)
            {
                saveRoot.placements.Add(placement.CreateSaveCopy());
            }

            return JsonUtility.ToJson(saveRoot, true) + Environment.NewLine;
        }
    }

    internal static class ABIAdsConfigGui
    {
        internal static void DrawPath(string label, string path)
        {
            EditorGUILayout.LabelField(label, path ?? string.Empty, EditorStyles.miniLabel);
        }

        internal static void DrawStringList(string label, List<string> values)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            for (var i = 0; i < values.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    values[i] = EditorGUILayout.TextField(values[i] ?? string.Empty);
                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        values.RemoveAt(i);
                        GUI.FocusControl(null);
                    }
                }
            }

            if (GUILayout.Button($"Add {label}"))
            {
                values.Add(string.Empty);
            }
        }

        internal static string DrawHexColorField(string label, string hex)
        {
            var value = hex ?? string.Empty;
            var color = Color.white;
            ColorUtility.TryParseHtmlString(value, out color);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                color = EditorGUILayout.ColorField(new GUIContent(label), color, false, false, false);
                if (EditorGUI.EndChangeCheck())
                {
                    value = "#" + ColorUtility.ToHtmlStringRGB(color);
                }

                if (GUILayout.Button("Clear", GUILayout.Width(48)))
                {
                    value = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            return value;
        }

        internal static string StringPopup(string label, string value, string[] values)
        {
            var index = Array.IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, values)];
        }

        internal static string LayoutFilePopup(string label, string value, string[] supportedValues)
        {
            value = value ?? string.Empty;
            if (supportedValues == null || supportedValues.Length == 0)
            {
                return EditorGUILayout.TextField(label, value);
            }

            var values = BuildLayoutPopupValues(value, supportedValues);
            var labels = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                labels[i] = string.IsNullOrEmpty(values[i]) ? "<Default>" : values[i];
            }

            var index = IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, labels)];
        }

        internal static int IntPopup(string label, int value, string[] labels, int[] values)
        {
            var index = Array.IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, labels)];
        }

        internal static int IntPopup(GUIContent label, int value, string[] labels, int[] values, params GUILayoutOption[] options)
        {
            var index = Array.IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, labels, options)];
        }

        private static string[] BuildLayoutPopupValues(string currentValue, string[] supportedValues)
        {
            var values = new List<string> { string.Empty };
            foreach (var supportedValue in supportedValues)
            {
                if (!string.IsNullOrEmpty(supportedValue) && IndexOf(values, supportedValue) < 0)
                {
                    values.Add(supportedValue);
                }
            }

            if (!string.IsNullOrEmpty(currentValue) && IndexOf(values, currentValue) < 0)
            {
                values.Add(currentValue);
            }

            return values.ToArray();
        }

        private static int IndexOf(IList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    [Serializable]
    internal sealed class ABIAdsGlobalConfigRoot
    {
        public ABIAdsGlobalConfigData global_config;
    }

    [Serializable]
    internal sealed class ABIAdsGlobalConfigData
    {
        public int mediation_provider;
        public int timeout_remote = 5000;
        public bool variant_dev = true;
        public List<string> enabled_versions = new List<string>();
        public List<string> test_devices = new List<string>();
        public bool enable_adjust;
        public bool enable_appsflyer;
        public bool enable_facebook;
        public bool enable_tiktok;
        public bool enable_firebase;
        public bool enable_fcm;
        public string adjust_token = string.Empty;
        public string appsflyer_token = string.Empty;
        public string facebook_client_token = string.Empty;
        public string tiktok_app_id = string.Empty;
        public string tiktok_access_token = string.Empty;
        public string app_id_tt = string.Empty;
        public bool enable_adjust_tracking;
        public bool enable_appsflyer_tracking;
        public bool enable_realtime_database_tracking;
        public string config_version = string.Empty;
        public int inter_ad_interval;
        public List<string> skip_interval_placements = new List<string>();
        public string admob_app_id = ABIAdsEditorPaths.DefaultGoogleMobileAdsAppId;
        public string max_sdk_key = string.Empty;
        public bool max_consent_flow_enabled;
        public string max_privacy_policy_url = string.Empty;
        public string max_terms_of_service_url = string.Empty;
        public bool max_show_terms_privacy_alert_in_gdpr;
        public bool max_consent_debug_geography_gdpr;

        public static ABIAdsGlobalConfigData CreateDefault()
        {
            var data = new ABIAdsGlobalConfigData();
            data.EnsureDefaults();
            return data;
        }

        public void EnsureDefaults()
        {
            if (timeout_remote <= 0)
            {
                timeout_remote = 5000;
            }

            if (enabled_versions == null)
            {
                enabled_versions = new List<string>();
            }

            if (test_devices == null)
            {
                test_devices = new List<string>();
            }

            if (skip_interval_placements == null)
            {
                skip_interval_placements = new List<string>();
            }

            if (string.IsNullOrWhiteSpace(admob_app_id))
            {
                admob_app_id = ABIAdsEditorPaths.DefaultGoogleMobileAdsAppId;
            }
        }
    }

    [Serializable]
    internal sealed class ABIAdsPlacementsRoot
    {
        public List<ABIAdsPlacementConfig> placements = new List<ABIAdsPlacementConfig>();

        public static ABIAdsPlacementsRoot CreateDefault()
        {
            var root = new ABIAdsPlacementsRoot();
            root.placements.Add(ABIAdsPlacementConfig.CreateDefault());
            root.EnsureDefaults();
            return root;
        }

        public void EnsureDefaults()
        {
            if (placements == null)
            {
                placements = new List<ABIAdsPlacementConfig>();
            }

            foreach (var placement in placements)
            {
                placement.EnsureDefaults();
            }
        }
    }

    [Serializable]
    internal sealed class ABIAdsPlacementConfig
    {
        public string ad_name = "main_interstitial";
        public string ads_type = "interstitial";
        public List<ABIAdsAdIdConfig> ad_ids = new List<ABIAdsAdIdConfig>();
        public List<ABIAdsAdIdConfig> backup_ad_ids = new List<ABIAdsAdIdConfig>();
        public bool is_show = true;
        public bool is_organic_show = true;
        public string config_version = string.Empty;
        public bool prioritize_by_weight = true;
        public List<string> disable_version = new List<string>();
        public string activity_trigger_load = string.Empty;
        public bool activity_load_and_show;
        public int delay_time_trigger_load;
        public string activity_trigger_show = string.Empty;
        public int delay_time_trigger_show;
        public string click_trigger_view_id = string.Empty;
        public bool click_load_and_show;
        public int click_delay_ms;
        public string click_trigger_show_view_id = string.Empty;
        public int click_trigger_show_delay_ms;
        public string click_trigger_count_view_id = string.Empty;
        public int click_trigger_count_threshold;
        public int click_trigger_count_delay_ms;
        public ABIAdsBannerAdConfig banner_ad = new ABIAdsBannerAdConfig();
        public ABIAdsNativeAdConfig native_ad = new ABIAdsNativeAdConfig();
        [NonSerialized] public bool foldout = true;

        public static ABIAdsPlacementConfig CreateDefault()
        {
            var placement = new ABIAdsPlacementConfig();
            placement.ad_ids.Add(new ABIAdsAdIdConfig());
            placement.EnsureDefaults();
            return placement;
        }

        public ABIAdsPlacementConfig Clone()
        {
            var clone = JsonUtility.FromJson<ABIAdsPlacementConfig>(JsonUtility.ToJson(this));
            clone.ad_name = string.IsNullOrEmpty(clone.ad_name) ? "placement_copy" : clone.ad_name + "_copy";
            clone.EnsureDefaults();
            return clone;
        }

        public ABIAdsPlacementConfig CreateSaveCopy()
        {
            var copy = JsonUtility.FromJson<ABIAdsPlacementConfig>(JsonUtility.ToJson(this));
            copy.EnsureDefaults();
            if (copy.ads_type != "banner" && copy.ads_type != "mrec")
            {
                copy.banner_ad = null;
            }

            if (copy.ads_type != "native")
            {
                copy.native_ad = null;
            }

            return copy;
        }

        public void EnsureDefaults()
        {
            if (string.IsNullOrEmpty(ads_type))
            {
                ads_type = "interstitial";
            }

            if (ad_ids == null)
            {
                ad_ids = new List<ABIAdsAdIdConfig>();
            }

            if (backup_ad_ids == null)
            {
                backup_ad_ids = new List<ABIAdsAdIdConfig>();
            }

            if (disable_version == null)
            {
                disable_version = new List<string>();
            }

            if (banner_ad == null)
            {
                banner_ad = new ABIAdsBannerAdConfig();
            }

            if (native_ad == null)
            {
                native_ad = new ABIAdsNativeAdConfig();
            }

            native_ad.EnsureDefaults();
            foldout = true;
        }
    }

    [Serializable]
    internal sealed class ABIAdsAdIdConfig
    {
        public string ad_id = string.Empty;
        public int ads_weight = 1;
        public int mediation;
    }

    [Serializable]
    internal sealed class ABIAdsBannerAdConfig
    {
        public string inline_style = string.Empty;
        public bool use_inline_adaptive;
        public bool use_collapsible;
        public string collapsible_gravity = string.Empty;
        public string banner_size = string.Empty;
        public int reload_time;
    }

    [Serializable]
    internal sealed class ABIAdsNativeAdConfig
    {
        public string ad_layout_file = string.Empty;
        public string organic_layout = string.Empty;
        public string layout_meta = string.Empty;
        public string bg_color = string.Empty;
        public string border_color = string.Empty;
        public float corner_radius_dp;
        public int stroke_width_dp = 1;
        public string headline_text_color = string.Empty;
        public string body_text_color = string.Empty;
        public string price_text_color = string.Empty;
        public string advertiser_text_color = string.Empty;
        public ABIAdsClickedConfig clicked = new ABIAdsClickedConfig();

        public void EnsureDefaults()
        {
            if (clicked == null)
            {
                clicked = new ABIAdsClickedConfig();
            }
        }
    }

    [Serializable]
    internal sealed class ABIAdsClickedConfig
    {
        public string btn_act_color = string.Empty;
        public string btn_act_text_color = string.Empty;
        public int delay_time_show_btn_next;
    }
}
