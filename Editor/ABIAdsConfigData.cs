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
        internal enum ConfigLoadSource
        {
            Project,
            Package,
            Defaults
        }

        internal static ABIAdsGlobalConfigData LoadGlobalConfig(out ConfigLoadSource source)
        {
            var path = ResolveGlobalConfigPath(out source);
            if (path == null)
            {
                Debug.Log("ABI Ads: không tìm thấy global_config.json trong project hoặc package — dùng giá trị mặc định editor.");
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
                    source = ConfigLoadSource.Defaults;
                    return ABIAdsGlobalConfigData.CreateDefault();
                }

                if (!TryParseGlobalConfigJson(raw, out var parsed))
                {
                    Debug.LogWarning($"ABI Ads: không parse được global config `{path}` — dùng giá trị mặc định editor.");
                    source = ConfigLoadSource.Defaults;
                    return ABIAdsGlobalConfigData.CreateDefault();
                }

                parsed.EnsureDefaults();
                Debug.Log($"ABI Ads: loaded global config ({DescribeSource(source)}) → `{path}`.");
                return parsed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse global config `{path}`: {ex.Message}");
            }

            source = ConfigLoadSource.Defaults;
            return ABIAdsGlobalConfigData.CreateDefault();
        }

        private static bool TryParseGlobalConfigJson(string raw, out ABIAdsGlobalConfigData config)
        {
            config = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var trimmed = raw.Trim();
            if (trimmed.IndexOf("\"global_config\"", StringComparison.Ordinal) >= 0)
            {
                var wrapped = JsonUtility.FromJson<ABIAdsGlobalConfigRootJsonDto>(trimmed);
                if (wrapped?.global_config != null)
                {
                    config = wrapped.global_config.ToData();
                    return config != null;
                }
            }

            var direct = JsonUtility.FromJson<ABIAdsGlobalConfigJsonDto>(trimmed);
            if (direct == null)
            {
                return false;
            }

            config = direct.ToData();
            return config != null;
        }

        internal static ABIAdsGlobalConfigData LoadGlobalConfig()
        {
            return LoadGlobalConfig(out _);
        }

        internal static ABIAdsPlacementsRoot LoadPlacements(out ConfigLoadSource source)
        {
            var path = ResolvePlacementsPath(out source);
            if (path == null)
            {
                Debug.Log("ABI Ads: không tìm thấy placements.json trong project hoặc package — dùng giá trị mặc định editor.");
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
                    source = ConfigLoadSource.Defaults;
                    return ABIAdsPlacementsRoot.CreateDefault();
                }

                if (!TryParsePlacementsJson(raw, out var parsed))
                {
                    Debug.LogWarning($"ABI Ads: không parse được placements `{path}` — dùng giá trị mặc định editor.");
                    source = ConfigLoadSource.Defaults;
                    return ABIAdsPlacementsRoot.CreateDefault();
                }

                parsed.EnsureDefaults();
                Debug.Log($"ABI Ads: loaded placements ({DescribeSource(source)}) → `{path}`.");
                return parsed;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse placements config `{path}`: {ex.Message}");
            }

            source = ConfigLoadSource.Defaults;
            return ABIAdsPlacementsRoot.CreateDefault();
        }

        private static bool TryParsePlacementsJson(string raw, out ABIAdsPlacementsRoot root)
        {
            root = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var dto = JsonUtility.FromJson<ABIAdsPlacementsRootJsonDto>(raw.Trim());
            if (dto?.placements == null)
            {
                return false;
            }

            root = dto.ToData();
            return root != null;
        }

        internal static ABIAdsPlacementsRoot LoadPlacements()
        {
            return LoadPlacements(out _);
        }

        private static string ResolveGlobalConfigPath(out ConfigLoadSource source)
        {
            var projectPath = ABIAdsEditorPaths.GlobalConfigPath();
            if (File.Exists(projectPath))
            {
                source = ConfigLoadSource.Project;
                return projectPath;
            }

            var packagePath = ABIAdsEditorPaths.PackageGlobalConfigPath();
            if (File.Exists(packagePath))
            {
                source = ConfigLoadSource.Package;
                return packagePath;
            }

            source = ConfigLoadSource.Defaults;
            return null;
        }

        private static string ResolvePlacementsPath(out ConfigLoadSource source)
        {
            var projectPath = ABIAdsEditorPaths.PlacementsPath();
            if (File.Exists(projectPath))
            {
                source = ConfigLoadSource.Project;
                return projectPath;
            }

            var packagePath = ABIAdsEditorPaths.PackagePlacementsPath();
            if (File.Exists(packagePath))
            {
                source = ConfigLoadSource.Package;
                return packagePath;
            }

            source = ConfigLoadSource.Defaults;
            return null;
        }

        private static string DescribeSource(ConfigLoadSource source)
        {
            switch (source)
            {
                case ConfigLoadSource.Project:
                    return "project Assets/Resources/Configs";
                case ConfigLoadSource.Package:
                    return "package Resources/Configs";
                default:
                    return "editor defaults";
            }
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
            return JsonUtility.ToJson(ABIAdsGlobalConfigJsonDto.FromData(config), true) + Environment.NewLine;
        }

        private static string WritePlacementsJson(ABIAdsPlacementsRoot root)
        {
            root.EnsureDefaults();
            var saveRoot = new ABIAdsPlacementsRoot { placements = new List<ABIAdsPlacementConfig>() };
            foreach (var placement in root.placements)
            {
                saveRoot.placements.Add(placement.CreateSaveCopy());
            }

            return JsonUtility.ToJson(ABIAdsPlacementsRootJsonDto.FromData(saveRoot), true) + Environment.NewLine;
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

        internal static string LabeledStringPopup(string label, string value, string[] values, string[] labels)
        {
            value = value ?? string.Empty;
            var index = Array.IndexOf(values, value);
            if (index < 0)
            {
                index = 0;
            }

            return values[EditorGUILayout.Popup(label, index, labels)];
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
    internal sealed class ABIAdsGlobalConfigRootJsonDto
    {
        public ABIAdsGlobalConfigJsonDto global_config;
    }

    /// <summary>
    /// JsonUtility-compatible DTO (uses string[] instead of List&lt;string&gt;).
    /// </summary>
    [Serializable]
    internal sealed class ABIAdsGlobalConfigJsonDto
    {
        public int mediation_provider;
        public int timeout_remote;
        public bool variant_dev;
        public string[] enabled_versions;
        public string[] test_devices;
        public bool enable_adjust;
        public bool enable_appsflyer;
        public bool enable_facebook;
        public bool enable_tiktok;
        public bool enable_firebase;
        public bool enable_fcm;
        public string adjust_token;
        public string appsflyer_token;
        public string facebook_client_token;
        public string tiktok_app_id;
        public string tiktok_access_token;
        public string app_id_tt;
        public bool enable_adjust_tracking;
        public bool enable_appsflyer_tracking;
        public bool enable_realtime_database_tracking;
        public string config_version;
        public int inter_ad_interval;
        public string[] skip_interval_placements;
        public string admob_app_id;
        public string max_sdk_key;
        public bool max_consent_flow_enabled;
        public string max_privacy_policy_url;
        public string max_terms_of_service_url;
        public bool max_show_terms_privacy_alert_in_gdpr;
        public bool max_consent_debug_geography_gdpr;

        internal ABIAdsGlobalConfigData ToData()
        {
            var data = new ABIAdsGlobalConfigData
            {
                mediation_provider = mediation_provider,
                timeout_remote = timeout_remote,
                variant_dev = variant_dev,
                enable_adjust = enable_adjust,
                enable_appsflyer = enable_appsflyer,
                enable_facebook = enable_facebook,
                enable_tiktok = enable_tiktok,
                enable_firebase = enable_firebase,
                enable_fcm = enable_fcm,
                adjust_token = adjust_token ?? string.Empty,
                appsflyer_token = appsflyer_token ?? string.Empty,
                facebook_client_token = facebook_client_token ?? string.Empty,
                tiktok_app_id = tiktok_app_id ?? string.Empty,
                tiktok_access_token = tiktok_access_token ?? string.Empty,
                app_id_tt = app_id_tt ?? string.Empty,
                enable_adjust_tracking = enable_adjust_tracking,
                enable_appsflyer_tracking = enable_appsflyer_tracking,
                enable_realtime_database_tracking = enable_realtime_database_tracking,
                config_version = config_version ?? string.Empty,
                inter_ad_interval = inter_ad_interval,
                admob_app_id = admob_app_id ?? string.Empty,
                max_sdk_key = max_sdk_key ?? string.Empty,
                max_consent_flow_enabled = max_consent_flow_enabled,
                max_privacy_policy_url = max_privacy_policy_url ?? string.Empty,
                max_terms_of_service_url = max_terms_of_service_url ?? string.Empty,
                max_show_terms_privacy_alert_in_gdpr = max_show_terms_privacy_alert_in_gdpr,
                max_consent_debug_geography_gdpr = max_consent_debug_geography_gdpr,
                enabled_versions = ABIAdsConfigJsonHelpers.ToStringList(enabled_versions),
                test_devices = ABIAdsConfigJsonHelpers.ToStringList(test_devices),
                skip_interval_placements = ABIAdsConfigJsonHelpers.ToStringList(skip_interval_placements)
            };
            data.EnsureDefaults();
            return data;
        }

        internal static ABIAdsGlobalConfigJsonDto FromData(ABIAdsGlobalConfigData data)
        {
            data.EnsureDefaults();
            return new ABIAdsGlobalConfigJsonDto
            {
                mediation_provider = data.mediation_provider,
                timeout_remote = data.timeout_remote,
                variant_dev = data.variant_dev,
                enable_adjust = data.enable_adjust,
                enable_appsflyer = data.enable_appsflyer,
                enable_facebook = data.enable_facebook,
                enable_tiktok = data.enable_tiktok,
                enable_firebase = data.enable_firebase,
                enable_fcm = data.enable_fcm,
                adjust_token = data.adjust_token,
                appsflyer_token = data.appsflyer_token,
                facebook_client_token = data.facebook_client_token,
                tiktok_app_id = data.tiktok_app_id,
                tiktok_access_token = data.tiktok_access_token,
                app_id_tt = data.app_id_tt,
                enable_adjust_tracking = data.enable_adjust_tracking,
                enable_appsflyer_tracking = data.enable_appsflyer_tracking,
                enable_realtime_database_tracking = data.enable_realtime_database_tracking,
                config_version = data.config_version,
                inter_ad_interval = data.inter_ad_interval,
                admob_app_id = data.admob_app_id,
                max_sdk_key = data.max_sdk_key,
                max_consent_flow_enabled = data.max_consent_flow_enabled,
                max_privacy_policy_url = data.max_privacy_policy_url,
                max_terms_of_service_url = data.max_terms_of_service_url,
                max_show_terms_privacy_alert_in_gdpr = data.max_show_terms_privacy_alert_in_gdpr,
                max_consent_debug_geography_gdpr = data.max_consent_debug_geography_gdpr,
                enabled_versions = ABIAdsConfigJsonHelpers.ToStringArray(data.enabled_versions),
                test_devices = ABIAdsConfigJsonHelpers.ToStringArray(data.test_devices),
                skip_interval_placements = ABIAdsConfigJsonHelpers.ToStringArray(data.skip_interval_placements)
            };
        }
    }

    internal static class ABIAdsConfigJsonHelpers
    {
        internal static List<string> ToStringList(string[] values)
        {
            return values != null && values.Length > 0
                ? new List<string>(values)
                : new List<string>();
        }

        internal static string[] ToStringArray(List<string> values)
        {
            return values != null && values.Count > 0
                ? values.ToArray()
                : Array.Empty<string>();
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
    internal sealed class ABIAdsPlacementsRootJsonDto
    {
        public ABIAdsPlacementConfigJsonDto[] placements;

        internal ABIAdsPlacementsRoot ToData()
        {
            var root = new ABIAdsPlacementsRoot();
            if (placements != null)
            {
                foreach (var placementDto in placements)
                {
                    if (placementDto == null)
                    {
                        continue;
                    }

                    root.placements.Add(placementDto.ToData());
                }
            }

            root.EnsureDefaults();
            return root;
        }

        internal static ABIAdsPlacementsRootJsonDto FromData(ABIAdsPlacementsRoot root)
        {
            root.EnsureDefaults();
            var dtos = new ABIAdsPlacementConfigJsonDto[root.placements.Count];
            for (var i = 0; i < root.placements.Count; i++)
            {
                dtos[i] = ABIAdsPlacementConfigJsonDto.FromData(root.placements[i]);
            }

            return new ABIAdsPlacementsRootJsonDto { placements = dtos };
        }
    }

    [Serializable]
    internal sealed class ABIAdsPlacementConfigJsonDto
    {
        public string ad_name;
        public string ads_type;
        public ABIAdsAdIdConfigJsonDto[] ad_ids;
        public ABIAdsAdIdConfigJsonDto[] backup_ad_ids;
        public bool is_show;
        public bool is_organic_show;
        public string config_version;
        public bool prioritize_by_weight;
        public int ad_load_mode;
        public string[] disable_version;
        public ABIAdsBannerAdConfigJsonDto banner_ad;
        public ABIAdsNativeAdConfigJsonDto native_ad;

        internal void EnsureDefaults()
        {
            ad_name = ad_name ?? string.Empty;
            ads_type = string.IsNullOrEmpty(ads_type) ? "interstitial" : ads_type;
            config_version = config_version ?? string.Empty;
            disable_version = disable_version ?? Array.Empty<string>();
            ad_ids = ad_ids ?? Array.Empty<ABIAdsAdIdConfigJsonDto>();
            backup_ad_ids = backup_ad_ids ?? Array.Empty<ABIAdsAdIdConfigJsonDto>();
        }

        internal ABIAdsPlacementConfig ToData()
        {
            EnsureDefaults();
            var placement = new ABIAdsPlacementConfig
            {
                ad_name = ad_name,
                ads_type = ads_type,
                is_show = is_show,
                is_organic_show = is_organic_show,
                config_version = config_version,
                prioritize_by_weight = prioritize_by_weight,
                ad_load_mode = ad_load_mode,
                disable_version = ABIAdsConfigJsonHelpers.ToStringList(disable_version),
                ad_ids = ABIAdsAdIdConfigJsonDto.ToList(ad_ids),
                backup_ad_ids = ABIAdsAdIdConfigJsonDto.ToList(backup_ad_ids),
                banner_ad = banner_ad != null ? banner_ad.ToData() : new ABIAdsBannerAdConfig(),
                native_ad = native_ad != null ? native_ad.ToData() : new ABIAdsNativeAdConfig()
            };
            placement.EnsureDefaults();
            return placement;
        }

        internal static ABIAdsPlacementConfigJsonDto FromData(ABIAdsPlacementConfig placement)
        {
            placement.EnsureDefaults();
            return new ABIAdsPlacementConfigJsonDto
            {
                ad_name = placement.ad_name,
                ads_type = placement.ads_type,
                is_show = placement.is_show,
                is_organic_show = placement.is_organic_show,
                config_version = placement.config_version,
                prioritize_by_weight = placement.prioritize_by_weight,
                ad_load_mode = placement.ad_load_mode,
                disable_version = ABIAdsConfigJsonHelpers.ToStringArray(placement.disable_version),
                ad_ids = ABIAdsAdIdConfigJsonDto.FromList(placement.ad_ids),
                backup_ad_ids = ABIAdsAdIdConfigJsonDto.FromList(placement.backup_ad_ids),
                banner_ad = ABIAdsBannerAdConfigJsonDto.FromData(placement.banner_ad),
                native_ad = ABIAdsNativeAdConfigJsonDto.FromData(placement.native_ad)
            };
        }
    }

    [Serializable]
    internal sealed class ABIAdsAdIdConfigJsonDto
    {
        public string ad_id;
        public int ads_weight;
        public int mediation;

        internal static List<ABIAdsAdIdConfig> ToList(ABIAdsAdIdConfigJsonDto[] values)
        {
            var list = new List<ABIAdsAdIdConfig>();
            if (values == null)
            {
                return list;
            }

            foreach (var value in values)
            {
                if (value == null)
                {
                    continue;
                }

                list.Add(value.ToData());
            }

            return list;
        }

        internal static ABIAdsAdIdConfigJsonDto[] FromList(List<ABIAdsAdIdConfig> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<ABIAdsAdIdConfigJsonDto>();
            }

            var dtos = new ABIAdsAdIdConfigJsonDto[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                dtos[i] = FromData(values[i]);
            }

            return dtos;
        }

        internal ABIAdsAdIdConfig ToData()
        {
            return new ABIAdsAdIdConfig
            {
                ad_id = ad_id ?? string.Empty,
                ads_weight = ads_weight,
                mediation = mediation
            };
        }

        internal static ABIAdsAdIdConfigJsonDto FromData(ABIAdsAdIdConfig config)
        {
            return new ABIAdsAdIdConfigJsonDto
            {
                ad_id = config.ad_id ?? string.Empty,
                ads_weight = config.ads_weight,
                mediation = config.mediation
            };
        }
    }

    [Serializable]
    internal sealed class ABIAdsBannerAdConfigJsonDto
    {
        public string inline_style;
        public bool use_inline_adaptive;
        public bool use_collapsible;
        public string collapsible_gravity;
        public string banner_size;
        public int reload_time;

        internal ABIAdsBannerAdConfig ToData()
        {
            return new ABIAdsBannerAdConfig
            {
                inline_style = inline_style ?? string.Empty,
                use_inline_adaptive = use_inline_adaptive,
                use_collapsible = use_collapsible,
                collapsible_gravity = collapsible_gravity ?? string.Empty,
                banner_size = banner_size ?? string.Empty,
                reload_time = reload_time
            };
        }

        internal static ABIAdsBannerAdConfigJsonDto FromData(ABIAdsBannerAdConfig config)
        {
            if (config == null)
            {
                return new ABIAdsBannerAdConfigJsonDto();
            }

            return new ABIAdsBannerAdConfigJsonDto
            {
                inline_style = config.inline_style,
                use_inline_adaptive = config.use_inline_adaptive,
                use_collapsible = config.use_collapsible,
                collapsible_gravity = config.collapsible_gravity,
                banner_size = config.banner_size,
                reload_time = config.reload_time
            };
        }
    }

    [Serializable]
    internal sealed class ABIAdsNativeAdConfigJsonDto
    {
        public string ad_layout_file;
        public string organic_layout;
        public string layout_meta;
        public string bg_color;
        public string border_color;
        public float corner_radius_dp;
        public int stroke_width_dp;
        public string headline_text_color;
        public string body_text_color;
        public string price_text_color;
        public string advertiser_text_color;
        public ABIAdsClickedConfigJsonDto clicked;

        internal ABIAdsNativeAdConfig ToData()
        {
            var config = new ABIAdsNativeAdConfig
            {
                ad_layout_file = ad_layout_file ?? string.Empty,
                organic_layout = organic_layout ?? string.Empty,
                layout_meta = layout_meta ?? string.Empty,
                bg_color = bg_color ?? string.Empty,
                border_color = border_color ?? string.Empty,
                corner_radius_dp = corner_radius_dp,
                stroke_width_dp = stroke_width_dp,
                headline_text_color = headline_text_color ?? string.Empty,
                body_text_color = body_text_color ?? string.Empty,
                price_text_color = price_text_color ?? string.Empty,
                advertiser_text_color = advertiser_text_color ?? string.Empty,
                clicked = clicked != null ? clicked.ToData() : new ABIAdsClickedConfig()
            };
            config.EnsureDefaults();
            return config;
        }

        internal static ABIAdsNativeAdConfigJsonDto FromData(ABIAdsNativeAdConfig config)
        {
            if (config == null)
            {
                return new ABIAdsNativeAdConfigJsonDto();
            }

            config.EnsureDefaults();
            return new ABIAdsNativeAdConfigJsonDto
            {
                ad_layout_file = config.ad_layout_file,
                organic_layout = config.organic_layout,
                layout_meta = config.layout_meta,
                bg_color = config.bg_color,
                border_color = config.border_color,
                corner_radius_dp = config.corner_radius_dp,
                stroke_width_dp = config.stroke_width_dp,
                headline_text_color = config.headline_text_color,
                body_text_color = config.body_text_color,
                price_text_color = config.price_text_color,
                advertiser_text_color = config.advertiser_text_color,
                clicked = ABIAdsClickedConfigJsonDto.FromData(config.clicked)
            };
        }
    }

    [Serializable]
    internal sealed class ABIAdsNormalizedPosJsonDto
    {
        public float x;
        public float y;

        internal ABIAdsNormalizedPos ToData()
        {
            return new ABIAdsNormalizedPos
            {
                x = x,
                y = y
            };
        }

        internal static ABIAdsNormalizedPosJsonDto FromData(ABIAdsNormalizedPos config)
        {
            if (config == null)
            {
                return null;
            }

            return new ABIAdsNormalizedPosJsonDto
            {
                x = config.x,
                y = config.y
            };
        }
    }

    [Serializable]
    internal sealed class ABIAdsClickedConfigJsonDto
    {
        public string btn_act_color;
        public string btn_act_text_color;
        public int close_countdown_time;
        public int close_btn_render_mode;
        public ABIAdsNormalizedPosJsonDto countdown_pos;
        public ABIAdsNormalizedPosJsonDto progress_pos;
        public ABIAdsNormalizedPosJsonDto close_btn_pos;
        public bool dismiss_on_ad_click;

        internal ABIAdsClickedConfig ToData()
        {
            return new ABIAdsClickedConfig
            {
                btn_act_color = btn_act_color ?? string.Empty,
                btn_act_text_color = btn_act_text_color ?? string.Empty,
                close_countdown_time = close_countdown_time,
                close_btn_render_mode = close_btn_render_mode,
                countdown_pos = countdown_pos != null ? countdown_pos.ToData() : null,
                progress_pos = progress_pos != null ? progress_pos.ToData() : null,
                close_btn_pos = close_btn_pos != null ? close_btn_pos.ToData() : null,
                dismiss_on_ad_click = dismiss_on_ad_click
            };
        }

        internal static ABIAdsClickedConfigJsonDto FromData(ABIAdsClickedConfig config)
        {
            if (config == null)
            {
                return new ABIAdsClickedConfigJsonDto();
            }

            return new ABIAdsClickedConfigJsonDto
            {
                btn_act_color = config.btn_act_color,
                btn_act_text_color = config.btn_act_text_color,
                close_countdown_time = config.close_countdown_time,
                close_btn_render_mode = config.close_btn_render_mode,
                countdown_pos = ABIAdsNormalizedPosJsonDto.FromData(config.countdown_pos),
                progress_pos = ABIAdsNormalizedPosJsonDto.FromData(config.progress_pos),
                close_btn_pos = ABIAdsNormalizedPosJsonDto.FromData(config.close_btn_pos),
                dismiss_on_ad_click = config.dismiss_on_ad_click
            };
        }
    }

    internal static class ABIAdsNativeAdDefaults
    {
        internal static bool IsFullScreenLayout(string layoutFile)
        {
            if (string.IsNullOrWhiteSpace(layoutFile))
            {
                return false;
            }

            var normalized = layoutFile.Trim().ToLowerInvariant();
            return normalized.Contains("fsn")
                   || normalized.Contains("full_screen")
                   || normalized.Contains("fullscreen");
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
        public int ad_load_mode = 1;
        public List<string> disable_version = new List<string>();
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
            var clone = ABIAdsPlacementConfigJsonDto.FromData(this).ToData();
            clone.ad_name = string.IsNullOrEmpty(clone.ad_name) ? "placement_copy" : clone.ad_name + "_copy";
            clone.EnsureDefaults();
            return clone;
        }

        public ABIAdsPlacementConfig CreateSaveCopy()
        {
            var dto = ABIAdsPlacementConfigJsonDto.FromData(this);
            dto.EnsureDefaults();
            if (dto.ads_type != "banner" && dto.ads_type != "mrec")
            {
                dto.banner_ad = null;
            }

            if (dto.ads_type != "native")
            {
                dto.native_ad = null;
            }

            var copy = dto.ToData();
            copy.EnsureDefaults();
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
        [NonSerialized] public bool dismissOnAdClickDefaulted;

        public void EnsureDefaults()
        {
            if (clicked == null)
            {
                clicked = new ABIAdsClickedConfig();
            }

            var isFullScreen = ABIAdsNativeAdDefaults.IsFullScreenLayout(ad_layout_file);
            if (isFullScreen && !dismissOnAdClickDefaulted)
            {
                clicked.dismiss_on_ad_click = true;
                dismissOnAdClickDefaulted = true;
            }
            else if (!isFullScreen)
            {
                dismissOnAdClickDefaulted = false;
            }
        }
    }

    [Serializable]
    internal sealed class ABIAdsNormalizedPos
    {
        public float x = 0.92f;
        public float y = 0.06f;
    }

    [Serializable]
    internal sealed class ABIAdsClickedConfig
    {
        public string btn_act_color = string.Empty;
        public string btn_act_text_color = string.Empty;
        public int close_countdown_time;
        public int close_btn_render_mode;
        public ABIAdsNormalizedPos countdown_pos = new ABIAdsNormalizedPos();
        public ABIAdsNormalizedPos progress_pos = new ABIAdsNormalizedPos { x = 0.08f, y = 0.06f };
        public ABIAdsNormalizedPos close_btn_pos = new ABIAdsNormalizedPos();
        public bool dismiss_on_ad_click;
    }
}
