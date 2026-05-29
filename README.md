# ABI Ads Unity Bridge

Unity Package Manager package tích hợp **ABI Module Ads** vào project Unity trên **Android** và **iOS**.

| | |
|---|---|
| Package | `com.abi.ads.unity` (hiện tại **v1.7.11**) |
| Repository | [ABI-Lib-Ads-Support](https://github.com/hongphuong0211/ABI-Lib-Ads-Support) |
| Namespace | `ABI.Ads.UnityBridge` |
| API chính | `ABIAds` |
| Editor | `ABI Ads > Configs` |
| Config runtime | `Resources/Configs/global_config.json`, `Resources/Configs/placements.json` |

Tài liệu song ngữ:

- [English](#english)
- [Tiếng Việt](#tieng-viet)

---

<a id="english"></a>

## English

### Quick start checklist

1. Add `com.abi.ads.unity` to `Packages/manifest.json`.
2. Open **ABI Ads → Configs → Edit Ads Config** — fill **Global Config** + **Placement Config**, save JSON to `Assets/Resources/Configs/`.
3. Tick mediation networks → **Apply To XML** → **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
4. Enable **Custom Main Gradle Template**, **Custom Gradle Settings Template**, **Custom Gradle Properties Template** (Android). See [Android build notes](#8-android-build-notes).
5. Set `AndroidManifest.xml` → `com.abi.ads.modules.unity.ABIUnityAdsApplication` (or extend `AdsMultiDexApplication`).
6. Add an `AdsBootstrap` component — drag **Global Config** and **Placements** `TextAsset`s into the Inspector → `ABIAds.Initialize(...)` → wait `OnInitialized` → preload/show by format.

---

### 1. Install into a Unity project

**Git URL** in `Packages/manifest.json`:

```json
"com.abi.ads.unity": "https://github.com/hongphuong0211/ABI-Lib-Ads-Support.git#v1.7.11"
```

**Local package** (monorepo):

```json
"com.abi.ads.unity": "file:../../unity-package/com.abi.ads.unity"
```

**Copy into project:**

1. Copy `unity-package/com.abi.ads.unity` → `YourGame/Packages/com.abi.ads.unity`
2. Add to `manifest.json`: `"com.abi.ads.unity": "file:com.abi.ads.unity"`

After resolve, Unity imports C# bridge + native plugins (`Plugins/Android/ads-release.aar`, `Plugins/iOS/`).

**ABI custom events** are embedded under `Runtime/ABILibsCustomEvents/` (from [ABI-Custom-Event](https://github.com/hongphuong0211/ABI-Custom-Event) v1.0.0). Do **not** add `com.abilibs.custom-events` separately (duplicate types break the build).

**Host dependencies (recommended):**

- Firebase Analytics (for TROAS/Bamboo forwarding)
- AppLovin MAX Unity SDK (when using MAX or Dual mediation)
- External Dependency Manager (EDM4U) for Android Gradle resolve

---

### 2. Configure global settings and placements

Editor menus:

| Menu | File saved |
|------|------------|
| **ABI Ads → Configs → Edit Ads Config** | Opens Global + Placement windows |
| **ABI Ads → Configs → Edit Global Config** | `Assets/Resources/Configs/global_config.json` |
| **ABI Ads → Configs → Edit Placement Config** | `Assets/Resources/Configs/placements.json` |

#### Global Config (`global_config.json`)

| Field | Description |
|-------|-------------|
| `mediation_provider` | `0` = AdMob, `1` = MAX, `2` = Dual |
| `admob_app_id` | AdMob App ID → Android manifest + iOS `Info.plist` |
| `max_sdk_key` | MAX SDK key (required for MAX/Dual) |
| `variant_dev` | Dev flag: verbose logs, AdMob test units on release, Adjust sandbox. Set **`false`** for store builds |
| `inter_ad_interval` | Minimum ms between interstitial shows (native SDK) |
| `skip_interval_placements` | Placements exempt from inter interval |
| `test_devices` | AdMob test device IDs |
| Optional SDK tokens | Adjust, AppsFlyer, Facebook, TikTok, Firebase, FCM, MAX consent URLs |

#### Placement Config (`placements.json`)

One entry per logical ad slot:

| Field | Description |
|-------|-------------|
| `ad_name` | Placement key used in code, e.g. `main_interstitial` |
| `ads_type` | See [Ad formats](#ad-formats-setup--api) |
| `ad_ids[]` | Ad unit IDs with `ads_weight` and `mediation` (`0` = AdMob, `1` = MAX) |
| `backup_ad_ids[]` | Fallback units |
| Banner/MREC options | inline adaptive, collapsible, size, reload time |
| Native options | layout colors, corner radius, CTA style (Android layout names from module) |

Click **Save Global Config** / **Save Placement Config**. JSON is plain text in the Unity bridge (no encryption in C#).

**Release / encrypted assets (Android):** copy encrypted `1.txt` (placements) and `2.txt` (global) from admin-web export into `Resources/Configs/`. `Initialize()` prefers encrypted assets when present.

---

### 3. Integrate mediation networks

ABI Ads manages **Gradle adapter dependencies** and **Maven repos** from the editor — you still must configure each network in the **AdMob** and/or **AppLovin MAX** dashboards.

#### Step-by-step (Android)

1. **Player Settings → Publishing Settings**
   - Enable **Custom Main Gradle Template**
   - Enable **Custom Gradle Settings Template**
   - Enable **Custom Gradle Properties Template** (`android.useAndroidX=true`, `android.enableJetifier=true`)
2. Open **ABI Ads → Configs → Edit Global Config**.
3. Under **AdMob Mediation Networks** / **MAX Mediation Networks**, tick every network you use in the dashboard.
4. Click **Apply To XML** for each section. This:
   - Writes adapter specs to `Packages/.../Editor/ABIAdsDependencies.xml`
   - Injects Maven repos into `Assets/Plugins/Android/settingsTemplate.gradle` (block `// ABI Ads Mediation Repos Start … End`)
   - Triggers EDM4U **Force Resolve**
5. Confirm `mainTemplate.gradle` still has **GMA classic** block **above** `// Android Resolver Dependencies Start` (EDM may overwrite — re-add if missing):

```gradle
implementation 'com.google.android.gms:play-services-ads:25.3.0'
implementation 'com.google.android.ump:user-messaging-platform:4.0.0'
configurations.configureEach {
}
```

6. Configure the same networks + ad units in **AdMob Mediation** or **MAX Mediation** web UI.
7. In **Placement Config**, set each `ad_ids[].mediation` to match the provider (`0` or `1`). In **Dual** mode, use both AdMob and MAX unit IDs on the same placement as needed.

#### Networks that need extra Maven repos (auto-injected on Apply)

Supported AdMob mediation networks with open-source adapters are listed in [Google choose-networks](https://developers.google.com/admob/android/choose-networks). Tick them in **ABI Ads → Configs → Edit Global Config → AdMob Mediation Networks**, then **Apply To XML**.

| Network | Maven URL (representative) |
|---------|---------------------------|
| Mintegral | `https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea` |
| Pangle | `https://artifact.bytedance.com/repository/pangle/` |
| ironSource | `https://android-sdk.is.com/` |
| PubMatic OpenWrap | `https://repo.pubmatic.com/artifactory/public-repos` |
| Chartboost | `https://cboost.jfrog.io/artifactory/chartboost-ads/` |
| i-mobile | `https://imobile.github.io/adnw-sdk-android` |
| maio | `https://imobile-maio.github.io/maven` |

Other mediated networks get Maven repos injected automatically when you **Apply To XML**. Bidding-only exchange sources on Google (no third-party SDK) do not need Gradle adapters in the app.

#### iOS mediation

After Unity iOS export, use `Plugins/iOS/Podfile.template` → `pod install`. Each mediated network must appear in the Podfile / MAX dashboard. Do not mix Firebase CocoaPods with SwiftPM Firebase (see [iOS build notes](#ios-build-notes)).

---

### 4. Initialize the SDK

Create a bootstrap `MonoBehaviour`, assign config **TextAssets in the Inspector** (from `Assets/Resources/Configs/` after **Save** in the config window), then call `Initialize`:

```csharp
using ABI.Ads.UnityBridge;
using UnityEngine;

public sealed class AdsBootstrap : MonoBehaviour
{
    [SerializeField] private TextAsset globalConfig;     // e.g. global_config.json or 2.txt (encrypted)
    [SerializeField] private TextAsset placementsConfig; // e.g. placements.json or 1.txt (encrypted)

    private void Awake()
    {
        ABIAds.OnBridgeReady += e => Debug.Log("[ABI] bridge ready");
        ABIAds.OnInitialized += OnAdsInitialized;
        ABIAds.Initialize(globalConfig, placementsConfig);
    }

    private void OnAdsInitialized(ABIAdsEvent e)
    {
        if (!e.ready) return;
        PreloadPlacements();
    }

    private void PreloadPlacements()
    {
        ABIAds.Load("main_interstitial");
        ABIAds.Load("main_aoa");
        ABIAds.LoadRewarded("main_reward");
    }

    private void OnDestroy()
    {
        ABIAds.OnInitialized -= OnAdsInitialized;
    }
}
```

**Inspector setup:** attach `AdsBootstrap` to a scene object (e.g. a persistent `AdsManager` GameObject) → drag `global_config.json` (or `2.txt`) and `placements.json` (or `1.txt`) into the two fields.

**Initialize overloads:**

| Method | Use case |
|--------|----------|
| `Initialize(TextAsset, TextAsset)` | **Recommended** — drag JSON/encrypted assets in Inspector |
| `Initialize()` | Fallback: auto-load from `Resources/Configs/` (encrypted `.txt` preferred, then `.json`) |
| `Initialize(globalJson, placementsJson)` | Inline JSON strings |
| `InitializeFromResources(pathGlobal, pathPlacements)` | Custom Resources paths |

**Readiness helpers:**

| API | Meaning |
|-----|---------|
| `IsReady()` | Native ads SDK initialized |
| `IsPlacementReady(placement)` | Ad loaded in pool; `Show()` can display immediately |
| `SetCurrentViewController()` | Call on iOS after scene / root VC change, before show |

Always subscribe to `OnInitialized` (or `EventReceived` with `initialized`) **before** calling `Initialize()`. Preload with `Load()` / `LoadRewarded()` after `ready == true`.

**Android main thread:** On device builds, `Initialize()` posts the JNI bridge call to the **Android UI thread** (`Activity.runOnUiThread`) so Lifecycle / UMP / Activity APIs are safe. You do **not** need to wrap `Initialize()` yourself. The C# call returns immediately — wait for `OnBridgeReady` and `OnInitialized` (check `e.ready`) instead of blocking the caller.

**Do not** rely on synchronous init from a background `Task` or worker thread; register callbacks first, then call `Initialize()` from `Awake` / `Start` as in the sample above.

---

### 5. Ad formats — setup & API

Placement `ads_type` in JSON determines native SDK behavior. Unity API mapping:

| `ads_type` | Primary API | Notes |
|------------|-------------|-------|
| `interstitial` | `Load`, `Show`, `LoadAndShow` | Fullscreen |
| `app_open` | `Load`, `Show`, `LoadAndShow` | Same API; show on resume |
| `rewarded_interstitial` | `Load`, `Show`, `LoadAndShow` | Fullscreen + reward callbacks |
| `rewarded` | `LoadRewarded`, `ShowRewarded`, `LoadAndShowRewarded` | Must use rewarded API |
| `banner` | `ShowBanner`, `HideBanner`, `DestroyBanner` | `position`: `"top"` or `"bottom"` |
| `mrec` | `ShowBanner`, `HideBanner`, `DestroyBanner` | MREC size from placement JSON |
| `native` | `ShowNative`, `SetNativePlaceholderBounds`, `HideNative`, `DestroyNative` | Overlay on native layer |

#### Interstitial / App Open / Rewarded Interstitial

```csharp
// Preload (recommended)
ABIAds.Load("main_interstitial");

// Show when ready
if (ABIAds.IsPlacementReady("main_interstitial"))
    ABIAds.Show("main_interstitial");
else
    ABIAds.LoadAndShow("main_interstitial");

// Or one-shot with loading timeout (ms)
ABIAds.Show("main_interstitial", timeoutLoadingAds: 3000);

// Splash: min delay before show
ABIAds.LoadAndShowWithTimeDelay("splash_interstitial", timeoutMs: 8000, timeDelayMs: 2000);
```

**App Open pattern:**

```csharp
void OnApplicationPause(bool paused)
{
    if (!ABIAds.IsReady()) return;
    if (paused)
        ABIAds.Load("main_aoa");
    else if (ABIAds.IsPlacementReady("main_aoa"))
        ABIAds.Show("main_aoa");
}
```

#### Rewarded

```csharp
ABIAds.RegisterPlacement("main_reward", new ABIAdsPlacementCallbacks
{
    OnRewardGranted = e => GrantReward(e.rewardType, e.rewardAmount),
    OnFailed = e => FallbackToInterstitial(),
    OnClosed = e => ResumeGameplay()
});
ABIAds.ShowRewarded("main_reward", timeoutLoadingAds: 3000);
```

Grant currency on **`reward_granted`**. Use **`reward_completed`** if you need to wait until the ad fully closes.

#### Banner / MREC

```csharp
ABIAds.ShowBanner("main_banner", "bottom");
// ABIAds.ShowBanner("main_mrec", "bottom");  // ads_type = mrec in JSON

ABIAds.HideBanner();    // hide overlay, keep instance
ABIAds.DestroyBanner(); // tear down
```

Banner does not expose a separate `Load()` — `ShowBanner` requests the ad. Overlay stays hidden until loaded (no early shimmer).

#### Native

```csharp
// Normalized screen bounds (0..1) before show
ABIAds.SetNativePlaceholderBounds(minX: 0f, minY: 0.6f, maxX: 1f, maxY: 1f);

// ShowNative loads internally — do NOT chain Load() + OnLoaded → ShowNative()
ABIAds.ShowNative(
    placement: "main_native",
    templateName: "ads_layout_native_language",
    size: "medium",      // small | medium | free_size
    position: "bottom",  // top | center | bottom
    duration: 0            // seconds; < 0 hides close button
);

// Enum overload (recommended for new code)
ABIAds.ShowNative("main_native", "ads_layout_native_language",
    NativeSize.Medium, NativePosition.Bottom, duration: 0);

// Fullscreen native (Google-style)
ABIAds.ShowNativeFullScreen("main_native_full", countDownSec: 3);

// Cleanup when leaving screen
ABIAds.UnregisterPlacement("main_native");
ABIAds.DestroyNative();
```

**Native tips:**

- `ShowNative()` already loads — avoid `Load()` + `OnLoaded` → `ShowNative()` (duplicate show / double callbacks).
- `SetNativePlaceholderBounds(…, maxY: 1f)` anchors to content bottom (not navigation bar) on current AAR.
- `templateName` must match a layout in the ads module. See [Native template files (review)](https://docs.google.com/spreadsheets/d/1LxvJKFlAn_9vDGtWCXLAHsGexKQmfJraV2_DgbhK6ng/edit?gid=0#gid=0).

---

### 6. Callbacks

#### Global events

```csharp
ABIAds.EventReceived += OnAdsEvent;           // all events
ABIAds.OnBridgeReady += e => { };             // bridge_ready
ABIAds.OnInitialized += e => { };             // initialized (check e.ready)
ABIAds.OnViewControllerUpdated += e => { };   // iOS
ABIAds.OnConfigApplied += e => { };           // remote config applied
```

#### Per-placement registration (recommended)

```csharp
ABIAds.RegisterPlacement("main_interstitial", new ABIAdsPlacementCallbacks
{
    OnLoaded = e => { },
    OnFailed = e => { },
    OnImpression = e => { },
    OnClicked = e => { },
    OnClosed = e => { },
    OnDisplayFailed = e => { },
    OnRevenue = e => { }
});

// Fluent
var cb = ABIAds.RegisterPlacement("main_reward");
cb.OnRewardGranted = e => GrantReward();

ABIAds.UnregisterPlacement("main_interstitial");
ABIAds.ClearPlacementCallbacks();
```

#### `ABIAdsEvent` fields

`eventName`, `placement`, `error`, `rewardType`, `rewardAmount`, `revenue`, `currency`, `adUnitId`, `adType`, `network`, `mediationProvider`, `ready`, `remoteApplied`, `platform`, `rawJson`

#### Events by format

| Event | Interstitial / App Open / Rew. Interstitial | Rewarded | Banner / MREC | Native |
|-------|---------------------------------------------|----------|---------------|--------|
| `loaded` | ✓ | ✓ | — | ✓ |
| `failed` | ✓ | ✓ | ✓ | ✓ |
| `impression` | ✓ | ✓ | ✓ | ✓ |
| `clicked` | ✓ | ✓ | ✓ | ✓ |
| `closed` | ✓ | ✓ | — | — |
| `display_failed` | ✓ | ✓ | ✓ | ✓ |
| `revenue` | ✓ | ✓ | ✓ | ✓ |
| `reward_granted` | — | ✓ | — | — |
| `reward_completed` | ✓ (rew. interstitial) | ✓ | — | — |
| `banner_requested` | — | — | ✓ | — |
| `banner_hidden` | — | — | ✓ | — |
| `banner_destroyed` | — | — | ✓ | — |
| `native_requested` | — | — | — | ✓ |
| `native_hidden` | — | — | — | ✓ |
| `native_destroyed` | — | — | — | ✓ |

Global-only: `bridge_ready`, `initialized`, `view_controller_updated`, `config_applied`.

Use constants: `ABIAdsEventNames.Loaded`, `ABIAdsEventNames.RewardGranted`, etc.

---

### 7. ABI custom event forwarding

After `Initialize()`, revenue is forwarded to embedded `ABILibsCustomEvent` (TROAS / Bamboo) when enabled:

- All revenue → `TROASEvent`, `TROASEvent2`
- Rewarded / rewarded interstitial → `BambooRewardedEvent`
- Interstitial / app open → `BambooAdEvent`

`ABIAds.SetCustomEventForwardingEnabled(false)` disables forwarding.

Config asset: `Runtime/ABILibsCustomEvents/Resources/ABILibsCustomEventConfig.asset`.

---

### 8. Android build notes

| Unity | JDK | Gradle templates |
|-------|-----|------------------|
| **Unity 6** (recommended) | 17 | Custom Main + **Settings** + Properties |
| Unity 2022.3 | 11 | Custom Main + Properties (+ Settings if Firebase/mediation) |

Key points:

- Custom `Application`: `com.abi.ads.modules.unity.ABIUnityAdsApplication` or extend `AdsMultiDexApplication`
- Package provides `abi-multidex-keep.pro` + `ABIAdsLauncherMultidexGradlePostProcessor`
- Host `mainTemplate.gradle`: keep **GMA classic** block above EDM resolver block (see [§3](#3-integrate-mediation-networks))
- Gradle snippet files in package `docs/` (`*.gradle.snippet`, `*.properties.snippet`) for host template blocks
- CI env: `ABI_ANDROID_GOOGLE_AD_APP_ID`, `ABI_ANDROID_MAX_SDK_KEY`
- Unity 6 + Firebase: Maven repos in `settingsTemplate.gradle`, not only `mainTemplate.gradle`
- If Gradle cannot resolve mediation artifacts (`mbridge`, `pag-sdk`, …): enable **Custom Gradle Settings Template**, **Apply To XML**, **Force Resolve**, then clean `Library/Bee/Android`
- **`IllegalStateException: addObserver must be called on the main thread`** on init: use a package build where `ABIAds.Initialize()` marshals to the Android UI thread, and rebuild/replace `Plugins/Android/ads-release.aar` from the matching `ads` module. Do not call the Java bridge `initialize` directly from a non-UI thread.

---

### 9. iOS build notes

- Link/embed `BBLModuleAds.framework`; post-build writes `GADApplicationIdentifier`, `AppLovinSdkKey`
- Copy `Plugins/iOS/Podfile.template` → export root → `pod install` → open `.xcworkspace`
- Do not mix Firebase CocoaPods + SwiftPM Firebase
- Avoid duplicate AppLovin embed (MAX export vs CocoaPods)
- CI env: `ABI_IOS_GOOGLE_AD_APP_ID`, `ABI_IOS_MAX_SDK_KEY`
- iOS 15.0+; call `SetCurrentViewController()` when changing scenes

---

<a id="tieng-viet"></a>

## Tiếng Việt

### Checklist tích hợp nhanh

1. Thêm `com.abi.ads.unity` vào `Packages/manifest.json`.
2. Mở **ABI Ads → Configs → Edit Ads Config** — điền **Global Config** + **Placement Config**, lưu vào `Assets/Resources/Configs/`.
3. Tick mediation network → **Apply To XML** → **Force Resolve** (EDM4U).
4. Bật **Custom Main / Settings / Properties Gradle Template** (Android). Xem [Lưu ý build Android](#8-lưu-y-build-android).
5. `AndroidManifest.xml` dùng `ABIUnityAdsApplication` (hoặc kế thừa `AdsMultiDexApplication`).
6. Gắn component bootstrap — kéo **Global Config** và **Placements** (`TextAsset`) vào Inspector → `ABIAds.Initialize(...)` → đợi `OnInitialized` → preload/show theo format.

---

### 1. Cài package vào Unity

**Git URL** trong `Packages/manifest.json`:

```json
"com.abi.ads.unity": "https://github.com/hongphuong0211/ABI-Lib-Ads-Support.git#v1.7.11"
```

**Local package:**

```json
"com.abi.ads.unity": "file:../../unity-package/com.abi.ads.unity"
```

**Copy sang project khác:**

1. Copy `unity-package/com.abi.ads.unity` → `YourGame/Packages/com.abi.ads.unity`
2. Thêm: `"com.abi.ads.unity": "file:com.abi.ads.unity"`

Sau resolve, Unity import C# bridge + native plugin (`Plugins/Android/ads-release.aar`, `Plugins/iOS/`).

**ABI custom events** nhúng sẵn tại `Runtime/ABILibsCustomEvents/`. **Không** thêm package `com.abilibs.custom-events` riêng (trùng type → lỗi build).

**Dependency khuyến nghị ở project host:**

- Firebase Analytics (forward TROAS/Bamboo)
- AppLovin MAX Unity SDK (khi dùng MAX hoặc Dual)
- External Dependency Manager (EDM4U) cho Android

---

### 2. Cấu hình Global và Placement

Menu Editor:

| Menu | File lưu |
|------|----------|
| **ABI Ads → Configs → Edit Ads Config** | Mở cả Global + Placement |
| **ABI Ads → Configs → Edit Global Config** | `Assets/Resources/Configs/global_config.json` |
| **ABI Ads → Configs → Edit Placement Config** | `Assets/Resources/Configs/placements.json` |

#### Global Config

| Trường | Mô tả |
|--------|-------|
| `mediation_provider` | `0` = AdMob, `1` = MAX, `2` = Dual |
| `admob_app_id` | AdMob App ID → manifest Android + `Info.plist` iOS |
| `max_sdk_key` | MAX SDK key (bắt buộc với MAX/Dual) |
| `variant_dev` | Cờ dev: log chi tiết, AdMob test unit trên release, Adjust sandbox. **Tắt (`false`)** khi lên store |
| `inter_ad_interval` | Khoảng cách tối thiểu (ms) giữa các lần show interstitial |
| `skip_interval_placements` | Placement không áp interval |
| `test_devices` | Test device ID AdMob |
| SDK tuỳ chọn | Adjust, AppsFlyer, Facebook, TikTok, Firebase, FCM, MAX consent |

#### Placement Config

Mỗi vị trí quảng cáo một entry:

| Trường | Mô tả |
|--------|-------|
| `ad_name` | Tên placement trong code, ví dụ `main_interstitial` |
| `ads_type` | Xem [Setup từng format](#setup-tung-format-quang-cao) |
| `ad_ids[]` | Ad unit ID + `ads_weight` + `mediation` (`0` AdMob, `1` MAX) |
| `backup_ad_ids[]` | ID dự phòng |
| Banner/MREC | inline adaptive, collapsible, size, reload time |
| Native | màu layout, bo góc, CTA (tên layout Android trong module ads) |

Bấm **Save Global Config** / **Save Placement Config**. JSON lưu dạng plain text (bridge Unity không mã hóa).

**Build release (Android):** copy `1.txt` / `2.txt` đã mã hóa từ admin-web vào `Resources/Configs/`. `Initialize()` ưu tiên file mã hóa nếu có.

---

### 3. Tích hợp mediation network

Package quản lý **Gradle adapter** và **Maven repo** từ Editor — bạn vẫn phải cấu hình network tương ứng trên **AdMob Mediation** và/hoặc **MAX Dashboard**.

#### Quy trình (Android)

1. **Player Settings → Publishing Settings**
   - Bật **Custom Main Gradle Template**
   - Bật **Custom Gradle Settings Template**
   - Bật **Custom Gradle Properties Template**
2. Mở **ABI Ads → Configs → Edit Global Config**.
3. Tick network trong **AdMob Mediation Networks** / **MAX Mediation Networks**.
4. Bấm **Apply To XML** — hệ thống sẽ:
   - Ghi adapter vào `Editor/ABIAdsDependencies.xml`
   - Inject Maven repo vào `settingsTemplate.gradle` (block `// ABI Ads Mediation Repos Start … End`)
   - Chạy EDM4U **Force Resolve**
5. Kiểm tra `mainTemplate.gradle` vẫn có block **GMA classic** **phía trên** `// Android Resolver Dependencies Start`:

```gradle
implementation 'com.google.android.gms:play-services-ads:25.3.0'
implementation 'com.google.android.ump:user-messaging-platform:4.0.0'
configurations.configureEach {
}
```

6. Cấu hình cùng network + ad unit trên **AdMob** / **MAX** web console.
7. Trong **Placement Config**, set `ad_ids[].mediation` đúng provider. Chế độ **Dual**: có thể khai báo cả unit AdMob và MAX trên cùng placement.

#### Network cần Maven repo riêng (tự inject khi Apply)

Danh sách network AdMob mediation (open-source adapter) theo [Google choose-networks](https://developers.google.com/admob/android/choose-networks). Tick trong **ABI Ads → Configs → Edit Global Config → AdMob Mediation Networks**, rồi **Apply To XML**.

| Network | Maven URL |
|---------|-----------|
| Mintegral | `https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea` |
| Pangle | `https://artifact.bytedance.com/repository/pangle/` |
| ironSource | `https://android-sdk.is.com/` |
| PubMatic OpenWrap | `https://repo.pubmatic.com/artifactory/public-repos` |
| Chartboost | `https://cboost.jfrog.io/artifactory/chartboost-ads/` |
| i-mobile | `https://imobile.github.io/adnw-sdk-android` |
| maio | `https://imobile-maio.github.io/maven` |

Các network khác được inject Maven repo tự động khi **Apply To XML**. Nguồn bidding-only trên Google (không cần SDK bên thứ ba) không cần adapter Gradle trong app. Nếu Gradle báo thiếu artifact, bật **Custom Gradle Settings Template** và **Force Resolve**.

#### iOS

Export iOS → dùng `Plugins/iOS/Podfile.template` → `pod install`. Cấu hình network trên MAX/AdMob dashboard tương ứng. Không trộn Firebase CocoaPods với SwiftPM.

---

### 4. Khởi tạo SDK (Init)

Tạo `MonoBehaviour` bootstrap, gán **TextAsset config trong Inspector** (file trong `Assets/Resources/Configs/` sau khi **Save** ở cửa sổ config), rồi gọi `Initialize`:

```csharp
using ABI.Ads.UnityBridge;
using UnityEngine;

public sealed class AdsBootstrap : MonoBehaviour
{
    [SerializeField] private TextAsset globalConfig;     // ví dụ global_config.json hoặc 2.txt (mã hóa)
    [SerializeField] private TextAsset placementsConfig; // ví dụ placements.json hoặc 1.txt (mã hóa)

    private void Awake()
    {
        ABIAds.OnBridgeReady += e => Debug.Log("[ABI] bridge ready");
        ABIAds.OnInitialized += OnAdsInitialized;
        ABIAds.Initialize(globalConfig, placementsConfig);
    }

    private void OnAdsInitialized(ABIAdsEvent e)
    {
        if (!e.ready) return;
        ABIAds.Load("main_interstitial");
        ABIAds.Load("main_aoa");
        ABIAds.LoadRewarded("main_reward");
    }

    private void OnDestroy()
    {
        ABIAds.OnInitialized -= OnAdsInitialized;
    }
}
```

**Setup Inspector:** gắn `AdsBootstrap` lên GameObject (ví dụ `AdsManager` DontDestroyOnLoad) → kéo `global_config.json` (hoặc `2.txt`) và `placements.json` (hoặc `1.txt`) vào hai field.

**Overload Initialize:**

| Method | Khi nào dùng |
|--------|--------------|
| `Initialize(TextAsset, TextAsset)` | **Khuyến nghị** — kéo JSON/file mã hóa vào Inspector |
| `Initialize()` | Dự phòng: tự load từ `Resources/Configs/` (ưu tiên `.txt` mã hóa) |
| `Initialize(globalJson, placementsJson)` | JSON string trực tiếp |
| `InitializeFromResources(...)` | Đường Resources tuỳ chỉnh |

**Kiểm tra trạng thái:**

| API | Ý nghĩa |
|-----|---------|
| `IsReady()` | SDK native đã init xong |
| `IsPlacementReady(placement)` | Ad đã load trong pool, `Show()` hiện ngay |
| `SetCurrentViewController()` | iOS: gọi sau khi đổi scene / root VC |

Luôn đăng ký `OnInitialized` **trước** `Initialize()`. Preload bằng `Load()` / `LoadRewarded()` khi `ready == true`.

**Main thread Android:** Trên bản build device, `Initialize()` tự post lên **Android UI thread** (`Activity.runOnUiThread`) để an toàn với Lifecycle / UMP / Activity. **Không** cần bọc `runOnUiThread` thủ công. Lời gọi C# trả về ngay — đợi `OnBridgeReady` và `OnInitialized` (kiểm tra `e.ready`) thay vì block luồng gọi.

**Không** init đồng bộ từ `Task` / worker thread; đăng ký callback trước, gọi `Initialize()` từ `Awake` / `Start` như mẫu trên.

---

### 5. Setup từng format quảng cáo

`ads_type` trong JSON quyết định hành vi native SDK. Mapping API Unity:

| `ads_type` | API chính | Ghi chú |
|------------|-----------|---------|
| `interstitial` | `Load`, `Show`, `LoadAndShow` | Fullscreen |
| `app_open` | `Load`, `Show`, `LoadAndShow` | Show khi app resume |
| `rewarded_interstitial` | `Load`, `Show`, `LoadAndShow` | Fullscreen + callback reward |
| `rewarded` | `LoadRewarded`, `ShowRewarded`, `LoadAndShowRewarded` | Bắt buộc API rewarded |
| `banner` | `ShowBanner`, `HideBanner`, `DestroyBanner` | `position`: `"top"` / `"bottom"` |
| `mrec` | `ShowBanner`, `HideBanner`, `DestroyBanner` | Kích thước MREC từ JSON placement |
| `native` | `ShowNative`, `SetNativePlaceholderBounds`, `HideNative`, `DestroyNative` | Overlay native |

#### Interstitial / App Open / Rewarded Interstitial

```csharp
ABIAds.Load("main_interstitial");

if (ABIAds.IsPlacementReady("main_interstitial"))
    ABIAds.Show("main_interstitial");
else
    ABIAds.LoadAndShow("main_interstitial");

ABIAds.Show("main_interstitial", timeoutLoadingAds: 3000);
ABIAds.LoadAndShowWithTimeDelay("splash_interstitial", timeoutMs: 8000, timeDelayMs: 2000);
```

**App Open:**

```csharp
void OnApplicationPause(bool paused)
{
    if (!ABIAds.IsReady()) return;
    if (paused)
        ABIAds.Load("main_aoa");
    else if (ABIAds.IsPlacementReady("main_aoa"))
        ABIAds.Show("main_aoa");
}
```

#### Rewarded

```csharp
ABIAds.RegisterPlacement("main_reward", new ABIAdsPlacementCallbacks
{
    OnRewardGranted = e => TraoThuong(e.rewardType, e.rewardAmount),
    OnFailed = e => FallbackInterstitial(),
    OnClosed = e => TiepTucGame()
});
ABIAds.ShowRewarded("main_reward", timeoutLoadingAds: 3000);
```

Trao thưởng tại **`reward_granted`**. Dùng **`reward_completed`** nếu cần đợi ad đóng hẳn.

#### Banner / MREC

```csharp
ABIAds.ShowBanner("main_banner", "bottom");
ABIAds.HideBanner();
ABIAds.DestroyBanner();
```

`ShowBanner` tự request ad; overlay ẩn đến khi load xong (không hiện shimmer sớm).

#### Native

```csharp
ABIAds.SetNativePlaceholderBounds(0f, 0.6f, 1f, 1f);

// ShowNative tự load — KHÔNG gọi Load() + OnLoaded → ShowNative()
ABIAds.ShowNative("main_native", "ads_layout_native_language", "medium", "bottom", 0);

ABIAds.ShowNativeFullScreen("main_native_full", countDownSec: 3);

ABIAds.UnregisterPlacement("main_native");
ABIAds.DestroyNative();
```

**Lưu ý native:**

- Tránh `Load()` + `OnLoaded` → `ShowNative()` (show lặp / callback trùng).
- `maxY = 1f` neo đáy vùng nội dung (không tính navigation bar) trên AAR mới.
- `templateName` phải khớp layout trong module ads. Xem [Danh sách native template (review)](https://docs.google.com/spreadsheets/d/1LxvJKFlAn_9vDGtWCXLAHsGexKQmfJraV2_DgbhK6ng/edit?gid=0#gid=0).

---

### 6. Callback theo format

#### Event toàn cục

```csharp
ABIAds.EventReceived += OnAdsEvent;
ABIAds.OnBridgeReady += e => { };
ABIAds.OnInitialized += e => { };        // kiểm tra e.ready
ABIAds.OnViewControllerUpdated += e => { };
ABIAds.OnConfigApplied += e => { };
```

#### Callback theo placement (khuyến nghị)

```csharp
ABIAds.RegisterPlacement("main_interstitial", new ABIAdsPlacementCallbacks
{
    OnLoaded = e => { },
    OnFailed = e => { },
    OnImpression = e => { },
    OnClicked = e => { },
    OnClosed = e => { },
    OnDisplayFailed = e => { },
    OnRevenue = e => { }
});

var cb = ABIAds.RegisterPlacement("main_reward");
cb.OnRewardGranted = e => TraoThuong();

ABIAds.UnregisterPlacement("main_interstitial");
```

#### Bảng event theo format

| Event | Interstitial / AOA / Rew. Interstitial | Rewarded | Banner / MREC | Native |
|-------|----------------------------------------|----------|---------------|--------|
| `loaded` | ✓ | ✓ | — | ✓ |
| `failed` | ✓ | ✓ | ✓ | ✓ |
| `impression` | ✓ | ✓ | ✓ | ✓ |
| `clicked` | ✓ | ✓ | ✓ | ✓ |
| `closed` | ✓ | ✓ | — | — |
| `display_failed` | ✓ | ✓ | ✓ | ✓ |
| `revenue` | ✓ | ✓ | ✓ | ✓ |
| `reward_granted` | — | ✓ | — | — |
| `reward_completed` | ✓ | ✓ | — | — |
| `banner_requested` | — | — | ✓ | — |
| `banner_hidden` | — | — | ✓ | — |
| `banner_destroyed` | — | — | ✓ | — |
| `native_requested` | — | — | — | ✓ |
| `native_hidden` | — | — | — | ✓ |
| `native_destroyed` | — | — | — | ✓ |

Event global: `bridge_ready`, `initialized`, `view_controller_updated`, `config_applied`.

Hằng số: `ABIAdsEventNames.Loaded`, `ABIAdsEventNames.RewardGranted`, …

#### Trường `ABIAdsEvent`

`eventName`, `placement`, `error`, `rewardType`, `rewardAmount`, `revenue`, `currency`, `adUnitId`, `adType`, `network`, `mediationProvider`, `ready`, `remoteApplied`, `platform`, `rawJson`

---

### 7. Forward ABI Custom Event

Sau `Initialize()`, doanh thu được forward sang `ABILibsCustomEvent` (TROAS/Bamboo) nếu bật:

- Mọi revenue → `TROASEvent`, `TROASEvent2`
- Rewarded / rewarded interstitial → `BambooRewardedEvent`
- Interstitial / app open → `BambooAdEvent`

`ABIAds.SetCustomEventForwardingEnabled(false)` để tắt.

Asset: `Runtime/ABILibsCustomEvents/Resources/ABILibsCustomEventConfig.asset`.

---

### 8. Lưu ý build Android

| Unity | JDK | Gradle template |
|-------|-----|-----------------|
| **Unity 6** (khuyến nghị) | 17 | Custom Main + **Settings** + Properties |
| Unity 2022.3 | 11 | Custom Main + Properties (+ Settings nếu Firebase/mediation) |

Điểm chính:

- Application: `com.abi.ads.modules.unity.ABIUnityAdsApplication` hoặc kế thừa `AdsMultiDexApplication`
- Package có `abi-multidex-keep.pro` + post-processor MultiDex
- `mainTemplate.gradle` host: giữ block **GMA classic** phía trên block EDM (xem [§3](#3-tích-hợp-mediation-network))
- File snippet trong `docs/` package (`*.gradle.snippet`, `*.properties.snippet`) để copy vào template host
- CI: `ABI_ANDROID_GOOGLE_AD_APP_ID`, `ABI_ANDROID_MAX_SDK_KEY`
- Unity 6 + Firebase: repo Maven trong `settingsTemplate.gradle`
- Gradle không resolve mediation (`mbridge`, `pag-sdk`, …): bật **Custom Gradle Settings Template**, **Apply To XML**, **Force Resolve**, xóa `Library/Bee/Android`
- **`IllegalStateException: addObserver must be called on the main thread`** khi init: dùng bản package có `ABIAds.Initialize()` post lên Android UI thread, và build lại / thay `Plugins/Android/ads-release.aar` từ module `ads` tương ứng. Không gọi trực tiếp Java bridge `initialize` từ thread không phải UI.

---

### 9. Lưu ý build iOS

- Link/embed `BBLModuleAds.framework`; post-build ghi `GADApplicationIdentifier`, `AppLovinSdkKey`
- `Podfile.template` → `pod install` → mở `.xcworkspace`
- Không trộn Firebase CocoaPods + SwiftPM
- Tránh embed AppLovin trùng (MAX export vs CocoaPods)
- CI: `ABI_IOS_GOOGLE_AD_APP_ID`, `ABI_IOS_MAX_SDK_KEY`
- iOS 15.0+; gọi `SetCurrentViewController()` khi đổi scene

---

*Repository: [ABI-Lib-Ads-Support](https://github.com/hongphuong0211/ABI-Lib-Ads-Support) — Cập nhật README: 2026-05-29.*
