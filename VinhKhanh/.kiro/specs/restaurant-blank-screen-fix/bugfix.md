# Bugfix Requirements Document

## Introduction

Khi người dùng cài APK release trên thiết bị thật (Android) và nhấn vào một quán ăn (POI) trong danh sách, màn hình chi tiết hiển thị hoàn toàn trắng — không có nội dung nào được render. Lỗi không tái hiện trên emulator.

Qua phân tích code, xác định được **hai nguyên nhân gốc rễ** gây ra màn hình trắng:

1. **Binding sai tên property trong `PoiDetailPage.xaml`**: XAML bind vào `Poi.ImageUrl` và `Poi.Category`, nhưng `PoiDetailViewModel` expose property tên là `PoiDetail` (không phải `Poi`). Kết quả: toàn bộ binding liên quan đến ảnh và danh mục trả về null/empty, và nếu compiled binding (`x:DataType`) được bật trong release build, lỗi này có thể khiến trang không render gì cả.

2. **`x:DataType` sai trong `PoiListPage.xaml`**: Trang khai báo `x:DataType="vm:MainViewModel"` nhưng code-behind không dùng `MainViewModel` — nó dùng `PoiListPage` làm `BindingContext` trực tiếp. Trong release build với compiled bindings, điều này gây lỗi binding compile-time hoặc runtime khiến trang không hiển thị đúng.

---

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN người dùng nhấn vào một quán ăn trong danh sách trên thiết bị thật (APK release) THEN hệ thống hiển thị màn hình trắng thay vì nội dung chi tiết quán ăn

1.2 WHEN `PoiDetailPage` được điều hướng đến với tham số `PoiId` THEN hệ thống không hiển thị ảnh banner và danh mục vì XAML bind vào `Poi.ImageUrl` / `Poi.Category` trong khi ViewModel chỉ expose `PoiDetail.ImageUrl` / `PoiDetail.Category`

1.3 WHEN `PoiListPage` được render với compiled bindings trong release build THEN hệ thống gặp lỗi binding do `x:DataType="vm:MainViewModel"` không khớp với `BindingContext` thực tế là `PoiListPage` (code-behind)

1.4 WHEN lỗi binding xảy ra trong release build (compiled bindings) THEN hệ thống không hiển thị nội dung trang, dẫn đến màn hình trắng

### Expected Behavior (Correct)

2.1 WHEN người dùng nhấn vào một quán ăn trong danh sách trên thiết bị thật (APK release) THEN hệ thống SHALL điều hướng đến trang chi tiết và hiển thị đầy đủ thông tin quán ăn (tên, ảnh, danh mục, mô tả)

2.2 WHEN `PoiDetailPage` được điều hướng đến với tham số `PoiId` THEN hệ thống SHALL hiển thị ảnh banner và danh mục bằng cách bind đúng vào `PoiDetail.ImageUrl` và `PoiDetail.Category`

2.3 WHEN `PoiListPage` được render THEN hệ thống SHALL sử dụng `x:DataType` khớp với `BindingContext` thực tế, hoặc loại bỏ `x:DataType` sai để tránh lỗi compiled binding

2.4 WHEN compiled bindings được kích hoạt trong release build THEN hệ thống SHALL không phát sinh lỗi binding type mismatch trên bất kỳ trang nào liên quan đến POI

### Unchanged Behavior (Regression Prevention)

3.1 WHEN người dùng quét mã QR hợp lệ của một quán ăn THEN hệ thống SHALL CONTINUE TO điều hướng đến trang chi tiết và phát thuyết minh tự động

3.2 WHEN người dùng sử dụng app trên emulator Android THEN hệ thống SHALL CONTINUE TO hiển thị danh sách và chi tiết quán ăn bình thường

3.3 WHEN `PoiDetailViewModel` nhận `PoiId` qua query parameter THEN hệ thống SHALL CONTINUE TO gọi API để lấy dữ liệu POI và cập nhật `PoiDetail`

3.4 WHEN người dùng nhấn nút phát thuyết minh trên trang chi tiết THEN hệ thống SHALL CONTINUE TO phát audio narration cho quán ăn đó

3.5 WHEN danh sách quán ăn được tải THEN hệ thống SHALL CONTINUE TO hiển thị đúng tên, ảnh thumbnail, danh mục và mô tả ngắn trong từng item

---

## Bug Condition (Pseudocode)

**Bug Condition Function** — xác định input kích hoạt lỗi:

```pascal
FUNCTION isBugCondition(context)
  INPUT: context gồm { platform: DeviceType, buildType: string, action: string }
  OUTPUT: boolean

  RETURN context.platform = "RealDevice"
     AND context.buildType = "Release"
     AND context.action IN ["TapPoiItem", "NavigateToPoiDetail"]
END FUNCTION
```

**Property: Fix Checking**

```pascal
FOR ALL context WHERE isBugCondition(context) DO
  result ← navigateToPoiDetail'(context)
  ASSERT result.pageRendered = true
     AND result.imageVisible = true
     AND result.nameVisible = true
     AND result.descriptionVisible = true
END FOR
```

**Property: Preservation Checking**

```pascal
FOR ALL context WHERE NOT isBugCondition(context) DO
  ASSERT navigateToPoiDetail(context) = navigateToPoiDetail'(context)
END FOR
```
