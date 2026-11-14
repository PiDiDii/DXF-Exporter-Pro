# DXF Exporter Pro — Inventor 2027

Đây là project add-in C#/.NET 10 được tách từ iLogic V14, giữ bản gốc bất biến ở `Source/V14_Original.iLogicVb`. Mục tiêu của checkpoint này là có pipeline và giao diện riêng để tiếp tục build/test trên máy Windows có Inventor 2027.

## Cấu trúc

```text
Core/       Xử lý tên file, post-process DXF và mô hình dữ liệu thuần
Services/   Convert STEP, xuất DXF, BOM ClosedXML, PartInfo, preflight
UI/Forms/   Form riêng cho Convert, DXF, BOM, Settings, preflight, preview, progress
Commands/   Mỗi lệnh Ribbon một command; ribbon theo ngữ cảnh Assembly/Part
Settings/   Đọc/ghi INI V14 và profile khách hàng/dự án
Source/     Bản V14 gốc, không sửa; chuẩn đối chiếu hành vi
Tests/      Kiểm tra cấu trúc DXF không cần Inventor
```

## Đã nối vào add-in

- Manifest/server lifecycle cho Inventor 2027; Ribbon có Convert STEP ở ngữ cảnh Assembly và Xuất DXF, Xuất BOM ở Part/Assembly. Cài đặt được mở từ luồng xem trước Xuất DXF.
- Nút Convert gọi service port từ V14; có lọc tên, hai chế độ Thickness, skip Flat Pattern, tolerance, log folder và hộp tiến trình/hủy.
- Nút Xuất DXF gọi service duyệt Part/Assembly, tạo Flat Pattern khi chưa có, xuất DXF, post-process qua `Core/DxfProcessor`, cập nhật Description/Thickness và xuất BOM khi bật tùy chọn.
- Có FastMode được khôi phục trạng thái trong `finally`, overwrite/skip/hỏi, cleanup DXF cũ của part suppressed, báo cáo kết quả và lỗi.
- Có preflight, kiểm tra giá trị material/thickness chưa xác định và tên file dự kiến bị trùng; preview hiển thị tên DXF, vật liệu, độ dày, số lượng và cho bỏ chọn từng part trước khi xuất.
- Cài đặt DXF cho phép bật/tắt từng layer; màu layer không tùy chỉnh. DXF xuất theo định dạng AutoCAD R12 để tăng tương thích.
- Add-in tự nạp khi mở tài liệu Inventor; báo cáo mỗi lần xuất có dấu thời gian và chỉ ghi DXF sau khi qua kiểm tra cấu trúc.
- BOM dùng ClosedXML, không gọi Microsoft Excel; xuất vào workbook `.xlsx` mẫu, cấu hình cột/dòng và thư mục đích được lưu riêng. Chỉ các cột dữ liệu BOM được ánh xạ mới bị ghi đè; tên dự án/ngày trong mẫu được giữ nguyên. Hình ảnh hiện được để dành cho giai đoạn sau.

## Phần Giai đoạn 3 còn thiếu hoặc chưa được xác minh

- Chọn part từ selection set hiện hành của Inventor chưa được nối. Hiện anh chọn/bỏ chọn từng part trong bảng preview; đây không giống việc đọc sẵn các occurrence đang được chọn trên vùng đồ họa. Ghi chú nguồn API ở `API_NOTES.md`.
- Tự cập nhật Description/Thickness khi người dùng lưu part chưa được gắn `DocumentEvents.OnSave`. Chữ ký event source/delegate Inventor 2027 cần xác minh trên máy Windows có đúng interop DLL trước khi thêm; không nên đưa callback chưa compile-verify vào build.
- Hành vi DXF/BOM/Convert chưa được so sánh hồi quy trên assembly V14. Không có Inventor 2027 hoặc .NET SDK trong sandbox này, nên project chưa được compile hoặc chạy trong Inventor tại đây. Vì vậy đây **chưa phải DLL đóng gói sẵn để nạp ngay**.

## Build và nạp add-in trên Windows

Cần máy Windows có Inventor 2027 và .NET 10 SDK. Mở PowerShell tại thư mục project, chạy:

```powershell
.\build-and-package.ps1
```

Nếu PowerShell chặn script chưa ký, chỉ trong cửa sổ PowerShell hiện tại chạy lệnh sau rồi chạy lại script:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build-and-package.ps1
```

Nếu Inventor được cài ở đường dẫn khác:

```powershell
.\build-and-package.ps1 -InventorBinPath "D:\Apps\Autodesk\Inventor 2027\Bin"
```

Script sẽ build, gom DLL và dependency (bao gồm `ClosedXML.dll`) vào `dist\InventorDxfExporter`. Chép **toàn bộ file** trong thư mục đó vào `%APPDATA%\Autodesk\Inventor 2027\Addins`, sau đó nạp **Inventor DXF Exporter Pro** bằng Add-In Manager. Nếu build hoặc kiểm tra dependency fail thì chưa copy/nạp bản đó.

### Cài đặt bằng MSI

Để tạo MSI cài per-user và cập nhật đè bản MSI trước đó, chạy:

```powershell
.\build-installer.ps1
```

MSI được tạo tại `dist\InventorDxfExporter-Setup.msi`; mở file để cài hoặc chạy:

```powershell
msiexec.exe /i .\dist\InventorDxfExporter-Setup.msi
```

MSI có giao diện cài đặt/tiến trình/kết quả của Windows Installer; khi thành công sẽ hiện hộp thoại hoàn tất, khi lỗi Windows Installer sẽ báo lỗi thay vì thoát im lặng. MSI cài manifest và các dependency vào `%APPDATA%\Autodesk\Inventor 2027\Addins`. Các lần build dùng cùng Upgrade Code và chấp nhận cùng phiên bản để có thể cài đè bản đang thử; có thể chỉ định phiên bản khác bằng `-ProductVersion 1.0.1`. Đóng Inventor trước khi cài/cập nhật để tránh file DLL đang được nạp bị khóa. Gỡ cài đặt bằng **Installed apps** trong Windows.

Manifest dùng `<LoadAutomatically>1</LoadAutomatically>` và `<LoadBehavior>1</LoadBehavior>` để Inventor tự nạp add-in khi mở tài liệu; `LoadBehavior=10` nghĩa là chỉ nạp theo yêu cầu trong Add-In Manager/API.

Chạy kiểm tra cấu trúc và hậu xử lý DXF bằng lệnh:

```powershell
dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj
```

Trong lệnh **Xuất BOM**, chọn file `.xlsx` mẫu, thư mục lưu và ánh xạ cột/dòng. Mặc định ghi lên worksheet đầu tiên, bắt đầu ở dòng 11. Các cột mặc định: STT (A), Tên chi tiết (C), Kích thước (E), Vật liệu (K), Số lượng (M). Kích thước lấy từ Flat Pattern. File mẫu không bị sửa, mỗi lần xuất tạo một file BOM mới; các ô metadata trong mẫu vẫn được giữ nguyên. Hình ảnh chưa xuất trong phiên bản này.

## Xem trước giao diện

Không cần mở Inventor để xem giao diện. Chạy `.\Preview-Settings.ps1` tại thư mục project để mở danh sách các màn hình: Cài đặt, Xuất DXF, Convert STEP, Xuất BOM, danh sách DXF, kiểm tra trước khi xuất và tiến trình. Các form dùng dữ liệu mẫu; nút xác nhận không gọi service xuất/Convert và không tạo file. Cài đặt ở chế độ xem trước sẽ vô hiệu hóa thao tác profile. Sau khi sửa code, đóng cửa sổ rồi chạy lại lệnh để build và xem phiên bản mới.

## Cách test mốc V14

Dùng một bản sao assembly đã xuất bằng V14. Trên cùng assembly, chạy Convert và Xuất DXF với cùng INI, rồi so sánh: danh sách part và quantity, file naming, DXF layer/style, bend marks, TEXT, mã hóa, thuộc tính Description/Thickness, BOM và hành vi file trùng tên. Kiểm tra riêng part thiếu Flat Pattern, suppressed, part chưa lưu, material/thickness trống và assembly có reference lỗi. Giữ lại INI/assembly/DXF gốc để tránh thử nghiệm làm ghi đè dữ liệu sản xuất.
