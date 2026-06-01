import Foundation
import UIKit
#if canImport(CryptoKit)
import CryptoKit
#endif
#if canImport(CommonCrypto)
import CommonCrypto
#endif

#if canImport(BBLModuleAds)
import BBLModuleAds
#elseif canImport(BBL_Module_Ads)
import BBL_Module_Ads
#endif

@_silgen_name("UnitySendMessage")
private func UnitySendMessage(
    _ gameObjectName: UnsafePointer<CChar>,
    _ methodName: UnsafePointer<CChar>,
    _ message: UnsafePointer<CChar>
)

private final class ABIUnityAdsBridgeStore {
    static let shared = ABIUnityAdsBridgeStore()

    var callbackGameObject = "ABIAdsBridgeListener"
    var callbackMethod = "OnABIAdsEvent"
    var isReady = false
    var readyListenerRegistered = false
    var initializedEventEmitted = false
    var bannerContainer: UIView?
    var bannerPlacementName: String?
    var rewardedPlacements = Set<String>()
    var placementAdTypes: [String: String] = [:]
}

private func stringFromCString(_ pointer: UnsafePointer<CChar>?) -> String? {
    guard let pointer else {
        return nil
    }
    return String(cString: pointer)
}

private func decodeUnityConfigString(_ value: String?) -> String? {
    guard let value else {
        return nil
    }

    let trimmed = value.trimmingCharacters(in: .whitespacesAndNewlines)
    guard trimmed.split(separator: ":", omittingEmptySubsequences: false).count == 3 else {
        return value
    }

#if canImport(CryptoKit) && canImport(CommonCrypto)
    let passphrase = Bundle.main.bundleIdentifier ?? ""
    return decryptRemoteConfig(trimmed, passphrase: passphrase) ?? value
#else
    return value
#endif
}

#if canImport(CryptoKit) && canImport(CommonCrypto)
private func decryptRemoteConfig(_ encryptedString: String, passphrase: String) -> String? {
    let parts = encryptedString.split(separator: ":", omittingEmptySubsequences: false)
    guard parts.count == 3,
          let salt = base64UrlDecode(String(parts[0])),
          let iv = base64UrlDecode(String(parts[1])),
          let cipherWithTag = base64UrlDecode(String(parts[2])),
          cipherWithTag.count >= 16 else {
        return nil
    }

    guard let key = deriveRemoteConfigKey(passphrase: passphrase, salt: salt) else {
        return nil
    }

    do {
        let cipherText = Data(cipherWithTag.prefix(cipherWithTag.count - 16))
        let tag = Data(cipherWithTag.suffix(16))
        let nonce = try AES.GCM.Nonce(data: iv)
        let sealedBox = try AES.GCM.SealedBox(nonce: nonce, ciphertext: cipherText, tag: tag)
        let plainData = try AES.GCM.open(sealedBox, using: key)
        return String(data: plainData, encoding: .utf8)
    } catch {
        return nil
    }
}

private func deriveRemoteConfigKey(passphrase: String, salt: Data) -> SymmetricKey? {
    var keyBytes = [UInt8](repeating: 0, count: 32)
    let status = passphrase.withCString { passwordPointer in
        salt.withUnsafeBytes { saltBytes -> Int32 in
            guard let saltBaseAddress = saltBytes.bindMemory(to: UInt8.self).baseAddress else {
                return -1
            }

            return CCKeyDerivationPBKDF(
                CCPBKDFAlgorithm(kCCPBKDF2),
                passwordPointer,
                strlen(passwordPointer),
                saltBaseAddress,
                salt.count,
                CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256),
                100_000,
                &keyBytes,
                keyBytes.count
            )
        }
    }

    guard status == kCCSuccess else {
        return nil
    }

    return SymmetricKey(data: keyBytes)
}

private func base64UrlDecode(_ value: String) -> Data? {
    var base64 = value
        .replacingOccurrences(of: "-", with: "+")
        .replacingOccurrences(of: "_", with: "/")
    let padding = base64.count % 4
    if padding > 0 {
        base64 += String(repeating: "=", count: 4 - padding)
    }

    return Data(base64Encoded: base64)
}
#endif

private func decodeJSONObject(from jsonString: String?) -> [String: Any]? {
    guard let jsonString, !jsonString.isEmpty,
          let data = jsonString.data(using: .utf8),
          let object = try? JSONSerialization.jsonObject(with: data) else {
        return nil
    }

    if let dictionary = object as? [String: Any] {
        return dictionary
    }

    if let array = object as? [[String: Any]] {
        return ["placements": array]
    }

    return nil
}

private func parseBool(_ value: Any?) -> Bool? {
    switch value {
    case let boolValue as Bool:
        return boolValue
    case let intValue as Int:
        return intValue != 0
    case let number as NSNumber:
        return number.intValue != 0
    case let stringValue as String:
        switch stringValue.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() {
        case "1", "true", "yes":
            return true
        case "0", "false", "no":
            return false
        default:
            return nil
        }
    default:
        return nil
    }
}

private func parseInt(_ value: Any?) -> Int? {
    switch value {
    case let intValue as Int:
        return intValue
    case let number as NSNumber:
        return number.intValue
    case let stringValue as String:
        return Int(stringValue.trimmingCharacters(in: .whitespacesAndNewlines))
    default:
        return nil
    }
}

private func parseStringArray(_ value: Any?) -> [String]? {
    if let value = value as? [String] {
        return value
    }
    if let value = value as? [Any] {
        return value.compactMap { $0 as? String }
    }
    return nil
}

private func parseNonEmptyString(_ value: Any?) -> String? {
    guard let string = value as? String else {
        return nil
    }
    let trimmed = string.trimmingCharacters(in: .whitespacesAndNewlines)
    return trimmed.isEmpty ? nil : trimmed
}

@MainActor
private func bridgeReadyState() -> Bool {
    AdsManager.shared.isReady || ABIUnityAdsBridgeStore.shared.isReady
}

private func parseGlobalConfig(from jsonString: String?) -> GlobalConfig {
    let config = GlobalConfig()
    guard let root = decodeJSONObject(from: jsonString) else {
        return config
    }

    let payload = (root["global_config"] as? [String: Any]) ?? root

    config.mediationProvider = GlobalConfig.MediationProvider(jsonValue: payload["mediation_provider"])
    if let value = parseInt(payload["timeout_remote"]) { config.timeoutRemote = value }
    if let value = parseBool(payload["enable_adjust"]) { config.enableAdjust = value }
    if let value = parseBool(payload["enable_appsflyer"]) { config.enableAppsFlyer = value }
    if let value = parseBool(payload["enable_facebook"]) { config.enableFacebook = value }
    if let value = parseBool(payload["enable_tiktok"]) { config.enableTikTok = value }
    if let value = parseBool(payload["enable_firebase"]) { config.enableFirebase = value }
    if let value = parseBool(payload["enable_fcm"]) { config.enableFCM = value }
    if let value = payload["adjust_token"] as? String { config.adjustToken = value }
    if let value = payload["appsflyer_token"] as? String { config.appsflyerToken = value }
    if let value = payload["facebook_client_token"] as? String { config.facebookClientToken = value }
    if let value = payload["tiktok_app_id"] as? String { config.tiktokAppId = value }
    if let value = payload["tiktok_access_token"] as? String { config.tiktokAccessToken = value }
    if let value = payload["app_id_tt"] as? String { config.appIDTT = value }
    if let value = parseBool(payload["enable_adjust_tracking"]) { config.enableAdjustTracking = value }
    if let value = parseBool(payload["enable_appsflyer_tracking"]) { config.enableAppsFlyerTracking = value }
    if let value = parseBool(payload["enable_realtime_database_tracking"]) { config.enableRealtimeDatabaseTracking = value }
    if let value = parseBool(payload["variant_dev"]) { config.variantDev = value }
    if let value = parseStringArray(payload["enabled_versions"]) { config.enabledVersions = value }
    if let value = parseStringArray(payload["test_devices"]) { config.testDevices = value }
    if let value = payload["config_version"] as? String { config.configVersion = value }
    if let value = parseInt(payload["inter_ad_interval"]) { config.interAdInterval = value }
    if let value = parseStringArray(payload["skip_interval_placements"]) { config.skipIntervalPlacements = value }
    if let value = parseBool(payload[GlobalConfig.KEY_MAX_CONSENT_FLOW_ENABLED]) {
        config.maxTermsPrivacyFlowEnabled = value
    }
    if let value = payload[GlobalConfig.KEY_MAX_PRIVACY_POLICY_URL] as? String {
        config.maxPrivacyPolicyUrl = value
    }
    if let value = payload[GlobalConfig.KEY_MAX_TERMS_OF_SERVICE_URL] as? String {
        config.maxTermsOfServiceUrl = value
    }
    if let value = parseBool(payload[GlobalConfig.KEY_MAX_SHOW_TERMS_IN_GDPR]) {
        config.maxShowTermsPrivacyAlertInGdpr = value
    }
    if let value = parseBool(payload[GlobalConfig.KEY_MAX_CONSENT_DEBUG_GDPR]) {
        config.maxConsentDebugGeographyGdpr = value
    }
    if let value = parseBool(payload[GlobalConfig.KEY_ENABLE_AD_RESUME]) {
        config.enableAdResume = value
    }
    if let value = parseNonEmptyString(payload[GlobalConfig.KEY_ADMOB_APP_ID]) {
        config.admobAppId = value
    }
    if let value = parseNonEmptyString(payload[GlobalConfig.KEY_MAX_SDK_KEY]) {
        config.maxSdkKey = value
    }
    if let value = parseNonEmptyString(payload[GlobalConfig.KEY_AMAZON_APS_APP_ID]) {
        config.amazonApsAppId = value
    }
    if let value = parseBool(payload[GlobalConfig.KEY_AMAZON_APS_TEST_MODE]) {
        config.amazonApsTestMode = value
    }
    if let value = parseBool(payload[GlobalConfig.KEY_META_AUDIENCE_LDU_ENABLED]) {
        config.metaAudienceLduEnabled = value
    }
    if let value = parseInt(payload[GlobalConfig.KEY_META_AUDIENCE_LDU_COUNTRY]) {
        config.metaAudienceLduCountry = value
    }
    if let value = parseInt(payload[GlobalConfig.KEY_META_AUDIENCE_LDU_STATE]) {
        config.metaAudienceLduState = value
    }
    if let value = parseInt(payload[GlobalConfig.KEY_TEST_USER_AD_CAPPING_INTERVAL]) {
        config.testUserAdCappingInterval = value
    }

    return config
}

private func parsePlacements(from jsonString: String?) -> [String: Any] {
    decodeJSONObject(from: jsonString) ?? ["placements": []]
}

private func cachePlacementAdTypes(from placements: [String: Any]) {
    var result: [String: String] = [:]
    guard let items = placements["placements"] as? [[String: Any]] else {
        ABIUnityAdsBridgeStore.shared.placementAdTypes = result
        return
    }

    for item in items {
        guard let name = item["ad_name"] as? String,
              let type = item["ads_type"] as? String,
              !name.isEmpty,
              !type.isEmpty else {
            continue
        }
        result[name] = type
    }

    ABIUnityAdsBridgeStore.shared.placementAdTypes = result
}

private func topViewController(from root: UIViewController?) -> UIViewController? {
    if let navigationController = root as? UINavigationController {
        return topViewController(from: navigationController.visibleViewController)
    }

    if let tabBarController = root as? UITabBarController {
        return topViewController(from: tabBarController.selectedViewController)
    }

    if let presentedViewController = root?.presentedViewController {
        return topViewController(from: presentedViewController)
    }

    return root
}

@MainActor
private func currentUnityViewController() -> UIViewController? {
    let scenes = UIApplication.shared.connectedScenes.compactMap { $0 as? UIWindowScene }
    let windows = scenes.flatMap(\.windows)
    let keyWindow = windows.first(where: \.isKeyWindow) ?? windows.first(where: { $0.rootViewController != nil })
    return topViewController(from: keyWindow?.rootViewController)
}

private func parseNativeSize(_ value: String?) -> BBLNativeAdView.NativeAdSize {
    switch value?.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() {
    case "small":
        return .small
    case "free_size", "free", "fullscreen", "full_screen", "full":
        return .fullScreen
    default:
        return .medium
    }
}

private func nativeHeight(for size: BBLNativeAdView.NativeAdSize) -> CGFloat {
    switch size {
    case .small:
        return 140
    case .fullScreen:
        return UIScreen.main.bounds.height
    case .medium:
        return 320
    @unknown default:
        return 320
    }
}

private func addOverlayView(
    _ overlayView: UIView,
    to parentView: UIView,
    position: String?,
    height: CGFloat,
    fillParent: Bool = false
) {
    overlayView.translatesAutoresizingMaskIntoConstraints = false
    parentView.addSubview(overlayView)

    let safeArea = parentView.safeAreaLayoutGuide
    let normalizedPosition = position?.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()

    var constraints: [NSLayoutConstraint] = [
        overlayView.leadingAnchor.constraint(equalTo: safeArea.leadingAnchor),
        overlayView.trailingAnchor.constraint(equalTo: safeArea.trailingAnchor)
    ]

    if fillParent {
        constraints.append(overlayView.topAnchor.constraint(equalTo: parentView.topAnchor))
        constraints.append(overlayView.bottomAnchor.constraint(equalTo: parentView.bottomAnchor))
        NSLayoutConstraint.activate(constraints)
        return
    }

    constraints.append(overlayView.heightAnchor.constraint(greaterThanOrEqualToConstant: height))

    switch normalizedPosition {
    case "top":
        constraints.append(overlayView.topAnchor.constraint(equalTo: safeArea.topAnchor))
    case "center":
        constraints.append(overlayView.centerYAnchor.constraint(equalTo: safeArea.centerYAnchor))
    default:
        constraints.append(overlayView.bottomAnchor.constraint(equalTo: safeArea.bottomAnchor))
    }

    NSLayoutConstraint.activate(constraints)
}

private func removeOverlayView(_ view: UIView?) {
    view?.removeFromSuperview()
}

private func hideNativePlacement(_ placementName: String?) {
    let overlayManager = BBLUnityNativeOverlayManager.shared
    overlayManager.hide(adName: placementName)
    let placement = placementName ?? ""
    emitEvent(
        eventName: "native_hidden",
        placement: placement,
        ready: bridgeReadyState()
    )
}

private func destroyNativePlacement(_ placementName: String?) {
    let overlayManager = BBLUnityNativeOverlayManager.shared
    if let placementName, !placementName.isEmpty {
        AdsManager.shared.unregisterNativePresentation(adName: placementName)
        overlayManager.destroy(adName: placementName)
        emitEvent(eventName: "native_destroyed", placement: placementName, ready: bridgeReadyState())
        return
    }

    overlayManager.destroy(adName: nil)
    emitEvent(eventName: "native_destroyed", ready: bridgeReadyState())
}

private func emitEvent(
    eventName: String,
    placement: String = "",
    error: String = "",
    rewardType: String = "",
    rewardAmount: Int = 0,
    revenue: String = "",
    ready: Bool,
    remoteApplied: Bool = false
) {
    let store = ABIUnityAdsBridgeStore.shared
    let payload: [String: Any] = [
        "eventName": eventName,
        "placement": placement,
        "error": error,
        "rewardType": rewardType,
        "rewardAmount": rewardAmount,
        "revenue": revenue,
        "adType": store.placementAdTypes[placement] ?? "",
        "ready": ready,
        "remoteApplied": remoteApplied,
        "platform": "ios"
    ]

    guard let data = try? JSONSerialization.data(withJSONObject: payload),
          let message = String(data: data, encoding: .utf8) else {
        return
    }

    store.callbackGameObject.withCString { gameObjectName in
        store.callbackMethod.withCString { methodName in
            message.withCString { jsonMessage in
                UnitySendMessage(gameObjectName, methodName, jsonMessage)
            }
        }
    }
}

private func makeCallback() -> AdCallback {
    var callback = AdCallback()

    callback.onAdLoaded = { placement in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(eventName: "loaded", placement: placement, ready: ready)
    }

    callback.onAdFailed = { placement, error in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(
            eventName: "failed",
            placement: placement,
            error: error,
            ready: ready
        )
    }

    callback.onAdDisplayFailed = { placement, error in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(
            eventName: "display_failed",
            placement: placement,
            error: error,
            ready: ready
        )
    }

    callback.onAdImpression = { placement in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(eventName: "impression", placement: placement, ready: ready)
    }

    callback.onAdClicked = { placement in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(eventName: "clicked", placement: placement, ready: ready)
    }

    callback.onAdClosed = { placement in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(eventName: "closed", placement: placement, ready: ready)
        if ABIUnityAdsBridgeStore.shared.rewardedPlacements.remove(placement) != nil {
            emitEvent(eventName: "reward_completed", placement: placement, ready: ready)
        }
    }

    callback.onAdRevenue = { placement, value in
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(
            eventName: "revenue",
            placement: placement,
            revenue: String(describing: value),
            ready: ready
        )
    }

    callback.onRewardGranted = { placement, reward in
        ABIUnityAdsBridgeStore.shared.rewardedPlacements.insert(placement)
        let ready = Thread.isMainThread
            ? MainActor.assumeIsolated { bridgeReadyState() }
            : ABIUnityAdsBridgeStore.shared.isReady
        emitEvent(
            eventName: "reward_granted",
            placement: placement,
            rewardType: reward.type,
            rewardAmount: reward.amount,
            ready: ready
        )
    }

    return callback
}

@MainActor
private func ensureReadyListener() {
    let store = ABIUnityAdsBridgeStore.shared
    guard !store.readyListenerRegistered else {
        return
    }
    store.readyListenerRegistered = true
    AdsManager.shared.addOnReadyListener {
        ABIUnityAdsBridgeStore.shared.isReady = true
    }
}

@MainActor
private func applyUnityConfigWhenReady(
    globalConfig: GlobalConfig,
    placements: [String: Any]
) {
    RemoteConfigManager.shared.setLocalPlacements(placements)
    AdsManager.shared.updateGlobalConfig(globalConfig)
    AdsManager.shared.reloadPlacements()
    cachePlacementAdTypes(from: placements)
    emitEvent(eventName: "config_applied", ready: bridgeReadyState())
}

@_cdecl("ABIUnityAds_SetCallbackTarget")
public func ABIUnityAds_SetCallbackTarget(
    _ gameObjectName: UnsafePointer<CChar>?,
    _ methodName: UnsafePointer<CChar>?
) {
    let store = ABIUnityAdsBridgeStore.shared
    if let gameObject = stringFromCString(gameObjectName), !gameObject.isEmpty {
        store.callbackGameObject = gameObject
    }
    if let method = stringFromCString(methodName), !method.isEmpty {
        store.callbackMethod = method
    }
    emitEvent(eventName: "bridge_ready", ready: Thread.isMainThread
        ? MainActor.assumeIsolated { bridgeReadyState() }
        : store.isReady)
}

@_cdecl("ABIUnityAds_Initialize")
public func ABIUnityAds_Initialize(
    _ globalConfigJson: UnsafePointer<CChar>?,
    _ placementsJson: UnsafePointer<CChar>?
) {
    let globalConfigString = decodeUnityConfigString(stringFromCString(globalConfigJson))
    let placementsString = decodeUnityConfigString(stringFromCString(placementsJson))

    DispatchQueue.main.async {
        let store = ABIUnityAdsBridgeStore.shared
        ensureReadyListener()

        if let viewController = currentUnityViewController() {
            AdsManager.shared.setCurrentViewController(viewController)
        }

        let globalConfig = parseGlobalConfig(from: globalConfigString)
        let placements = parsePlacements(from: placementsString)
        cachePlacementAdTypes(from: placements)

        if let requirementError = AdsManager.validateUnityMediationRequirements(globalConfig) {
            emitEvent(
                eventName: "failed",
                error: requirementError,
                ready: bridgeReadyState()
            )
            return
        }

        if AdsManager.shared.isReady {
            applyUnityConfigWhenReady(globalConfig: globalConfig, placements: placements)
            store.isReady = true
            if !store.initializedEventEmitted {
                store.initializedEventEmitted = true
                emitEvent(eventName: "initialized", ready: true, remoteApplied: false)
            }
            return
        }

        AdsManager.shared.requestInitSecurityPermission(config: globalConfig) { allowed, reason in
            DispatchQueue.main.async {
                guard allowed else {
                    emitEvent(
                        eventName: "failed",
                        error: "Denied: \(reason ?? "denied")",
                        ready: bridgeReadyState()
                    )
                    return
                }

                AdsManager.shared.initialize(
                    application: UIApplication.shared,
                    config: globalConfig,
                    localPlacements: placements
                ) { remoteApplied in
                    store.isReady = true
                    if !store.initializedEventEmitted {
                        store.initializedEventEmitted = true
                        emitEvent(eventName: "initialized", ready: true, remoteApplied: remoteApplied)
                    }
                }

                emitEvent(eventName: "config_applied", ready: bridgeReadyState())
            }
        }
    }
}

@_cdecl("ABIUnityAds_IsReady")
public func ABIUnityAds_IsReady() -> Int32 {
    if Thread.isMainThread {
        return MainActor.assumeIsolated {
            bridgeReadyState() ? 1 : 0
        }
    }

    var ready = ABIUnityAdsBridgeStore.shared.isReady
    DispatchQueue.main.sync {
        ready = bridgeReadyState()
        if ready {
            ABIUnityAdsBridgeStore.shared.isReady = true
        }
    }
    return ready ? 1 : 0
}

@_cdecl("ABIUnityAds_IsPlacementReady")
public func ABIUnityAds_IsPlacementReady(_ placement: UnsafePointer<CChar>?) -> Int32 {
    let placementName = stringFromCString(placement) ?? ""
    if placementName.isEmpty {
        return 0
    }

    if Thread.isMainThread {
        return MainActor.assumeIsolated {
            AdsManager.shared.isAdReadyInPool(adName: placementName) ? 1 : 0
        }
    }

    var ready = false
    DispatchQueue.main.sync {
        ready = AdsManager.shared.isAdReadyInPool(adName: placementName)
    }
    return ready ? 1 : 0
}

@_cdecl("ABIUnityAds_SetCurrentViewController")
public func ABIUnityAds_SetCurrentViewController() {
    DispatchQueue.main.async {
        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                error: "No active UIViewController found",
                ready: ABIUnityAdsBridgeStore.shared.isReady
            )
            return
        }

        AdsManager.shared.setCurrentViewController(viewController)
        emitEvent(eventName: "view_controller_updated", ready: ABIUnityAdsBridgeStore.shared.isReady)
    }
}

@_cdecl("ABIUnityAds_Load")
public func ABIUnityAds_Load(_ placement: UnsafePointer<CChar>?) {
    let placementName = stringFromCString(placement) ?? ""
    DispatchQueue.main.async {
        AdsManager.shared.load(adName: placementName, callback: makeCallback())
    }
}

private func performUnityShow(placement: UnsafePointer<CChar>?, timeoutMs: Int32) {
    let placementName = stringFromCString(placement) ?? ""
    DispatchQueue.main.async {
        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "No active UIViewController found",
                ready: ABIUnityAdsBridgeStore.shared.isReady
            )
            return
        }

        AdsManager.shared.setCurrentViewController(viewController)
        AdsManager.shared.show(
            adName: placementName,
            viewController: viewController,
            callback: makeCallback(),
            timeoutLoadingAds: Int(timeoutMs)
        )
    }
}

@_cdecl("ABIUnityAds_Show")
public func ABIUnityAds_Show(_ placement: UnsafePointer<CChar>?) {
    performUnityShow(placement: placement, timeoutMs: 0)
}

@_cdecl("ABIUnityAds_ShowWithTimeout")
public func ABIUnityAds_ShowWithTimeout(_ placement: UnsafePointer<CChar>?, _ timeoutMs: Int32) {
    performUnityShow(placement: placement, timeoutMs: timeoutMs)
}

@_cdecl("ABIUnityAds_LoadAndShow")
public func ABIUnityAds_LoadAndShow(_ placement: UnsafePointer<CChar>?) {
    let placementName = stringFromCString(placement) ?? ""
    DispatchQueue.main.async {
        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "No active UIViewController found",
                ready: ABIUnityAdsBridgeStore.shared.isReady
            )
            return
        }

        AdsManager.shared.setCurrentViewController(viewController)
        AdsManager.shared.loadAndShow(adName: placementName, viewController: viewController, callback: makeCallback())
    }
}

@_cdecl("ABIUnityAds_LoadAndShowWithTimeDelay")
public func ABIUnityAds_LoadAndShowWithTimeDelay(
    _ placement: UnsafePointer<CChar>?,
    _ timeoutMs: Int64,
    _ timeDelayMs: Int64
) {
    let placementName = stringFromCString(placement) ?? ""
    DispatchQueue.main.async {
        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "No active UIViewController found",
                ready: ABIUnityAdsBridgeStore.shared.isReady
            )
            return
        }

        AdsManager.shared.setCurrentViewController(viewController)
        AdsManager.shared.loadandShowWithTimeDalay(
            adName: placementName,
            viewController: viewController,
            timeoutMs: timeoutMs,
            timeDelayMs: timeDelayMs,
            callback: makeCallback()
        )
    }
}

@_cdecl("ABIUnityAds_LoadRewarded")
public func ABIUnityAds_LoadRewarded(_ placement: UnsafePointer<CChar>?) {
    ABIUnityAds_Load(placement)
}

@_cdecl("ABIUnityAds_ShowRewarded")
public func ABIUnityAds_ShowRewarded(_ placement: UnsafePointer<CChar>?) {
    performUnityShow(placement: placement, timeoutMs: 0)
}

@_cdecl("ABIUnityAds_ShowRewardedWithTimeout")
public func ABIUnityAds_ShowRewardedWithTimeout(_ placement: UnsafePointer<CChar>?, _ timeoutMs: Int32) {
    performUnityShow(placement: placement, timeoutMs: timeoutMs)
}

@_cdecl("ABIUnityAds_LoadAndShowRewarded")
public func ABIUnityAds_LoadAndShowRewarded(_ placement: UnsafePointer<CChar>?) {
    ABIUnityAds_LoadAndShow(placement)
}

@_cdecl("ABIUnityAds_ShowBanner")
public func ABIUnityAds_ShowBanner(
    _ placement: UnsafePointer<CChar>?,
    _ position: UnsafePointer<CChar>?
) {
    let placementName = stringFromCString(placement) ?? ""
    let positionName = stringFromCString(position)

    DispatchQueue.main.async {
        guard !placementName.isEmpty else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "Placement is empty",
                ready: bridgeReadyState()
            )
            return
        }

        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "No active UIViewController found",
                ready: bridgeReadyState()
            )
            return
        }

        let store = ABIUnityAdsBridgeStore.shared
        removeOverlayView(store.bannerContainer)

        let container = UIView(frame: .zero)
        container.backgroundColor = .clear
        container.isHidden = false
        store.bannerContainer = container
        store.bannerPlacementName = placementName
        viewController.view.clipsToBounds = false
        AdsManager.shared.setCurrentViewController(viewController)
        AdsManager.shared.registerBannerContainer(adName: placementName, containerView: container)
        addOverlayView(container, to: viewController.view, position: positionName, height: 0)

        var callback = makeCallback()
        let previousLoaded = callback.onAdLoaded
        let previousFailed = callback.onAdFailed
        callback.onAdLoaded = { placement in
            container.setNeedsLayout()
            container.layoutIfNeeded()
            previousLoaded?(placement)
        }
        callback.onAdFailed = { placement, error in
            removeOverlayView(container)
            store.bannerContainer = nil
            store.bannerPlacementName = nil
            AdsManager.shared.unregisterBannerContainer(adName: placement)
            previousFailed?(placement, error)
        }

        AdsManager.shared.show(
            adName: placementName,
            viewController: viewController,
            callback: callback
        )
        emitEvent(eventName: "banner_requested", placement: placementName, ready: bridgeReadyState())
    }
}

@_cdecl("ABIUnityAds_HideBanner")
public func ABIUnityAds_HideBanner() {
    DispatchQueue.main.async {
        ABIUnityAdsBridgeStore.shared.bannerContainer?.isHidden = true
        AdsManager.shared.clearAllBannerLoadingStates()
        emitEvent(eventName: "banner_hidden", ready: bridgeReadyState())
    }
}

@_cdecl("ABIUnityAds_DestroyBanner")
public func ABIUnityAds_DestroyBanner() {
    DispatchQueue.main.async {
        let store = ABIUnityAdsBridgeStore.shared
        if let placementName = store.bannerPlacementName {
            AdsManager.shared.unregisterBannerContainer(adName: placementName)
        }
        removeOverlayView(store.bannerContainer)
        store.bannerContainer = nil
        store.bannerPlacementName = nil
        AdsManager.shared.clearAllBannerLoadingStates()
        emitEvent(eventName: "banner_destroyed", ready: bridgeReadyState())
    }
}

@_cdecl("ABIUnityAds_ShowNative")
public func ABIUnityAds_ShowNative(
    _ placement: UnsafePointer<CChar>?,
    _ templateName: UnsafePointer<CChar>?,
    _ sizeName: UnsafePointer<CChar>?,
    _ position: UnsafePointer<CChar>?
) {
    ABIUnityAds_ShowNativeWithDuration(placement, templateName, sizeName, position, 0)
}

@_cdecl("ABIUnityAds_SetNativePlaceholderBounds")
public func ABIUnityAds_SetNativePlaceholderBounds(
    _ minX: Float,
    _ minY: Float,
    _ maxX: Float,
    _ maxY: Float
) {
    DispatchQueue.main.async {
        BBLUnityNativeOverlayManager.shared.setDefaultPlaceholderBounds(
            minX: minX,
            minY: minY,
            maxX: maxX,
            maxY: maxY
        )
    }
}

@_cdecl("ABIUnityAds_SetNativePlaceholderBoundsForPlacement")
public func ABIUnityAds_SetNativePlaceholderBoundsForPlacement(
    _ placement: UnsafePointer<CChar>?,
    _ minX: Float,
    _ minY: Float,
    _ maxX: Float,
    _ maxY: Float
) {
    let placementName = stringFromCString(placement)
    DispatchQueue.main.async {
        BBLUnityNativeOverlayManager.shared.setPlaceholderBounds(
            adName: placementName,
            minX: minX,
            minY: minY,
            maxX: maxX,
            maxY: maxY
        )
    }
}

@_cdecl("ABIUnityAds_PrepareNativeFullScreenShow")
public func ABIUnityAds_PrepareNativeFullScreenShow(
    _ placement: UnsafePointer<CChar>?,
    _ dismissOnAdClick: Int32
) {
    let placementName = stringFromCString(placement) ?? ""
    guard !placementName.isEmpty else { return }
    DispatchQueue.main.async {
        BBLUnityNativeOverlayManager.shared.prepareNativeFullScreenShow(
            adName: placementName,
            dismissOnAdClick: dismissOnAdClick != 0
        )
    }
}

@_cdecl("ABIUnityAds_ShowNativeWithDuration")
public func ABIUnityAds_ShowNativeWithDuration(
    _ placement: UnsafePointer<CChar>?,
    _ templateName: UnsafePointer<CChar>?,
    _ sizeName: UnsafePointer<CChar>?,
    _ position: UnsafePointer<CChar>?,
    _ duration: Int32
) {
    ABIUnityAds_ShowNativeWithDurationAndBounds(
        placement,
        templateName,
        sizeName,
        position,
        duration,
        -1,
        -1,
        -1,
        -1
    )
}

@_cdecl("ABIUnityAds_ShowNativeWithDurationAndBounds")
public func ABIUnityAds_ShowNativeWithDurationAndBounds(
    _ placement: UnsafePointer<CChar>?,
    _ templateName: UnsafePointer<CChar>?,
    _ sizeName: UnsafePointer<CChar>?,
    _ position: UnsafePointer<CChar>?,
    _ duration: Int32,
    _ minX: Float,
    _ minY: Float,
    _ maxX: Float,
    _ maxY: Float
) {
    let placementName = stringFromCString(placement) ?? ""
    let template = stringFromCString(templateName)
    let size = parseNativeSize(stringFromCString(sizeName))
    let containerStyle = stringFromCString(sizeName) ?? "medium"

    DispatchQueue.main.async {
        guard !placementName.isEmpty else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "Placement is empty",
                ready: bridgeReadyState()
            )
            return
        }

        guard let viewController = currentUnityViewController() else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "No active UIViewController found",
                ready: bridgeReadyState()
            )
            return
        }

        AdsManager.shared.setCurrentViewController(viewController)

        let customBounds: (minX: Float, minY: Float, maxX: Float, maxY: Float)?
        if minX >= 0, minY >= 0, maxX >= 0, maxY >= 0 {
            customBounds = (minX, minY, maxX, maxY)
        } else {
            customBounds = nil
        }

        let overlayManager = BBLUnityNativeOverlayManager.shared
        guard let built = overlayManager.showNative(
            adName: placementName,
            templateName: template,
            size: size,
            containerStyle: containerStyle,
            duration: duration,
            customBounds: customBounds,
            in: viewController,
            onHide: { adName in
                hideNativePlacement(adName)
            },
            onAdClicked: { _ in }
        ) else {
            emitEvent(
                eventName: "failed",
                placement: placementName,
                error: "Unable to create native slot",
                ready: bridgeReadyState()
            )
            return
        }

        AdsManager.shared.registerNativePresentation(
            adName: placementName,
            nativeAdView: built.nativeAdView,
            size: size,
            enableLegacyReadyLookup: true
        )

        var callback = makeCallback()
        let overlayCallback = built.callback
        let previousLoaded = callback.onAdLoaded
        let previousFailed = callback.onAdFailed
        let previousClicked = callback.onAdClicked
        callback.onAdLoaded = { name in
            overlayCallback.onAdLoaded?(name)
            previousLoaded?(name)
        }
        callback.onAdFailed = { name, error in
            overlayCallback.onAdFailed?(name, error)
            previousFailed?(name, error)
        }
        callback.onAdClicked = { name in
            overlayCallback.onAdClicked?(name)
            previousClicked?(name)
        }

        AdsManager.shared.loadAndRenderNativeAd(
            adName: placementName,
            nativeAdView: built.nativeAdView,
            size: size,
            callback: callback
        )
        emitEvent(eventName: "native_requested", placement: placementName, ready: bridgeReadyState())
    }
}

@_cdecl("ABIUnityAds_HideNative")
public func ABIUnityAds_HideNative() {
    DispatchQueue.main.async {
        hideNativePlacement(nil)
    }
}

@_cdecl("ABIUnityAds_HideNativeForPlacement")
public func ABIUnityAds_HideNativeForPlacement(_ placement: UnsafePointer<CChar>?) {
    let placementName = stringFromCString(placement)
    DispatchQueue.main.async {
        hideNativePlacement(placementName)
    }
}

@_cdecl("ABIUnityAds_DestroyNative")
public func ABIUnityAds_DestroyNative() {
    DispatchQueue.main.async {
        destroyNativePlacement(nil)
    }
}

@_cdecl("ABIUnityAds_DestroyNativeForPlacement")
public func ABIUnityAds_DestroyNativeForPlacement(_ placement: UnsafePointer<CChar>?) {
    let placementName = stringFromCString(placement)
    DispatchQueue.main.async {
        destroyNativePlacement(placementName)
    }
}
