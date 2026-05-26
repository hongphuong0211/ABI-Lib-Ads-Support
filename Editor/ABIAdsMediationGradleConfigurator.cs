using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    /// <summary>
    /// Injects Maven repository URLs required by enabled mediation adapters into the host
    /// project's Custom Gradle Settings Template (settingsTemplate.gradle).
    /// </summary>
    internal static class ABIAdsMediationGradleConfigurator
    {
        private const string StartMarker = "// ABI Ads Mediation Repos Start";
        private const string EndMarker = "// ABI Ads Mediation Repos End";
        private const string SettingsTemplateRelative = "Plugins/Android/settingsTemplate.gradle";

        internal static void SyncFromDependenciesXml()
        {
            SyncRepositories(
                ABIAdsDependenciesXmlStore.LoadEnabledMediationNetworkIds(),
                ABIAdsDependenciesXmlStore.LoadEnabledMaxMediationNetworkIds());
        }

        internal static void SyncRepositories(
            HashSet<string> enabledAdMobNetworkIds,
            HashSet<string> enabledMaxNetworkIds)
        {
            var repositoryUrls = CollectRepositoryUrls(enabledAdMobNetworkIds, enabledMaxNetworkIds);
            var settingsPath = Path.Combine(Application.dataPath, SettingsTemplateRelative);
            if (!File.Exists(settingsPath))
            {
                if (repositoryUrls.Count > 0)
                {
                    Debug.LogWarning(
                        "ABI Ads mediation requires extra Maven repositories but " +
                        $"`Assets/{SettingsTemplateRelative}` was not found. " +
                        "Enable Player Settings → Publishing Settings → Custom Gradle Settings Template, then Apply mediation again.");
                }

                return;
            }

            var original = File.ReadAllText(settingsPath);
            var patched = PatchSettingsTemplate(original, repositoryUrls);
            if (patched == original)
            {
                return;
            }

            File.WriteAllText(settingsPath, patched);
            Debug.Log(
                $"ABI Ads updated `{SettingsTemplateRelative}` with {repositoryUrls.Count} mediation Maven repo(s). " +
                "Run Assets → External Dependency Manager → Android Resolver → Force Resolve, then rebuild.");
        }

        private static HashSet<string> CollectRepositoryUrls(
            HashSet<string> enabledAdMobNetworkIds,
            HashSet<string> enabledMaxNetworkIds)
        {
            var urls = new HashSet<string>(StringComparer.Ordinal);
            AddRepositoryUrls(urls, ABIAdsAdMobMediationNetworks.All, enabledAdMobNetworkIds);
            AddRepositoryUrls(urls, ABIAdsMaxMediationNetworks.All, enabledMaxNetworkIds);
            return urls;
        }

        private static void AddRepositoryUrls(
            HashSet<string> urls,
            ABIAdsAdMobMediationNetwork[] networks,
            HashSet<string> enabledIds)
        {
            foreach (var network in networks)
            {
                if (!enabledIds.Contains(network.Id) || network.MavenRepositoryUrls == null)
                {
                    continue;
                }

                foreach (var url in network.MavenRepositoryUrls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url.Trim());
                    }
                }
            }
        }

        private static string PatchSettingsTemplate(string content, HashSet<string> repositoryUrls)
        {
            content = RemoveMarkedBlock(content);

            if (repositoryUrls.Count == 0)
            {
                return content;
            }

            var block = BuildRepositoryBlock(repositoryUrls);
            const string resolverEndMarker = "// Android Resolver Repos End";
            var insertIndex = content.IndexOf(resolverEndMarker, StringComparison.Ordinal);
            if (insertIndex >= 0)
            {
                insertIndex += resolverEndMarker.Length;
                return content.Insert(insertIndex, block);
            }

            const string flatDirMarker = "flatDir {";
            insertIndex = content.IndexOf(flatDirMarker, StringComparison.Ordinal);
            if (insertIndex >= 0)
            {
                return content.Insert(insertIndex, block);
            }

            Debug.LogWarning("ABI Ads could not find an insertion point in settingsTemplate.gradle for mediation Maven repos.");
            return content;
        }

        private static string RemoveMarkedBlock(string content)
        {
            var start = content.IndexOf(StartMarker, StringComparison.Ordinal);
            if (start < 0)
            {
                return content;
            }

            var end = content.IndexOf(EndMarker, start, StringComparison.Ordinal);
            if (end < 0)
            {
                return content;
            }

            end += EndMarker.Length;
            while (end < content.Length && (content[end] == '\r' || content[end] == '\n'))
            {
                end++;
            }

            return content.Remove(start, end - start);
        }

        private static string BuildRepositoryBlock(HashSet<string> repositoryUrls)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine("        " + StartMarker);
            foreach (var url in repositoryUrls.OrderBy(u => u, StringComparer.Ordinal))
            {
                var networkLabel = ResolveNetworkLabel(url);
                builder.Append("        maven { url \"");
                builder.Append(url);
                builder.Append("\" }");
                if (!string.IsNullOrEmpty(networkLabel))
                {
                    builder.Append(" // ABI Ads: ");
                    builder.Append(networkLabel);
                }

                builder.AppendLine();
            }

            builder.Append("        ");
            builder.AppendLine(EndMarker);
            return builder.ToString();
        }

        private static string ResolveNetworkLabel(string url)
        {
            foreach (var network in ABIAdsAdMobMediationNetworks.All.Concat(ABIAdsMaxMediationNetworks.All))
            {
                if (network.MavenRepositoryUrls == null)
                {
                    continue;
                }

                if (network.MavenRepositoryUrls.Any(repo => string.Equals(repo, url, StringComparison.Ordinal)))
                {
                    return network.DisplayName;
                }
            }

            return string.Empty;
        }
    }
}
