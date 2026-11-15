# Checklist kiểm tra trước khi phát hành

Dùng checklist này để quyết định MSI có đủ điều kiện phát hành hay chưa. Đánh dấu từng mục và lưu bằng chứng (log, ảnh chụp, tên file thử) cùng kết quả; không đánh dấu đạt chỉ dựa trên việc build thành công.

## Thông tin bản phát hành

- Người kiểm tra: ____________________
- Ngày kiểm tra: ____________________
- Phiên bản Inventor: ____________________
- Phiên bản add-in (`ProductVersion`): ____________________
- Máy / tài khoản thử: ____________________
- MSI: `dist\InventorDxfExporter-Setup.msi`

## 1. Build và gói cài đặt

- [ ] Đã cập nhật `ProductVersion`; phiên bản phải cao hơn bản đang cài nếu kiểm tra nâng cấp.
- [ ] Chạy `.\build-installer.ps1` thành công; build add-in và MSI không có lỗi hoặc cảnh báo WiX ngoài các cảnh báo đã được chủ động suppress.
- [ ] MSI mới có mặt tại `dist\InventorDxfExporter-Setup.msi`; ghi nhận kích thước hoặc SHA-256 để xác định đúng file đem thử/phát hành.
- [ ] Không cài add-in bằng cách chép thư mục hoặc manifest thủ công; thử đúng luồng MSI per-user.
- [ ] Nếu phân phối ngoài phạm vi nội bộ, đã quyết định yêu cầu ký số. Nếu yêu cầu ký, xác minh chữ ký MSI hợp lệ trên máy nhận.

## 2. Cài mới và khởi động Inventor

Thực hiện trên máy thử hoặc tài khoản Windows sạch có Inventor 2027.

- [ ] Cài MSI bằng Windows Installer; cài đặt hoàn tất thành công, không cần quyền quản trị ngoài dự kiến.
- [ ] Xác nhận manifest và payload được cài vào thư mục Add-ins per-user của Inventor 2027.
- [ ] Mở Inventor 2027; add-in xuất hiện trong Add-In Manager và nạp được.
- [ ] Khởi động lại Inventor và xác nhận add-in nạp tự động, Ribbon hiển thị đúng trong môi trường Assembly và Part.
- [ ] Không có lỗi load assembly/dependency, thông báo lỗi bất thường hoặc lỗi liên quan phiên bản .NET.
- [ ] Đóng/mở lại tài liệu và Inventor; xác nhận không có Ribbon trùng hoặc mất lệnh sau khi reset giao diện.

## 3. Cấu hình và lưu trữ

- [ ] Mở Settings, thay đổi một vài tùy chọn, lưu, đóng/mở lại form và xác nhận giá trị còn nguyên.
- [ ] Khởi động lại Inventor và xác nhận cấu hình JSON vẫn được đọc đúng.
- [ ] Lưu/nạp profile dự án/khách hàng; xác nhận profile không làm thay đổi ngoài ý muốn các thiết lập khác.
- [ ] Kiểm tra cấu hình BOM: lưu/nạp lại đường dẫn mẫu, thư mục đích và ánh xạ cột.
- [ ] Nếu có cấu hình từ bản cũ, xác nhận hành vi đúng với thông báo sản phẩm: INI cũ không tự import; add-in không xóa file INI.

## 4. Smoke test chức năng trong Inventor

Chỉ dùng bản sao của assembly/part thử nghiệm; giữ nguyên file nguồn để đối chiếu.

### Convert STEP

- [ ] Chạy Convert trên STEP/assembly hợp lệ; xác nhận tiến trình hoàn tất, kết quả có thể mở và không làm hỏng file nguồn.
- [ ] Thử part đã có Flat Pattern và part cần tạo Flat Pattern.
- [ ] Thử bộ lọc tên và các chế độ Thickness/tolerance được hỗ trợ.
- [ ] Thử hủy giữa chừng; xác nhận thao tác dừng có kiểm soát và báo cáo nêu rõ kết quả.

### Xuất DXF

- [ ] Chạy preflight/preview; xác nhận danh sách part và thông tin cảnh báo hợp lý. Thử Cancel để chắc chắn chưa xuất file ngoài dự kiến.
- [ ] Xuất DXF cho part có Flat Pattern; mở DXF và kiểm tra hình dạng, đơn vị/kích thước, layer, bend marks và TEXT theo cấu hình.
- [ ] Thử part thiếu Flat Pattern, suppressed, chưa lưu, thiếu material/thickness và reference lỗi; xác nhận từng trường hợp được xử lý/báo cáo rõ, không làm hỏng assembly.
- [ ] Thử tên trùng, file đích đã tồn tại và lựa chọn overwrite/skip; xác nhận file cũ không bị thay sai.
- [ ] Xác nhận DXF lỗi/không hợp lệ không thay thế file đích hợp lệ.
- [ ] Kiểm tra dọn DXF của part suppressed nếu bật tùy chọn tương ứng.
- [ ] Bend rời có thể tiếp tục được giữ nguyên; đây là hành vi đã được người dùng chấp thuận, không xem là blocker phát hành.

### BOM và metadata

- [ ] Xuất BOM từ assembly/part mẫu; xác nhận số lượng, tên, kích thước, độ dày/vật liệu và ánh xạ vào đúng ô.
- [ ] Mở workbook kết quả; xác nhận sheet khác, định dạng và các ô không ánh xạ trong template được giữ nguyên.
- [ ] Thử xuất nhiều lần; xác nhận mỗi lần tạo kết quả đúng, không ghi đè template.
- [ ] Xác nhận Xuất DXF không cập nhật Description/Thickness và không lưu part nguồn.
- [ ] Bỏ chọn các layer DXF, đặc biệt `IV_TOOL_CENTER`; xác nhận entity và tên layer đều không còn trong DXF đầu ra khi mở bằng CAD.
- [ ] Thử BOM sau DXF nếu bật; xác nhận BOM phản ánh trạng thái sau thao tác DXF.

## 5. Nâng cấp và gỡ cài đặt

- [ ] Cài một MSI phiên bản thấp hơn, sau đó cài MSI bản mới với `ProductVersion` cao hơn; xác nhận nâng cấp hoàn tất và add-in vẫn nạp.
- [ ] Kiểm tra dependency/manifest sau nâng cấp là phiên bản mới; không còn file payload cũ gây lỗi.
- [ ] Xác nhận cấu hình người dùng sau nâng cấp theo chính sách mong muốn; ghi lại nếu cấu hình được giữ hoặc cần thiết lập lại.
- [ ] Gỡ add-in qua **Installed apps** và thử lối tắt **Uninstall Inventor DXF Exporter Pro** trong Start Menu; xác nhận cả hai luồng gỡ thành công.
- [ ] Xác nhận payload/manifest và lối tắt Start Menu được gỡ, Inventor không còn nạp add-in, còn cấu hình người dùng trong `%APPDATA%\Inventor_DXF_Exporter` được giữ nguyên.
- [ ] Cài lại MSI sau khi gỡ; xác nhận có thể cài và nạp lại bình thường.

## 6. Chạy kiểm tra tự động và kết luận

- [ ] Chạy `dotnet run --project .\Tests\DxfProcessorValidation\DxfProcessorValidation.csproj`; kết quả đạt.
- [ ] Lưu log build MSI, kết quả test tự động và bằng chứng smoke test trong Inventor.
- [ ] Không còn lỗi nghiêm trọng/cao chưa xử lý; các giới hạn đã biết được ghi rõ và không mâu thuẫn với nội dung quảng bá.

**Kết luận phát hành:** [ ] Đạt [ ] Chưa đạt  
**Mục còn mở / lý do chưa đạt:** ____________________________________________  
**Người duyệt:** ____________________ **Ngày:** ____________________

## Ghi chú hiện tại của dự án

- Build add-in và MSI, cùng bộ kiểm tra tự động, đã đạt trên môi trường phát triển. ICE40/ICE61 đã được xử lý; ICE91 được suppress có chủ đích vì gói chỉ cài vào thư mục per-user phù hợp với cấu hình MSI.
- Cần hoàn thành kiểm thử cài/nâng cấp/gỡ trên máy thử và smoke test Inventor 2027 trước khi kết luận phát hành.
- MSI hiện chưa được ký số. Việc ký có cần thiết hay không phụ thuộc phạm vi phân phối và chính sách của đơn vị nhận.
- Chọn part theo Selection Set hiện hành và tự cập nhật metadata khi người dùng lưu part chưa được triển khai; xác nhận đây không phải cam kết của bản phát hành hoặc xử lý trước khi quảng bá tính năng đó.
