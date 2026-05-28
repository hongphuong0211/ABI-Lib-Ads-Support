# Build Android — Unity 6 + JDK 17

Hướng dẫn **đầy đủ** cho **Unity 6.x** (6000.x) + **JDK 17** (Unity bundled). Unity 2022.3 + JDK 11: [android-build-unity-2022-jdk11.md](android-build-unity-2022-jdk11.md). Troubleshooting chung: [android-build-notes.md](android-build-notes.md).

**Mẫu copy-paste:**

| File | Mẫu trong package |
|------|-------------------|
| Block host `mainTemplate.gradle` | [mainTemplate.host-unity6.gradle.snippet](mainTemplate.host-unity6.gradle.snippet) |
| `gradleTemplate.properties` | [gradleTemplate.host-unity6.properties.snippet](gradleTemplate.host-unity6.properties.snippet) |
| Firebase `settingsTemplate.gradle` | [settingsTemplate.host-unity6.firebase.snippet](settingsTemplate.host-unity6.firebase.snippet) |

---

## 1. Yêu cầu môi trường

| Thành phần | Giá trị |
|------------|---------|
| Unity | **6000.x** (Unity 6 LTS) |
| JDK | **17** — *JDK installed with Unity (recommended)* |
| AGP / Gradle | Do Unity 6 cung cấp (AGP 8.x) |
| `compileOptions` | `JavaVersion.VERSION_17` |
| Application Entry | **Game Activity** (`UnityPlayerGameActivity`) hoặc **Activity** (`UnityPlayerActivity`) |

**Không** dùng template Gradle Unity 2022.3 (`compileSdkVersion`, `**MINSDKVERSION**`, `lintOptions`, `aaptOptions`, `JavaVersion.VERSION_11`).

---

## 2. Player Settings → Publishing Settings

Bật:

- [x] **Custom Main Gradle Template**
- [x] **Custom Gradle Properties Template**
- [x] **Custom Gradle Settings Template** — **bắt buộc** nếu dùng Firebase Unity và/hoặc mediation Mintegral / Pangle / …

### JDK 17 trong Unity

**Edit → Preferences → External Tools**

1. Bật **JDK installed with Unity (recommended)** (Temurin 17).
2. **Không** ép Gradle build về JDK 11 — Unity 6 + AGP 8 cần Java 17.

---

## 3. `mainTemplate.gradle` (host project)

Xuất phát từ template gốc Unity 6:

`{UnityEditor}/Data/PlaybackEngines/AndroidPlayer/Tools/GradleTemplates/mainTemplate.gradle`

### 3.1 Block host (phía **trên** `// Android Resolver Dependencies Start`)

EDM4U **không** tự thêm block này — copy từ [mainTemplate.host-unity6.gradle.snippet](mainTemplate.host-unity6.gradle.snippet):

```gradle
// ABI Ads — Unity 6 + JDK 17 (docs/android-build-unity-6.md)
configurations.configureEach {
    resolutionStrategy {
        eachDependency { details ->
            if (details.requested.group == 'androidx.lifecycle') {
                details.useVersion '2.6.2'
                details.because 'ProcessLifecycleOwner.Companion (GMA banner refresh)'
            }
        }
    }
}

dependencies {
    implementation 'com.google.android.gms:play-services-ads:25.3.0'
    implementation 'com.google.android.ump:user-messaging-platform:4.0.0'
```

| Dòng | Mục đích |
|------|----------|
| `play-services-ads:25.3.0` | GMA classic — **bắt buộc** cho `ads-debug.aar` / `ads-release.aar` |
| `user-messaging-platform:4.0.0` | UMP trước `MobileAds.initialize` |
| Không exclude `play-services-ads*` | Dùng GMA classic nên không chặn dependency này |
| `lifecycle → 2.6.2` | Tránh `NoSuchFieldError: ProcessLifecycleOwner$Companion` khi show banner refresh |

EDM cũng resolve lifecycle từ `Editor/ABIAdsDependencies.xml` (`lifecycle-runtime`, `lifecycle-process`, … **2.6.2**).

**Không** thêm `com.google.android.gms:play-services-ads:24.x` song song `play-services-ads`.

**Unity 6:** **không** cần `force error_prone_annotations` / `force webkit` (chỉ dùng trên Unity 2022.3 + AGP 7.4).

Package còn **tự inject** block lifecycle lên `unityLibrary/build.gradle` khi build nếu thiếu — vẫn nên giữ block host trên `mainTemplate` để exclude GMA cũ.

### 3.2 Khung `android { }` — Unity 6

```gradle
apply plugin: 'com.android.library'
apply from: '../shared/keepUnitySymbols.gradle'
**APPLY_PLUGINS**

android {
    namespace "com.unity3d.player"
    ndkPath "**NDKPATH**"
    compileSdk **APIVERSION**
    buildToolsVersion '**BUILDTOOLS**'

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_17
        targetCompatibility JavaVersion.VERSION_17
    }

    defaultConfig {
        minSdk **MINSDK**
        targetSdk **TARGETSDK**
        ...
    }

    lint {
        abortOnError false
    }

    androidResources {
        noCompress = **BUILTIN_NOCOMPRESS** + unityStreamingAssets.tokenize(', ')
        ignoreAssetsPattern = "!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~"
    }**PACKAGING**
}
```

**Không dùng** (Unity 2022.3): `compileSdkVersion`, `minSdkVersion`, `lintOptions`, `aaptOptions`, `JavaVersion.VERSION_11`.

---

## 4. `gradleTemplate.properties`

Merge từ [gradleTemplate.host-unity6.properties.snippet](gradleTemplate.host-unity6.properties.snippet):

```properties
android.useAndroidX=true
android.enableJetifier=true
```

Giữ `**JVM_HEAP_SIZE**`, `unityStreamingAssets=**STREAMING_ASSETS**`, `**ADDITIONAL_PROPERTIES**`.

---

## 5. `settingsTemplate.gradle` + Firebase

Unity 6 + `dependencyResolutionManagement` thường **không** resolve `firebase-*-unity` chỉ qua `mainTemplate` — cần repo local trong **Settings template**.

1. Bật **Custom Gradle Settings Template**.
2. Sau `// Android Resolver Repos End`, thêm (hoặc để EDM + snippet):

   [settingsTemplate.host-unity6.firebase.snippet](settingsTemplate.host-unity6.firebase.snippet)

3. Mediation: **ABI Ads → Configs → Apply** inject block `// ABI Ads Mediation Repos Start … End`.

---

## 6. Package tự động khi build

| Thành phần | Unity 6 |
|------------|---------|
| `ABIAdsAndroidPostGenerateGradleProject` | AdMob / MAX meta-data; bật `UnityPlayerGameActivity` khi Application Entry = Game Activity |
| `ABIAdsLauncherMultidexGradlePostProcessor` | MultiDex + `abi-multidex-keep.pro`; **lifecycle 2.6.2** trên launcher + unityLibrary; **không** hạ Java 17 → 11 |
| `ABIAdsMediationGradleConfigurator` | Maven repo mediation trong `settingsTemplate.gradle` |

Console sau build Android:

```text
[ABI Ads] Patched launcher/build.gradle (MultiDex keep + Unity 6 lifecycle alignment).
[ABI Ads] Patched unityLibrary/build.gradle (lifecycle alignment).
```

---

## 7. Thứ tự setup (checklist)

1. Cài `com.abi.ads.unity` (UPM / `file:`).
2. Bật Custom Main / Gradle Properties / **Settings** template.
3. Tạo `mainTemplate.gradle` từ template Unity 6 + block mục 3.1.
4. Merge `gradleTemplate.properties` (mục 4).
5. Firebase: merge repo mục 5 (nếu dùng Firebase Unity).
6. **ABI Ads → Configs** — AdMob App ID, MAX key, mediation → **Apply**.
7. **EDM4U → Force Resolve**.
8. Kiểm tra block GMA classic **vẫn trên** `// Android Resolver Dependencies Start`.
9. Xóa `Library/Bee/Android` → build; gỡ app cũ trước khi cài bản mới.

---

## 8. Lỗi thường gặp (Unity 6)

### 8.1 `NoSuchFieldError: ProcessLifecycleOwner$Companion`

**Nguyên nhân:** `androidx.lifecycle` lệch phiên bản (AAR ads kéo `lifecycle-runtime:2.0.0`).

**Sửa:** Block `eachDependency` lifecycle **2.6.2** mục 3.1; rebuild. Package tự inject nếu thiếu — kiểm tra log `[ABI Ads] Patched unityLibrary`.

### 8.2 `keepUnitySymbols.gradle` does not exist

**Nguyên nhân:** Template Unity 2022.3 trên Unity 6 (thiếu `shared/`) hoặc ngược lại.

**Sửa:** Dùng đúng template theo phiên bản Unity — mục 3.2.

### 8.3 Duplicate `com.google.android.gms.ads.*`

Bỏ block exclude `play-services-ads*` ở mục 3.1. Xem [android-build-unity-2022-jdk11.md §7.2](android-build-unity-2022-jdk11.md#72-duplicate-class-comgoogleandroidgmsads).

### 8.4 `Could not find firebase-*-unity`

Bật Settings template + repo Firebase mục 5. Comment XML trong `*Dependencies.xml` **không** chứa `--`.

### 8.5 `ClassNotFoundException: UnityPlayerGameActivity` (trên Unity 2022.3)

Manifest Game Activity trên project 2022.3 — dùng manifest mẫu package (cả hai activity). Xem [android-build-notes.md](android-build-notes.md).

### 8.6 `NoClassDefFoundError: MobileAds$Builder`

Thiếu GMA classic — thêm `play-services-ads:25.3.0` mục 3.1.

---

## 9. Manifest & Application

```xml
<application
    android:name="com.abi.ads.modules.unity.ABIUnityAdsApplication"
    ...>
    <!-- Unity 6 Game Activity: enabled khi Player Settings → Application Entry = Game Activity -->
    <activity android:name="com.unity3d.player.UnityPlayerGameActivity" ... />
    <activity android:name="com.unity3d.player.UnityPlayerActivity" ... />
</application>
```

Mẫu: `Plugins/Android/AndroidManifest.template`. Post-processor bật đúng launcher theo **Application Entry**.

---

## 10. Checklist nhanh

- [ ] Unity **6.x**, JDK **17** (Unity bundled)
- [ ] Custom Main + Properties + **Settings** template
- [ ] `mainTemplate`: `keepUnitySymbols.gradle`, Java **17**, block GMA classic + lifecycle **2.6.2**
- [ ] Firebase maven trong `settingsTemplate` (nếu dùng Firebase Unity)
- [ ] ABI Ads Configs → Apply → Force Resolve
- [ ] Log `[ABI Ads] Patched launcher` + `unityLibrary` lifecycle
- [ ] Xóa `Library/Bee/Android`, build lại

---

## 11. Tài liệu liên quan

- [android-build-unity-2022-jdk11.md](android-build-unity-2022-jdk11.md) — Unity 2022.3 + JDK 11
- [android-build-notes.md](android-build-notes.md) — so sánh phiên bản, runtime native/banner
- [README.md](../README.md) — tích hợp package tổng quan
