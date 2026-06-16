using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    [Serializable]
    internal sealed class ABIAdsMediationNetworksConfigData
    {
        public List<string> admob_mediation_networks = new List<string>();
        public List<string> max_mediation_networks = new List<string>();

        public void EnsureDefaults()
        {
            if (admob_mediation_networks == null)
            {
                admob_mediation_networks = new List<string>();
            }

            if (max_mediation_networks == null)
            {
                max_mediation_networks = new List<string>();
            }
        }

        internal static ABIAdsMediationNetworksConfigData CreateDefault()
        {
            var data = new ABIAdsMediationNetworksConfigData();
            data.EnsureDefaults();
            return data;
        }
    }

    [Serializable]
    internal sealed class ABIAdsMediationNetworksConfigJsonDto
    {
        public string[] admob_mediation_networks;
        public string[] max_mediation_networks;

        internal ABIAdsMediationNetworksConfigData ToData()
        {
            return new ABIAdsMediationNetworksConfigData
            {
                admob_mediation_networks = ABIAdsConfigJsonHelpers.ToStringList(admob_mediation_networks),
                max_mediation_networks = ABIAdsConfigJsonHelpers.ToStringList(max_mediation_networks)
            };
        }

        internal static ABIAdsMediationNetworksConfigJsonDto FromData(ABIAdsMediationNetworksConfigData data)
        {
            data.EnsureDefaults();
            return new ABIAdsMediationNetworksConfigJsonDto
            {
                admob_mediation_networks = ABIAdsConfigJsonHelpers.ToStringArray(data.admob_mediation_networks),
                max_mediation_networks = ABIAdsConfigJsonHelpers.ToStringArray(data.max_mediation_networks)
            };
        }
    }

    /// <summary>
    /// JsonUtility can read legacy mediation fields still present in global_config.json.
    /// </summary>
    [Serializable]
    internal sealed class ABIAdsGlobalConfigLegacyMediationJsonDto
    {
        public string[] admob_mediation_networks;
        public string[] max_mediation_networks;
    }

    internal enum ABIAdsMediationNetworksConfigSource
    {
        Project,
        Package,
        LegacyGlobalConfig,
        XmlFallback,
        Empty
    }

    internal static class ABIAdsMediationNetworksConfigStore
    {
        internal static ABIAdsMediationNetworksConfigData Load(out ABIAdsMediationNetworksConfigSource source)
        {
            var projectPath = ABIAdsEditorPaths.MediationNetworksConfigPath();
            if (File.Exists(projectPath) && TryParse(File.ReadAllText(projectPath), out var projectConfig))
            {
                source = ABIAdsMediationNetworksConfigSource.Project;
                return projectConfig;
            }

            var packagePath = ABIAdsEditorPaths.PackageMediationNetworksConfigPath();
            if (File.Exists(packagePath) && TryParse(File.ReadAllText(packagePath), out var packageConfig))
            {
                source = ABIAdsMediationNetworksConfigSource.Package;
                return packageConfig;
            }

            if (TryLoadLegacyFromGlobalConfig(out var legacyConfig))
            {
                source = ABIAdsMediationNetworksConfigSource.LegacyGlobalConfig;
                return legacyConfig;
            }

            source = ABIAdsMediationNetworksConfigSource.Empty;
            return ABIAdsMediationNetworksConfigData.CreateDefault();
        }

        internal static void LoadPersistedNetworkIds(out HashSet<string> adMobIds, out HashSet<string> maxIds)
        {
            adMobIds = new HashSet<string>(StringComparer.Ordinal);
            maxIds = new HashSet<string>(StringComparer.Ordinal);

            var projectPath = ABIAdsEditorPaths.MediationNetworksConfigPath();
            if (File.Exists(projectPath) && TryParse(File.ReadAllText(projectPath), out var config))
            {
                adMobIds = ABIAdsMediationNetworksApplier.ToHashSet(config.admob_mediation_networks);
                maxIds = ABIAdsMediationNetworksApplier.ToHashSet(config.max_mediation_networks);
                return;
            }

            if (TryLoadLegacyFromGlobalConfig(out config))
            {
                adMobIds = ABIAdsMediationNetworksApplier.ToHashSet(config.admob_mediation_networks);
                maxIds = ABIAdsMediationNetworksApplier.ToHashSet(config.max_mediation_networks);
                if (adMobIds.Count > 0 || maxIds.Count > 0)
                {
                    Save(adMobIds, maxIds);
                }

                return;
            }
        }

        internal static void Save(
            HashSet<string> adMobIds,
            HashSet<string> maxIds,
            bool migrateLegacyGlobalConfig = true)
        {
            var config = new ABIAdsMediationNetworksConfigData
            {
                admob_mediation_networks = ABIAdsMediationNetworksApplier.ToSortedList(adMobIds),
                max_mediation_networks = ABIAdsMediationNetworksApplier.ToSortedList(maxIds)
            };
            Save(config);

            if (migrateLegacyGlobalConfig)
            {
                TryRemoveLegacyFieldsFromGlobalConfig();
            }
        }

        internal static void Save(ABIAdsMediationNetworksConfigData config)
        {
            config.EnsureDefaults();
            var path = ABIAdsEditorPaths.MediationNetworksConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = JsonUtility.ToJson(ABIAdsMediationNetworksConfigJsonDto.FromData(config), true) + Environment.NewLine;
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"ABI Ads mediation networks saved → `{path}`.");
        }

        private static bool TryParse(string raw, out ABIAdsMediationNetworksConfigData config)
        {
            config = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            try
            {
                var dto = JsonUtility.FromJson<ABIAdsMediationNetworksConfigJsonDto>(raw.Trim());
                if (dto == null)
                {
                    return false;
                }

                config = dto.ToData();
                config.EnsureDefaults();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse mediation networks config: {ex.Message}");
                return false;
            }
        }

        private static bool TryLoadLegacyFromGlobalConfig(out ABIAdsMediationNetworksConfigData config)
        {
            config = null;
            var globalPath = ABIAdsEditorPaths.GlobalConfigPath();
            if (!File.Exists(globalPath))
            {
                return false;
            }

            try
            {
                var legacy = JsonUtility.FromJson<ABIAdsGlobalConfigLegacyMediationJsonDto>(File.ReadAllText(globalPath));
                if (legacy == null)
                {
                    return false;
                }

                var adMob = ABIAdsMediationNetworksApplier.ToHashSet(legacy.admob_mediation_networks);
                var max = ABIAdsMediationNetworksApplier.ToHashSet(legacy.max_mediation_networks);
                if (adMob.Count == 0 && max.Count == 0)
                {
                    return false;
                }

                config = new ABIAdsMediationNetworksConfigData
                {
                    admob_mediation_networks = ABIAdsMediationNetworksApplier.ToSortedList(adMob),
                    max_mediation_networks = ABIAdsMediationNetworksApplier.ToSortedList(max)
                };
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not read legacy mediation fields from global_config.json: {ex.Message}");
                return false;
            }
        }

        private static void TryRemoveLegacyFieldsFromGlobalConfig()
        {
            var globalPath = ABIAdsEditorPaths.GlobalConfigPath();
            if (!File.Exists(globalPath))
            {
                return;
            }

            try
            {
                var raw = File.ReadAllText(globalPath);
                if (raw.IndexOf("admob_mediation_networks", StringComparison.Ordinal) < 0
                    && raw.IndexOf("max_mediation_networks", StringComparison.Ordinal) < 0)
                {
                    return;
                }

                if (!ABIAdsConfigStore.TryLoadProjectGlobalConfig(out var globalConfig))
                {
                    return;
                }

                ABIAdsConfigStore.SaveGlobalConfig(globalConfig);
                Debug.Log("ABI Ads removed legacy mediation network fields from global_config.json.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not clean legacy mediation fields from global_config.json: {ex.Message}");
            }
        }
    }
}
