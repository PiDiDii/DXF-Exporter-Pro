# DXF Exporter Pro — Inventor 2027

Add-in C# / .NET 10 độc lập cho Autodesk Inventor 2027. Mã nguồn hiện tại là nguồn chuẩn của sản phẩm. Theo xác nhận của người dùng, add-in đã được nạp và chạy thử thành công trong Inventor 2027.

## Cấu trúc

```text
Core/       Xử lý tên file, DXF và các mô hình dữ liệu
Services/   Convert STEP, xuất DXF, BOM, PartInfo và preflight
UI/Forms/   Giao diện Convert, DXF, BOM, Settings, preview và progress
Commands/   Lệnh Ribbon theo ngữ cảnh Assembly/Part
Settings/   Cài đặt JSON, profile dự án/khách hàng và cấu hình BOM
Tests/      Kiểm tra cấu trúc DXF, cài đặt và BOM không cần Inventor
Examples/   Công cụ C# đổi Part Number/Browser Name và rule iLogic K-Factor
```

## Chức năng

- Ribbon cho Convert STEP (Assembly), Xuất DXF và Xuất BOM (Part/Assembly); thêm Đổi hệ số K bằng iLogic, Đổi Part Number/Browser Name (Assembly) và Xuất STEP cho Assembly/Part.
- Công cụ C# cho xem trước, đánh số theo phân cấp và chọn đổi Browser Name occurrence hoặc iProperty Part Number; không đổi tên file và không tự lưu tài liệu. Lệnh Đổi hệ số K vẫn chạy rule iLogic được đóng gói cạnh DLL và mặc định tự lưu từng part theo tùy chọn trong form.
- Dashboard Preview có màn hình Part Number/Browser Name với dữ liệu mẫu, không kết nối Inventor và không thực hiện thay đổi; form khôi phục số lượng, sơ đồ mã, tùy chọn thư viện, trạng thái/bỏ qua và log/sao lưu CSV. Không có phần xem 3D.
- Convert STEP sang Sheet Metal, xác định độ dày, tùy chọn gán A-Side vào mặt trong, tạo Flat Pattern; có lọc tên, hai chế độ Thickness, bỏ qua Flat Pattern sẵn có, tolerance, log và hủy tiến trình.
- Xuất DXF từ Flat Pattern, kiểm tra trước khi xuất, preview và chọn từng part; nút Cài đặt nằm ngay cửa sổ xuất đầu tiên. Cấu hình layer, bend marks, TEXT, overwrite/skip, FastMode, dọn DXF của part suppressed và bật/tắt file báo cáo `.txt` sau xuất với thư mục lưu riêng.
- Tùy chọn layer `IV_MARK_SURFACE` (mặc định tắt); khi tắt Tool Center, cả tên `IV_TOOL_CENTER` và biến thể `IV_Tool_center` đều được đưa vào danh sách ẩn.
- Xuất BOM sau DXF là tùy chọn, mặc định tắt. Lệnh Xuất DXF không cập nhật Description/Thickness và không lưu part nguồn.
- Xuất BOM vào bản sao workbook `.xlsx` mẫu bằng ClosedXML, không cần Microsoft Excel; có xem trước danh sách Part Number/vật liệu/số lượng/kích thước trước khi xuất, tiến trình/hủy, định dạng kích thước `dài x rộng x độ dày` với dấu phẩy thập phân, và tùy chọn ghi Ngày (`dd/MM/yyyy`) cùng Tên dự án vào các ô tiêu đề đã cấu hình. Tên dự án lấy từ tên file tài liệu, không gồm phần mở rộng.
- Convert STEP có tùy chọn ghi log (mặc định bật) và hỏi có mở thư mục log sau khi ghi thành công; Ribbon được kiểm tra và khôi phục tab còn thiếu sau khi chạy lệnh.
- DXF được hậu xử lý và xác thực trước khi thay file đích. Báo cáo xuất ghi nhận kết quả và lỗi.

## Trạng thái kiểm thử

- Build add-in, build UI Preview, bộ validation DXF/BOM và build MSI đã chạy thành công sau các thay đổi hiện tại, đều 0 cảnh báo/0 lỗi. Các kết quả này không thay thế kiểm thử host Inventor hoặc kiểm tra cài đặt.
- Lệnh iLogic K-Factor và công cụ C# Part Number/Browser Name đã build thành công. Bộ validation DXF/BOM đã chạy qua. Chưa chạy các lệnh trong Inventor 2027.
- Người dùng xác nhận phiên bản trước đã được nạp và chạy thử thành công trong Inventor 2027. Nhóm thay đổi DXF/BOM/Convert STEP hiện tại vẫn cần kiểm tra thủ công trên Inventor 2027.
- MSI của mã nguồn hiện tại đã được tạo thành công. WiX cấu hình không nâng cấp cùng phiên bản; ICE91 được suppress có chủ đích cho gói cài per-user. Chưa thực hiện cài/nâng cấp/gỡ MSI trên máy thử sạch trong lần xác minh này.
- Cài đặt JSON đã qua kiểm tra tự động; cần xác nhận thêm việc đọc/ghi cấu hình khi nạp add-in trong Inventor sau thay đổi này.
- Việc chọn part theo Selection Set hiện hành và tự cập nhật metadata khi người dùng lưu part chưa được triển khai.

## Trạng thái phát hành

**Chưa nên phát hành MSI rộng rãi.** Cần xác nhận cài/nâng cấp/gỡ MSI trên máy thử sạch và kiểm tra Inventor 2027 sau thay đổi định dạng cài đặt JSON. Gói hiện không được ký số; cần quyết định và cấu hình ký nếu phân phối ngoài phạm vi nội bộ. Dùng [RELEASE_CHECKLIST.md](./RELEASE_CHECKLIST.md) để ghi nhận kết quả và quyết định phát hành.

## Cài đặt và lưu cấu hình

Cài đặt chính được lưu tại `%APPDATA%\Inventor_DXF_Exporter\ExportSettings.json`; profile tại `%APPDATA%\Inventor_DXF_Exporter\Profiles`; cấu hình BOM tại `%APPDATA%\Inventor_DXF_Exporter\BomExportSettings.json`.

File cấu hình INI dùng ở các bản trước không được tự import. Add-in không xóa file đó; các lựa chọn cần thiết có thể được thiết lập lại trong giao diện.

## Build và cài đặt MSI trên Windows

Cần Windows, Autodesk Inventor 2027 và .NET 10 SDK. Tại thư mục project chạy:

```powershell
.\build-installer.ps1
```

Nếu PowerShell chặn script chưa ký, chỉ trong cửa sổ PowerShell hiện tại chạy:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build-installer.ps1
```

Nếu Inventor được cài ở đường dẫn khác:

```powershell
.\build-installer.ps1 -InventorBinPath "D:\Apps\Autodesk\Inventor 2027\Bin"
```

Script build add-in, gom dependency và tạo MSI per-user tại `dist\InventorDxfExporter-Setup.msi`. Các file trung gian được tạo trong thư mục tạm và tự dọn sau khi build. Phiên bản mặc định hiện tại là `1.0.2`, cao hơn MSI `1.0.1` trước đó để cho phép nâng cấp. WiX không nâng cấp MSI nếu `ProductVersion` bằng phiên bản đang cài; mỗi bản sau phải tăng phiên bản, ví dụ `.\build-installer.ps1 -ProductVersion "1.0.3"`. Đóng hẳn Inventor trước khi cài, sau khi cài xong mở lại để add-in được nạp từ DLL mới. Cài MSI bằng cách mở file hoặc chạy:

```powershell
msiexec.exe /i .\dist\InventorDxfExporter-Setup.msi
```

Không cần cài add-in bằng cách chép thư mục hoặc nạp thủ công `.addin`; manifest được MSI cài cùng add-in. Đóng Inventor trước khi cài/cập nhật để tránh DLL đang dùng bị khóa. Có thể gỡ bằng **Installed apps** trong Windows hoặc lối tắt **Uninstall Inventor DXF Exporter Pro** trong Start Menu; lối tắt gọi Windows Installer để gỡ đúng phiên bản sản phẩm. Việc gỡ bỏ add-in không xóa cấu hình người dùng trong `%APPDATA%\Inventor_DXF_Exporter`.

Manifest dùng `<LoadAutomatically>1</LoadAutomatically>` và `<LoadBehavior>1</LoadBehavior>` để tự nạp add-in khi mở tài liệu. `LoadBehavior=10` nghĩa là chỉ nạp theo yêu cầu trong Add-In Manager/API.

## Kiểm tra tự động

Chạy kiểm tra DXF, TEXT, bend marks, lưu/đọc cài đặt JSON, profile và BOM:

```powershell
dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj
```

## Kiểm tra trong Inventor

Khi xác nhận thay đổi hoặc phát hành bản mới, chạy Convert, Xuất DXF và Xuất BOM trên một bản sao assembly thử nghiệm. Bao gồm part thiếu Flat Pattern, suppressed, part chưa lưu, thiếu material/thickness, reference lỗi và file trùng tên. Kiểm tra thêm Ribbon sau khi khởi động lại và sau khi reset giao diện.

Tùy chọn **Xóa DXF cũ của Part đang Suppress** chỉ xóa các DXF đã xuất trước đó tương ứng với Part bị Suppress khỏi thư mục kết quả DXF; không xóa tài liệu Part/Assembly trong Inventor. Nếu tắt, các DXF cũ được giữ lại và add-in ghi cảnh báo vào báo cáo. Chỉ bật khi muốn dọn file đầu ra không còn đại diện cho cấu hình assembly hiện tại.

## Điều chỉnh kích thước cửa sổ giao diện

Các kích thước ban đầu được đặt trong constructor của từng form bằng `ClientSize` (vùng bên trong cửa sổ). `MinimumSize` giới hạn kích thước thu nhỏ; `FormBorderStyle` quyết định người dùng có kéo đổi kích thước được hay không. Ví dụ: [BomForm.cs](./UI/Forms/BomForm.cs) và [ExportSettingsForm.cs](./UI/Forms/ExportSettingsForm.cs) dùng `FormBorderStyle.Sizable`; [DxfExportForm.cs](./UI/Forms/DxfExportForm.cs) là hộp thoại cố định.

Khi chỉnh layout:

- Đổi kích thước mở đầu tại `ClientSize`; nếu cho phép thu nhỏ, điều chỉnh `MinimumSize` tương ứng.
- Sửa các hàng/cột trong `TableLayoutPanel.RowStyles` và `ColumnStyles` khi một vùng cần thêm chỗ. Các vùng chính thường dùng `Dock = Fill`; tránh đặt tọa độ tuyệt đối cho control co giãn.
- Với cửa sổ phải thay đổi theo người dùng, đặt `FormBorderStyle.Sizable`, `MinimumSize` hợp lý và dùng `Dock`/`Anchor` cùng hàng `SizeType.Percent` để nội dung co giãn.
- Chạy `.\Preview-Settings.ps1` để kiểm tra trực quan các form mẫu; sau đó build add-in để kiểm tra cửa sổ chạy trong Inventor. Kích thước WinForms chịu ảnh hưởng DPI, nên cần thử ở độ phân giải/tỷ lệ màn hình thực tế.

## Xem trước giao diện

Chạy `.\Preview-Settings.ps1` để xem form mà không cần mở Inventor. Dữ liệu là mẫu; nút xác nhận không gọi service xuất/Convert và không tạo file. Sau khi sửa code, đóng cửa sổ rồi chạy lại để build và xem phiên bản mới.

## Xuất BOM

Chọn file `.xlsx` mẫu, thư mục lưu và ánh xạ cột/dòng. Mặc định ghi worksheet đầu tiên từ dòng 11: STT (A), Tên chi tiết (C), Hình ảnh (D), Kích thước (E), Vật liệu (K), Số lượng (M). Có thể nhập riêng địa chỉ ô tiêu đề để ghi Tên dự án và Ngày; để trống nếu không dùng. Mỗi lần xuất tạo file BOM mới; file mẫu, worksheet khác và các ô không ánh xạ được giữ nguyên. Tiến trình được cập nhật khi thu thập chi tiết, chuẩn bị từng hàng và ghi workbook; thao tác lưu workbook của ClosedXML là đồng bộ.
