# Ghi chú build Android (ABI Ads)

Troubleshooting **chung** + **Unity 6**. Setup Gradle đầy đủ cho **Unity 2022.3 + JDK 11** nằm ở doc riêng — không trùng lặp nội dung ở đây.

| Phiên bản | Tài liệu |
|-----------|----------|
| **Unity 2022.3 + JDK 11** | **[android-build-unity-2022-jdk11.md](android-build-unity-2022-jdk11.md)** — template, GMA Next-Gen, D8 pins, duplicate class, checklist |
| **Unity 6 + JDK 17** | **[android-build-unity-6.md](android-build-unity-6.md)** — template, GMA Next-Gen, lifecycle, Firebase settings, checklist |

**Không** copy `mainTemplate.gradle` / manifest giữa Unity 2022.3 và Unity 6.

---

## Unity 2022.3 vs Unity 6

| | Unity 2022.3 | Unity 6 |
|---|--------------|---------|
| Gradle `android {}` | `compileSdkVersion`, `minSdkVersion`, `lintOptions`, `aaptOptions` | `compileSdk`, `minSdk`, `lint`, `androidResources` |
| Placeholder | `**MINSDKVERSION**` | `**MINSDK**` |
| `mainTemplate` thêm | — | `apply from: '../shared/keepUnitySymbols.gradle'` (chỉ U6) |
| Java | `VERSION_11` | `VERSION_17` |
| Launcher | `UnityPlayerActivity` | `UnityPlayerGameActivity` (Game Activity) |
| Firebase local repo | Thường đủ qua EDM | Cần **Custom Gradle Settings Template** |

---

## Setup nhanh (mọi phiên bản)

1. Cài `com.abi.ads.unity` (UPM / `file:`).
2. **Player Settings → Publishing Settings:** Custom Main Gradle Template + Gradle Properties (+ Settings nếu Firebase/mediation).
3. **ABI Ads → Configs** — AdMob App ID, MAX key (nếu có), mediation → **Apply**.
4. **EDM4U → Force Resolve** → xóa `Library/Bee/Android` → build lại.

**Manifest** (`Assets/Plugins/Android/AndroidManifest.xml`):

- Application: `com.abi.ads.modules.unity.ABIUnityAdsApplication` (hoặc kế thừa `AdsMultiDexApplication`).
- Khai báo **cả hai** activity (`UnityPlayerActivity` + `UnityPlayerGameActivity`); mẫu: `Plugins/Android/AndroidManifest.template`. Post-processor bật đúng entry theo Application Entry.

**Package tự cung cấp:** `ABIAdsDependencies.xml`, `abi-multidex-keep.pro`, `ABIAdsLauncherMultidexGradlePostProcessor` (MultiDex; Unity 2022.3: D8 pins + Java 11 trên launcher; Unity 6: lifecycle alignment trên launcher + unityLibrary, giữ Java 17). Sau build cần log `[ABI Ads] Patched launcher/build.gradle` (và `unityLibrary` trên Unity 6).

**CI:** `ABI_ANDROID_GOOGLE_AD_APP_ID`, `ABI_ANDROID_MAX_SDK_KEY`.

---

## Dependency `ads-debug.aar`

AAR flat **không kéo** transitive — EDM resolve từ `Editor/ABIAdsDependencies.xml` (layout/UI). Host thêm **GMA Next-Gen** (không do EDM tự thêm):

| Host `mainTemplate.gradle` (trên block EDM) | |
|---------------------------------------------|---|
| `ads-mobile-sdk:1.1.0` | Bắt buộc |
| `user-messaging-platform:4.0.0` | UMP |
| `exclude play-services-ads*` | Tránh duplicate với mediation |

Chi tiết + snippet: [android-build-unity-2022-jdk11.md](android-build-unity-2022-jdk11.md). `ABIAdsDependencies.xml` **không** khai báo Firebase.

| Thiếu resource (ví dụ) | Gradle (đã trong `ABIAdsDependencies.xml`) |
|--------------------------|------------------------------------------|
| AppCompat theme | `appcompat:1.7.0` |
| `@dimen/_*sdp` | `sdp-android:1.0.6` |
| ConstraintLayout | `constraintlayout:2.2.0` |
| RecyclerView | `recyclerview:1.3.2` |
| Shimmer | `shimmer:0.5.0` |
| Material behaviors | `material:1.12.0` |
| Lottie | `lottie:5.0.2` |
| MultiDex | `multidex:2.0.1` + `multiDexEnabled` trên launcher |
| `ProcessLifecycleOwner$Companion` | `lifecycle-*:2.6.2` (runtime, process, ktx, …) |

---

## Unity 6

Hướng dẫn đầy đủ: **[android-build-unity-6.md](android-build-unity-6.md)** (snippet `mainTemplate.host-unity6.gradle.snippet`, Firebase `settingsTemplate.host-unity6.firebase.snippet`).

Tóm tắt:

- **Custom Gradle Settings Template** bắt buộc nếu dùng Firebase Unity hoặc mediation cần repo Maven riêng.
- `mainTemplate`: `keepUnitySymbols.gradle`, Java **17**, GMA Next-Gen + align `androidx.lifecycle` **2.6.2**.
- Post-processor **không** hạ Java 17 → 11 trên Unity 6.

---

## Lỗi build (Gradle / EDM)

| Lỗi | Gợi ý |
|-----|--------|
| `keepUnitySymbols.gradle` không tồn tại | Template Unity 6 trên Unity 2022.3 → [android-build-unity-2022-jdk11.md §7.1](android-build-unity-2022-jdk11.md#71-keepunitysymbolsgradle-does-not-exist) |
| Duplicate `com.google.android.gms.ads.*` | [§7.2](android-build-unity-2022-jdk11.md#72-duplicate-class-comgoogleandroidgmsads) |
| D8 NPE `webkit` / `error_prone` | [§7.3–7.4](android-build-unity-2022-jdk11.md#73-d8-nullpointerexception--webkit-1150-error_prone_annotations-2410) |
| `Could not find firebase-*-unity` | Bật Settings template; repo local Firebase; Force Resolve. Comment XML trong `*Dependencies.xml` **không** chứa `--` (EDM parse fail). |
| `Could not find mbridge` / `pag-sdk` | Bật Settings template; **ABI Ads → Configs → Apply** (inject repo) hoặc thêm `// ABI Ads Mediation Repos` trong `settingsTemplate.gradle` (Mintegral, Pangle, PubMatic, …). |

---

## Lỗi runtime

### `ClassNotFoundException: ABIUnityAdsApplication`

Application nằm **dex phụ** — cần MultiDex + `abi-multidex-keep.pro` + patch launcher (package). Gỡ app cũ, clean `Library/Bee/Android`.

### `NoClassDefFoundError: InitializationConfig$Builder`

Thiếu GMA Next-Gen → [android-build-unity-2022-jdk11.md §3](android-build-unity-2022-jdk11.md#31-block-host-phía-trên--android-resolver-dependencies-start) hoặc [android-build-unity-6.md §3.1](android-build-unity-6.md#31-block-host-phía-trên-android-resolver-dependencies-start).

### `NoSuchFieldError: ProcessLifecycleOwner$Companion`

Xung đột `androidx.lifecycle` (banner refresh GMA) → align lifecycle **2.6.2** — [android-build-unity-6.md §8.1](android-build-unity-6.md#81-nosuchfielderror-processlifecycleownercompanion) / [android-build-unity-2022-jdk11.md](android-build-unity-2022-jdk11.md) block `eachDependency`.

### `ClassNotFoundException: UnityPlayerGameActivity` (trên 2022.3)

Manifest chỉ bật Game Activity trong khi Unity 2022.3 không có class đó → dùng manifest mẫu package (cả hai activity).

### Native / Banner (C#)

```csharp
// Native: chỉ ShowNative, tránh Load + OnLoaded → ShowNative lặp
ABIAds.SetNativePlaceholderBounds(0, 0.6f, 1f, 1f);
ABIAds.ShowNative("splash_native", "ads_layout_native_language", "medium", "bottom", 0);

if (ABIAds.IsPlacementReady("main_interstitial"))
    ABIAds.Show("main_interstitial");
else
    ABIAds.Load("main_interstitial");
```

`IsReady()` = SDK init; `IsPlacementReady()` = ad trong pool. Rời splash: `UnregisterPlacement` + `DestroyNative`. Cập nhật `ads-debug.aar` sau khi sửa module `ads/`.

---

## Checklist

**Unity 2022.3 + JDK 11:** [android-build-unity-2022-jdk11.md §10](android-build-unity-2022-jdk11.md#10-checklist-nhanh)

**Unity 6:** [android-build-unity-6.md §10](android-build-unity-6.md#10-checklist-nhanh)

---

## Khác

- `UnityPlayerActivity uses deprecated API` — cảnh báo Unity, bỏ qua.
- Store build: `ads-release.aar` thay `ads-debug.aar`.
- R8/minify: keep rules trong `ads/proguard-rules.pro`.
- Rebuild AAR: `BBL-Module-Ads/ads/` → `unity-package/.../Plugins/Android/ads-debug.aar`.
