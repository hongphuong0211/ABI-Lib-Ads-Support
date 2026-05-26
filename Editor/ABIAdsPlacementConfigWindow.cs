using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsPlacementConfigWindow : EditorWindow
    {
        private static readonly string[] PerAdMediationLabels = { "AdMob (0)", "MAX (1)" };
        private static readonly int[] PerAdMediationValues = { 0, 1 };
        private static readonly string[] AdTypeValues =
        {
            "interstitial",
            "rewarded",
            "banner",
            "mrec",
            "native",
            "app_open",
            "rewarded_interstitial"
        };

        private ABIAdsPlacementsRoot _placementsRoot;
        private ABIAdsConfigStore.ConfigLoadSource _configLoadSource;
        private Vector2 _scroll;
        private string[] _nativeAdLayoutValues = new string[0];

        [MenuItem("ABI Ads/Configs/Edit Placement Config")]
        internal static void Open()
        {
            var window = GetWindow<ABIAdsPlacementConfigWindow>("ABI Placement Config");
            window.minSize = new Vector2(620, 560);
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
                "Quản lý Assets/Resources/Configs/placements.json. Save ghi vào Assets; Reload ưu tiên file project, rồi fallback package mẫu.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload", GUILayout.Width(96)))
                {
                    LoadConfig();
                }

                if (GUILayout.Button("Save Placement Config", GUILayout.Width(176)))
                {
                    SaveConfig();
                }

                GUILayout.FlexibleSpace();
            }

            ABIAdsConfigGui.DrawPath("Placements (Save target)", ABIAdsEditorPaths.PlacementsPath());
            EditorGUILayout.LabelField(
                "Loaded from",
                DescribeLoadSource(_configLoadSource),
                EditorStyles.miniLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawPlacements();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPlacements()
        {
            for (var i = 0; i < _placementsRoot.placements.Count; i++)
            {
                var placement = _placementsRoot.placements[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        placement.foldout = EditorGUILayout.Foldout(
                            placement.foldout,
                            string.IsNullOrEmpty(placement.ad_name) ? $"Placement {i + 1}" : placement.ad_name,
                            true);

                        if (GUILayout.Button("Duplicate", GUILayout.Width(82)))
                        {
                            _placementsRoot.placements.Insert(i + 1, placement.Clone());
                            GUI.FocusControl(null);
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(72)))
                        {
                            _placementsRoot.placements.RemoveAt(i);
                            GUI.FocusControl(null);
                            continue;
                        }
                    }

                    if (placement.foldout)
                    {
                        DrawPlacement(placement);
                    }
                }
            }

            if (GUILayout.Button("Add Placement"))
            {
                _placementsRoot.placements.Add(ABIAdsPlacementConfig.CreateDefault());
            }
        }

        private void DrawPlacement(ABIAdsPlacementConfig placement)
        {
            placement.ad_name = EditorGUILayout.TextField("Ad Name", placement.ad_name ?? string.Empty);
            placement.ads_type = ABIAdsConfigGui.StringPopup("Ads Type", placement.ads_type, AdTypeValues);
            placement.is_show = EditorGUILayout.Toggle("Is Show", placement.is_show);
            placement.is_organic_show = EditorGUILayout.Toggle("Is Organic Show", placement.is_organic_show);
            placement.config_version = EditorGUILayout.TextField("Config Version", placement.config_version ?? string.Empty);
            placement.prioritize_by_weight = EditorGUILayout.Toggle("Prioritize By Weight", placement.prioritize_by_weight);
            ABIAdsConfigGui.DrawStringList("Disable Versions", placement.disable_version);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Trigger", EditorStyles.boldLabel);
            placement.activity_trigger_load = EditorGUILayout.TextField("Activity Trigger Load", placement.activity_trigger_load ?? string.Empty);
            placement.activity_load_and_show = EditorGUILayout.Toggle("Activity Load And Show", placement.activity_load_and_show);
            placement.delay_time_trigger_load = EditorGUILayout.IntField("Delay Trigger Load (ms)", placement.delay_time_trigger_load);
            placement.activity_trigger_show = EditorGUILayout.TextField("Activity Trigger Show", placement.activity_trigger_show ?? string.Empty);
            placement.delay_time_trigger_show = EditorGUILayout.IntField("Delay Trigger Show (ms)", placement.delay_time_trigger_show);
            placement.click_trigger_view_id = EditorGUILayout.TextField("Click Trigger View ID", placement.click_trigger_view_id ?? string.Empty);
            placement.click_load_and_show = EditorGUILayout.Toggle("Click Load And Show", placement.click_load_and_show);
            placement.click_delay_ms = EditorGUILayout.IntField("Click Delay (ms)", placement.click_delay_ms);
            placement.click_trigger_show_view_id = EditorGUILayout.TextField("Click Trigger Show View ID", placement.click_trigger_show_view_id ?? string.Empty);
            placement.click_trigger_show_delay_ms = EditorGUILayout.IntField("Click Trigger Show Delay (ms)", placement.click_trigger_show_delay_ms);
            placement.click_trigger_count_view_id = EditorGUILayout.TextField("Click Trigger Count View ID", placement.click_trigger_count_view_id ?? string.Empty);
            placement.click_trigger_count_threshold = EditorGUILayout.IntField("Click Trigger Count Threshold", placement.click_trigger_count_threshold);
            placement.click_trigger_count_delay_ms = EditorGUILayout.IntField("Click Trigger Count Delay (ms)", placement.click_trigger_count_delay_ms);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ad IDs", EditorStyles.boldLabel);
            DrawAdIdList(placement.ad_ids);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Backup Ad IDs", EditorStyles.boldLabel);
            DrawAdIdList(placement.backup_ad_ids);

            if (placement.ads_type == "banner")
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Banner", EditorStyles.boldLabel);
                placement.banner_ad.inline_style = EditorGUILayout.TextField("Inline Style", placement.banner_ad.inline_style ?? string.Empty);
                placement.banner_ad.use_inline_adaptive = EditorGUILayout.Toggle("Use Inline Adaptive", placement.banner_ad.use_inline_adaptive);
                placement.banner_ad.use_collapsible = EditorGUILayout.Toggle("Use Collapsible", placement.banner_ad.use_collapsible);
                placement.banner_ad.collapsible_gravity = EditorGUILayout.TextField("Collapsible Gravity", placement.banner_ad.collapsible_gravity ?? string.Empty);
                placement.banner_ad.banner_size = EditorGUILayout.TextField("Banner Size", placement.banner_ad.banner_size ?? string.Empty);
                placement.banner_ad.reload_time = EditorGUILayout.IntField("Reload Time (s)", placement.banner_ad.reload_time);
            }

            if (placement.ads_type == "native")
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Native", EditorStyles.boldLabel);
                placement.native_ad.ad_layout_file = ABIAdsConfigGui.LayoutFilePopup("Ad Layout File", placement.native_ad.ad_layout_file, _nativeAdLayoutValues);
                placement.native_ad.organic_layout = ABIAdsConfigGui.LayoutFilePopup("Organic Layout", placement.native_ad.organic_layout, _nativeAdLayoutValues);
                placement.native_ad.layout_meta = ABIAdsConfigGui.LayoutFilePopup("Layout Meta", placement.native_ad.layout_meta, _nativeAdLayoutValues);
                placement.native_ad.bg_color = ABIAdsConfigGui.DrawHexColorField("BG Color", placement.native_ad.bg_color);
                placement.native_ad.border_color = ABIAdsConfigGui.DrawHexColorField("Border Color", placement.native_ad.border_color);
                placement.native_ad.corner_radius_dp = EditorGUILayout.FloatField("Corner Radius DP", placement.native_ad.corner_radius_dp);
                placement.native_ad.stroke_width_dp = EditorGUILayout.IntField("Stroke Width DP", placement.native_ad.stroke_width_dp);
                placement.native_ad.headline_text_color = ABIAdsConfigGui.DrawHexColorField("Headline Text Color", placement.native_ad.headline_text_color);
                placement.native_ad.body_text_color = ABIAdsConfigGui.DrawHexColorField("Body Text Color", placement.native_ad.body_text_color);
                placement.native_ad.price_text_color = ABIAdsConfigGui.DrawHexColorField("Price Text Color", placement.native_ad.price_text_color);
                placement.native_ad.advertiser_text_color = ABIAdsConfigGui.DrawHexColorField("Advertiser Text Color", placement.native_ad.advertiser_text_color);
                placement.native_ad.clicked.btn_act_color = ABIAdsConfigGui.DrawHexColorField("Clicked Button Color", placement.native_ad.clicked.btn_act_color);
                placement.native_ad.clicked.btn_act_text_color = ABIAdsConfigGui.DrawHexColorField("Clicked Button Text Color", placement.native_ad.clicked.btn_act_text_color);
                placement.native_ad.clicked.delay_time_show_btn_next = EditorGUILayout.IntField("Delay Show Next Button", placement.native_ad.clicked.delay_time_show_btn_next);
            }
        }

        private static void DrawAdIdList(List<ABIAdsAdIdConfig> adIds)
        {
            for (var i = 0; i < adIds.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    adIds[i].ad_id = EditorGUILayout.TextField(adIds[i].ad_id ?? string.Empty);
                    adIds[i].ads_weight = Mathf.Max(1, EditorGUILayout.IntField(adIds[i].ads_weight, GUILayout.Width(56)));
                    adIds[i].mediation = ABIAdsConfigGui.IntPopup(GUIContent.none, adIds[i].mediation, PerAdMediationLabels, PerAdMediationValues, GUILayout.Width(104));

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        adIds.RemoveAt(i);
                        GUI.FocusControl(null);
                    }
                }
            }

            if (GUILayout.Button("Add Ad ID"))
            {
                adIds.Add(new ABIAdsAdIdConfig());
            }
        }

        private void LoadConfig()
        {
            _placementsRoot = ABIAdsConfigStore.LoadPlacements(out _configLoadSource);
            _nativeAdLayoutValues = ABIAdsEditorPaths.LoadNativeAdLayoutValues();
        }

        private static string DescribeLoadSource(ABIAdsConfigStore.ConfigLoadSource source)
        {
            switch (source)
            {
                case ABIAdsConfigStore.ConfigLoadSource.Project:
                    return "Assets/Resources/Configs/placements.json";
                case ABIAdsConfigStore.ConfigLoadSource.Package:
                    return "Packages/com.abi.ads.unity/Resources/Configs/placements.json (chưa có bản project — bấm Save để copy sang Assets)";
                default:
                    return "Editor defaults (không tìm thấy hoặc không parse được JSON)";
            }
        }

        private void SaveConfig()
        {
            EnsureData();
            ABIAdsConfigStore.SavePlacements(_placementsRoot);
            _configLoadSource = ABIAdsConfigStore.ConfigLoadSource.Project;
        }

        private void EnsureData()
        {
            if (_placementsRoot == null)
            {
                _placementsRoot = ABIAdsPlacementsRoot.CreateDefault();
            }
        }
    }
}
