using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal static class ABIAdsMediationNetworksApplier
    {
        internal static HashSet<string> ToHashSet(IList<string> ids)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (ids == null)
            {
                return set;
            }

            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    set.Add(id.Trim());
                }
            }

            return set;
        }

        internal static List<string> ToSortedList(HashSet<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<string>();
            }

            var list = ids.ToList();
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        internal static void LoadPersistedNetworkIds(out HashSet<string> adMobIds, out HashSet<string> maxIds)
        {
            ABIAdsMediationNetworksConfigStore.LoadPersistedNetworkIds(out adMobIds, out maxIds);
        }

        internal static bool IsXmlOutOfSync(HashSet<string> expectedAdMob, HashSet<string> expectedMax)
        {
            expectedAdMob = expectedAdMob ?? new HashSet<string>(StringComparer.Ordinal);
            expectedMax = expectedMax ?? new HashSet<string>(StringComparer.Ordinal);

            if (expectedAdMob.Count == 0 && expectedMax.Count == 0)
            {
                return ABIAdsDependenciesXmlStore.ContainsAnyManagedMediationPackages();
            }

            var xmlAdMob = ABIAdsDependenciesXmlStore.LoadEnabledMediationNetworkIds();
            var xmlMax = ABIAdsDependenciesXmlStore.LoadEnabledMaxMediationNetworkIds();
            return !SetsEqual(expectedAdMob, xmlAdMob) || !SetsEqual(expectedMax, xmlMax);
        }

        internal static void ApplyToDependenciesXml(HashSet<string> adMobIds, HashSet<string> maxIds)
        {
            adMobIds = adMobIds ?? new HashSet<string>(StringComparer.Ordinal);
            maxIds = maxIds ?? new HashSet<string>(StringComparer.Ordinal);
            ABIAdsDependenciesXmlStore.SaveEnabledMediationNetworks(adMobIds);
            ABIAdsDependenciesXmlStore.SaveEnabledMaxMediationNetworks(maxIds);
        }

        internal static void PersistAndApply(HashSet<string> adMobIds, HashSet<string> maxIds)
        {
            ABIAdsMediationNetworksConfigStore.Save(adMobIds, maxIds);
            ApplyToDependenciesXml(adMobIds, maxIds);
            AssetDatabase.Refresh();
            TryForceResolveAndroidDependencies();
        }

        internal static void RestoreFromPersistedConfigIfNeeded(string reason)
        {
            LoadPersistedNetworkIds(out var adMobIds, out var maxIds);

            if (!IsXmlOutOfSync(adMobIds, maxIds))
            {
                return;
            }

            ApplyToDependenciesXml(adMobIds, maxIds);
            AssetDatabase.Refresh();
            TryForceResolveAndroidDependencies();

            if (adMobIds.Count == 0 && maxIds.Count == 0)
            {
                Debug.Log(
                    $"ABI Ads cleared mediation adapters because no networks are configured " +
                    $"(missing/empty Assets/Resources/Configs/mediation_networks.json) ({reason}).");
                return;
            }

            Debug.Log(
                $"ABI Ads restored mediation adapters from Assets/Resources/Configs/mediation_networks.json ({reason}). " +
                $"AdMob: {adMobIds.Count}, MAX: {maxIds.Count}.");
        }

        internal static void TryForceResolveAndroidDependencies()
        {
            var resolverTypes = new[]
            {
                "GooglePlayServices.PlayServicesResolver, Google.JarResolver",
                "GooglePlayServices.PlayServicesResolver, GooglePlayServicesResolver"
            };

            foreach (var typeName in resolverTypes)
            {
                try
                {
                    var resolverType = Type.GetType(typeName);
                    if (resolverType == null)
                    {
                        continue;
                    }

                    var resolveSync = resolverType.GetMethod(
                        "ResolveSync",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Type.EmptyTypes,
                        null);
                    if (resolveSync != null)
                    {
                        resolveSync.Invoke(null, null);
                        return;
                    }

                    var resolve = resolverType.GetMethod(
                        "Resolve",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Type.EmptyTypes,
                        null);
                    resolve?.Invoke(null, null);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ABI Ads could not run EDM4U Force Resolve automatically: {ex.Message}");
                }
            }
        }

        private static bool SetsEqual(HashSet<string> left, HashSet<string> right)
        {
            left = left ?? new HashSet<string>(StringComparer.Ordinal);
            right = right ?? new HashSet<string>(StringComparer.Ordinal);
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var value in left)
            {
                if (!right.Contains(value))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [InitializeOnLoad]
    internal static class ABIAdsMediationNetworksPackageRestore
    {
        static ABIAdsMediationNetworksPackageRestore()
        {
            UnityEditor.PackageManager.Events.registeredPackages += OnRegisteredPackages;
            EditorApplication.delayCall += OnEditorStartup;
        }

        private static void OnEditorStartup()
        {
            ABIAdsMediationNetworksApplier.RestoreFromPersistedConfigIfNeeded("editor startup sync");
        }

        private static void OnRegisteredPackages(UnityEditor.PackageManager.PackageRegistrationEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            foreach (var package in args.added.Concat(args.changed))
            {
                if (!string.Equals(package.name, ABIAdsEditorPaths.PackageName, StringComparison.Ordinal))
                {
                    continue;
                }

                EditorApplication.delayCall += () =>
                    ABIAdsMediationNetworksApplier.RestoreFromPersistedConfigIfNeeded(
                        $"UPM package update ({package.version})");
                return;
            }
        }
    }
}
