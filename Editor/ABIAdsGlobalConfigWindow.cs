using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsGlobalConfigWindow : EditorWindow
    {
        private static readonly string[] MediationLabels = { "AdMob (0)", "MAX (1)", "Dual (2)" };
        private static readonly int[] MediationValues = { 0, 1, 2 };

        private ABIAdsGlobalConfigData _globalConfig;
        private ABIAdsConfigStore.ConfigLoadSource _configLoadSource;
        private HashSet<string> _enabledAdMobNetworkIds;
        private HashSet<string> _enabledMaxNetworkIds;
        private Vector2 _scroll;
        private bool _adMobMediationFoldout = true;
        private bool _maxMediationFoldout = true;

        [MenuItem("ABI Ads/Configs/Edit Global Config")]
        internal static void Open()
        {
            var window = GetWindow<ABIAdsGlobalConfigWindow>("ABI Global Config");
            window.minSize = new Vector2(760, 560);
            window.LoadConfig();
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            EnsureData();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Quản lý Assets/Resources/Configs/global_config.json (tên file: global_config.json, gạch dưới). " +
                "Save luôn ghi vào Assets; Reload ưu tiên file project, rồi mới fallback package mẫu. " +
                "Mediation network lưu riêng tại Assets/Resources/Configs/mediation_networks.json và tự apply lại sau update UPM.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload", GUILayout.Width(96)))
                {
                    LoadConfig();
                }

                if (GUILayout.Button("Save Global Config", GUILayout.Width(160)))
                {
                    SaveConfig();
                }

                GUILayout.FlexibleSpace();
            }

            ABIAdsConfigGui.DrawPath("Global (Save target)", ABIAdsEditorPaths.GlobalConfigPath());
            EditorGUILayout.LabelField(
                "Loaded from",
                DescribeLoadSource(_configLoadSource),
                EditorStyles.miniLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawGlobalConfig();
            DrawAdMobMediationNetworks();
            DrawMaxMediationNetworks();
            EditorGUILayout.EndScrollView();
        }

        private void DrawGlobalConfig()
        {
            _globalConfig.mediation_provider = ABIAdsConfigGui.IntPopup("Mediation Provider", _globalConfig.mediation_provider, MediationLabels, MediationValues);
            _globalConfig.timeout_remote = EditorGUILayout.IntField("Timeout Remote (ms)", _globalConfig.timeout_remote);
            _globalConfig.variant_dev = EditorGUILayout.Toggle("Variant Dev", _globalConfig.variant_dev);
            _globalConfig.inter_ad_interval = EditorGUILayout.IntField("Inter Ad Interval (ms)", _globalConfig.inter_ad_interval);
            _globalConfig.config_version = EditorGUILayout.TextField("Config Version", _globalConfig.config_version ?? string.Empty);

            ABIAdsConfigGui.DrawStringList("Enabled Versions", _globalConfig.enabled_versions);
            ABIAdsConfigGui.DrawStringList("Test Devices", _globalConfig.test_devices);
            ABIAdsConfigGui.DrawStringList("Skip Interval Placements", _globalConfig.skip_interval_placements);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Native Build Settings", EditorStyles.boldLabel);
            _globalConfig.admob_app_id = EditorGUILayout.TextField("AdMob App ID", _globalConfig.admob_app_id ?? string.Empty);
            _globalConfig.max_sdk_key = EditorGUILayout.TextField("MAX SDK Key", _globalConfig.max_sdk_key ?? string.Empty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tracking / SDK Tokens", EditorStyles.boldLabel);
            _globalConfig.enable_adjust = EditorGUILayout.Toggle("Enable Adjust", _globalConfig.enable_adjust);
            _globalConfig.adjust_token = EditorGUILayout.TextField("Adjust Token", _globalConfig.adjust_token ?? string.Empty);
            _globalConfig.enable_adjust_tracking = EditorGUILayout.Toggle("Enable Adjust Tracking", _globalConfig.enable_adjust_tracking);
            _globalConfig.enable_appsflyer = EditorGUILayout.Toggle("Enable AppsFlyer", _globalConfig.enable_appsflyer);
            _globalConfig.appsflyer_token = EditorGUILayout.TextField("AppsFlyer Token", _globalConfig.appsflyer_token ?? string.Empty);
            _globalConfig.enable_appsflyer_tracking = EditorGUILayout.Toggle("Enable AppsFlyer Tracking", _globalConfig.enable_appsflyer_tracking);
            _globalConfig.enable_facebook = EditorGUILayout.Toggle("Enable Facebook", _globalConfig.enable_facebook);
            _globalConfig.facebook_client_token = EditorGUILayout.TextField("Facebook Client Token", _globalConfig.facebook_client_token ?? string.Empty);
            _globalConfig.enable_tiktok = EditorGUILayout.Toggle("Enable TikTok", _globalConfig.enable_tiktok);
            _globalConfig.tiktok_app_id = EditorGUILayout.TextField("TikTok App ID", _globalConfig.tiktok_app_id ?? string.Empty);
            _globalConfig.tiktok_access_token = EditorGUILayout.TextField("TikTok Access Token", _globalConfig.tiktok_access_token ?? string.Empty);
            _globalConfig.app_id_tt = EditorGUILayout.TextField("App ID TT", _globalConfig.app_id_tt ?? string.Empty);
            _globalConfig.enable_firebase = EditorGUILayout.Toggle("Enable Firebase", _globalConfig.enable_firebase);
            _globalConfig.enable_fcm = EditorGUILayout.Toggle("Enable FCM", _globalConfig.enable_fcm);
            _globalConfig.enable_realtime_database_tracking = EditorGUILayout.Toggle("Enable Realtime DB Tracking", _globalConfig.enable_realtime_database_tracking);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MAX Consent", EditorStyles.boldLabel);
            _globalConfig.max_consent_flow_enabled = EditorGUILayout.Toggle("Consent Flow Enabled", _globalConfig.max_consent_flow_enabled);
            _globalConfig.max_privacy_policy_url = EditorGUILayout.TextField("Privacy Policy URL", _globalConfig.max_privacy_policy_url ?? string.Empty);
            _globalConfig.max_terms_of_service_url = EditorGUILayout.TextField("Terms Of Service URL", _globalConfig.max_terms_of_service_url ?? string.Empty);
            _globalConfig.max_show_terms_privacy_alert_in_gdpr = EditorGUILayout.Toggle("Show Terms In GDPR", _globalConfig.max_show_terms_privacy_alert_in_gdpr);
            _globalConfig.max_consent_debug_geography_gdpr = EditorGUILayout.Toggle("Debug Geography GDPR", _globalConfig.max_consent_debug_geography_gdpr);
        }

        private void DrawAdMobMediationNetworks()
        {
            EnsureAdMobNetworks();
            DrawMediationNetworks(
                "AdMob Mediation Networks",
                "Tick network cần integrate cho AdMob. Apply ghi vào mediation_networks.json + ABIAdsDependencies.xml, thêm Maven repo vào settingsTemplate.gradle, rồi chạy EDM4U Force Resolve.",
                ABIAdsAdMobMediationNetworks.VersionSource,
                ABIAdsAdMobMediationNetworks.All,
                ref _adMobMediationFoldout,
                _enabledAdMobNetworkIds,
                LoadAdMobNetworks,
                ApplyAdMobMediationNetworks);
        }

        private void DrawMaxMediationNetworks()
        {
            EnsureMaxNetworks();
            DrawMediationNetworks(
                "MAX Mediation Networks",
                "Tick network cần integrate cho MAX. Apply ghi vào mediation_networks.json + ABIAdsDependencies.xml, thêm Maven repo vào settingsTemplate.gradle, rồi chạy EDM4U Force Resolve.",
                ABIAdsMaxMediationNetworks.VersionSource,
                ABIAdsMaxMediationNetworks.All,
                ref _maxMediationFoldout,
                _enabledMaxNetworkIds,
                LoadMaxNetworks,
                ApplyMaxMediationNetworks);
        }

        private static void DrawMediationNetworks(
            string title,
            string helpText,
            string versionSource,
            ABIAdsAdMobMediationNetwork[] networks,
            ref bool foldout,
            HashSet<string> enabledNetworkIds,
            Action reload,
            Action apply)
        {
            EditorGUILayout.Space();
            foldout = EditorGUILayout.Foldout(foldout, title, true);
            if (!foldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
                EditorGUILayout.LabelField("Version Source", versionSource, EditorStyles.miniLabel);
                ABIAdsConfigGui.DrawPath("Dependencies XML", ABIAdsDependenciesXmlStore.DependenciesPath());
                ABIAdsConfigGui.DrawPath("Persisted networks", ABIAdsEditorPaths.MediationNetworksConfigPath());

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select All", GUILayout.Width(96)))
                    {
                        foreach (var network in networks)
                        {
                            enabledNetworkIds.Add(network.Id);
                        }
                    }

                    if (GUILayout.Button("Clear All", GUILayout.Width(96)))
                    {
                        enabledNetworkIds.Clear();
                    }

                    if (GUILayout.Button("Reload From Config", GUILayout.Width(140)))
                    {
                        reload();
                    }

                    if (GUILayout.Button("Apply To XML", GUILayout.Width(120)))
                    {
                        apply();
                    }

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space();
                foreach (var network in networks)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var enabled = enabledNetworkIds.Contains(network.Id);
                        var nextEnabled = EditorGUILayout.ToggleLeft(
                            $"{network.DisplayName}  ({network.AdapterSpec})",
                            enabled);
                        if (nextEnabled != enabled)
                        {
                            if (nextEnabled)
                            {
                                enabledNetworkIds.Add(network.Id);
                            }
                            else
                            {
                                enabledNetworkIds.Remove(network.Id);
                            }
                        }

                        foreach (var extraSpec in network.ExtraSpecs)
                        {
                            EditorGUILayout.LabelField("Extra", extraSpec, EditorStyles.miniLabel);
                        }

                        if (!string.IsNullOrEmpty(network.Notes))
                        {
                            EditorGUILayout.LabelField("Note", network.Notes, EditorStyles.miniLabel);
                        }
                    }
                }
            }
        }

        private void LoadConfig()
        {
            _globalConfig = ABIAdsConfigStore.LoadGlobalConfig(out _configLoadSource);
            LoadAdMobNetworks();
            LoadMaxNetworks();
        }

        private static string DescribeLoadSource(ABIAdsConfigStore.ConfigLoadSource source)
        {
            switch (source)
            {
                case ABIAdsConfigStore.ConfigLoadSource.Project:
                    return "Assets/Resources/Configs/global_config.json";
                case ABIAdsConfigStore.ConfigLoadSource.Package:
                    return "Packages/com.abi.ads.unity/Resources/Configs/global_config.json (chưa có bản project — bấm Save để copy sang Assets)";
                default:
                    return "Editor defaults (không tìm thấy hoặc không parse được JSON)";
            }
        }

        private void SaveConfig()
        {
            EnsureData();
            ABIAdsConfigStore.SaveGlobalConfig(_globalConfig);
            _configLoadSource = ABIAdsConfigStore.ConfigLoadSource.Project;
        }

        private void ApplyAdMobMediationNetworks()
        {
            EnsureData();
            ABIAdsMediationNetworksConfigStore.Save(_enabledAdMobNetworkIds, _enabledMaxNetworkIds);
            ABIAdsDependenciesXmlStore.SaveEnabledMediationNetworks(_enabledAdMobNetworkIds);
            AssetDatabase.Refresh();
            ABIAdsMediationNetworksApplier.TryForceResolveAndroidDependencies();
        }

        private void ApplyMaxMediationNetworks()
        {
            EnsureData();
            ABIAdsMediationNetworksConfigStore.Save(_enabledAdMobNetworkIds, _enabledMaxNetworkIds);
            ABIAdsDependenciesXmlStore.SaveEnabledMaxMediationNetworks(_enabledMaxNetworkIds);
            AssetDatabase.Refresh();
            ABIAdsMediationNetworksApplier.TryForceResolveAndroidDependencies();
        }

        private void EnsureData()
        {
            if (_globalConfig == null)
            {
                _globalConfig = ABIAdsGlobalConfigData.CreateDefault();
            }

            EnsureAdMobNetworks();
            EnsureMaxNetworks();
        }

        private void LoadAdMobNetworks()
        {
            var config = ABIAdsMediationNetworksConfigStore.Load(out _);
            _enabledAdMobNetworkIds = ABIAdsMediationNetworksApplier.ToHashSet(config.admob_mediation_networks);
            _enabledMaxNetworkIds = ABIAdsMediationNetworksApplier.ToHashSet(config.max_mediation_networks);
        }

        private void EnsureAdMobNetworks()
        {
            if (_enabledAdMobNetworkIds == null)
            {
                LoadAdMobNetworks();
            }
        }

        private void LoadMaxNetworks()
        {
            LoadAdMobNetworks();
        }

        private void EnsureMaxNetworks()
        {
            if (_enabledMaxNetworkIds == null)
            {
                LoadMaxNetworks();
            }
        }
    }
}
