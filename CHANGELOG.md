# Changelog

## 1.8.6_beta

- Native placement editor: add `close_btn_render_mode` `3` (Random Style) and `4` (Random All).
- Refresh `Plugins/Android/ads-release.aar`.

## 1.8.5

- Fix Unity 6 compile error: use `PackageRegistrationEventArgs.changedTo` instead of removed `changed` when restoring mediation adapters after UPM update.

## 1.8.4

- Persist AdMob/MAX mediation network selections in `Assets/Resources/Configs/mediation_networks.json` (separate from `global_config.json`).
- Auto-restore mediation adapters to `ABIAdsDependencies.xml` after UPM package update or when XML drifts from saved config.
- Migrate legacy `admob_mediation_networks` / `max_mediation_networks` fields from `global_config.json` when present.
- Treat missing or empty `mediation_networks.json` as no mediation adapters; clear stale MAX/AdMob specs from `ABIAdsDependencies.xml` on sync.

## 1.8.3

- **Android inline native:** fix video/media rendering on `ShowNative` overlay (non-fullscreen) over the Unity Activity.
- Refresh `Plugins/Android/ads-release.aar`.

## 1.8.2

- Editor: normalized position DTO for ad configs; `LabeledStringPopup` UI helper; remove unused activity-trigger fields from placement editor.
- Update sample `Resources/Configs/placements.json`.
- Refresh `Plugins/Android/ads-release.aar`.

## 1.8.1

- **Android native fullscreen:** `ShowNativeFullScreen` opens a dedicated Activity instead of overlaying Unity — fixes video/native media not rendering on the Unity surface.
- **Custom events:** TROAS/Bamboo (`ABILibsCustomEvent`) no longer depends on AppLovin MAX Unity SDK — uses `ABIAdRevenueInfo` / `ABIAdFormat`; removed `MaxSdk.Scripts` asmdef reference.
- Bamboo routing: interstitial / rewarded / app open → `BambooAdEvent`; rewarded also fires `BambooRewardedEvent`.
- Refresh `Plugins/Android/ads-release.aar`.

## 1.8.0

- Remove default AdMob mediation adapter `com.google.ads.mediation:applovin` from `Editor/ABIAdsDependencies.xml`; enable networks via **ABI Ads → Configs → Apply**.
- Fix `ABIAdsDependencies.xml` structure (`</androidPackages>` closing tag).
- Refresh `Plugins/Android/ads-release.aar`.

## 1.7.15

- Release `v1.7.15` — align `package.json` version with Git tag for UPM.

## 1.7.14

- **Breaking:** Package requires **Unity 6** (6000.0+) and JDK 17. Unity 2022.3 is no longer supported.
- `package.json`: `unity` minimum `6000.0`, `version` aligned with release tag; README and editor templates updated.

## 1.7.11

- Migrate Android integration in Unity package from GMA Next-Gen to GMA classic (`play-services-ads:25.3.0`).
- Update editor build pipeline checks so `mediation_provider` validates required keys:
  - AdMob/Dual requires `admob_app_id`
  - MAX/Dual requires `max_sdk_key`
- Refresh docs/snippets/templates for classic GMA and dual AdMob + MAX setup consistency.

## 1.7.10

- Add missing `docs/native-template-files.md.meta` (fixes Unity “immutable folder” warning for UPM installs).

## 1.7.9

- `Log.e` / `Log.w` always write to logcat (not gated by `SetEnable`).
- Update `Editor/ABIAdsDependencies.xml` for Unity 6: AndroidX 1.7+/1.12+, explicit `lifecycle-*:2.6.2`, `recyclerview:1.3.2`.
- Align `androidx.lifecycle` to **2.6.2** in `ads/build.gradle` (fix `ProcessLifecycleOwner$Companion` with GMA banner refresh).
- Add [docs/android-build-unity-6.md](docs/android-build-unity-6.md) — full Android build guide for Unity 6 + JDK 17.
- Add Gradle snippets: `mainTemplate.host-unity6.gradle.snippet`, `gradleTemplate.host-unity6.properties.snippet`, `settingsTemplate.host-unity6.firebase.snippet`.
- `ABIAdsLauncherMultidexGradlePostProcessor`: Unity 6 keeps Java 17 (no downgrade); injects lifecycle **2.6.2** on `unityLibrary` + launcher; Unity 2022.3 keeps D8 pins + Java 11.
- Align `androidx.lifecycle` to **2.6.2** in Unity 2022 snippet (fix `ProcessLifecycleOwner$Companion` crash with GMA banner refresh).
- `package.json`: minimum Unity **2022.3** (Unity 6 documented as recommended).

## 1.7.8

- Fix Unity Editor load/save `global_config.json` and `placements.json` (JsonUtility DTO with arrays instead of `List<>`).
- Editor config windows show **Loaded from** (project / package / defaults) and log parse path.
- Fix invalid GUID in `docs/gradleTemplate.host-unity2022-jdk11.properties.snippet.meta`.
- Add [docs/native-template-files.md](docs/native-template-files.md) — native layout template reference for `templateName`.

## 1.7.7

- Unity bridge for ABI Module Ads on Android and iOS (AdMob, MAX, Dual mediation).
- Embedded ABI-Custom-Event for TROAS/Bamboo revenue forwarding.
- Per-placement callbacks (`RegisterPlacement`, `ABIAdsPlacementCallbacks`) and global lifecycle events.
- iOS native overlay: programmatic placeholder/shimmer; `SetNativePlaceholderBounds`, `ShowNative` screen bounds.
- Android: GMA classic, MultiDex keep rules, EDM4U mediation adapter management.
- Add [docs/android-build-unity-2022-jdk11.md](docs/android-build-unity-2022-jdk11.md) for Unity 2022.3 + JDK 11.
- Add Gradle/property snippets for Unity 2022 host projects.
- `ABIAdsLauncherMultidexGradlePostProcessor`: JDK 11 D8 dependency pins.
