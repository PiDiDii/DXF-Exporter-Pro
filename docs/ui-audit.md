# Audit giao diện WinForms — InventorDxfExporter

## Tóm tắt điều hành

- Audit ban đầu ghi nhận **0 phát hiện Cao, 5 Trung bình, 4 Thấp**; các điểm có thể sửa từ mã đã được xử lý.
- Dialog/progress của add-in hiện dùng owner lấy từ `Application.MainFrameHWND` của Inventor 2027.
- Các form chính có thể co giãn/cuộn và có kích thước khởi tạo nhỏ hơn; DPI/màn hình thực tế chưa được kiểm chứng trực quan.
- BOM mapping dựng từ cấu hình, cuộn theo số hàng và hỗ trợ thêm tên file, đường dẫn file, độ dày xuyên suốt writer.
- Progress vẫn bơm message để nhận nút Hủy; khi bơm, cửa sổ Inventor bị vô hiệu hóa tạm thời và cập nhật lồng bị chặn.
- Bộ parse số chấp nhận dấu thập phân theo locale hiện hành hoặc invariant; đã có kiểm thử tự động cho `2,5` và `2.5` dưới `vi-VN`.
- Không có ảnh chụp trong `docs/ui-audit/`; DPI, font lớn, bố cục thực tế và owner runtime vẫn cần xác minh trong Preview/Inventor.

## Trạng thái khắc phục

| ID | Trạng thái | Thay đổi |
|---|---|---|
| UI-01 | Đã xử lý mã | Thêm `InventorWindowOwner`, truyền owner cho dialog, progress và thông báo từ các lệnh. Z-order/focus cần chạy thử trong Inventor. |
| UI-02 | Đã xử lý mã, chờ kiểm tra trực quan | Giảm kích thước khởi tạo/tối thiểu, cho phép resize Convert/DXF, bổ sung vùng cuộn cho nội dung dài và profile. |
| UI-03 | Đã xử lý và có test | BOM hiển thị mapping chuẩn trong DataGridView một hàng cho mỗi trường; checkbox, tên trường và cột Excel cùng hàng; chỉ cột Excel có thể sửa; có Tên file, Đường dẫn file, Độ dày. |
| UI-04 | Đã giảm rủi ro | Giữ `DoEvents` để hỗ trợ hủy, tạm vô hiệu hóa HWND Inventor khi pump và chặn gọi cập nhật lồng; test runtime vẫn cần. |
| UI-05 | Đã xử lý và có test | `UiNumberParser` nhận decimal separator theo culture hiện tại hoặc invariant. |
| UI-06 | Đã xử lý mã | `ExportSettingsForm` chỉnh working copy; chỉ `ApplyTo` sau khi người gọi nhận OK. |
| UI-07 | Đã xử lý mã | Dùng chung field grid, label, folder picker, footer và style nút qua `UiStyles`. |
| UI-08 | Đã xử lý mã | Dispose `ToolTip` cùng form; dùng font dùng chung thay vì tạo lặp trong mỗi control. |
| UI-09 | Đã xử lý và có test | Layer metadata, formatter độ dày và parser số chuyển về Core và được kiểm thử độc lập. |

## Bảng phát hiện ban đầu

| ID | Mức độ | Form | Nhóm | Mô tả | Bằng chứng (file:dòng) | Cách sửa đề xuất | Công sức |
|---|---|---|---|---|---|---|---|
| UI-01 | Trung bình | ConvertStep, DXF, BOM, Preflight, Preview, Progress | E | Các lệnh add-in gọi `ShowDialog()`/`Show()` không truyền owner. Riêng progress là modeless; form đặt `CenterParent` nhưng khi gọi không có parent. Điều này có thể làm dialog không gắn/định vị đúng với cửa sổ Inventor hoặc bị khuất; chưa kiểm tra runtime. | `Commands/ConvertStepCommand.cs:27,33`; `Commands/ExportBomCommand.cs:27,32`; `Commands/ExportDxfCommand.cs:40,68,73,85` | Tạo adapter owner `IWin32Window` cho cửa sổ Inventor và truyền owner thống nhất; kiểm tra lại thứ tự cửa sổ, focus và modal/modeless trong Inventor 2027. | M |
| UI-02 | Trung bình | ConvertStep, DxfExport, OperationProgress, ExportSettings, BOM | A | Các form có kích thước cố định hoặc `MinimumSize` đáng kể: Convert 760×570 và DXF 720×440 là `FixedDialog`; Progress 560×310 là `FixedDialog`; Settings có min 900×750; BOM min 720×730. Các layout không khai báo `AutoScroll` cho form. Chưa xác minh chúng có tràn màn hình/cắt nội dung ở 1366×768 hoặc scale cao. | `UI/Forms/ConvertStepForm.cs:37-40`; `DxfExportForm.cs:20-23`; `OperationProgressForm.cs:27-32`; `ExportSettingsForm.cs:41-45`; `BomForm.cs:30-34` | Sau khi người dùng kiểm tra trực quan, giảm min size nơi cần; ưu tiên hàng/cột Percent, vùng cuộn cho nội dung dài và bảo đảm footer luôn truy cập được. | M |
| UI-03 | Trung bình | BomForm | A, D | Chiều cao các vùng file, metadata và cột BOM được đặt hằng số; vùng cột cao 252 px nhưng số hàng lấy từ `CreateDefaultColumns()`. UI chỉ lặp danh sách mặc định và khi áp dụng sẽ thay `_settings.Columns` bằng các mapping đang hiển thị, nên mapping bổ sung ngoài danh sách mặc định không được trình bày/giữ lại. Cấu hình trường BOM nhiều hơn hiện tại chưa được hỗ trợ tổng quát. | `UI/Forms/BomForm.cs:57-63,225-263,354-355`; `Core/BomExportSettings.cs:14-22` | Sinh UI trực tiếp từ mappings cấu hình, tính chiều cao/scroll theo số trường; tách metadata nghiệp vụ khỏi form nếu muốn cấu hình thêm field linh hoạt. | M |
| UI-04 | Trung bình | OperationProgressForm | C | `Report` cập nhật UI rồi gọi `Application.DoEvents()`. Đây là điểm cho phép message/control event được xử lý lồng trong khi caller còn ở giữa thao tác. `RibbonCommandSet` có khóa chống chạy lại command Ribbon, nhưng không loại bỏ mọi dạng tái nhập từ message loop. | `UI/Forms/OperationProgressForm.cs:84-93`; `Commands/RibbonCommandSet.cs:20,161-176` | Ưu tiên cơ chế cập nhật UI không dùng `DoEvents` hoặc đặt rõ trạng thái busy/khóa thao tác trong thời gian pump message; xác minh luồng COM vẫn trên STA. | M |
| UI-05 | Trung bình | ExportSettingsForm, ConvertStepForm | C | Convert dùng `NumericUpDown` cho tolerance, còn thông số DXF dùng `TextBox` và parse theo `CurrentCulture` với `AllowDecimalPoint`. Vì vậy chuỗi thập phân hợp lệ phụ thuộc locale; trong locale dùng dấu phẩy, `2.5` không được parse theo cấu hình parser hiện tại. Chưa thử nhập trực tiếp trên máy/locale thật. | `UI/Forms/ConvertStepForm.cs:20-27`; `ExportSettingsForm.cs:385-422` | Chuẩn hóa parser và thông báo định dạng nhập; cân nhắc dùng `NumericUpDown` có min/max/decimal places cho các số có miền xác định. | S |
| UI-06 | Thấp | ExportSettingsForm | C | Form giữ tham chiếu trực tiếp đến `ExportSettings`; nạp profile ghi trực tiếp vào đối tượng này, và lưu profile gọi `ApplyTo(_settings)` trước khi form đóng/được caller xác nhận. Nút Hủy không rollback các thay đổi đã áp dụng trong bộ nhớ. Caller hiện thường lưu cài đặt chính sau khi dialog được chấp nhận, nhưng đối tượng dùng chung trong luồng gọi đã bị sửa trước đó. | `UI/Forms/ExportSettingsForm.cs:20,119-135,298-335`; `Settings/SettingsProfileStore.cs:24-34`; `Commands/ExportDxfCommand.cs:29-43` | Dùng working copy cho thao tác form; chỉ copy về đối tượng caller sau xác nhận. Tách rõ hành vi lưu profile (tác động bền vững riêng) khỏi chỉnh cài đặt phiên hiện tại. | M |
| UI-07 | Thấp | ConvertStep, DxfExport, BOM, Settings | B | Có mẫu dựng lặp: `CreateFieldGrid`/`CreateFieldLabel` tương tự ở Convert và DXF; chọn thư mục lặp với `FolderBrowserDialog` ở Convert/DXF/BOM; footer BOM tự dựng chiều trái→phải trong khi các form khác dùng `UiStyles.CreateFooter` (phải→trái). Một số nút trong Settings tạo `Button` trực tiếp, bỏ qua style helper. | `UI/Forms/ConvertStepForm.cs:115-125,156-178`; `DxfExportForm.cs:64-80,122,144-169`; `BomForm.cs:113-148,72-88`; `ExportSettingsForm.cs:146-159,367-368`; `UiStyles.cs:76-111` | Gom `PathPickerField`, field-grid/label và kiểu nút/footer; thống nhất thứ tự nút chính và nút đóng/hủy. | M |
| UI-08 | Thấp | DxfExport, UiStyles, Progress, Preflight, BOM | B | `ToolTip` được tạo biến cục bộ trong form nhưng không được dispose trực tiếp; `Font` được tạo nhiều lần trong style helper và một số form. Đây là điểm cần rà vòng đời tài nguyên khi form mở/đóng lặp, chưa quan sát thấy rò rỉ runtime. | `UI/Forms/DxfExportForm.cs:30-33`; `UI/Forms/UiStyles.cs:19,46,125`; `OperationProgressForm.cs:58`; `PreflightReviewForm.cs:89,131`; `BomForm.cs:455` | Quản lý component trong container/dispose của form; chia sẻ font dùng chung hoặc bảo đảm dispose theo ownership đã xác định. | S |
| UI-09 | Thấp | ExportSettingsForm, ExportPreviewForm | D | Danh sách layer mặc định và mô tả nằm trực tiếp trong form; form cũng chứa logic chuyển mode và parse/format số. Preview tự format độ dày và sentinel. Đây là coupling UI–nghiệp vụ, làm khó unit-test riêng và dễ phân tán quy ước. | `UI/Forms/ExportSettingsForm.cs:10-18,119-135,274-289,397-422`; `UI/Forms/ExportPreviewForm.cs:240-255` | Chuyển danh sách/options và formatter/validator dùng chung về Core hoặc presentation model có test độc lập; giữ form tập trung thu thập/hiển thị input. | M |

## Kết quả theo form

**Lưu ý chung:** mọi kiểm thử trực quan ở 100%, 125%, 150%, 175%, kích thước nhỏ nhất, 1366×768, font hệ thống lớn và dữ liệu cực trị đều **được bỏ qua theo yêu cầu của người dùng**. Không có ảnh chụp; các ghi chú dưới đây chỉ mô tả cấu hình thấy trong mã, không phải kết quả hiển thị thực tế.

| Form | Bằng chứng cấu hình tĩnh | Tình trạng quan sát |
|---|---|---|
| `ConvertStepForm` | 680×460, sizable, minimum 620×420; nội dung scroll; có `NumericUpDown` cho tolerance. | Không chạy Preview. DPI, font lớn, chuỗi lọc dài và vị trí control chưa xác minh; chưa có ảnh. |
| `DxfExportForm` | 700×420, sizable, minimum 620×380; vùng nội dung scroll; `ToolTip` được dispose cùng form. | Không chạy Preview. Cắt nội dung, tooltip và co giãn ở màn hình nhỏ chưa xác minh; chưa có ảnh. |
| `ExportPreviewForm` | 820×480, sizable, minimum 700×420; hiển thị items bằng `DataGridView` cuộn được, cột Fill. | Không thử 0/200 items hoặc chuỗi dài; cách cột co giãn và footer ở min size chưa xác minh trực quan; chưa có ảnh. |
| `OperationProgressForm` | 500×260, `FixedDialog`; label `AutoEllipsis`; progress bar 0–100; owner HWND bị khóa tạm trong message pump. | Không thử chuỗi stage/item dài hoặc hủy trên Preview; DPI/owner thực tế chưa xác minh; chưa có ảnh. |
| `PreflightReviewForm` | 820×480, minimum 700×420, sizable; `SplitContainer` distance 170, panel min 120/90; bảng và ListBox dock Fill. | Không thử danh sách rỗng/200 dòng/cảnh báo dài; splitter ở min size và DPI chưa xác minh; chưa có ảnh. |
| `BomForm` | ClientSize cố định 760×680, không phóng to/thu nhỏ; file mẫu/thư mục ở phía trên, bảng mapping bên trái dùng DataGridView với một hàng cho mỗi trường, cấu hình tên dự án/ngày/dòng bắt đầu bên phải; loại trường Nhà cung cấp. | Chưa mở Preview để kiểm tra căn chỉnh trực quan, DPI, font lớn hoặc đường dẫn dài; chưa có ảnh. |
| `ExportSettingsForm` | 700×460, minimum 660×440, sizable; tab/profile hỗ trợ scroll; làm việc trên settings copy. | Không mở Preview, không thử 125–175%, font lớn hay giá trị chuỗi dài; co giãn tab ở min size chưa xác minh; chưa có ảnh. |
| `UiStyles` | Đặt `AutoScaleMode.Dpi`, dùng font dùng chung và factory cho field grid/label/folder picker/footer/button. Preview standalone gọi `ApplicationConfiguration.Initialize()`. | DPI awareness của Inventor host và khác biệt Preview standalone so với add-in chưa xác minh; add-in không tự cấu hình DPI awareness của tiến trình Inventor. |

## Mã lặp cần gom

- Field grid/label và chọn folder trong Convert/DXF/BOM: đã gom vào `UiStyles.CreateFieldGrid`, `CreateFieldLabel` và `CreateFolderPicker`.
- Footer BOM và các form nhập liệu: đã dùng `UiStyles.CreateFooter` với nút chính đặt cùng vị trí; footer Preview giữ riêng vì có vùng tóm tắt selection.
- Buttons và fonts trong Settings/Progress/Preflight/BOM: đã dùng factory/nền font dùng chung; Preview dashboard độc lập không đổi.
- Validation số: Convert tiếp tục dùng `NumericUpDown`; các `TextBox` số trong Settings dùng `UiNumberParser` hỗ trợ culture locale và invariant.

## Kiến nghị thứ tự sửa

1. **Đã hoàn thành:** owner, UI helpers, parser, working copy, resource lifetime, shared metadata/formatters, BOM mapping/writer, layout co giãn cơ bản.
2. **Cần xác minh trong Preview/Inventor:** 100/125/150/175% DPI, 1366×768, min size, font lớn, 0/200 dòng, chuỗi cực dài, owner/z-order/focus và thao tác Hủy progress.
3. Nếu kiểm tra trực quan phát hiện cắt/tràn hoặc thứ tự focus chưa đúng, ghi ảnh vào `docs/ui-audit/` và cập nhật phần kết quả theo form.

## Mục chưa xác minh

- Không chạy UI Preview và không tạo ảnh theo giới hạn kiểm thử; chưa kết luận control có thực sự bị che/cắt, khoảng trắng thừa hay form tràn.
- Chưa thử 100/125/150/175%, 1366×768, kích thước nhỏ nhất hoặc font hệ thống lớn.
- Parser được kiểm thử tự động với `2.5`/`2,5` dưới `vi-VN`, nhưng chưa thử gõ trực tiếp trong UI.
- Chưa kiểm tra owner/focus, z-order và progress modeless trong Inventor 2027; cần runtime test trong host.
- Chưa thử UI với dữ liệu rỗng, 200 dòng, chuỗi cực dài hoặc 200 mapping; kiểm thử tự động có xác nhận writer ghi các mapping BOM mới.
- Chưa đo GDI handle qua nhiều lần mở/đóng; vòng đời đã được cải thiện trong mã nhưng không có số đo runtime.
