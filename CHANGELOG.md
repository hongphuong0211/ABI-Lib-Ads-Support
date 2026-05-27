# Changelog

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
- Android: GMA Next-Gen, MultiDex keep rules, EDM4U mediation adapter management.
- Add [docs/android-build-unity-2022-jdk11.md](docs/android-build-unity-2022-jdk11.md) for Unity 2022.3 + JDK 11.
- Add Gradle/property snippets for Unity 2022 host projects.
- `ABIAdsLauncherMultidexGradlePostProcessor`: JDK 11 D8 dependency pins.
