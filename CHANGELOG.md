# Changelog

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
