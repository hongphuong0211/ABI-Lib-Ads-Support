# Hướng dẫn sử dụng Native Ads (Small, Medium, FullScreen)

## Tổng quan

Module hỗ trợ 3 loại native ad:
- **Small**: Layout compact, horizontal (Icon + Headline + Body + CTA)
- **Medium**: Layout đầy đủ với media view (Icon + Headline + Body + Media + Advertiser + CTA)
- **FullScreen**: Layout fullscreen với media view fullscreen và container ở bottom

## 1. Sử dụng trong UIKit

### 1.1. Small Native Ad

```swift
import UIKit
import BBLModuleAds

class ViewController: UIViewController {
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Tạo small native ad view
        let nativeAdView = NativeAdView(frame: .zero, size: .small)
        nativeAdView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(nativeAdView)
        
        // Set constraints
        NSLayoutConstraint.activate([
            nativeAdView.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 16),
            nativeAdView.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -16),
            nativeAdView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 20),
            nativeAdView.heightAnchor.constraint(equalToConstant: 100) // Small ad height
        ])
        
        // Load và render ad
        AdsManager.shared.loadAndRenderNativeAd(
            adName: "native_ad_small",
            nativeAdView: nativeAdView,
            size: .small
        ) { [weak self] adName in
            print("Small native ad loaded: \(adName)")
        } onAdFailed: { adName, error in
            print("Small native ad failed: \(adName), error: \(error)")
        }
    }
}
```

### 1.2. Medium Native Ad

```swift
import UIKit
import BBLModuleAds

class ViewController: UIViewController {
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Tạo medium native ad view
        let nativeAdView = NativeAdView(frame: .zero, size: .medium)
        nativeAdView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(nativeAdView)
        
        // Set constraints
        NSLayoutConstraint.activate([
            nativeAdView.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 16),
            nativeAdView.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -16),
            nativeAdView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 20),
            nativeAdView.heightAnchor.constraint(greaterThanOrEqualToConstant: 400) // Medium ad height
        ])
        
        // Load và render ad
        AdsManager.shared.loadAndRenderNativeAd(
            adName: "native_ad_medium",
            nativeAdView: nativeAdView,
            size: .medium
        ) { [weak self] adName in
            print("Medium native ad loaded: \(adName)")
        } onAdFailed: { adName, error in
            print("Medium native ad failed: \(adName), error: \(error)")
        }
    }
}
```

### 1.3. FullScreen Native Ad

```swift
import UIKit
import BBLModuleAds

class ViewController: UIViewController {
    
    var fullScreenNativeAdView: NativeAdView?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Tạo fullscreen native ad view
        let nativeAdView = NativeAdView(frame: view.bounds, size: .fullScreen)
        nativeAdView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        view.addSubview(nativeAdView)
        fullScreenNativeAdView = nativeAdView
        
        // Load và render ad
        AdsManager.shared.loadAndRenderNativeAd(
            adName: "native_ad_fullscreen",
            nativeAdView: nativeAdView,
            size: .fullScreen
        ) { [weak self] adName in
            print("FullScreen native ad loaded: \(adName)")
        } onAdFailed: { adName, error in
            print("FullScreen native ad failed: \(adName), error: \(error)")
        }
    }
    
    // Hoặc hiển thị fullscreen ad trong modal
    func showFullScreenAd() {
        let adViewController = UIViewController()
        let nativeAdView = NativeAdView(frame: adViewController.view.bounds, size: .fullScreen)
        nativeAdView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        adViewController.view.addSubview(nativeAdView)
        
        // Add close button
        let closeButton = UIButton(type: .system)
        closeButton.setTitle("✕", for: .normal)
        closeButton.titleLabel?.font = .systemFont(ofSize: 24)
        closeButton.translatesAutoresizingMaskIntoConstraints = false
        closeButton.addTarget(self, action: #selector(closeAd), for: .touchUpInside)
        adViewController.view.addSubview(closeButton)
        
        NSLayoutConstraint.activate([
            closeButton.topAnchor.constraint(equalTo: adViewController.view.safeAreaLayoutGuide.topAnchor, constant: 16),
            closeButton.trailingAnchor.constraint(equalTo: adViewController.view.trailingAnchor, constant: -16),
            closeButton.widthAnchor.constraint(equalToConstant: 44),
            closeButton.heightAnchor.constraint(equalToConstant: 44)
        ])
        
        // Load ad
        AdsManager.shared.loadAndRenderNativeAd(
            adName: "native_ad_fullscreen",
            nativeAdView: nativeAdView,
            size: .fullScreen
        )
        
        // Present modal
        adViewController.modalPresentationStyle = .fullScreen
        present(adViewController, animated: true)
    }
    
    @objc func closeAd() {
        dismiss(animated: true)
    }
}
```

### 1.4. Load từ XIB

```swift
// Load medium ad từ XIB
if let nativeAdView = NativeAdView.loadFromXIB(size: .medium) {
    nativeAdView.translatesAutoresizingMaskIntoConstraints = false
    view.addSubview(nativeAdView)
    
    NSLayoutConstraint.activate([
        nativeAdView.centerXAnchor.constraint(equalTo: view.centerXAnchor),
        nativeAdView.centerYAnchor.constraint(equalTo: view.centerYAnchor),
        nativeAdView.widthAnchor.constraint(equalToConstant: 350),
        nativeAdView.heightAnchor.constraint(greaterThanOrEqualToConstant: 400)
    ])
    
    AdsManager.shared.loadAndRenderNativeAd(
        adName: "native_ad_medium",
        nativeAdView: nativeAdView,
        size: .medium
    )
}

// Load fullscreen ad từ XIB
if let nativeAdView = NativeAdView.loadFromXIB(size: .fullScreen) {
    nativeAdView.frame = view.bounds
    nativeAdView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
    view.addSubview(nativeAdView)
    
    AdsManager.shared.loadAndRenderNativeAd(
        adName: "native_ad_fullscreen",
        nativeAdView: nativeAdView,
        size: .fullScreen
    )
}
```

## 2. Sử dụng trong SwiftUI

**📚 Hướng dẫn đầy đủ:** [SwiftUI README](./SwiftUI/README.md)

Module hỗ trợ đầy đủ SwiftUI với `EnhancedNativeAdView` - SwiftUI-native component với loading state, error handling, và auto cleanup.

### Quick Example

```swift
import SwiftUI
import BBL_Module_Ads

struct ContentView: View {
    var body: some View {
        VStack {
            Text("My App Content")
            
            // Native ad - chỉ 1 dòng!
            EnhancedNativeAdView(adName: "home_native")
                .frame(height: 350)
        }
    }
}
```

### Với Callbacks

```swift
EnhancedNativeAdView(
    adName: "home_native",
    size: .medium,
    onAdLoaded: { adName in
        print("✅ Ad loaded: \(adName)")
    },
    onAdFailed: { adName, error in
        print("❌ Ad failed: \(error)")
    }
)
.frame(height: 350)
```

### Các kích thước hỗ trợ

```swift
// Small (100-120px height)
EnhancedNativeAdView(adName: "ad", size: .small)
    .frame(height: 100)

// Medium (350-400px height) - Default
EnhancedNativeAdView(adName: "ad", size: .medium)
    .frame(height: 350)

// FullScreen
EnhancedNativeAdView(adName: "ad", size: .fullScreen)
    .edgesIgnoringSafeArea(.all)
```

**Xem thêm:**
- [SwiftUI Complete Guide](./SwiftUI/README.md)
- [BaseApp & Auto-trigger Ads](./SwiftUI/README.md#-baseapp---auto-trigger-ads)
- [ViewModifiers & Async/Await](./SwiftUI/README.md#-advanced-usage)

## 3. Sử dụng trong UITableView/UICollectionView

### 3.1. Small Native Ad trong TableView

```swift
import UIKit
import BBLModuleAds

class TableViewController: UITableViewController {
    
    let adRowIndex = 3 // Hiển thị ad ở row 3
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        if indexPath.row == adRowIndex {
            // Reuse cell cho native ad
            let cell = tableView.dequeueReusableCell(withIdentifier: "NativeAdCell", for: indexPath)
            
            // Remove old ad view nếu có
            cell.contentView.subviews.forEach { $0.removeFromSuperview() }
            
            // Tạo small native ad view
            let nativeAdView = NativeAdView(frame: cell.contentView.bounds, size: .small)
            nativeAdView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
            cell.contentView.addSubview(nativeAdView)
            
            // Load ad
            AdsManager.shared.loadAndRenderNativeAd(
                adName: "native_ad_small",
                nativeAdView: nativeAdView,
                size: .small
            )
            
            return cell
        }
        
        // Normal cell
        let cell = tableView.dequeueReusableCell(withIdentifier: "NormalCell", for: indexPath)
        cell.textLabel?.text = "Row \(indexPath.row)"
        return cell
    }
    
    override func tableView(_ tableView: UITableView, heightForRowAt indexPath: IndexPath) -> CGFloat {
        if indexPath.row == adRowIndex {
            return 100 // Height cho small ad
        }
        return 44
    }
}
```

### 3.2. Medium Native Ad trong CollectionView

```swift
import UIKit
import BBLModuleAds

class CollectionViewController: UICollectionViewController {
    
    let adItemIndex = 2 // Hiển thị ad ở item 2
    
    override func collectionView(_ collectionView: UICollectionView, cellForItemAt indexPath: IndexPath) -> UICollectionViewCell {
        if indexPath.item == adItemIndex {
            let cell = collectionView.dequeueReusableCell(withReuseIdentifier: "NativeAdCell", for: indexPath)
            
            // Remove old ad view
            cell.contentView.subviews.forEach { $0.removeFromSuperview() }
            
            // Tạo medium native ad view
            let nativeAdView = NativeAdView(frame: cell.contentView.bounds, size: .medium)
            nativeAdView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
            cell.contentView.addSubview(nativeAdView)
            
            // Load ad
            AdsManager.shared.loadAndRenderNativeAd(
                adName: "native_ad_medium",
                nativeAdView: nativeAdView,
                size: .medium
            )
            
            return cell
        }
        
        // Normal cell
        let cell = collectionView.dequeueReusableCell(withReuseIdentifier: "NormalCell", for: indexPath)
        return cell
    }
    
    func collectionView(_ collectionView: UICollectionView, layout collectionViewLayout: UICollectionViewLayout, sizeForItemAt indexPath: IndexPath) -> CGSize {
        if indexPath.item == adItemIndex {
            return CGSize(width: collectionView.bounds.width - 32, height: 400)
        }
        return CGSize(width: 100, height: 100)
    }
}
```

## 4. Render ad đã load sẵn

Nếu ad đã được load trước đó, bạn có thể render trực tiếp:

```swift
// Render small ad
let smallAdView = NativeAdView(frame: .zero, size: .small)
if AdsManager.shared.renderNativeAd(adName: "native_ad_small", nativeAdView: smallAdView) {
    print("Small ad rendered successfully")
} else {
    print("Small ad not ready, loading...")
    AdsManager.shared.loadAndRenderNativeAd(adName: "native_ad_small", nativeAdView: smallAdView, size: .small)
}

// Render medium ad
let mediumAdView = NativeAdView(frame: .zero, size: .medium)
if AdsManager.shared.renderNativeAd(adName: "native_ad_medium", nativeAdView: mediumAdView) {
    print("Medium ad rendered successfully")
}

// Render fullscreen ad
let fullScreenAdView = NativeAdView(frame: view.bounds, size: .fullScreen)
if AdsManager.shared.renderNativeAd(adName: "native_ad_fullscreen", nativeAdView: fullScreenAdView) {
    view.addSubview(fullScreenAdView)
}
```

## 5. Best Practices

### 5.1. Preload Ads

```swift
// Preload ads trước khi cần hiển thị
override func viewDidLoad() {
    super.viewDidLoad()
    
    // Preload các loại ad
    AdsManager.shared.load(adName: "native_ad_small")
    AdsManager.shared.load(adName: "native_ad_medium")
    AdsManager.shared.load(adName: "native_ad_fullscreen")
}
```

### 5.2. Reuse Ad Views

```swift
class AdContainerView: UIView {
    private var nativeAdView: NativeAdView?
    
    func setupAd(size: NativeAdSize, adName: String) {
        // Remove old ad view
        nativeAdView?.removeFromSuperview()
        nativeAdView?.unregisterAd()
        
        // Create new ad view
        let adView = NativeAdView(frame: bounds, size: size)
        adView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        addSubview(adView)
        nativeAdView = adView
        
        // Load ad
        AdsManager.shared.loadAndRenderNativeAd(
            adName: adName,
            nativeAdView: adView,
            size: size
        )
    }
    
    deinit {
        nativeAdView?.unregisterAd()
    }
}
```

### 5.3. Handle Ad Callbacks

```swift
class AdCallbackHandler: AdCallback {
    func onAdLoaded(adName: String) {
        print("✅ Ad loaded: \(adName)")
        // Update UI, track analytics, etc.
    }
    
    func onAdFailed(adName: String, error: String) {
        print("❌ Ad failed: \(adName), error: \(error)")
        // Show fallback content, retry, etc.
    }
}

// Sử dụng
let callback = AdCallbackHandler()
AdsManager.shared.loadAndRenderNativeAd(
    adName: "native_ad_medium",
    nativeAdView: nativeAdView,
    size: .medium,
    callback: callback
)
```

## 6. Configuration trong PlacementConfig

Đảm bảo config đúng trong `PlacementConfig`:

```json
{
  "ad_name": "native_ad_small",
  "ads_type": "native",
  "is_show": true,
  "ad_ids": [
    {
      "ad_id": "ca-app-pub-xxxxx/xxxxx",
      "mediation": "admob"
    }
  ],
  "native_ad_config": {
    "bgColor": "#FFFFFF",
    "borderColor": "#E0E0E0",
    "headlineTextColor": "#000000",
    "bodyTextColor": "#666666",
    "advertiserTextColor": "#999999"
  }
}
```

## 7. Kích thước khuyến nghị

- **Small**: Height ~100-120px, phù hợp cho list items
- **Medium**: Height ~400-500px, phù hợp cho feed content
- **FullScreen**: Chiếm toàn bộ màn hình, phù hợp cho interstitial ads

## 8. Lưu ý quan trọng

1. **User Interaction**: UIButton trong native ad đã được disable user interaction tự động (theo Google docs)
2. **Memory Management**: Luôn gọi `unregisterAd()` khi không còn sử dụng
3. **Thread Safety**: `loadAndRenderNativeAd` tự động chạy trên main thread
4. **XIB vs Programmatic**: Có thể dùng XIB hoặc tạo programmatically, cả hai đều được hỗ trợ

