# DXF Exporter Pro — Kiến trúc và luồng hoạt động

Add-in C# / .NET 10 (`net10.0-windows`) độc lập cho **Autodesk Inventor 2027**. Mã nguồn add-in hiện tại là nguồn chuẩn để phát triển và kiểm thử.

Ba chức năng, hiện trên Ribbon:

| Nút | Ngữ cảnh | Việc làm |
|---|---|---|
| Convert STEP | Chỉ Assembly | Đổi các part import (STEP) sang Sheet Metal, đặt Thickness, gán A-Side, tạo Flat Pattern, **Save part** |
| Xuất DXF | Part, Assembly | Xuất DXF từ Flat Pattern, hậu xử lý DXF và (tuỳ chọn) xuất BOM; không cập nhật thuộc tính hoặc lưu part nguồn |
| Xuất BOM | Part, Assembly | Ghi bảng kê vào file Excel mẫu (ClosedXML, không cần Excel) |

---

## 1. Các lớp (layer) và quy tắc phụ thuộc

```mermaid
flowchart TD
    INV["Inventor 2027 (host, COM, luồng UI/STA)"]
    SRV["InventorDxfExporterServer.cs<br/>ApplicationAddInServer"]
    RIB["Commands/RibbonCommandSet.cs<br/>nút, icon, tab Ribbon"]
    CMD["Commands/*Command.cs<br/>Convert / ExportDxf / ExportBom"]
    UI["UI/Forms/*<br/>WinForms, không gọi COM"]
    SVC["Services/*<br/>mọi thao tác COM Inventor"]
    CORE["Core/*<br/>thuần .NET: DXF, tên file, model"]
    SET["Settings/*<br/>Export settings JSON, profiles, BOM JSON"]

    INV -->|"Activate / Deactivate"| SRV
    SRV --> RIB
    RIB -->|"OnExecute của nút"| CMD
    CMD --> UI
    CMD --> SET
    CMD --> SVC
    SVC --> CORE
    UI --> CORE
    SET --> CORE
    SVC -.->|"COM"| INV
```

Quy tắc giữ nguyên khi sửa code:
- `Core/` **không** tham chiếu Inventor/COM (để test được không cần Inventor). `ExportItem` chỉ giữ `DocumentPath` (chuỗi), không giữ đối tượng COM.
- Thay đổi hành vi phải được kiểm tra bằng build/test và trên Inventor 2027 khi có liên quan đến host.
- Đối tượng COM (`PartDocument`...) chỉ sống trong `Services/` (`WorkItem` trong `DxfExportServiceImplementation`).
- Mọi gọi COM chạy trên luồng UI của Inventor. Tiến trình báo giữa các mục qua `IOperationMonitor`; `OperationProgressForm.Report` gọi `Application.DoEvents()` và ném `OperationCanceledException` khi bấm Hủy.
- `Skill/` là bộ tài liệu cho AI, bị loại khỏi build (`Compile Remove="Skill\**"`).

---

## 2. Khởi động add-in

```mermaid
sequenceDiagram
    participant I as Inventor
    participant S as InventorDxfExporterServer
    participant R as RibbonCommandSet
    I->>I: Quét InventorDxfExporter.addin (LoadAutomatically=1)
    I->>S: Activate(site, firstTime)
    S->>R: new RibbonCommandSet(app).Activate(firstTime)
    R->>R: RemoveRetiredRibbonTabs (V1, V3 trên Assembly và Part)
    R->>R: Define x3: ConvertStep, ExportDxf, ExportBom (AddButtonDefinition + icon vẽ bằng GDI+)
    R->>R: Nếu firstTime hoặc thiếu tab V4, tạo tab còn thiếu
    R->>I: Subscribe UserInterfaceEvents.OnResetRibbonInterface
    I->>R: OnResetRibbonInterface(Context)
    R->>R: Tạo lại các tab add-in còn thiếu
    Note over S: Lỗi trong Activate thì Deactivate rồi ném lại
    I->>S: Deactivate()
    S->>R: gỡ OnExecute và OnResetRibbonInterface handler
```

- `ClassId`/`ClientId` trong `.addin` = `[Guid]` của class server = `6B93E5C1-9A6A-4D1B-8FA9-47567B4A1D20` (cũng là `ClientId` trong `RibbonCommandSet`). **Phải khớp nhau.**
- Mỗi `*Command` tự tạo một `ServiceContainer` riêng (3 bản, trạng thái không chia sẻ). Bộ chặn dùng chung ngăn chạy lệnh thứ hai khi `DoEvents` nhường UI.
- Khi đổi bố cục Ribbon: tăng hậu tố phiên bản (`V4` → `V5`) và thêm tên cũ vào `retiredTabNames`. Xoá tab chỉ theo `InternalName`, không theo tên hiển thị.
- `Define` giữ `ButtonDefinitionSink_Event` + handler trong `_subscriptions` để không bị GC và gỡ được khi `Deactivate`. Lỗi trong lệnh được bắt và hiện MessageBox, không để lọt qua ranh giới COM.

---

## 3. Luồng Convert STEP

`ConvertStepCommand` → `ConvertServiceImplementation`

```mermaid
flowchart TD
    A["Bấm Convert STEP"] --> B{"ActiveDocument là Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["ExportSettingsStore.Load"]
    C -->     D["ConvertStepForm: lọc tên, chế độ 1/2, bỏ qua part đã có Flat Pattern, dung sai, bật/tắt log và thư mục log"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["ApplyTo + Save JSON"]
    E --> F["OperationProgressForm.Show"]
    F --> G["Convert: SilentOperation=true, ScreenUpdating=false"]
    G --> H["Duyệt assembly.AllReferencedDocuments, chỉ lấy Part, lọc theo tên"]
    H --> I{"Đã Sheet Metal và có Flat Pattern<br/>và bật bỏ qua?"}
    I -- Có --> I1["Skipped++"]
    I -- Không --> J["part.SubType = GUID Sheet Metal"]
    J --> K["GetInnerFaceAndThickness<br/>2 mặt phẳng song song lớn nhất, đo khoảng cách"]
    K --> L{"Thickness hợp lệ? (0.1 đến 30 mm, có mặt trong)"}
    L -- Không --> L1["Errors++ (ghi log)"]
    L -- Có --> M{"ConvMode"}
    M -- "1" --> M1["UseSheetMetalStyleThickness=false<br/>Thickness.Value = mm / 10"]
    M -- "2" --> M2["FindMatchingSheetMetalRule trong dung sai<br/>rule.Activate, UseSheetMetalStyleThickness=true"]
    M1 --> N["part.Update, tìm lại mặt trong (face cũ đã stale)"]
    M2 --> N
    N --> O["ASideFace = mặt trong"]
    O --> P["Unfold nếu chưa có Flat Pattern"]
    P --> Q["part.Update; Save nếu Dirty"]
    Q --> R["Success++"]
    I1 --> S["Tổng kết"]
    L1 --> S
    R --> S
    S --> T["finally: trả SilentOperation/ScreenUpdating; ghi log nếu bật"]
    T --> U["MessageBox kết quả và hỏi có mở thư mục log không"]
    U --> V["ConvertStepCommand finally: khôi phục tab Ribbon còn thiếu"]
```

- **Có tác dụng phụ lên file thật:** đổi SubType, đổi Thickness, tạo Flat Pattern, **Save**. Không có backup.
- Ghi log mặc định bật để giữ hành vi cũ; có thể tắt trong form. Chỉ hỏi mở thư mục sau khi log đã ghi thành công.
- Đơn vị: API trả **cm**, UI/BOM/tên file dùng **mm** (nhân/chia 10). `SafeThicknessToCm` xử lý style trả chuỗi kèm đơn vị (`in`, `mm`).
- Hủy giữa chừng: các part đã Save trước đó được giữ nguyên.

---

## 4. Luồng Xuất DXF (luồng chính)

`ExportDxfCommand` → `DxfExportServiceImplementation` → `DxfProcessor`

```mermaid
flowchart TD
    A["Bấm Xuất DXF"] --> B{"Part hoặc Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["ExportSettingsStore.Load (JSON)"]
    C --> D["DxfExportForm: thư mục, ghi đè, FastMode, dọn part suppress, cập nhật metadata (mặc định tắt), xuất BOM (tách riêng)"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["ApplyTo + Save JSON"]
    E --> F["CollectItems(document) lần 1"]
    F --> G{"Có part Sheet Metal?"}
    G -- Không --> G1["MessageBox, dừng"]
    G -- Có --> H["ExportPreflightService.Inspect(collection snapshot)<br/>+ cảnh báo suppressed/lỗi duyệt/kích thước/material/thickness/tên file"]
    H --> I{"Có cảnh báo?"}
    I -- Có --> J["PreflightReviewForm"]
    J -- Hủy --> Z
    I -- Không --> K
    J -- Tiếp tục --> K["ExportPreviewForm: chọn/bỏ chọn từng part<br/>nút Cài đặt mở ExportSettingsForm, lưu JSON"]
    K -- Đóng --> Z
    K -- Xuất --> L["OperationProgressForm.Show"]
    L --> M["Export(document, settings, progress, selectedPaths)"]
    M --> N["FastMode: ScreenUpdating=false, SilentOperation=true"]
    N --> O["CollectDocumentItems lần 2 sau preview, lọc theo selectedPaths"]
    O --> P["Với mỗi part"]
    P --> Q["Thư mục = OutputRoot / Vật liệu / Độ dày<br/>Tên = pattern DoDay_Qty_VatLieu_TenChiTiet.dxf"]
    Q --> R{"File đã tồn tại?"}
    R -- "Bỏ qua file đã có" --> R1["skippedExisting++, sang part sau"]
    R -- "Hỏi từng file" --> R2{"Người dùng đồng ý?"}
    R2 -- Không --> R1
    R2 -- Có --> S
    R -- "Ghi đè tự động / chưa có" --> S["Ghi DXF ra file tạm path.tmp_GUID.dxf"]
    S --> T["ExportOnePart (mục 4.1)"]
    T --> U["DxfProcessor.Process: tạm → file đích (mục 5)"]
    U --> W["exported++; xoá file tạm"]
    W --> P
    P -- "Hết part" --> X{"ExportBomAfterDxf?"}
    X -- Có --> X1["Bom.Export (mục 6)"]
    X -- Không --> Y
    X1 --> Y
    Y --> Y1["Xử lý DXF cũ của part suppress: xoá nếu CleanupSuppressed, ngược lại cảnh báo"]
    Y1 --> Y2["Ghi BaoCao_Export_DXF_timestamp.txt vào OutputRoot"]
    Y2 --> Y3["finally: trả FastMode; MessageBox kết quả"]
```

### 4.1 `ExportOnePart`: chuỗi thử ghi DXF

```mermaid
flowchart TD
    A["Part là SheetMetal?"] -- Không --> E0["Lỗi: Không phải Sheet Metal"]
    A -- Có --> B["HasFlatPattern? Nếu chưa thì sm.Unfold (đổi model!)"]
    B --> T1["1. sm.DataIO + R12 options (không LineType)"]
    T1 -- "lỗi hoặc DXF sai cấu trúc" --> T2["2. FlatPattern.DataIO + cùng options"]
    T2 -- lỗi --> E["Ném lỗi gộp mọi lỗi con"]
    T1 -- OK --> OK1["thành công"]
    T2 -- OK --> OK2["thành công; ghi rõ lỗi và provider dự phòng"]
```

Mỗi lần thử được kiểm bằng `DxfProcessor.Validate`; thất bại thì xoá file và thử provider kế. Không thử lại cùng provider với option khác. Kết quả fallback ghi rõ lỗi provider đầu trong báo cáo.

### 4.2 Chuỗi tùy chọn DXF

`DxfLayerOptionsBuilder.Build(settings)` tạo `FLAT PATTERN DXF?AcadVersion=R12&…`:
- 11 layer có khóa cấu hình (Outer, Interior, BendUp, BendDown, ToolCenter, ArcCenters, Tangent, FeatureProfiles, UnconsumedSketches, RollTangent, Roll) ánh xạ sang tên `IV_*`; `IV_MARK_SURFACE` là tùy chọn layer riêng, mặc định tắt và không cần thêm tham số ánh xạ.
- Layer không được tick được đưa vào `InvisibleLayers=a;b;c` và bị loại hậu xử lý khỏi entity/table LAYER của DXF. Vì vậy CAD không còn liệt kê tên layer chưa chọn. Mặc định chỉ bật Outer + Interior. Khi Tool Center tắt, lọc cả `IV_TOOL_CENTER` và biến thể `IV_Tool_center`.
- `&RebaseGeometry=True` ở cuối. `LineType` được bỏ khỏi luồng xuất chính vì log thực tế cho thấy option này khiến 60/60 part phải thử lại.

---

## 5. Hậu xử lý DXF (`Core/DxfProcessor.cs`)

```mermaid
flowchart TD
    A["Đọc file tạm bằng Latin1 (ISO-8859-1), thành danh sách dòng"] --> A1["Loại entity và table LAYER của các layer không chọn"]
    A1 --> B{"BendMode = Chỉ đánh dấu 2 đầu?"}
    B -- Có --> C["ConvertBendLinesToEndMarks"]
    C --> C1["Tìm ENTITIES..ENDSEC, tách entity"]
    C1 --> C2["Gom LINE ở layer IV_BEND_UP / IV_BEND_DOWN<br/>cùng phương, cùng vị trí thành 1 đường chấn"]
    C2 --> C3["Xoá các LINE gốc; tạo lại 2 vạch ngắn ở hai đầu<br/>(đủ chỗ: 2 vạch; ngắn: 1 đoạn giữa; cực ngắn: giữ cả đường)"]
    C3 --> D
    B -- Không --> D{"AddInfoText?"}
    D -- Có --> E["AddInfoTextToDxf"]
    E --> E1["EnsureInfoLayerAndStyle: thêm layer DXF_INFO và style DXF_INFO_STYLE (txt.shx) vào TABLES"]
    E1 --> E2["Quét lại ENTITIES, tính hộp bao"]
    E2 --> E3["Chèn 4 TEXT dưới hình: DO DAY, SO LUONG, VAT LIEU, TEN CHI TIET<br/>(bỏ dấu tiếng Việt, căn trái/giữa, cách Y theo TextYOffset)"]
    E3 --> F
    D -- Không --> F["ValidateDxfLines (SECTION/ENTITIES/ENDSEC/EOF, mã nhóm số, có entity)"]
    F -- Sai --> X["Ném InvalidDataException, file đích giữ nguyên"]
    F -- Đúng --> G["WriteAtomically: ghi file .tmp rồi File.Copy đè file đích"]
```

- Đơn vị DXF là mm (khác với API cm).
- `BendUpLen/Inset`, `BendDownLen/Inset`, `TextHeight`, `TextLineGap`, `TextYOffset` đến từ `ExportSettings`.
- Mã nguồn add-in hiện tại là nguồn chuẩn; không phụ thuộc vào một iLogic source bên ngoài.

---

## 6. Luồng Xuất BOM

`ExportBomCommand` → `BomServiceImplementation` → `BomTemplateWriter`

```mermaid
flowchart TD
    A["Bấm Xuất BOM"] --> B{"Part hoặc Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["BomExportSettingsStore.Load (JSON)"]
    C --> D["BomForm: file .xlsx mẫu, thư mục lưu, ô Tên dự án/Ngày, dòng bắt đầu, ánh xạ cột"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["Save settings"]
    E --> F["CollectItems(document, monitor), cập nhật tiến trình/hủy"]
    F --> G["Bom.Export(items, settings, tên file tài liệu, monitor)"]
    G --> H["Với mỗi item: dài x rộng x độ dày; bỏ đơn vị mm, dùng dấu phẩy thập phân"]
    H --> I{"Cột Hình ảnh bật?"}
    I -- Có --> J["ReadThumbnail: dùng part đang mở, hoặc mở ẩn rồi Close(SkipSave=true)<br/>Document.Thumbnail → PNG"]
    I -- Không --> K
    J --> K["BomTemplateRow(STT, Tên, Kích thước, Vật liệu, SL, ảnh)"]
    K --> L["BomTemplateWriter.Write: mở bản sao mẫu, worksheet đầu tiên<br/>ghi từ StartRow theo ánh xạ cột; ghi ngày dd/MM/yyyy và tên dự án vào ô tiêu đề nếu đã cấu hình"]
    L --> M["Lưu BOM_yyyyMMdd_HHmmss_fff.xlsx vào thư mục lưu; file mẫu không bị sửa"]
```

Cột mặc định: STT=A, Tên chi tiết=C, Hình ảnh=D, Kích thước=E, Vật liệu=K, Số lượng=M. Địa chỉ ô tiêu đề cho ngày và tên dự án cấu hình độc lập; để trống thì không ghi. Tên dự án lấy từ tên file tài liệu không gồm phần mở rộng. Các ô khác của mẫu được giữ nguyên. Tiến trình có thể hủy giữa các mục; `SaveAs` của ClosedXML là đồng bộ nên không báo tiến trình giữa lúc lưu.

---

## 7. Thu thập part (`CollectItems`)

```mermaid
flowchart TD
    A["Document"] --> B{"Loại"}
    B -- Part --> C["AddPartItem"]
    B -- Assembly --> D["ScanOccurrences(Occurrences) đệ quy"]
    B -- Khác --> E["skipped: chỉ hỗ trợ Part/Assembly"]
    D --> F{"occurrence.Suppressed?"}
    F -- Có --> G["skipped + ghi nhớ part Sheet Metal để dọn DXF cũ"]
    F -- Không --> H{"DefinitionDocumentType"}
    H -- Assembly --> D2["ScanOccurrences(SubOccurrences)"]
    H -- Part --> C
    C --> I{"Là SheetMetalComponentDefinition và đã Save?"}
    I -- Không --> I1["skipped"]
    I -- Có --> J{"FullFileName đã có trong từ điển?"}
    J -- Có --> J1["Quantity += 1"]
    J -- Không --> J2["Tạo ExportItem: tên, vật liệu, độ dày, kích thước Flat Pattern nếu đã có"]
```

- Khóa duy nhất là `FullFileName` (so sánh không phân biệt hoa thường) → mỗi part một lần, `Quantity` = số occurrence.
- **Không tạo Flat Pattern** ở bước này; chưa có thì kích thước = "Chưa có Flat Pattern". `Unfold` chỉ xảy ra trong `ExportOnePart`.
- Kích thước = hai cạnh lớn nhất của `FlatPattern.RangeBox` (đổi database unit → mm).
- Độ dày: `"T" + Thickness.Value × 10` (mm). Vật liệu: `ActiveMaterial.DisplayName`, mặc định `Khong_Xac_Dinh`.

---

## 8. Cài đặt lưu ở đâu

| Nơi | Đường dẫn | Nội dung |
|---|---|---|
| `ExportSettingsStore` | `%AppData%\Inventor_DXF_Exporter\ExportSettings.json` | Cài đặt DXF/Convert dạng JSON |
| `SettingsProfileStore` | `%AppData%\Inventor_DXF_Exporter\Profiles\<tên>.json` | Cấu hình theo khách hàng/dự án (nạp/lưu/xoá trong ExportSettingsForm) |
| `BomExportSettingsStore` | `%AppData%\Inventor_DXF_Exporter\BomExportSettings.json` | Mẫu Excel, thư mục lưu, dòng bắt đầu, ánh xạ cột; có `SchemaVersion` để migrate |

Các thư mục build (`bin/`, `obj/`, `dist/`) là output cục bộ, không phải nguồn phát hành. Luồng phát hành duy nhất là MSI; `build-installer.ps1` giữ output trung gian trong thư mục tạm và chỉ xuất MSI vào `dist/`.
`Examples/AssemblyImageExport.iLogicVb` là rule iLogic độc lập để tham khảo, không được add-in sử dụng hay đóng gói.

---

## 9. Build, đóng gói, test

| Việc | Lệnh / file |
|---|---|
| Build và đóng gói MSI per-user (WiX 4) | `.\build-installer.ps1` → `dist\InventorDxfExporter-Setup.msi`; cùng UpgradeCode, đóng Inventor trước khi cài |
| Test không cần Inventor | `dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj` (validate DXF, layer options, JSON settings, BOM template/metadata/dimensions) |
| Xem trước form | `.\Preview-Settings.ps1` (thư mục `Preview/` có thể không nằm trong bản đang xem) |
| Manifest cài cùng MSI | `InventorDxfExporter.addin`: `LoadAutomatically=1`, `LoadBehavior=1`, `UseInventorAssemblyContext` đang để mặc định |

Phụ thuộc: ClosedXML 0.104.2 và các DLL phụ được đóng gói trong MSI; `Autodesk.Inventor.Interop.dll` và `stdole.dll` được tham chiếu từ `InventorBinPath` (`EmbedInteropTypes=false`, `Private=false`).

---

## 10. Điểm cần nhớ khi sửa code

- Chỉ dùng API Inventor đã xác minh theo `Skill/AGENTS.md` và `Skill/references/API_VERIFICATION.md`. Ghi rõ cái gì build được, cái gì chưa chạy trong Inventor.
- Bộ sưu tập COM đánh chỉ số từ **1**. Đơn vị database = cm.
- Convert STEP có tác dụng phụ lên model/file: đổi `SubType`, đặt Thickness, tạo Flat Pattern và Save part. Xuất DXF có thể tạo Flat Pattern trong phiên làm việc nhưng không cập nhật Description/Thickness và không gọi Save part.
- Mọi biến đổi DXF qua file tạm; file đích chỉ bị thay sau khi validate xong.
- Giữ trả lại `SilentOperation`/`ScreenUpdating` trong `finally`.
- Tên người dùng thấy (form, báo cáo, log) là tiếng Việt.

## 11. Kết quả rà soát ngày 15/11/2026

- Đã loại `LineType` khỏi option DXF chính và chỉ thử từng provider một lần; fallback provider/lỗi được ghi trong báo cáo.
- Tên chi tiết lấy từ tên file, không từ `DisplayName` có thể gắn Model State.
- Xuất DXF không cập nhật Description/Thickness và không lưu part nguồn. BOM sau DXF là lựa chọn riêng, mặc định tắt.
- Tiến trình vẫn dùng `DoEvents` để cập nhật UI, nhưng chặn lệnh Ribbon chạy lồng nhau.
- Ribbon dùng `firstTime`, kiểm tra tab hiện hữu và tái tạo tab thiếu sau `OnResetRibbonInterface`.
- BOM và Description đọc chung kích thước từ `FlatPattern.RangeBox`; khi xuất DXF tạo Flat Pattern mới, dữ liệu BOM được làm mới sau khi Unfold.
- Preflight dùng danh sách đã thu thập, tránh lượt duyệt assembly COM thứ hai. Lượt export vẫn duyệt lại sau bước preview để lấy trạng thái tài liệu hiện thời.
- Đã thêm kiểm tra hậu xử lý bend/TEXT trên DXF fixture có TABLES. Hình học của các đoạn bend rời không được thay đổi theo yêu cầu người dùng.
- Build add-in, test tự động và MSI đã chạy thành công trên mã nguồn hiện tại. WiX build không còn cảnh báo: bỏ override `REINSTALLMODE` để xử lý ICE40, không cho nâng cấp cùng phiên bản để xử lý ICE61, và suppress ICE91 vì gói cố ý cài vào thư mục Add-ins per-user khớp với `Scope="perUser"`. Người dùng xác nhận add-in đã chạy thử thành công trong Inventor 2027; cần xác nhận việc đọc/ghi cấu hình JSON và cài/nâng cấp/gỡ MSI trên host/máy thử sạch trước khi phát hành.
