using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsAdMobMediationNetwork
    {
        internal readonly string Id;
        internal readonly string DisplayName;
        internal readonly string AdapterSpec;
        internal readonly string Notes;
        internal readonly string[] ExtraSpecs;
        internal readonly string[] MavenRepositoryUrls;

        internal ABIAdsAdMobMediationNetwork(
            string id,
            string displayName,
            string adapterSpec,
            string notes = "",
            string[] extraSpecs = null,
            string[] mavenRepositoryUrls = null)
        {
            Id = id;
            DisplayName = displayName;
            AdapterSpec = adapterSpec;
            Notes = notes;
            ExtraSpecs = extraSpecs ?? new string[0];
            MavenRepositoryUrls = mavenRepositoryUrls ?? new string[0];
        }

        internal IEnumerable<string> Specs
        {
            get
            {
                yield return AdapterSpec;
                foreach (var spec in ExtraSpecs)
                {
                    yield return spec;
                }
            }
        }
    }

    internal static class ABIAdsAdMobMediationNetworks
    {
        internal const string VersionSource = "Google AdMob mediation adapter docs (classic GMA), scanned 2026-05-28";

        internal static readonly ABIAdsAdMobMediationNetwork[] All =
        {
            new ABIAdsAdMobMediationNetwork("applovin", "AppLovin", "com.google.ads.mediation:applovin:13.6.2.0"),
            new ABIAdsAdMobMediationNetwork(
                "chartboost",
                "Chartboost",
                "com.google.ads.mediation:chartboost:9.12.0.0",
                mavenRepositoryUrls: new[] { "https://cboost.jfrog.io/artifactory/chartboost-ads/" }),
            new ABIAdsAdMobMediationNetwork("dt-exchange", "DT Exchange", "com.google.ads.mediation:fyber:8.4.5.0"),
            new ABIAdsAdMobMediationNetwork("facebook", "Meta Audience Network", "com.google.ads.mediation:facebook:6.21.0.3", "Bidding only in AdMob."),
            new ABIAdsAdMobMediationNetwork(
                "imobile",
                "i-mobile",
                "com.google.ads.mediation:imobile:2.3.2.3",
                "Requires i-mobile Maven repo in Gradle settings.",
                mavenRepositoryUrls: new[] { "https://imobile.github.io/adnw-sdk-android" }),
            new ABIAdsAdMobMediationNetwork("inmobi", "InMobi", "com.google.ads.mediation:inmobi:11.3.0.0"),
            new ABIAdsAdMobMediationNetwork(
                "ironsource",
                "ironSource Ads",
                "com.google.ads.mediation:ironsource:9.4.2.0",
                mavenRepositoryUrls: new[] { "https://android-sdk.is.com/" }),
            new ABIAdsAdMobMediationNetwork("line", "LINE Ads Network", "com.google.ads.mediation:line:3.1.0.0"),
            new ABIAdsAdMobMediationNetwork("liftoff", "Liftoff Monetize", "com.google.ads.mediation:vungle:7.7.4.0"),
            new ABIAdsAdMobMediationNetwork(
                "maio",
                "maio",
                "com.google.ads.mediation:maio:2.0.8.2",
                "Requires maio Maven repo in Gradle settings.",
                mavenRepositoryUrls: new[] { "https://imobile-maio.github.io/maven" }),
            new ABIAdsAdMobMediationNetwork(
                "mintegral",
                "Mintegral",
                "com.google.ads.mediation:mintegral:17.1.61.0",
                mavenRepositoryUrls: new[] { "https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea" }),
            new ABIAdsAdMobMediationNetwork("moloco", "Moloco", "com.google.ads.mediation:moloco:4.8.1.0"),
            new ABIAdsAdMobMediationNetwork("mytarget", "myTarget", "com.google.ads.mediation:mytarget:5.45.3.0"),
            new ABIAdsAdMobMediationNetwork(
                "pangle",
                "Pangle",
                "com.google.ads.mediation:pangle:8.0.0.5.0",
                mavenRepositoryUrls: new[] { "https://artifact.bytedance.com/repository/pangle/" }),
            new ABIAdsAdMobMediationNetwork(
                "pubmatic",
                "PubMatic OpenWrap",
                "com.google.ads.mediation:pubmatic:5.1.2.0",
                "Requires PubMatic Maven repo in Gradle settings.",
                mavenRepositoryUrls: new[] { "https://repo.pubmatic.com/artifactory/public-repos" }),
            new ABIAdsAdMobMediationNetwork(
                "unity",
                "Unity Ads",
                "com.google.ads.mediation:unity:4.18.0.0",
                "Unity Ads SDK is required separately.",
                new[] { "com.unity3d.ads:unity-ads:4.18.0" })
        };
    }

    internal static class ABIAdsMaxMediationNetworks
    {
        internal const string VersionSource = "AppLovin MAX preparing-mediated-networks docs, Gradle adapter versions use +";
        internal const string MaxSdkSpec = "com.applovin:applovin-sdk:+";

        internal static readonly ABIAdsAdMobMediationNetwork[] All =
        {
            new ABIAdsAdMobMediationNetwork("amazon", "Amazon", "com.applovin.mediation:amazon-tam-adapter:+", "Requires Amazon APS initialization and ad response forwarding.", new[] { "com.amazon.android:aps-sdk:+" }),
            new ABIAdsAdMobMediationNetwork("bidmachine", "BidMachine", "com.applovin.mediation:bidmachine-adapter:+", "Requires BidMachine Maven repo.", mavenRepositoryUrls: new[] { "https://artifactory.bidmachine.io/bidmachine" }),
            new ABIAdsAdMobMediationNetwork("bigoads", "BIGO Ads", "com.applovin.mediation:bigoads-adapter:+"),
            new ABIAdsAdMobMediationNetwork("chartboost", "Chartboost", "com.applovin.mediation:chartboost-adapter:+", "Requires Chartboost Maven repo.", new[] { "com.google.android.gms:play-services-base:16.1.0" }, new[] { "https://cboost.jfrog.io/artifactory/chartboost-ads/" }),
            new ABIAdsAdMobMediationNetwork("dt-exchange", "DT Exchange", "com.applovin.mediation:fyber-adapter:+"),
            new ABIAdsAdMobMediationNetwork("google-ad-manager", "Google Ad Manager", "com.applovin.mediation:google-ad-manager-adapter:+"),
            new ABIAdsAdMobMediationNetwork("google-admob", "Google Bidding and Google AdMob", "com.applovin.mediation:google-adapter:+"),
            new ABIAdsAdMobMediationNetwork("hyprmx", "HyprMX", "com.applovin.mediation:hyprmx-adapter:+"),
            new ABIAdsAdMobMediationNetwork("inmobi", "InMobi", "com.applovin.mediation:inmobi-adapter:+", "", new[] { "com.squareup.picasso:picasso:2.8", "androidx.recyclerview:recyclerview:1.1.0" }),
            new ABIAdsAdMobMediationNetwork("ironsource", "ironSource", "com.applovin.mediation:ironsource-adapter:+", mavenRepositoryUrls: new[] { "https://android-sdk.is.com/" }),
            new ABIAdsAdMobMediationNetwork("liftoff", "Liftoff Monetize", "com.applovin.mediation:vungle-adapter:+"),
            new ABIAdsAdMobMediationNetwork("line", "LINE", "com.applovin.mediation:line-adapter:+"),
            new ABIAdsAdMobMediationNetwork("maio", "Maio", "com.applovin.mediation:maio-adapter:+", "Requires Maio Maven repo.", mavenRepositoryUrls: new[] { "https://imobile-maio.github.io/maven" }),
            new ABIAdsAdMobMediationNetwork("facebook", "Meta Audience Network", "com.applovin.mediation:facebook-adapter:+"),
            new ABIAdsAdMobMediationNetwork("mintegral", "Mintegral", "com.applovin.mediation:mintegral-adapter:+", "Requires Mintegral Maven repo.", mavenRepositoryUrls: new[] { "https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea" }),
            new ABIAdsAdMobMediationNetwork("mobilefuse", "MobileFuse", "com.applovin.mediation:mobilefuse-adapter:+"),
            new ABIAdsAdMobMediationNetwork("moloco", "Moloco", "com.applovin.mediation:moloco-adapter:+"),
            new ABIAdsAdMobMediationNetwork("ogury", "Ogury", "com.applovin.mediation:ogury-presage-adapter:+", "Requires Ogury Maven repo.", mavenRepositoryUrls: new[] { "https://maven.ogury.co" }),
            new ABIAdsAdMobMediationNetwork("pangle", "Pangle", "com.applovin.mediation:bytedance-adapter:+", "Requires Pangle Maven repo.", mavenRepositoryUrls: new[] { "https://artifact.bytedance.com/repository/pangle/" }),
            new ABIAdsAdMobMediationNetwork("pubmatic", "PubMatic", "com.applovin.mediation:pubmatic-adapter:+", "Requires PubMatic Maven repo.", mavenRepositoryUrls: new[] { "https://repo.pubmatic.com/artifactory/public-repos" }),
            new ABIAdsAdMobMediationNetwork("smaato", "Smaato", "com.applovin.mediation:smaato-adapter:+", "Requires Smaato Maven repo.", mavenRepositoryUrls: new[] { "https://s3.amazonaws.com/smaato-sdk-releases/" }),
            new ABIAdsAdMobMediationNetwork("unityads", "Unity Ads", "com.applovin.mediation:unityads-adapter:+"),
            new ABIAdsAdMobMediationNetwork("verve", "Verve", "com.applovin.mediation:verve-adapter:+", "Requires Verve Maven repo.", mavenRepositoryUrls: new[] { "https://verve.jfrog.io/artifactory/verve-gradle-release" }),
            new ABIAdsAdMobMediationNetwork("mytarget", "VK Ad Network", "com.applovin.mediation:mytarget-adapter:+"),
            new ABIAdsAdMobMediationNetwork("yandex", "Yandex", "com.applovin.mediation:yandex-adapter:+"),
            new ABIAdsAdMobMediationNetwork("yso", "YSO Network", "com.applovin.mediation:yso-network-adapter:+", "Requires YSO Network Maven repo.", mavenRepositoryUrls: new[] { "https://ysonetwork.s3.eu-west-3.amazonaws.com/sdk/android" })
        };
    }

    internal static class ABIAdsDependenciesXmlStore
    {
        private const string DependenciesFileName = "ABIAdsDependencies.xml";

        internal static string DependenciesPath()
        {
            return Path.Combine(ABIAdsEditorPaths.ResolvePackageRoot(), "Editor", DependenciesFileName);
        }

        internal static HashSet<string> LoadEnabledMediationNetworkIds()
        {
            var enabledIds = new HashSet<string>(StringComparer.Ordinal);
            var specs = LoadAndroidPackageSpecs();
            foreach (var network in ABIAdsAdMobMediationNetworks.All)
            {
                if (specs.Contains(network.AdapterSpec))
                {
                    enabledIds.Add(network.Id);
                }
            }

            return enabledIds;
        }

        internal static HashSet<string> LoadEnabledMaxMediationNetworkIds()
        {
            var enabledIds = new HashSet<string>(StringComparer.Ordinal);
            var specs = LoadAndroidPackageSpecs();
            foreach (var network in ABIAdsMaxMediationNetworks.All)
            {
                if (specs.Contains(network.AdapterSpec))
                {
                    enabledIds.Add(network.Id);
                }
            }

            return enabledIds;
        }

        internal static void SaveEnabledMediationNetworks(HashSet<string> enabledIds)
        {
            var path = DependenciesPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var document = File.Exists(path)
                ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
                : CreateEmptyDocument();

            var androidPackages = EnsureAndroidPackages(document);
            var mediationSpecs = new HashSet<string>(
                ABIAdsAdMobMediationNetworks.All.SelectMany(network => network.Specs),
                StringComparer.Ordinal);

            RemoveSpecs(androidPackages, mediationSpecs);

            foreach (var network in ABIAdsAdMobMediationNetworks.All)
            {
                if (!enabledIds.Contains(network.Id))
                {
                    continue;
                }

                foreach (var spec in network.Specs)
                {
                    androidPackages.Add(new XElement("androidPackage", new XAttribute("spec", spec)));
                }
            }

            document.Save(path);
            ABIAdsMediationGradleConfigurator.SyncFromDependenciesXml();
            Debug.Log($"ABI Ads AdMob mediation dependencies saved to `{path}`.");
        }

        internal static void SaveEnabledMaxMediationNetworks(HashSet<string> enabledIds)
        {
            var path = DependenciesPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var document = File.Exists(path)
                ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
                : CreateEmptyDocument();

            var androidPackages = EnsureAndroidPackages(document);
            RemoveMaxManagedPackages(androidPackages);

            if (enabledIds.Count > 0)
            {
                androidPackages.Add(new XElement("androidPackage", new XAttribute("spec", ABIAdsMaxMediationNetworks.MaxSdkSpec)));
            }

            foreach (var network in ABIAdsMaxMediationNetworks.All)
            {
                if (!enabledIds.Contains(network.Id))
                {
                    continue;
                }

                foreach (var spec in network.Specs)
                {
                    androidPackages.Add(new XElement("androidPackage", new XAttribute("spec", spec)));
                }
            }

            document.Save(path);
            ABIAdsMediationGradleConfigurator.SyncFromDependenciesXml();
            Debug.Log($"ABI Ads MAX mediation dependencies saved to `{path}`.");
        }

        internal static bool ContainsAnyManagedMediationPackages()
        {
            foreach (var spec in LoadAndroidPackageSpecs())
            {
                if (IsManagedAdMobMediationSpec(spec) || IsManagedMaxMediationSpec(spec))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<string> LoadAndroidPackageSpecs()
        {
            var path = DependenciesPath();
            var specs = new HashSet<string>(StringComparer.Ordinal);
            if (!File.Exists(path))
            {
                return specs;
            }

            try
            {
                var document = XDocument.Load(path);
                foreach (var package in document.Descendants("androidPackage"))
                {
                    var spec = (string)package.Attribute("spec");
                    if (!string.IsNullOrWhiteSpace(spec))
                    {
                        specs.Add(spec.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ABI Ads could not parse `{path}`: {ex.Message}");
            }

            return specs;
        }

        private static XElement EnsureAndroidPackages(XDocument document)
        {
            var dependencies = document.Element("dependencies");
            if (dependencies == null)
            {
                dependencies = new XElement("dependencies");
                document.RemoveNodes();
                document.Add(dependencies);
            }

            var androidPackages = dependencies.Element("androidPackages");
            if (androidPackages == null)
            {
                androidPackages = new XElement("androidPackages");
                dependencies.Add(androidPackages);
            }

            return androidPackages;
        }

        private static void RemoveSpecs(XElement androidPackages, HashSet<string> specs)
        {
            foreach (var package in androidPackages.Elements("androidPackage").ToList())
            {
                var spec = (string)package.Attribute("spec");
                if (spec != null && specs.Contains(spec))
                {
                    package.Remove();
                }
            }
        }

        private static void RemoveMaxManagedPackages(XElement androidPackages)
        {
            foreach (var package in androidPackages.Elements("androidPackage").ToList())
            {
                var spec = (string)package.Attribute("spec");
                if (spec != null && IsManagedMaxMediationSpec(spec))
                {
                    package.Remove();
                }
            }
        }

        private static bool IsManagedAdMobMediationSpec(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
            {
                return false;
            }

            foreach (var network in ABIAdsAdMobMediationNetworks.All)
            {
                foreach (var managedSpec in network.Specs)
                {
                    if (string.Equals(managedSpec, spec, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsManagedMaxMediationSpec(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
            {
                return false;
            }

            if (spec.StartsWith("com.applovin:applovin-sdk:", StringComparison.Ordinal)
                || spec.StartsWith("com.applovin.mediation:", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (var network in ABIAdsMaxMediationNetworks.All)
            {
                foreach (var managedSpec in network.Specs)
                {
                    if (string.Equals(managedSpec, spec, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static XDocument CreateEmptyDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("dependencies", new XElement("androidPackages")));
        }
    }
}
