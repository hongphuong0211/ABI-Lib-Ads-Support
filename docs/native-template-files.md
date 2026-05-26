# Native template files (review)

Tài liệu tham chiếu để **xem và chọn** tên layout native (`templateName` / `ad_layout_file`) khi tích hợp Unity bridge.

## `templateName` trong Unity

`ABIAds.ShowNative(..., templateName, ...)` nhận **tên file layout không có extension**:

```csharp
ABIAds.ShowNative("main_native", "ads_layout_native_language", "medium", "bottom", 0);
//                              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                              templateName — phải khớp layout Android (.xml) / iOS (.xib)
```

Trong **Placement Config** (`placements.json`), trường `native_ad.ad_layout_file` dùng cùng quy ước tên.

Editor **ABI Ads → Configs → Edit Placement Config** hiển thị dropdown layout khi mở monorepo `BBL-Module-Ads` (Android XML + iOS XIB trùng tên).

---

### Template `ads_layout_native_*` (hay dùng)

| Fil | Ghi chú |
|---------------------|---------|
| `ads_layout_native_language` | Language screen |
| `ads_layout_native_language_2` | Language screen (variant 2) |
| `ads_layout_native_language_3` | Language screen (variant 3) |
| `ads_layout_native_on_boarding` | Onboarding |
| `ads_layout_native_permission` | Permission screen |
| `ads_layout_native_welcome_back` | Welcome back |

Các layout native khác (custom, collapsible, shimmer, …) cùng thư mục — tìm theo prefix `ads_native_`, `custom_native_`, `layout_native_`.

**Review trên Sheet:** [Native Add Templates](https://docs.google.com/spreadsheets/d/1LxvJKFlAn_9vDGtWCXLAHsGexKQmfJraV2_DgbhK6ng/edit?gid=0#gid=0)

## Checklist chọn template

1. Mở dropdown **Ad Layout File** trong Placement Config (hoặc xem bảng Android ở trên).
2. Dùng **cùng tên** cho `native_ad.ad_layout_file` (JSON) và `ShowNative(..., templateName, ...)`.
3. Build thử trên **cả Android và iOS** — tên phải tồn tại trên cả hai nền tảng nếu dùng layout shared.
4. Styling (màu, bo góc, CTA) cấu hình trong `placements.json` → `native_ad.*`, không sửa trực tiếp từ Unity C#.
