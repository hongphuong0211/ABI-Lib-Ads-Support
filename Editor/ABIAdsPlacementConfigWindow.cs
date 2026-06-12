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

        // AdmobBanner.java + PlacementConfig.BannerAdConfig
        private static readonly string[] BannerInlineStyleValues =
        {
            string.Empty,
            "BANNER_INLINE_SMALL_STYLE",
            "BANNER_INLINE_LARGE_STYLE"
        };

        private static readonly string[] BannerInlineStyleLabels =
        {
            "<Default (Small 50dp)>",
            "BANNER_INLINE_SMALL_STYLE",
            "BANNER_INLINE_LARGE_STYLE"
        };

        private static readonly string[] BannerCollapsibleGravityValues =
        {
            string.Empty,
            "top",
            "bottom"
        };

        private static readonly string[] BannerCollapsibleGravityLabels =
        {
            "<Default (bottom)>",
            "top",
            "bottom"
        };

        private static readonly string[] BannerSizeValues =
        {
            string.Empty,
            "BANNER",
            "MEDIUM_RECTANGLE",
            "LARGE_BANNER",
            "FULL_BANNER",
            "LEADERBOARD",
            "SMART_BANNER",
            "ADAPTIVE_BANNER"
        };

        private static readonly string[] BannerSizeLabels =
        {
            "<Default (BANNER)>",
            "BANNER",
            "MEDIUM_RECTANGLE",
            "LARGE_BANNER",
            "FULL_BANNER",
            "LEADERBOARD",
            "SMART_BANNER (→ adaptive)",
            "ADAPTIVE_BANNER"
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
            placement.ad_load_mode = EditorGUILayout.IntPopup(
                "Ad Load Mode",
                placement.ad_load_mode,
                new[] { "0 Waterfall", "1 Parallel Priority", "2 Load All" },
                new[] { 0, 1, 2 });
            ABIAdsConfigGui.DrawStringList("Disable Versions", placement.disable_version);

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
                placement.banner_ad.inline_style = ABIAdsConfigGui.LabeledStringPopup(
                    "Inline Style",
                    placement.banner_ad.inline_style,
                    BannerInlineStyleValues,
                    BannerInlineStyleLabels);
                placement.banner_ad.use_inline_adaptive = EditorGUILayout.Toggle("Use Inline Adaptive", placement.banner_ad.use_inline_adaptive);
                placement.banner_ad.use_collapsible = EditorGUILayout.Toggle("Use Collapsible", placement.banner_ad.use_collapsible);
                placement.banner_ad.collapsible_gravity = ABIAdsConfigGui.LabeledStringPopup(
                    "Collapsible Gravity",
                    placement.banner_ad.collapsible_gravity,
                    BannerCollapsibleGravityValues,
                    BannerCollapsibleGravityLabels);
                placement.banner_ad.banner_size = ABIAdsConfigGui.LabeledStringPopup(
                    "Banner Size",
                    placement.banner_ad.banner_size,
                    BannerSizeValues,
                    BannerSizeLabels);
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
                placement.native_ad.clicked.close_countdown_time = EditorGUILayout.IntField(
                    "Close Countdown Time (s)",
                    placement.native_ad.clicked.close_countdown_time);
                EditorGUILayout.HelpBox(
                    "Close Countdown Time: -1 = ẩn countdown/X, 0 = hiện X ngay, >0 = countdown theo render mode.",
                    MessageType.None);
                placement.native_ad.clicked.close_btn_render_mode = EditorGUILayout.IntPopup(
                    "Close Button Render Mode",
                    placement.native_ad.clicked.close_btn_render_mode,
                    new[] { "0 Countdown + X", "1 Arrow + Progress + X", "2 Delay Only (ẩn countdown)" },
                    new[] { 0, 1, 2 });
                DrawNormalizedPos("Countdown Pos (0-1)", placement.native_ad.clicked.countdown_pos);
                DrawNormalizedPos("Progress Pos (0-1)", placement.native_ad.clicked.progress_pos);
                DrawNormalizedPos("Close Button Pos (0-1)", placement.native_ad.clicked.close_btn_pos);
                placement.native_ad.clicked.dismiss_on_ad_click = EditorGUILayout.Toggle(
                    "Dismiss On Ad Click (fullscreen)",
                    placement.native_ad.clicked.dismiss_on_ad_click);
            }
        }

        private static void DrawNormalizedPos(string label, ABIAdsNormalizedPos pos)
        {
            if (pos == null)
            {
                pos = new ABIAdsNormalizedPos();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                pos.x = EditorGUILayout.Slider(pos.x, 0f, 1f);
                pos.y = EditorGUILayout.Slider(pos.y, 0f, 1f);
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
