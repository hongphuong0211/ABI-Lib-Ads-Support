# Build Android — Unity 2022.3 + JDK 11

Hướng dẫn **đầy đủ** cho **Unity 2022.3.x** + **JDK 11**. Troubleshooting chung / Unity 6: [android-build-notes.md](android-build-notes.md).

**Mẫu copy-paste:**

| File | Mẫu trong package |
|------|-------------------|
| Block host `mainTemplate.gradle` | [mainTemplate.host-unity2022-jdk11.gradle.snippet](mainTemplate.host-unity2022-jdk11.gradle.snippet) |
| `gradleTemplate.properties` | [gradleTemplate.host-unity2022-jdk11.properties.snippet](gradleTemplate.host-unity2022-jdk11.properties.snippet) |

---

## 1. Yêu cầu môi trường

| Thành phần | Giá trị |
|------------|---------|
| Unity | **2022.3 LTS** (ví dụ 2022.3.62f2) |
| JDK | **11** — bật *JDK installed with Unity (recommended)* |
| Đường dẫn JDK mặc định | `{UnityEditor}/Data/PlaybackEngines/AndroidPlayer/OpenJDK` (Temurin 11.0.14.x) |
| AGP / Gradle | Do Unity cung cấp (AGP **7.4.2**) — **không** nâng AGP 8+ nếu chưa đổi Gradle tùy chỉnh |
| `compileOptions` | `JavaVersion.VERSION_11` |
| Activity launcher | `com.unity3d.player.UnityPlayerActivity` |

**Không** dùng template Gradle Unity 6 (`apply from: '../shared/keepUnitySymbols.gradle'`, `compileSdk` không có `Version`, `**MINSDK**` thay `**MINSDKVERSION**`).

---

## 2. Player Settings → Publishing Settings

Bật các mục sau:

- [x] **Custom Main Gradle Template**
- [x] **Custom Gradle Properties Template**
- [x] **Custom Gradle Settings Template** (khuyến nghị khi dùng AdMob mediation Mintegral / Pangle / …)
- [ ] Custom Launcher Gradle Template — Unity 2022.3 **không** có flag riêng; `launcher/build.gradle` do Unity generate, package patch qua `ABIAdsLauncherMultidexGradlePostProcessor`

### JDK 11 trong Unity

**Edit → Preferences → External Tools**

1. Bật **JDK installed with Unity (recommended)**
2. **Không** trỏ Gradle/Android build sang JDK 17 trên máy nếu muốn giữ JDK 11 thống nhất với `compileOptions` và R8/D8 của Unity 2022.3

---

## 3. `mainTemplate.gradle` (host project)

Xuất phát từ template gốc Unity 2022.3:

`{UnityEditor}/Data/PlaybackEngines/AndroidPlayer/Tools/GradleTemplates/mainTemplate.gradle`

### 3.1 Block host (phía **trên** `// Android Resolver Dependencies Start`)

EDM4U **không** tự thêm block này — copy từ [mainTemplate.host-unity2022-jdk11.gradle.snippet](mainTemplate.host-unity2022-jdk11.gradle.snippet):

```gradle
// ABI Ads — Unity 2022.3 + JDK 11 (docs/android-build-unity-2022-jdk11.md)
configurations.configureEach {
    resolutionStrategy {
        force 'com.google.errorprone:error_prone_annotations:2.20.0'
        force 'androidx.webkit:webkit:1.11.0'
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
| `force error_prone_annotations:2.20.0` | Tránh D8 NPE trên bản 2.36+ với R8 cũ (Unity 2022.3) |
| `force webkit:1.11.0` | Tránh D8 NPE trên `webkit:1.15+` |
| `lifecycle → 2.6.2` | Tránh `NoSuchFieldError: ProcessLifecycleOwner$Companion` (banner refresh GMA) |

**Không** thêm `com.google.android.gms:play-services-ads:24.x` song song `play-services-ads`.

### 3.2 Khung `android { }` — Unity 2022.3

```gradle
android {
    namespace "com.unity3d.player"
    ndkPath "**NDKPATH**"
    compileSdkVersion **APIVERSION**
    buildToolsVersion '**BUILDTOOLS**'

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_11
        targetCompatibility JavaVersion.VERSION_11
    }

    defaultConfig {
        minSdkVersion **MINSDKVERSION**
        targetSdkVersion **TARGETSDKVERSION**
        ...
    }

    lintOptions {
        abortOnError false
    }

    aaptOptions {
        noCompress = **BUILTIN_NOCOMPRESS** + unityStreamingAssets.tokenize(', ')
        ...
    }**PACKAGING_OPTIONS**
}
```

**Không dùng** (Unity 6): `apply from: '../shared/…'`, `compileSdk`, `minSdk`, `lint { }`, `androidResources`, `JavaVersion.VERSION_17`.

### 3.3 Layout/UI + mediation

- Layout/UI (`appcompat`, `sdp`, `lottie`, …): EDM resolve từ `Editor/ABIAdsDependencies.xml` (package).
- Mediation adapters: **ABI Ads → Configs** → tick network → **Apply** → **Force Resolve**.

---

## 4. `gradleTemplate.properties`

Copy / merge từ [gradleTemplate.host-unity2022-jdk11.properties.snippet](gradleTemplate.host-unity2022-jdk11.properties.snippet):

```properties
org.gradle.jvmargs=-Xmx**JVM_HEAP_SIZE**M -XX:MaxMetaspaceSize=256m -Dfile.encoding=UTF-8 -Xss4m
org.gradle.parallel=false
org.gradle.workers.max=2
android.useAndroidX=true
android.enableJetifier=true
android.lint.checkReleaseBuilds=false
```

| Thuộc tính | Lý do (JDK 11) |
|------------|----------------|
| `-Xss4m` | Giảm `StackOverflowError` khi resolve graph lớn |
| `android.lint.checkReleaseBuilds=false` | Lint jar một số AndroidX (class 61) cần JVM 17 — tắt lint release khi build JDK 11 |

Giữ dòng `**ADDITIONAL_PROPERTIES**` và `unityStreamingAssets=**STREAMING_ASSETS**` từ template Unity.

---

## 5. Package tự động patch `launcher/build.gradle`

`ABIAdsLauncherMultidexGradlePostProcessor` (trong `com.abi.ads.unity`):

- Copy `abi-multidex-keep.pro` → `launcher/`
- `multiDexEnabled true` + `multiDexKeepProguard`
- Pin D8-friendly deps trên **launcher** (cùng `error_prone` / `webkit` như `unityLibrary`)
- Đảm bảo `JavaVersion.VERSION_11` (đổi `VERSION_17` → `VERSION_11` nếu template lỡ dùng 17)

**Sau build**, Console cần có:

```text
[ABI Ads] Patched launcher/build.gradle for MultiDex ...
```

---

## 6. Thứ tự setup (checklist)

1. Cài `com.abi.ads.unity` (UPM / `file:` local path).
2. Bật Custom Main / Gradle Properties / Settings template (mục 2).
3. Tạo `Assets/Plugins/Android/mainTemplate.gradle` từ template Unity 2022.3 + block mục 3.1.
4. Merge `gradleTemplate.properties` (mục 4).
5. **ABI Ads → Configs** — AdMob App ID, MAX key (nếu có), mediation → **Apply**.
6. **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
7. Kiểm tra `mainTemplate.gradle`: block GMA classic **vẫn nằm trên** `// Android Resolver Dependencies Start` (EDM có thể chèn dependency bên dưới — **không** xóa block host).
8. Xóa `Library/Bee/Android` (và cache transform nếu D8 vẫn lỗi):

   ```powershell
   Remove-Item "{Project}/Library/Bee/Android" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item "$env:USERPROFILE\.gradle\caches\transforms-3" -Recurse -Force -ErrorAction SilentlyContinue
   ```

9. Build Android; gỡ app cũ trước khi cài bản mới.

---

## 7. Lỗi thường gặp (Unity 2022.3 + JDK 11)

### 7.1 `keepUnitySymbols.gradle` does not exist

**Nguyên nhân:** `mainTemplate.gradle` copy từ Unity 6.

**Sửa:** Xóa `apply from: '../shared/…'`; dùng DSL mục 3.2. Chi tiết: [android-build-notes.md §11](android-build-notes.md#11-gradle-could-not-read-script-sharedkeepunitysymbolsgradle-unity-20223).

### 7.2 Duplicate class `com.google.android.gms.ads.*`

**Nguyên nhân:** `play-services-ads` + `play-services-ads-api` (từ MAX/Google mediation).

**Sửa:** Bỏ block exclude `play-services-ads*` ở mục 3.1. Đảm bảo chỉ còn một phiên bản `play-services-ads` (khuyến nghị `25.3.0`) sau Force Resolve.

### 7.3 D8 `NullPointerException` — `webkit-1.15.0`, `error_prone_annotations-2.41.0`

**Nguyên nhân:** Dependency mới + R8/D8 AGP 7.4 (Unity 2022.3), Gradle JVM 11.

**Sửa:**

1. Giữ `resolutionStrategy` force mục 3.1 (host + launcher patch).
2. JDK 11 qua Unity (mục 2).
3. Xóa cache `transforms-3` (mục 6).
4. Nếu vẫn lỗi, thêm exclude (chỉ khi cần):

   ```gradle
   exclude group: 'com.google.errorprone', module: 'error_prone_annotations'
   ```

   trong `configurations.configureEach` (cùng block exclude ads).

### 7.4 Lint: `UnsupportedClassVersionError` … class file version 61.0

**Nguyên nhân:** Custom lint check build bằng Java 17, Gradle chạy JVM 11.

**Sửa:** `android.lint.checkReleaseBuilds=false` trong `gradleTemplate.properties` (mục 4). Cảnh báo `UnityPlayerActivity uses deprecated API` — bỏ qua.

### 7.5 `ClassNotFoundException: ABIUnityAdsApplication`

MultiDex + primary dex — xem [android-build-notes.md §7](android-build-notes.md#7-runtime-classnotfoundexception-abiunityadsapplication-adsmultidexapplication).

### 7.6 `ClassNotFoundException: UnityPlayerGameActivity`

Manifest Unity 6 trên Unity 2022.3 — xem [android-build-notes.md](android-build-notes.md).

### 7.7 `NoSuchFieldError: ProcessLifecycleOwner$Companion`

Xung đột `androidx.lifecycle` — thêm `eachDependency` lifecycle **2.6.2** trong block host (xem [mainTemplate.host-unity2022-jdk11.gradle.snippet](mainTemplate.host-unity2022-jdk11.gradle.snippet)). Unity 6: [android-build-unity-6.md §8.1](android-build-unity-6.md#81-nosuchfielderror-processlifecycleownercompanion).

### 7.8 `NoClassDefFoundError: MobileAds$Builder`

Thiếu GMA classic — thêm `play-services-ads:25.3.0` mục 3.1.

---

## 8. Firebase + EDM (Unity 2022.3)

- Firebase Unity resolve `firebase-*-unity` qua `Assets/Firebase/m2repository` (và có thể `Assets/GeneratedLocalRepo/Firebase/m2repository`).
- `ABIAdsDependencies.xml` **không** khai báo Firebase — tránh clash version với Firebase Unity EDM.
- Lỗi EDM parse XML / không tìm thấy `firebase-*-unity`: [android-build-notes.md §10](android-build-notes.md#10-unable-to-read-android-dependencies-edm4u--could-not-find-firebase--unity).

---

## 9. Manifest & Application

```xml
<application
    android:name="com.abi.ads.modules.unity.ABIUnityAdsApplication"
    ...>
    <activity android:name="com.unity3d.player.UnityPlayerActivity" ... />
    <!-- Unity 2022.3: UnityPlayerGameActivity disabled hoặc có nhưng không làm launcher -->
</application>
```

Mẫu đầy đủ: `Plugins/Android/AndroidManifest.template` trong package.

CI: `ABI_ANDROID_GOOGLE_AD_APP_ID`, `ABI_ANDROID_MAX_SDK_KEY`.

---

## 10. Checklist nhanh

- [ ] Unity **2022.3**, JDK **11** (Unity bundled)
- [ ] Custom Main + Gradle Properties + Settings template
- [ ] `mainTemplate.gradle`: **không** `shared/keepUnitySymbols.gradle`
- [ ] Block host: GMA classic  + force `error_prone` / `webkit`
- [ ] `compileSdkVersion`, `**MINSDKVERSION**`, `JavaVersion.VERSION_11`
- [ ] `gradleTemplate.properties`: Jetifier, `lint.checkReleaseBuilds=false`, `-Xss4m`
- [ ] ABI Ads Configs → Apply → Force Resolve
- [ ] Log `[ABI Ads] Patched launcher/build.gradle`
- [ ] Xóa `Library/Bee/Android`, build lại

---

## 11. Tài liệu liên quan

- [android-build-unity-6.md](android-build-unity-6.md) — Unity 6 + JDK 17
- [android-build-notes.md](android-build-notes.md) — so sánh phiên bản, runtime native/banner, mediation Maven
- [Editor/ABIAdsGradleDependencies.template](../Editor/ABIAdsGradleDependencies.template) — gợi ý khi **không** có Google Mobile Ads Unity Plugin
- [README.md](../README.md) — tích hợp package tổng quan
