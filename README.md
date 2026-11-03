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
Tests/      Chưa có bộ regression test tự động ở checkpoint này
```

## Đã nối vào add-in

- Manifest/server lifecycle cho Inventor 2027, Ribbon có Convert STEP ở ngữ cảnh Assembly và Xuất DXF, Xuất BOM, Cài đặt ở Part/Assembly.
- Nút Convert gọi service port từ V14; có lọc tên, hai chế độ Thickness, skip Flat Pattern, tolerance, log folder và hộp tiến trình/hủy.
- Nút Xuất DXF gọi service duyệt Part/Assembly, tạo Flat Pattern khi chưa có, xuất DXF, post-process qua `Core/DxfProcessor`, cập nhật Description/Thickness và xuất BOM khi bật tùy chọn.
- Có FastMode được khôi phục trạng thái trong `finally`, overwrite/skip/hỏi, cleanup DXF cũ của part suppressed, báo cáo kết quả và lỗi.
- Có preflight, kiểm tra giá trị material/thickness chưa xác định và tên file dự kiến bị trùng; preview hiển thị tên DXF, vật liệu, độ dày, số lượng và cho bỏ chọn từng part trước khi xuất.
- Cài đặt DXF cho phép chọn màu RGB, kiểu nét và độ dày riêng cho từng layer; thiết lập được lưu cùng profile và INI V14. DXF xuất theo định dạng 2000 để hỗ trợ lineweight.
- BOM dùng ClosedXML, không gọi Microsoft Excel. Settings giữ đường dẫn/khóa INI V14; có profile theo khách/dự án.

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

## Xem trước giao diện

Không cần mở Inventor để xem giao diện. Chạy `.\Preview-Settings.ps1` tại thư mục project để mở danh sách các màn hình: Cài đặt, Xuất DXF, Convert STEP, Xuất BOM, danh sách DXF, kiểm tra trước khi xuất và tiến trình. Các form dùng dữ liệu mẫu; nút xác nhận không gọi service xuất/Convert và không tạo file. Cài đặt ở chế độ xem trước sẽ vô hiệu hóa thao tác profile. Sau khi sửa code, đóng cửa sổ rồi chạy lại lệnh để build và xem phiên bản mới.

## Cách test mốc V14

Dùng một bản sao assembly đã xuất bằng V14. Trên cùng assembly, chạy Convert và Xuất DXF với cùng INI, rồi so sánh: danh sách part và quantity, file naming, DXF layer/style, bend marks, TEXT, mã hóa, thuộc tính Description/Thickness, BOM và hành vi file trùng tên. Kiểm tra riêng part thiếu Flat Pattern, suppressed, part chưa lưu, material/thickness trống và assembly có reference lỗi. Giữ lại INI/assembly/DXF gốc để tránh thử nghiệm làm ghi đè dữ liệu sản xuất.
