# DXF Exporter Pro — Kiến trúc và luồng hoạt động

Add-in C# / .NET 10 (`net10.0-windows`) cho **Autodesk Inventor 2027**, chuyển từ iLogic V14 (`Source/V14_Original.iLogicVb`, **không sửa**, dùng làm chuẩn đối chiếu hành vi).

Ba chức năng, hiện trên Ribbon:

| Nút | Ngữ cảnh | Việc làm |
|---|---|---|
| Convert STEP | Chỉ Assembly | Đổi các part import (STEP) sang Sheet Metal, đặt Thickness, gán A-Side, tạo Flat Pattern, **Save part** |
| Xuất DXF | Part, Assembly | Xuất DXF từ Flat Pattern, hậu xử lý DXF, (tuỳ chọn) cập nhật Description/Thickness và xuất BOM |
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
    SET["Settings/*<br/>INI V14, profile JSON, BOM JSON"]

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
    R->>R: AddTab Assembly "DXF Exporter" (V4): panel Thao tác = 3 nút
    R->>R: AddTab Part "DXF Exporter" (V4): panel Xuất = 2 nút
    Note over R: firstTime bị bỏ qua. Tab đã tồn tại thì COMException bị bắt và ghi Trace
    Note over S: Lỗi trong Activate thì Deactivate rồi ném lại
    I->>S: Deactivate()
    S->>R: gỡ OnExecute handler, xoá danh sách
```

- `ClassId`/`ClientId` trong `.addin` = `[Guid]` của class server = `6B93E5C1-9A6A-4D1B-8FA9-47567B4A1D20` (cũng là `ClientId` trong `RibbonCommandSet`). **Phải khớp nhau.**
- Mỗi `*Command` tự tạo một `ServiceContainer` riêng (3 bản, trạng thái không chia sẻ).
- Khi đổi bố cục Ribbon: tăng hậu tố phiên bản (`V4` → `V5`) và thêm tên cũ vào `retiredTabNames`. Xoá tab chỉ theo `InternalName`, không theo tên hiển thị.
- `Define` giữ `ButtonDefinitionSink_Event` + handler trong `_subscriptions` để không bị GC và gỡ được khi `Deactivate`. Lỗi trong lệnh được bắt và hiện MessageBox, không để lọt qua ranh giới COM.

---

## 3. Luồng Convert STEP

`ConvertStepCommand` → `ConvertServiceImplementation`

```mermaid
flowchart TD
    A["Bấm Convert STEP"] --> B{"ActiveDocument là Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["LegacyIniSettingsStore.Load"]
    C --> D["ConvertStepForm: lọc tên, chế độ 1/2, bỏ qua part đã có Flat Pattern, dung sai, thư mục log"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["ApplyTo + Save INI"]
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
    S --> T["finally: trả SilentOperation/ScreenUpdating; ghi SM_Log_yyyyMMdd_HHmmss.txt"]
    T --> U["MessageBox kết quả"]
```

- **Có tác dụng phụ lên file thật:** đổi SubType, đổi Thickness, tạo Flat Pattern, **Save**. Không có backup.
- Đơn vị: API trả **cm**, UI/BOM/tên file dùng **mm** (nhân/chia 10). `SafeThicknessToCm` xử lý style trả chuỗi kèm đơn vị (`in`, `mm`).
- Hủy giữa chừng: các part đã Save trước đó được giữ nguyên.

---

## 4. Luồng Xuất DXF (luồng chính)

`ExportDxfCommand` → `DxfExportServiceImplementation` → `DxfProcessor`

```mermaid
flowchart TD
    A["Bấm Xuất DXF"] --> B{"Part hoặc Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["Load INI"]
    C --> D["DxfExportForm: thư mục xuất, kiểu ghi đè, FastMode, xoá DXF cũ của part suppress, cập nhật thuộc tính và BOM"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["ApplyTo + Save INI"]
    E --> F["CollectItems(document) lần 1"]
    F --> G{"Có part Sheet Metal?"}
    G -- Không --> G1["MessageBox, dừng"]
    G -- Có --> H["ExportPreflightService.Inspect<br/>+ cảnh báo vật liệu/độ dày không xác định<br/>+ cảnh báo trùng tên file dự kiến"]
    H --> I{"Có cảnh báo?"}
    I -- Có --> J["PreflightReviewForm"]
    J -- Hủy --> Z
    I -- Không --> K
    J -- Tiếp tục --> K["ExportPreviewForm: chọn/bỏ chọn từng part<br/>nút Cài đặt mở ExportSettingsForm, lưu INI"]
    K -- Đóng --> Z
    K -- Xuất --> L["OperationProgressForm.Show"]
    L --> M["Export(document, settings, progress, selectedPaths)"]
    M --> N["FastMode: ScreenUpdating=false, SilentOperation=true"]
    N --> O["CollectDocumentItems lần 2, lọc theo selectedPaths"]
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
    U --> V{"UpdateInfoAndBom?"}
    V -- Có --> V1["PartInfo.UpdateFromFlatPattern: Description, Thickness, Save part"]
    V -- Không --> W
    V1 --> W["exported++; xoá file tạm"]
    W --> P
    P -- "Hết part" --> X["Nếu UpdateInfoAndBom: Bom.Export (mục 6)"]
    X --> Y["Xử lý DXF cũ của part suppress: xoá nếu CleanupSuppressed, ngược lại cảnh báo"]
    Y --> Y1["Ghi BaoCao_Export_DXF_timestamp.txt vào OutputRoot"]
    Y1 --> Y2["finally: trả FastMode; MessageBox kết quả"]
```

### 4.1 `ExportOnePart`: chuỗi thử ghi DXF

```mermaid
flowchart TD
    A["Part là SheetMetal?"] -- Không --> E0["Lỗi: Không phải Sheet Metal"]
    A -- Có --> B["HasFlatPattern? Nếu chưa thì sm.Unfold (đổi model!)"]
    B --> C["Kích hoạt translator DXF {C24E3AC4-...} nếu chưa Activated"]
    C --> T1["1. sm.DataIO + tùy chọn đầy đủ (có LineType)"]
    T1 -- "lỗi hoặc DXF sai cấu trúc" --> T2["2. sm.DataIO + tùy chọn tối giản (không LineType)"]
    T2 -- lỗi --> T3["3. FlatPattern.DataIO + tùy chọn đầy đủ"]
    T3 -- lỗi --> T4["4. FlatPattern.DataIO + tùy chọn tối giản"]
    T4 -- lỗi --> E["Ném lỗi gộp mọi lỗi con"]
    T1 -- OK --> OK1["thành công, usedFallback=false"]
    T2 -- OK --> OK2["thành công, usedFallback=true (log: tùy chọn DXF tối giản)"]
    T3 -- OK --> OK1
    T4 -- OK --> OK2
```

Mỗi lần thử được kiểm bằng `DxfProcessor.Validate`; thất bại thì xoá file và thử cách kế. Lỗi của các lần thử không được log nếu cuối cùng có một cách thành công.

### 4.2 Chuỗi tùy chọn DXF

`DxfLayerOptionsBuilder.Build(settings, includeLineTypes)` tạo `FLAT PATTERN DXF?AcadVersion=R12&…`:
- 11 layer (Outer, Interior, BendUp, BendDown, ToolCenter, ArcCenters, Tangent, FeatureProfiles, UnconsumedSketches, RollTangent, Roll) ánh xạ khóa → tên layer `IV_*`.
- Layer không được tick vào `InvisibleLayers=a;b;c`. Mặc định chỉ bật Outer + Interior.
- `&RebaseGeometry=True` ở cuối. `LineType` chỉ có ở bản đầy đủ.

---

## 5. Hậu xử lý DXF (`Core/DxfProcessor.cs`)

```mermaid
flowchart TD
    A["Đọc file tạm bằng Latin1 (ISO-8859-1), thành danh sách dòng"] --> B{"BendMode = Chỉ đánh dấu 2 đầu?"}
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
- Port nguyên từ V14: khi sửa phải so với `Source/V14_Original.iLogicVb` (các hàm cùng tên).

---

## 6. Luồng Xuất BOM

`ExportBomCommand` → `BomServiceImplementation` → `BomTemplateWriter`

```mermaid
flowchart TD
    A["Bấm Xuất BOM"] --> B{"Part hoặc Assembly?"}
    B -- Không --> B1["MessageBox, dừng"]
    B -- Có --> C["BomExportSettingsStore.Load (JSON)"]
    C --> D["BomForm: file .xlsx mẫu, thư mục lưu, dòng bắt đầu (mặc định 11), ánh xạ cột"]
    D -- Hủy --> Z["Kết thúc"]
    D -- OK --> E["Save settings"]
    E --> F["CollectItems(document), không có bước chọn part"]
    F --> G["Bom.Export(items, settings)"]
    G --> H["Với mỗi item: Kích thước từ FlatPatternXmm × FlatPatternYmm"]
    H --> I{"Cột Hình ảnh bật?"}
    I -- Có --> J["ReadThumbnail: dùng part đang mở, hoặc mở ẩn rồi Close(SkipSave=true)<br/>Document.Thumbnail → PNG"]
    I -- Không --> K
    J --> K["BomTemplateRow(STT, Tên, Kích thước, Vật liệu, SL, ảnh)"]
    K --> L["BomTemplateWriter.Write: mở bản sao mẫu, worksheet đầu tiên<br/>ghi từ StartRow theo ánh xạ cột"]
    L --> M["Lưu BOM_yyyyMMdd_HHmmss_fff.xlsx vào thư mục lưu; file mẫu không bị sửa"]
```

Cột mặc định: STT=A, Tên chi tiết=C, Hình ảnh=D, Kích thước=E, Vật liệu=K, Số lượng=M. Các ô khác của mẫu (tên dự án, ngày…) được giữ nguyên. Trường BOM lạ thì báo lỗi.

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
- **Không tạo Flat Pattern** ở bước này; chưa có thì kích thước = "Chưa có Flat Pattern". `Unfold` chỉ xảy ra trong `ExportOnePart` hoặc `UpdateFromFlatPattern`.
- Kích thước = hai cạnh lớn nhất của `FlatPattern.RangeBox` (đổi database unit → mm).
- Độ dày: `"T" + Thickness.Value × 10` (mm). Vật liệu: `ActiveMaterial.DisplayName`, mặc định `Khong_Xac_Dinh`.

---

## 8. Cài đặt lưu ở đâu

| Nơi | Đường dẫn | Nội dung |
|---|---|---|
| `LegacyIniSettingsStore` | `%AppData%\Inventor_DXF_Export_Form.ini` | Cài đặt DXF/Convert, tương thích V14 (key=value, `Layer.*`, `LayerStyle.*`) |
| `SettingsProfileStore` | `%AppData%\Inventor_DXF_Exporter\Profiles\<tên>.json` | Cấu hình theo khách hàng/dự án (nạp/lưu/xoá trong ExportSettingsForm) |
| `BomExportSettingsStore` | `%AppData%\Inventor_DXF_Exporter\BomExportSettings.json` | Mẫu Excel, thư mục lưu, dòng bắt đầu, ánh xạ cột; có `SchemaVersion` để migrate |

---

## 9. Build, đóng gói, test

| Việc | Lệnh / file |
|---|---|
| Build + gom DLL | `.\build-and-package.ps1` → `dist\InventorDxfExporter` (copy cả thư mục vào `%APPDATA%\Autodesk\Inventor 2027\Addins`) |
| MSI per-user (WiX 4) | `.\build-installer.ps1` → `dist\InventorDxfExporter-Setup.msi`; cùng UpgradeCode, đóng Inventor trước khi cài |
| Test không cần Inventor | `dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj` (validate DXF, layer options, BOM template, migrate settings) |
| Xem trước form | `.\Preview-Settings.ps1` (thư mục `Preview/` có thể không nằm trong bản đang xem) |
| Manifest | `InventorDxfExporter.addin`: `LoadAutomatically=1`, `LoadBehavior=1`, `UseInventorAssemblyContext` đang để mặc định |

Phụ thuộc: ClosedXML 0.104.2 (kèm 6 DLL phụ phải nằm cùng thư mục add-in), `Autodesk.Inventor.Interop.dll` và `stdole.dll` từ `InventorBinPath` (`EmbedInteropTypes=false`, `Private=false`).

---

## 10. Điểm cần nhớ khi sửa code

- Chỉ dùng API Inventor đã xác minh theo `Skill/AGENTS.md` và `Skill/references/API_VERIFICATION.md`. Ghi rõ cái gì build được, cái gì chưa chạy trong Inventor.
- Bộ sưu tập COM đánh chỉ số từ **1**. Đơn vị database = cm.
- Tác dụng phụ lên model/file: `Unfold`, `part.Save()` (Convert, `UpdateFromFlatPattern`), `part.SubType`. Đừng thêm Save ngầm mới.
- Mọi biến đổi DXF qua file tạm; file đích chỉ bị thay sau khi validate xong.
- Giữ trả lại `SilentOperation`/`ScreenUpdating` trong `finally`.
- Tên người dùng thấy (form, báo cáo, log) là tiếng Việt; tên layer/key INI giữ nguyên để tương thích V14.

## 11. Vấn đề đã biết (từ lần kiểm tra ngày 14/11/2026)

1. Báo cáo thực tế cho thấy 60/60 file đều chạy bằng "tùy chọn DXF tối giản": lần thử 1 (có `LineType`) luôn lỗi nhưng lỗi bị nuốt, tốn thời gian (xuất DXF 50,8 s/60 part).
2. `DetailName` lấy từ `part.DisplayName` nên tên file/BOM có hậu tố `(Default)` của Model State.
3. `UpdateInfoAndBom` mặc định `true` nên xuất DXF sẽ Unfold + Save part gốc không backup.
4. Có thể bấm lệnh Ribbon lần hai trong lúc đang chạy (do `DoEvents`).
5. `firstTime` bị bỏ qua và chưa xử lý `OnResetRibbonInterface`.
6. Kích thước BOM (`RangeBox`) và Description (`FlatPattern.Length × Width`) lấy từ hai nguồn khác nhau.
7. `CollectItems` chạy 2–3 lần trên assembly lớn; preflight dùng `AllLeafOccurrences` còn export dùng đệ quy `SubOccurrences`.
8. Chưa có test cho `ConvertBendLinesToEndMarks` và `AddInfoTextToDxf` với DXF thật có TABLES.
9. README còn ghi "chưa compile / chưa chạy trong Inventor"; cần cập nhật theo thực tế.
