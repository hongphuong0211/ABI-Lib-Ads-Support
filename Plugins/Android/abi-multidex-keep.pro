# Keep BBL Application classes in the primary dex (manifest android:name loads before secondary dexes).
-keep class com.abi.ads.modules.unity.ABIUnityAdsApplication { *; }
-keep class com.abi.ads.modules.application.AdsMultiDexApplication { *; }
