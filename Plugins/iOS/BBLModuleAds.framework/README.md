# 🎨 SwiftUI Support - BBL Module Ads

Hỗ trợ đầy đủ SwiftUI với declarative API, reactive state management, và async/await.

## ✨ Tính năng

| Feature | Status |
|---------|--------|
| Native Ads | ✅ |
| Interstitial Ads | ✅ |
| Rewarded Ads | ✅ |
| Banner Ads | ✅ |
| @Published State | ✅ |
| Async/Await | ✅ |
| ViewModifiers | ✅ |
| Auto ViewController | ✅ |
| **BaseApp Protocol** | ⭐ **NEW** |
| **Auto-trigger Ads** | ⭐ **NEW** |

---

## 🚀 Quick Start

### 1. Native Ad (1 dòng!)

```swift
import BBL_Module_Ads
import SwiftUI

struct ContentView: View {
    var body: some View {
        VStack {
            Text("My App")
            
            // Native Ad - chỉ 1 dòng!
            EnhancedNativeAdView(adName: "home_native")
                .frame(height: 350)
        }
    }
}
```

### 2. Interstitial với ViewModifier

```swift
struct GameView: View {
    @State private var showAd = false
    
    var body: some View {
        Button("Complete Level") {
            showAd = true
        }
        .interstitialAd("level_complete", trigger: $showAd)
    }
}
```

### 3. Rewarded Ad

```swift
struct StoreView: View {
    @State private var showRewarded = false
    @State private var coins = 0
    
    var body: some View {
        Button("Watch Ad for Coins") {
            showRewarded = true
        }
        .rewardedAd(
            "rewarded_coins",
            trigger: $showRewarded,
            onReward: { _, _ in coins += 100 }
        )
    }
}
```

### 4. Banner Ad

```swift
struct HomeView: View {
    var body: some View {
        VStack {
            Text("Home Screen")
            Spacer()
        }
        .bannerAd("home_banner")
    }
}
```

---

## ⭐ BaseApp - Auto-trigger Ads

**Tự động load/show ads dựa trên lifecycle - KHÔNG CẦN CODE THỦ CÔNG!**

### Setup App

```swift
import SwiftUI
import BBL_Module_Ads

@main
@available(iOS 14.0, *)
struct MyGameApp: App, BaseApp {
    
    // Config global cho ads
    var adsGlobalConfig: GlobalConfig {
        let config = GlobalConfig()
        config.mediationProvider = .admob
        config.enableFirebase = true
        config.variantDev = true  // Debug mode
        return config
    }
    
    // Config placements với auto-trigger
    var adsLocalPlacements: [String: Any] {
        return [
            "placements": [
                // Auto load khi SplashView appear
                [
                    "ad_name": "splash_inter",
                    "ads_type": "interstitial",
                    "ad_ids": [
                        ["ad_id": "ca-app-pub-3940256099942544/4411468910", "ads_weight": 3]
                    ],
                    "is_show": true,
                    "activity_trigger_load": "SplashView",  // 🎯 Auto load
                    "delay_time_trigger_load": 0
                ],
                // Auto show khi HomeView appear
                [
                    "ad_name": "home_inter",
                    "ads_type": "interstitial",
                    "ad_ids": [
                        ["ad_id": "ca-app-pub-3940256099942544/4411468910", "ads_weight": 3]
                    ],
                    "is_show": true,
                    "activity_trigger_show": "HomeView",  // 🎯 Auto show
                    "delay_time_trigger_show": 1000
                ]
            ]
        ]
    }
    
    var body: some Scene {
        WindowGroup {
            configureRootView(SplashView())
        }
    }
}
```

### Tạo Views với BaseActivityView

```swift
struct SplashView: View, BaseActivityView {
    var activityName: String { "SplashView" }
    
    @State private var navigateToHome = false
    
    var body: some View {
        applyActivityLifecycle(
            ZStack {
                Text("Loading...")
                    .onAppear {
                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                            navigateToHome = true
                        }
                    }
            }
            .fullScreenCover(isPresented: $navigateToHome) {
                HomeView()
            }
        )
    }
}

struct HomeView: View, BaseActivityView {
    var activityName: String { "HomeView" }
    
    var body: some View {
        applyActivityLifecycle(
            VStack {
                Text("Home Screen")
                
                // Native ad tự động load
                EnhancedNativeAdView(adName: "home_native")
                    .frame(height: 350)
            }
        )
    }
}
```

**Kết quả:**
- Khi `SplashView` appear → Tự động load `splash_inter`
- Khi `HomeView` appear → Tự động show `home_inter` sau 1s

### Alternative: Không dùng BaseActivityView

```swift
struct GameView: View {
    var body: some View {
        VStack {
            Text("Game")
        }
        .activityLifecycle("GameView")  // Tự động trigger từ config
        .bannerAd("game_banner")
    }
}
```

---

## 🎯 PlacementConfig Cheat Sheet

### Auto Load

```json
{
  "ad_name": "my_ad",
  "activity_trigger_load": "MyView",
  "delay_time_trigger_load": 0
}
```

Khi `MyView` appear → Auto load `my_ad`

### Auto Show

```json
{
  "ad_name": "my_ad",
  "activity_trigger_show": "MyView",
  "delay_time_trigger_show": 1000
}
```

Khi `MyView` appear → Auto show `my_ad` sau 1s

---

## 💡 Advanced Usage

### 1. ViewModel Pattern

```swift
struct AdvancedView: View {
    @StateObject private var adViewModel = AdViewModel()
    
    var body: some View {
        VStack {
            if adViewModel.isLoading {
                ProgressView("Loading ad...")
            } else if let error = adViewModel.errorMessage {
                Text("Error: \(error)")
                    .foregroundColor(.red)
            }
            
            Button("Show Ad") {
                adViewModel.showAd("interstitial")
            }
            .disabled(!adViewModel.isLoaded)
        }
        .onAppear {
            adViewModel.loadAd("interstitial")
        }
    }
}
```

### 2. Async/Await

```swift
struct AsyncAdView: View {
    @State private var isLoading = false
    
    var body: some View {
        Button("Show Ad") {
            Task {
                isLoading = true
                defer { isLoading = false }
                
                do {
                    guard let vc = ViewControllerHolder.getTopViewController() else {
                        return
                    }
                    let result = try await AsyncAdManager.shared.showAd(
                        "interstitial",
                        from: vc
                    )
                    print("Revenue: \(result.revenue ?? 0)")
                } catch {
                    print("Error: \(error)")
                }
            }
        }
    }
}
```

### 3. Custom Native Ad

```swift
EnhancedNativeAdView(
    adName: "home_native",
    size: .medium,
    onAdLoaded: { adName in
        print("✅ Loaded: \(adName)")
    },
    onAdFailed: { adName, error in
        print("❌ Failed: \(error)")
    },
    onAdImpression: { adName in
        print("👁 Impression tracked")
    }
)
.frame(height: 350)
```

### 4. Combine Multiple Ads

```swift
struct GameFlowView: View {
    @State private var showInterstitial = false
    @State private var showRewarded = false
    @State private var hints = 3
    
    var body: some View {
        VStack {
            Text("Hints: \(hints)")
            
            Button("Use Hint") {
                if hints > 0 {
                    hints -= 1
                } else {
                    showRewarded = true
                }
            }
            
            Button("Complete Level") {
                showInterstitial = true
            }
        }
        .activityLifecycle("GameView")  // Auto-trigger từ config
        .interstitialAd("level_complete", trigger: $showInterstitial)
        .rewardedAd("hint_reward", trigger: $showRewarded, onReward: { _, _ in
            hints += 3
        })
        .bannerAd("game_banner")
    }
}
```

---

## 🔧 API Reference

### EnhancedNativeAdView

```swift
EnhancedNativeAdView(
    adName: String,
    size: BBLNativeAdView.NativeAdSize = .medium,
    onAdLoaded: ((String) -> Void)? = nil,
    onAdFailed: ((String, String) -> Void)? = nil,
    onAdImpression: ((String) -> Void)? = nil,
    onAdClicked: ((String) -> Void)? = nil
)
```

### AdViewModel

```swift
@Published var isLoading: Bool
@Published var isLoaded: Bool
@Published var errorMessage: String?
@Published var revenue: Double?

func loadAd(_ adName: String)
func showAd(_ adName: String, from viewController: UIViewController?)
```

### AsyncAdManager

```swift
func loadAd(_ adName: String) async throws
func showAd(_ adName: String, from: UIViewController?) async throws -> AdResult
func showRewardedAd(_ adName: String, from: UIViewController?) async throws -> RewardedAdResult
```

### ViewModifiers

```swift
.activityLifecycle(_ activityName: String) // Auto-trigger
.interstitialAd(_ adName: String, trigger: Binding<Bool>)
.rewardedAd(_ adName: String, trigger: Binding<Bool>)
.bannerAd(_ adName: String, isVisible: Binding<Bool> = .constant(true))
```

---

## 📋 Components

### Core Files (Giữ lại)
- ✅ `AdModifiers.swift` - ViewModifiers cho ads
- ✅ `AdViewModel.swift` - Reactive state management
- ✅ `AsyncAdManager.swift` - Async/await wrapper
- ✅ `BaseApp.swift` - BaseApp protocol
- ✅ `BaseActivityView.swift` - Auto-trigger lifecycle
- ✅ `EnhancedNativeAdView.swift` - SwiftUI native ad view
- ✅ `ViewControllerHolder.swift` - ViewController helper

---

## ✅ Best Practices

1. **Preload ads** khi app launch
2. **Use BaseApp** cho auto-trigger thay vì manual code
3. **Handle errors** với alerts hoặc fallback UI
4. **Debug mode**: Set `config.variantDev = true` để xem logs
5. **Delay hợp lý**: Cho user thời gian xem UI trước khi show ad

---

## 🐛 Troubleshooting

### Ads không auto-trigger

**Check:**
1. `activityName` có match với PlacementConfig?
2. `is_show: true` trong PlacementConfig?
3. Có gọi `applyActivityLifecycle()` hoặc `.activityLifecycle()`?
4. Enable debug: `config.variantDev = true`

### Ad không load/show

Check logs khi `variantDev = true`:

```
LifecycleManager: Checking auto-load for view controller names: ["HomeView"]
LifecycleManager: Auto-loading ad home_inter with delay 0.0s
```

Nếu không thấy → `activityName` không match PlacementConfig.

---

## 📚 Native Ads

Xem hướng dẫn chi tiết về Native Ads (Small, Medium, FullScreen) tại:
**[NATIVE_ADS_USAGE.md](../NATIVE_ADS_USAGE.md)**

---

## 📝 Changelog

### v2.1.0 (2025-12-24) ⭐ NEW
- ✨ Added BaseApp protocol
- ✨ Added BaseActivityView protocol
- ✨ Auto load/show ads dựa trên lifecycle
- 🎯 Không cần code thủ công!

### v2.0.0 (2025-12-22)
- ✨ SwiftUI support
- ✨ ViewModel pattern
- ✨ Async/await API
- ✨ ViewModifiers

---

**Made with ❤️ for SwiftUI developers**
