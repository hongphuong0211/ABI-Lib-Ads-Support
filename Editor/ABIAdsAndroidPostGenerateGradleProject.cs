#if UNITY_ANDROID
using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace ABI.Ads.UnityBridge.Editor
{
    internal sealed class ABIAdsAndroidPostGenerateGradleProject : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string GoogleMobileAdsAppIdMetaName = "com.google.android.gms.ads.APPLICATION_ID";
        private const string AppLovinSdkKeyMetaName = "applovin.sdk.key";
        private const string EnvironmentGoogleMobileAdsAppIdKey = "ABI_ANDROID_GOOGLE_AD_APP_ID";
        private const string EnvironmentMaxSdkKey = "ABI_ANDROID_MAX_SDK_KEY";
        private const string UnityPlayerActivityClass = "com.unity3d.player.UnityPlayerActivity";
        private const string UnityPlayerGameActivityClass = "com.unity3d.player.UnityPlayerGameActivity";
        private const string MessagingUnityPlayerActivityClass = "com.google.firebase.MessagingUnityPlayerActivity";

        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!TryResolveGeneratedManifestPath(path, out var manifestPath))
            {
                Debug.LogWarning($"ABI Ads could not find generated AndroidManifest.xml under `{path}`.");
                return;
            }

            PatchAndroidManifest(manifestPath);
        }

        private static void PatchAndroidManifest(string manifestPath)
        {
            var settings = ABIAdsBuildSettings.Load();
            var admobAppId = ABIAdsBuildSettings.ResolveEnvironmentString(EnvironmentGoogleMobileAdsAppIdKey, settings.admob_app_id);
            var maxSdkKey = ABIAdsBuildSettings.ResolveEnvironmentString(EnvironmentMaxSdkKey, settings.max_sdk_key);

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(manifestPath);

            var applicationNode = doc.DocumentElement?["application"];
            if (applicationNode == null)
            {
                Debug.LogWarning($"ABI Ads could not find `<application>` in `{manifestPath}`.");
                return;
            }

            SetMetaData(doc, applicationNode, GoogleMobileAdsAppIdMetaName, admobAppId);
            if (!string.IsNullOrWhiteSpace(maxSdkKey))
            {
                SetMetaData(doc, applicationNode, AppLovinSdkKeyMetaName, maxSdkKey);
            }

            EnsureLauncherActivities(doc, applicationNode);

            doc.Save(manifestPath);
            Debug.Log(
                $"ABI Ads patched generated AndroidManifest.xml (`{manifestPath}`): " +
                $"AdMob meta-data{(string.IsNullOrWhiteSpace(maxSdkKey) ? string.Empty : ", MAX SDK key")}, launcher activities.");
        }

        /// <summary>
        /// Custom LibraryManifest replaces Unity's internal template. Host projects must expose
        /// both UnityPlayerActivity and UnityPlayerGameActivity; Unity enables one based on
        /// Player Settings → Application Entry (2022.3 Activity vs Unity 6 Game Activity).
        /// When Firebase MessagingUnityPlayerActivity is the launcher, remove Unity defaults
        /// so Unity Editor and Android only expose a single launch activity.
        /// </summary>
        private static void EnsureLauncherActivities(XmlDocument doc, XmlNode applicationNode)
        {
            var messagingActivity = FindActivityNode(applicationNode, MessagingUnityPlayerActivityClass);
            if (messagingActivity != null && HasLauncherIntentFilter(messagingActivity))
            {
                RemoveActivityNode(applicationNode, UnityPlayerActivityClass);
                RemoveActivityNode(applicationNode, UnityPlayerGameActivityClass);
                return;
            }

            var useGameActivity = UsesGameActivity();
            var activityActivity = FindActivityNode(applicationNode, UnityPlayerActivityClass);
            var gameActivity = FindActivityNode(applicationNode, UnityPlayerGameActivityClass);

            if (activityActivity == null)
            {
                activityActivity = CreateUnityPlayerActivity(doc, applicationNode, useGameActivity: false);
            }

            if (gameActivity == null)
            {
                gameActivity = CreateUnityPlayerGameActivity(doc, applicationNode, useGameActivity: true);
            }

            SetAndroidAttribute(activityActivity, "enabled", useGameActivity ? "false" : "true");
            SetAndroidAttribute(gameActivity, "enabled", useGameActivity ? "true" : "false");

            if (!HasLauncherIntentFilter(activityActivity))
            {
                AppendLauncherIntentFilter(doc, activityActivity);
            }

            if (!HasLauncherIntentFilter(gameActivity))
            {
                AppendLauncherIntentFilter(doc, gameActivity);
            }
        }

        private static bool UsesGameActivity()
        {
#if UNITY_6000_0_OR_NEWER
            return PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.GameActivity;
#else
            return false;
#endif
        }

        private static XmlNode CreateUnityPlayerActivity(XmlDocument doc, XmlNode applicationNode, bool useGameActivity)
        {
            var activity = doc.CreateElement("activity");
            applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "        "));
            applicationNode.AppendChild(activity);
            SetAndroidAttribute(activity, "name", UnityPlayerActivityClass);
            SetAndroidAttribute(activity, "theme", "@style/UnityThemeSelector");
            SetAndroidAttribute(activity, "exported", "true");
            SetAndroidAttribute(activity, "enabled", useGameActivity ? "false" : "true");
            AppendLauncherIntentFilter(doc, activity);
            AppendMetaData(doc, activity, "unityplayer.UnityActivity", "true");
            applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "    "));
            return activity;
        }

        private static XmlNode CreateUnityPlayerGameActivity(XmlDocument doc, XmlNode applicationNode, bool useGameActivity)
        {
            var activity = doc.CreateElement("activity");
            applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "        "));
            applicationNode.AppendChild(activity);
            SetAndroidAttribute(activity, "name", UnityPlayerGameActivityClass);
            SetAndroidAttribute(activity, "theme", "@style/BaseUnityGameActivityTheme");
            SetAndroidAttribute(activity, "exported", "true");
            SetAndroidAttribute(activity, "enabled", useGameActivity ? "true" : "false");
            AppendLauncherIntentFilter(doc, activity);
            AppendMetaData(doc, activity, "unityplayer.UnityActivity", "true");
            AppendMetaData(doc, activity, "android.app.lib_name", "game");
            applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "    "));
            return activity;
        }

        private static void AppendLauncherIntentFilter(XmlDocument doc, XmlNode activityNode)
        {
            var filter = doc.CreateElement("intent-filter");
            activityNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "            "));

            var action = doc.CreateElement("action");
            SetAndroidAttribute(action, "name", "android.intent.action.MAIN");
            filter.AppendChild(action);
            filter.AppendChild(doc.CreateWhitespace(Environment.NewLine + "                "));

            var category = doc.CreateElement("category");
            SetAndroidAttribute(category, "name", "android.intent.category.LAUNCHER");
            filter.AppendChild(category);
            filter.AppendChild(doc.CreateWhitespace(Environment.NewLine + "            "));

            activityNode.AppendChild(filter);
            activityNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "        "));
        }

        private static void AppendMetaData(XmlDocument doc, XmlNode parent, string name, string value)
        {
            parent.AppendChild(doc.CreateWhitespace(Environment.NewLine + "            "));
            var meta = doc.CreateElement("meta-data");
            SetAndroidAttribute(meta, "name", name);
            SetAndroidAttribute(meta, "value", value);
            parent.AppendChild(meta);
        }

        private static XmlNode FindActivityNode(XmlNode applicationNode, string activityClass)
        {
            foreach (XmlNode child in applicationNode.ChildNodes)
            {
                if (child.Name != "activity")
                {
                    continue;
                }

                var nameAttribute = child.Attributes?["android:name"] ?? child.Attributes?["name", AndroidNamespace];
                if (nameAttribute != null && nameAttribute.Value == activityClass)
                {
                    return child;
                }
            }

            return null;
        }

        private static void RemoveActivityNode(XmlNode applicationNode, string activityClass)
        {
            for (var i = 0; i < applicationNode.ChildNodes.Count; i++)
            {
                var child = applicationNode.ChildNodes[i];
                if (child.Name != "activity")
                {
                    continue;
                }

                var nameAttribute = child.Attributes?["android:name"] ?? child.Attributes?["name", AndroidNamespace];
                if (nameAttribute == null || nameAttribute.Value != activityClass)
                {
                    continue;
                }

                if (i > 0 && applicationNode.ChildNodes[i - 1].NodeType == XmlNodeType.Whitespace)
                {
                    applicationNode.RemoveChild(applicationNode.ChildNodes[i - 1]);
                    i--;
                }

                applicationNode.RemoveChild(child);
                return;
            }
        }

        private static bool HasLauncherIntentFilter(XmlNode activityNode)
        {
            foreach (XmlNode child in activityNode.ChildNodes)
            {
                if (child.Name != "intent-filter" || !IntentFilterHasMainLauncher(child))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IntentFilterHasMainLauncher(XmlNode intentFilter)
        {
            var hasMain = false;
            var hasLauncher = false;

            foreach (XmlNode child in intentFilter.ChildNodes)
            {
                if (child.Name == "action")
                {
                    var name = child.Attributes?["android:name"] ?? child.Attributes?["name", AndroidNamespace];
                    if (name?.Value == "android.intent.action.MAIN")
                    {
                        hasMain = true;
                    }
                }
                else if (child.Name == "category")
                {
                    var name = child.Attributes?["android:name"] ?? child.Attributes?["name", AndroidNamespace];
                    if (name?.Value == "android.intent.category.LAUNCHER")
                    {
                        hasLauncher = true;
                    }
                }
            }

            return hasMain && hasLauncher;
        }

        private static void SetMetaData(XmlDocument doc, XmlNode applicationNode, string name, string value)
        {
            var metaDataNode = FindMetaDataNode(applicationNode, name);
            if (metaDataNode == null)
            {
                metaDataNode = doc.CreateElement("meta-data");
                applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "        "));
                applicationNode.AppendChild(metaDataNode);
                applicationNode.AppendChild(doc.CreateWhitespace(Environment.NewLine + "    "));
            }

            SetAndroidAttribute(metaDataNode, "name", name);
            SetAndroidAttribute(metaDataNode, "value", value);
        }

        private static XmlNode FindMetaDataNode(XmlNode applicationNode, string name)
        {
            foreach (XmlNode child in applicationNode.ChildNodes)
            {
                if (child.Name != "meta-data")
                {
                    continue;
                }

                var nameAttribute = child.Attributes?["android:name"] ?? child.Attributes?["name", AndroidNamespace];
                if (nameAttribute != null && nameAttribute.Value == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void SetAndroidAttribute(XmlNode node, string localName, string value)
        {
            var doc = node.OwnerDocument;
            var attribute = node.Attributes?["android:" + localName] ?? node.Attributes?[localName, AndroidNamespace];
            if (attribute == null)
            {
                attribute = doc.CreateAttribute("android", localName, AndroidNamespace);
                node.Attributes.Append(attribute);
            }

            attribute.Value = value;
        }

        private static bool TryResolveGeneratedManifestPath(string path, out string manifestPath)
        {
            var candidates = new[]
            {
                Path.Combine(path, "src", "main", "AndroidManifest.xml"),
                Path.Combine(path, "AndroidManifest.xml")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    manifestPath = candidate;
                    return true;
                }
            }

            manifestPath = null;
            return false;
        }
    }
}
#endif
