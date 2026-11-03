AddReference "System.Windows.Forms"
AddReference "System.Drawing"

'=====================================================================
' Export_DXF_Convert_STEP_Inventor2026_V14_TABS
' Inventor 2026 - iLogic
' Tác giả: Phạm Đức Duy
' Phiên bản: V14 - Tích hợp Convert STEP + Xuất DXF/BOM, form chia tab
'
' THAY ĐỔI V14 (tối ưu, chuẩn bị chuyển add-in):
'  - Convert: tắt ScreenUpdating khi chạy hàng loạt (trả lại trong Finally)
'  - Không Save thừa: UpdateFlatPatternInfo chỉ lưu khi file thực sự Dirty
'  - BOM Excel: ghi cả bảng bằng mảng 2 chiều 1 lần, thay vì từng ô
'  - DXF: đọc 1 lần -> xử lý đường chấn + chèn TEXT trong bộ nhớ -> kiểm tra -> ghi 1 lần (PostProcessDxf)
'  - Encoding DXF: dùng ISO-8859-1 (giữ nguyên byte) thay cho Encoding.Default (là UTF-8 trên .NET 8)
'  - TEXT trong DXF: tự bỏ dấu tiếng Việt (CleanTextForDxf)
'  - Báo cáo có thêm thời gian từng giai đoạn để tìm chỗ chậm
'  - Sửa nhãn/comment lệch với code (layer DXF_INFO / style DXF_INFO_STYLE)
'
' THAY ĐỔI V13:
'  - Tích hợp chức năng Convert Part (STEP/import) -> Sheet Metal + Flat Pattern
'    (logic giữ nguyên từ 1_Convert_Step.iLogicVb v2.0.2)
'  - Form chia 4 tab: Convert STEP | Xuất DXF | Layer & Đường chấn | Thông tin DXF
'  - Ô "Hành động": Chỉ Convert / Chỉ Xuất DXF + BOM / Convert rồi Xuất DXF
'  - Chạy liên hoàn: một báo cáo tổng hợp chung
'  - Mở Part: tab Convert bị ẩn (Convert chỉ chạy trên Assembly)
'  - SilentOperation của Convert luôn được trả về như cũ (Try/Finally)
'  - Giữ nguyên toàn bộ phần V12: xử lý đường chấn, TEXT, BOM, suppress
'=====================================================================

Sub Main()
    Dim doc As Inventor.Document = ThisApplication.ActiveDocument
    If doc Is Nothing Then Return

    Dim settings As ExportSettings = ShowExportForm(doc)
    If settings Is Nothing Then Return

    SaveSettings(settings)

    Dim convertText As String = ""
    Dim exportText As String = ""
    Dim convertLog As String = ""

    If settings.DoConvert Then
        Dim asmDoc As Inventor.AssemblyDocument = TryCast(doc, Inventor.AssemblyDocument)
        If asmDoc Is Nothing Then
            System.Windows.Forms.MessageBox.Show("Convert chỉ chạy được trên Assembly.", "Convert STEP")
            Return
        End If

        Dim cr As ConvertResult = RunConvertStep(asmDoc, settings)
        convertLog = cr.LogFile
        convertText = "Thành công : " & cr.Success & vbCrLf & _
                      "Bỏ qua     : " & cr.Skip & vbCrLf & _
                      "Lỗi        : " & cr.Err & vbCrLf & _
                      "Log        : " & cr.LogFile

        If settings.DoExport AndAlso cr.Success = 0 AndAlso cr.Skip = 0 AndAlso cr.Err > 0 Then
            Dim ask As System.Windows.Forms.DialogResult = System.Windows.Forms.MessageBox.Show( _
                "Convert không thành công part nào (" & cr.Err & " lỗi)." & vbCrLf & _
                "Vẫn tiếp tục xuất DXF?" & vbCrLf & vbCrLf & "Log: " & cr.LogFile, "Convert STEP", _
                System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning)
            If ask <> System.Windows.Forms.DialogResult.Yes Then
                System.Windows.Forms.MessageBox.Show("=== CONVERT STEP ===" & vbCrLf & convertText & vbCrLf & vbCrLf & "Đã dừng, không xuất DXF.", "Hoàn tất")
                Return
            End If
        End If
    End If

    If settings.DoExport Then
        exportText = RunExport(doc, settings)
    End If

    Dim msg As New System.Text.StringBuilder()
    If convertText <> "" Then
        msg.AppendLine("=== CONVERT STEP → SHEET METAL ===")
        msg.AppendLine(convertText)
        msg.AppendLine()
    End If
    If exportText <> "" Then
        msg.AppendLine("=== XUẤT DXF + BOM ===")
        msg.AppendLine(exportText)
    End If

    ' Chạy liên hoàn: ghi thêm 1 báo cáo tổng hợp chung
    If settings.DoConvert AndAlso settings.DoExport Then
        Try
            System.IO.Directory.CreateDirectory(settings.OutputRoot)
            Dim allPath As String = System.IO.Path.Combine(settings.OutputRoot, "BaoCao_TongHop_" & System.DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".txt")
            Dim allText As New System.Text.StringBuilder()
            allText.AppendLine("BÁO CÁO TỔNG HỢP - CONVERT + XUẤT DXF")
            allText.AppendLine("Thời gian: " & System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))
            allText.AppendLine("Assembly : " & doc.DisplayName)
            allText.AppendLine()
            allText.AppendLine(msg.ToString())
            System.IO.File.WriteAllText(allPath, allText.ToString(), System.Text.Encoding.UTF8)
            msg.AppendLine()
            msg.AppendLine("Báo cáo tổng hợp: " & allPath)
        Catch
        End Try
    End If

    System.Windows.Forms.MessageBox.Show(msg.ToString(), "Hoàn tất")
End Sub

'=====================================================================
' XUẤT DXF + BOM  (logic V12, trả về nội dung tóm tắt để Main ghép báo cáo)
'=====================================================================
Function RunExport(doc As Inventor.Document, settings As ExportSettings) As String
    Dim items As New System.Collections.Generic.Dictionary(Of String, ExportItem)(System.StringComparer.OrdinalIgnoreCase)
    Dim skipped As New System.Collections.Generic.List(Of String)
    Dim errors As New System.Collections.Generic.List(Of String)
    Dim suppressedParts As New System.Collections.Generic.List(Of ExportItem)

    Dim oldScreenUpdating As Boolean = True
    Dim oldSilentOperation As Boolean = False
    Dim speedModeApplied As Boolean = False
    Try
        If settings.FastMode Then
            oldScreenUpdating = ThisApplication.ScreenUpdating
            oldSilentOperation = ThisApplication.SilentOperation
            ThisApplication.ScreenUpdating = False
            ThisApplication.SilentOperation = True
            speedModeApplied = True
        End If

        If doc.DocumentType = Inventor.DocumentTypeEnum.kPartDocumentObject Then
            AddPartItem(TryCast(doc, Inventor.PartDocument), 1, items, skipped, errors)
        ElseIf doc.DocumentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject Then
            ScanOccurrences(TryCast(doc, Inventor.AssemblyDocument).ComponentDefinition.Occurrences, items, skipped, errors, suppressedParts)
        Else
            Return "Chỉ hỗ trợ Part Sheet Metal hoặc Assembly."
        End If

        If items.Count = 0 Then
            Return "Không tìm thấy part Sheet Metal hợp lệ."
        End If

        Dim exported As Integer = 0
        Dim skippedExisting As Integer = 0
        Dim bendCount As Integer = 0
        Dim report As New System.Text.StringBuilder()
        ' V14: đo thời gian từng giai đoạn để biết chỗ nào chậm
        Dim swDxf As New System.Diagnostics.Stopwatch()
        Dim swPost As New System.Diagnostics.Stopwatch()
        Dim swInfo As New System.Diagnostics.Stopwatch()
        Dim swBom As New System.Diagnostics.Stopwatch()
        Dim swTotal As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()

        For Each pair As System.Collections.Generic.KeyValuePair(Of String, ExportItem) In items
            Dim item As ExportItem = pair.Value
            Try
                Dim folder As String = System.IO.Path.Combine(settings.OutputRoot, CleanName(item.Material), CleanName(item.Thickness))
                System.IO.Directory.CreateDirectory(folder)
                Dim dxfPath As String = System.IO.Path.Combine(folder, BuildFileName(item, settings.FilePattern))

                If System.IO.File.Exists(dxfPath) Then
                    If settings.OverwriteMode = "Bỏ qua file đã có" Then
                        skippedExisting += 1
                        report.AppendLine("BỎ QUA FILE ĐÃ CÓ: " & dxfPath)
                        Continue For
                    ElseIf settings.OverwriteMode = "Hỏi từng file" Then
                        Dim a As System.Windows.Forms.DialogResult = System.Windows.Forms.MessageBox.Show( _
                            "File đã tồn tại:" & vbCrLf & dxfPath & vbCrLf & "Ghi đè?", "Xác nhận", _
                            System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question)
                        If a <> System.Windows.Forms.DialogResult.Yes Then
                            skippedExisting += 1
                            Continue For
                        End If
                    End If
                End If

                swDxf.Start()
                Try
                    ExportOnePart(item.Document, dxfPath, settings.DxfOptions)
                Finally
                    swDxf.Stop()
                End Try

                ' V14: đọc DXF 1 lần, xử lý đường chấn (V12) rồi chèn TEXT, ghi 1 lần
                swPost.Start()
                Try
                    bendCount += PostProcessDxf(dxfPath, item, settings, errors)
                Finally
                    swPost.Stop()
                End Try

                If settings.UpdateInfoAndBom Then
                    swInfo.Start()
                    Try
                        UpdateFlatPatternInfo(item.Document, errors)
                    Finally
                        swInfo.Stop()
                    End Try
                End If

                exported += 1
                report.AppendLine("OK: " & dxfPath)
            Catch ex As Exception
                errors.Add(item.DetailName & ": " & ex.Message)
            End Try
        Next

        Dim bomPath As String = ""
        If settings.UpdateInfoAndBom Then
            swBom.Start()
            bomPath = ExportBomToExcel(items, settings.OutputRoot, errors)
            swBom.Stop()
        End If
        swTotal.Stop()

        Dim timingText As String = "Thời gian (giây): Tổng " & (swTotal.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & _
            " | Xuất DXF (Unfold+DataIO) " & (swDxf.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & _
            " | Xử lý file DXF " & (swPost.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & _
            " | Cập nhật thuộc tính + Save " & (swInfo.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & _
            " | BOM Excel " & (swBom.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)

        Dim summary As New System.Text.StringBuilder()
        summary.AppendLine("HOÀN TẤT XUẤT DXF")
        summary.AppendLine(timingText)
        summary.AppendLine("Part duy nhất: " & items.Count)
        summary.AppendLine("Đã xuất: " & exported)
        summary.AppendLine("Bỏ qua file đã có: " & skippedExisting)
        summary.AppendLine("Bỏ qua khi duyệt: " & skipped.Count)
        If settings.BendMode = "Chỉ đánh dấu 2 đầu" Then
            summary.AppendLine("Đường chấn đã đánh dấu 2 đầu: " & bendCount)
        End If
        summary.AppendLine("Lỗi: " & errors.Count)

        If skipped.Count > 0 Then
            summary.AppendLine(vbCrLf & "BỎ QUA:")
            For Each s As String In skipped
                summary.AppendLine(s)
            Next
        End If
        If errors.Count > 0 Then
            summary.AppendLine(vbCrLf & "LỖI:")
            For Each s As String In errors
                summary.AppendLine(s)
            Next
        End If

        Dim staleCount As Integer = 0
        Dim deletedCount As Integer = 0
        For Each sp As ExportItem In suppressedParts
            Try
                If sp.Document IsNot Nothing AndAlso items.ContainsKey(sp.Document.FullFileName) Then
                    summary.AppendLine("LƯU Ý: " & sp.DetailName & " có occurrence đang Suppress nhưng vẫn được xuất vì còn occurrence khác.")
                    Continue For
                End If

                Dim spFolder As String = System.IO.Path.Combine(settings.OutputRoot, CleanName(sp.Material), CleanName(sp.Thickness))
                Dim spPath As String = System.IO.Path.Combine(spFolder, BuildFileName(sp, settings.FilePattern))
                If System.IO.File.Exists(spPath) Then
                    staleCount += 1
                    If settings.CleanupSuppressed Then
                        Try
                            System.IO.File.Delete(spPath)
                            deletedCount += 1
                            summary.AppendLine("ĐÃ XÓA (Suppress): " & spPath)
                        Catch exDel As Exception
                            summary.AppendLine("KHÔNG THỂ XÓA: " & spPath & " - " & exDel.Message)
                        End Try
                    Else
                        summary.AppendLine("CẢNH BÁO: File cũ của part Suppress vẫn còn: " & spPath)
                    End If
                End If
            Catch exSp As Exception
                errors.Add(sp.DetailName & " (suppress): " & exSp.Message)
            End Try
        Next

        If staleCount > 0 Then
            summary.AppendLine()
            summary.AppendLine("File DXF cũ của part Suppress: " & staleCount & " (đã xóa: " & deletedCount & ")")
        End If

        If settings.UpdateInfoAndBom Then
            summary.AppendLine()
            If bomPath <> "" Then
                summary.AppendLine("BOM Excel: " & bomPath)
            Else
                summary.AppendLine("BOM Excel: KHÔNG xuất được (xem LỖI).")
            End If
        End If

        Dim reportPath As String = System.IO.Path.Combine(settings.OutputRoot, "BaoCao_Export_DXF.txt")
        System.IO.File.WriteAllText(reportPath, summary.ToString() & vbCrLf & report.ToString(), System.Text.Encoding.UTF8)

        Dim shortMsg As New System.Text.StringBuilder()
        shortMsg.AppendLine("Part duy nhất : " & items.Count)
        shortMsg.AppendLine("Đã xuất      : " & exported)
        shortMsg.AppendLine("Bỏ qua file  : " & skippedExisting)
        shortMsg.AppendLine("Bỏ qua duyệt : " & skipped.Count)
        If settings.BendMode = "Chỉ đánh dấu 2 đầu" Then
            shortMsg.AppendLine("Đường chấn   : " & bendCount)
        End If
        shortMsg.AppendLine("Lỗi          : " & errors.Count)
        shortMsg.AppendLine("Thời gian    : " & (swTotal.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & " giây (chi tiết trong báo cáo)")
        If settings.UpdateInfoAndBom AndAlso bomPath <> "" Then
            shortMsg.AppendLine("BOM Excel    : " & bomPath)
        End If
        shortMsg.AppendLine()
        shortMsg.AppendLine("Chi tiết: " & reportPath)
        Return shortMsg.ToString()
    Catch ex As Exception
        Return "Lỗi tổng quát: " & ex.Message
    Finally
        If speedModeApplied Then
            ThisApplication.ScreenUpdating = oldScreenUpdating
            ThisApplication.SilentOperation = oldSilentOperation
        End If
    End Try
End Function

'=====================================================================
' CONVERT STEP -> SHEET METAL  (logic giữ nguyên từ 1_Convert_Step v2.0.2)
'=====================================================================
Function RunConvertStep(oAsmDoc As Inventor.AssemblyDocument, s As ExportSettings) As ConvertResult
    Dim res As New ConvertResult()

    Dim filterText As String = s.ConvFilter.Trim().ToUpper()
    Dim skipExisting As Boolean = s.ConvSkipExisting
    Dim conversionMode As Integer = s.ConvMode
    Dim toleranceMM As Double = s.ConvTolerance
    If toleranceMM <= 0 Then toleranceMM = 0.01

    Dim logFolder As String = s.ConvLogFolder.Trim()
    If logFolder = "" Then
        If Not String.IsNullOrEmpty(oAsmDoc.FullFileName) Then
            logFolder = System.IO.Path.GetDirectoryName(oAsmDoc.FullFileName)
        Else
            logFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
        End If
    End If
    Try
        If Not System.IO.Directory.Exists(logFolder) Then System.IO.Directory.CreateDirectory(logFolder)
    Catch
        logFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
    End Try

    Dim log As New System.Text.StringBuilder
    log.AppendLine("SM BATCH CONVERTER - tích hợp V14 (logic v2.0.2: 2 chế độ Thickness, fix stale-face, bỏ Unfold Rule, bỏ ExitEdit gây E_FAIL, log lỗi chi tiết)")
    log.AppendLine("Chế độ    : " & If(conversionMode = 1, "1 - Tự phát hiện độ dày, điền thủ công", "2 - Tự phát hiện độ dày, khớp Sheet Metal Rule"))
    log.AppendLine("Dung sai  : ±" & toleranceMM.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) & " mm")
    log.AppendLine("Thời gian : " & Now.ToString("dd/MM/yyyy HH:mm:ss"))
    log.AppendLine("Assembly  : " & oAsmDoc.DisplayName)
    log.AppendLine("Lọc       : " & If(filterText = "", "(Tất cả)", filterText))
    log.AppendLine(New String("="c, 80))
    log.AppendLine()

    Dim successCount As Integer = 0
    Dim skipCount As Integer = 0
    Dim errorCount As Integer = 0

    Dim oldSilent As Boolean = ThisApplication.SilentOperation
    Dim oldScreenUpd As Boolean = ThisApplication.ScreenUpdating
    Dim swConv As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
    Try
        ThisApplication.SilentOperation = True
        ThisApplication.ScreenUpdating = False   ' V14: tắt vẽ lại giao diện khi Convert hàng loạt

        For Each oDoc As Inventor.Document In oAsmDoc.AllReferencedDocuments
            If oDoc.DocumentType <> Inventor.DocumentTypeEnum.kPartDocumentObject Then Continue For

            Dim oPartDoc As Inventor.PartDocument = CType(oDoc, Inventor.PartDocument)
            Dim fullName As String = System.IO.Path.GetFileName(oPartDoc.FullFileName)
            Dim partName As String = System.IO.Path.GetFileNameWithoutExtension(oPartDoc.FullFileName)

            If filterText <> "" AndAlso Not partName.ToUpper().Contains(filterText) Then Continue For

            log.AppendLine(">>> " & fullName)

            Try
                ' Bỏ qua nếu đã có Flat Pattern
                If skipExisting AndAlso oPartDoc.SubType = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}" Then
                    Dim smCheck As Inventor.SheetMetalComponentDefinition = CType(oPartDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)
                    If smCheck.HasFlatPattern Then
                        log.AppendLine("    → BỎ QUA (đã có Flat Pattern)")
                        skipCount += 1
                        log.AppendLine()
                        Continue For
                    End If
                End If

                ' Convert sang Sheet Metal
                If oPartDoc.SubType <> "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}" Then
                    oPartDoc.SubType = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}"
                End If

                Dim oSMDef As Inventor.SheetMetalComponentDefinition = CType(oPartDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)

                ' === Tìm mặt trong + tính Thickness (lần 1 - chỉ để lấy giá trị Thickness / chọn rule) ===
                Dim tempFace As Inventor.Face = Nothing
                Dim thicknessMM As Double = GetInnerFaceAndThickness(oSMDef, tempFace)

                log.AppendLine("    Thickness = " & thicknessMM.ToString("0.000") & " mm")

                If thicknessMM <= 0.1 OrElse thicknessMM > 30 OrElse tempFace Is Nothing Then
                    Throw New Exception("Không xác định được mặt trong / Thickness")
                End If

                If conversionMode = 1 Then
                    ' Chế độ 1: tự phát hiện độ dày, điền thủ công vào Thickness
                    oSMDef.UseSheetMetalStyleThickness = False
                    oSMDef.Thickness.Value = thicknessMM / 10
                    log.AppendLine("    Điền tay Thickness = " & thicknessMM.ToString("0.000") & " mm")
                Else
                    ' Chế độ 2: khớp với Sheet Metal Rule có sẵn theo Thickness đo được
                    Dim matchedRule As Inventor.SheetMetalStyle = FindMatchingSheetMetalRule(oSMDef, thicknessMM, toleranceMM)
                    If matchedRule Is Nothing Then
                        Throw New Exception("Không tìm thấy Sheet Metal Rule khớp Thickness " & thicknessMM.ToString("0.000") & " mm (tolerance ±" & toleranceMM.ToString("0.000") & "mm)")
                    End If

                    matchedRule.Activate()
                    oSMDef.UseSheetMetalStyleThickness = True
                    log.AppendLine("    Áp Sheet Metal Rule: " & matchedRule.Name & " (Thickness rule = " & (SafeThicknessToCM(matchedRule.Thickness) * 10).ToString("0.000") & " mm)")
                End If

                ' Đổi Thickness/Style khiến Inventor rebuild solid -> tempFace không còn hợp lệ (stale).
                ' Update model rồi tìm lại mặt trong để lấy Face còn "sống" trước khi gán A-Side.
                oPartDoc.Update()
                Dim innerFace As Inventor.Face = Nothing
                GetInnerFaceAndThickness(oSMDef, innerFace)
                If innerFace Is Nothing Then
                    Throw New Exception("Không xác định lại được mặt trong sau khi đổi Thickness")
                End If

                ' Gán A-Side = mặt trong
                Try
                    oSMDef.ASideFace = innerFace
                    log.AppendLine("    Đã gán A-Side = mặt trong")
                Catch
                    log.AppendLine("    Cảnh báo: Không gán được ASideFace")
                End Try

                ' Tạo Flat Pattern (không gọi ExitEdit vì Unfold qua code không vào edit mode)
                Try
                    If Not oSMDef.HasFlatPattern Then
                        oSMDef.Unfold()
                    End If
                Catch exUnfold As Exception
                    Throw New Exception("Lỗi khi Unfold(): " & exUnfold.Message)
                End Try

                Try
                    oPartDoc.Update()
                Catch exUpdate As Exception
                    Throw New Exception("Lỗi khi Update(): " & exUpdate.Message)
                End Try

                Try
                    If oPartDoc.Dirty Then oPartDoc.Save()
                Catch exSave As Exception
                    Throw New Exception("Lỗi khi Save(): " & exSave.Message)
                End Try

                log.AppendLine("    ★ THÀNH CÔNG")
                successCount += 1

            Catch ex As Exception
                log.AppendLine("    ✗ LỖI: " & ex.Message)
                errorCount += 1
            End Try

            log.AppendLine()
        Next
    Catch exAll As Exception
        log.AppendLine("LỖI TỔNG QUÁT: " & exAll.Message)
        errorCount += 1
    Finally
        ThisApplication.SilentOperation = oldSilent
        ThisApplication.ScreenUpdating = oldScreenUpd
        swConv.Stop()
    End Try

    ' Tổng kết
    log.AppendLine(New String("="c, 80))
    log.AppendLine("TỔNG KẾT")
    log.AppendLine("Thành công : " & successCount)
    log.AppendLine("Bỏ qua     : " & skipCount)
    log.AppendLine("Lỗi        : " & errorCount)
    log.AppendLine("Thời gian  : " & (swConv.ElapsedMilliseconds / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) & " giây")
    log.AppendLine("Kết thúc   : " & Now.ToString("dd/MM/yyyy HH:mm:ss"))

    Dim logFile As String = System.IO.Path.Combine(logFolder, "SM_Log_" & Now.ToString("yyyyMMdd_HHmmss") & ".txt")
    Try
        System.IO.File.WriteAllText(logFile, log.ToString(), System.Text.Encoding.UTF8)
    Catch
        logFile = "(không ghi được file log)"
    End Try

    res.Success = successCount
    res.Skip = skipCount
    res.Err = errorCount
    res.LogFile = logFile
    Return res
End Function

' Đọc giá trị Thickness của SheetMetalStyle 1 cách an toàn.
' Bình thường là Double (cm), nhưng 1 số style trả về chuỗi kèm đơn vị (ví dụ "0.120 in").
Function SafeThicknessToCM(raw As Object) As Double
    If TypeOf raw Is Double Then
        Return CDbl(raw)
    End If

    Dim s As String = raw.ToString().Trim()
    Dim numPart As String = System.Text.RegularExpressions.Regex.Match(s, "[-+]?[0-9]*[\.,]?[0-9]+").Value
    numPart = numPart.Replace(",", ".")

    Dim numVal As Double
    If Not Double.TryParse(numPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, numVal) Then
        Throw New Exception("Không đọc được giá trị Thickness: '" & s & "'")
    End If

    Dim sLower As String = s.ToLower()
    If sLower.Contains("in") Then
        Return numVal * 2.54   ' inch -> cm
    ElseIf sLower.Contains("mm") Then
        Return numVal / 10     ' mm -> cm
    Else
        Return numVal          ' cm (hoặc không rõ đơn vị -> coi như đã là cm)
    End If
End Function

' Tìm Sheet Metal Style (Rule) có Thickness khớp gần nhất với thicknessMM (trong phạm vi tolerance).
' Chỉ tìm được trong các style đã có sẵn trong file/project hiện tại.
Function FindMatchingSheetMetalRule(oSMDef As Inventor.SheetMetalComponentDefinition, thicknessMM As Double, toleranceMM As Double) As Inventor.SheetMetalStyle
    Dim bestRule As Inventor.SheetMetalStyle = Nothing
    Dim bestDiff As Double = Double.MaxValue

    For Each oRule As Inventor.SheetMetalStyle In oSMDef.SheetMetalStyles
        Try
            Dim ruleThicknessMM As Double = SafeThicknessToCM(oRule.Thickness) * 10 ' cm -> mm
            Dim diff As Double = Math.Abs(ruleThicknessMM - thicknessMM)
            If diff <= toleranceMM AndAlso diff < bestDiff Then
                bestDiff = diff
                bestRule = oRule
            End If
        Catch
            ' Style này đọc Thickness lỗi -> bỏ qua
        End Try
    Next

    Return bestRule
End Function

' Trả về Thickness (mm) và mặt trong qua ByRef
Function GetInnerFaceAndThickness(oSMDef As Inventor.SheetMetalComponentDefinition, ByRef innerFace As Inventor.Face) As Double
    Try
        If oSMDef.SurfaceBodies.Count = 0 Then Return -1

        Dim oBody As Inventor.SurfaceBody = oSMDef.SurfaceBodies.Item(1)
        Dim minPt As Inventor.Point = oBody.RangeBox.MinPoint
        Dim maxPt As Inventor.Point = oBody.RangeBox.MaxPoint
        Dim centerPt As Inventor.Point = ThisApplication.TransientGeometry.CreatePoint(
            (minPt.X + maxPt.X) / 2,
            (minPt.Y + maxPt.Y) / 2,
            (minPt.Z + maxPt.Z) / 2)

        ' Lấy danh sách mặt phẳng
        Dim faceList As New System.Collections.Generic.List(Of Inventor.Face)
        For Each oFace As Inventor.Face In oBody.Faces
            If TypeOf oFace.Geometry Is Inventor.Plane Then
                faceList.Add(oFace)
            End If
        Next

        If faceList.Count < 2 Then Return FallbackThickness(oSMDef, innerFace)

        ' Sắp xếp theo diện tích giảm dần (không dùng LINQ để không phụ thuộc Imports)
        faceList.Sort(Function(fa As Inventor.Face, fb As Inventor.Face) fb.Evaluator.Area.CompareTo(fa.Evaluator.Area))

        Dim face1 As Inventor.Face = faceList(0)
        Dim face2 As Inventor.Face = Nothing
        Dim maxArea2 As Double = 0
        Dim plane1 As Inventor.Plane = CType(face1.Geometry, Inventor.Plane)

        ' Tìm mặt song song lớn thứ 2
        For i As Integer = 1 To faceList.Count - 1
            Dim f As Inventor.Face = faceList(i)
            Dim pl As Inventor.Plane = CType(f.Geometry, Inventor.Plane)
            If Math.Abs(plane1.Normal.DotProduct(pl.Normal)) > 0.999 Then
                If f.Evaluator.Area > maxArea2 Then
                    maxArea2 = f.Evaluator.Area
                    face2 = f
                End If
            End If
        Next

        If face2 Is Nothing Then
            Return FallbackThickness(oSMDef, innerFace)
        End If

        ' Tính Thickness
        Dim distCM As Double = ThisApplication.MeasureTools.GetMinimumDistance(face1, face2)
        Dim thicknessMM As Double = Math.Round(distCM * 10, 3)

        ' Xác định mặt trong (pháp tuyến hướng về tâm)
        Dim n1 As Inventor.UnitVector
        If face1.IsParamReversed Then
            n1 = plane1.Normal
        Else
            n1 = ThisApplication.TransientGeometry.CreateUnitVector(-plane1.Normal.X, -plane1.Normal.Y, -plane1.Normal.Z)
        End If

        Dim vec1 As Inventor.Vector = centerPt.VectorTo(face1.PointOnFace)
        Dim dot1 As Double = n1.DotProduct(vec1.AsUnitVector)

        Dim plane2 As Inventor.Plane = CType(face2.Geometry, Inventor.Plane)
        Dim n2 As Inventor.UnitVector
        If face2.IsParamReversed Then
            n2 = plane2.Normal
        Else
            n2 = ThisApplication.TransientGeometry.CreateUnitVector(-plane2.Normal.X, -plane2.Normal.Y, -plane2.Normal.Z)
        End If

        Dim vec2 As Inventor.Vector = centerPt.VectorTo(face2.PointOnFace)
        Dim dot2 As Double = n2.DotProduct(vec2.AsUnitVector)

        If dot1 > dot2 Then
            innerFace = face1
        Else
            innerFace = face2
        End If

        Return thicknessMM

    Catch
        Return -1
    End Try
End Function

Function FallbackThickness(oSMDef As Inventor.SheetMetalComponentDefinition, ByRef baseFace As Inventor.Face) As Double
    Try
        Dim oBody As Inventor.SurfaceBody = oSMDef.SurfaceBodies.Item(1)
        Dim maxArea As Double = 0
        baseFace = Nothing

        For Each oFace As Inventor.Face In oBody.Faces
            If TypeOf oFace.Geometry Is Inventor.Plane Then
                If oFace.Evaluator.Area > maxArea Then
                    maxArea = oFace.Evaluator.Area
                    baseFace = oFace
                End If
            End If
        Next

        If baseFace Is Nothing Then Return -1

        Dim pt1 As Inventor.Point = baseFace.PointOnFace
        Dim oPlane As Inventor.Plane = CType(baseFace.Geometry, Inventor.Plane)
        Dim normal As Inventor.UnitVector
        If baseFace.IsParamReversed Then
            normal = oPlane.Normal
        Else
            normal = ThisApplication.TransientGeometry.CreateUnitVector(-oPlane.Normal.X, -oPlane.Normal.Y, -oPlane.Normal.Z)
        End If

        Dim objs As Inventor.ObjectsEnumerator = Nothing
        Dim pts As Inventor.ObjectsEnumerator = Nothing
        oBody.FindUsingRay(pt1, normal, 0.001, objs, pts)

        If pts Is Nothing OrElse pts.Count < 2 Then Return -1

        Dim pt2 As Inventor.Point = CType(pts.Item(2), Inventor.Point)
        Return Math.Round(pt1.DistanceTo(pt2) * 10, 3)
    Catch
        Return -1
    End Try
End Function

'=====================================================================
' FORM CHIA TAB
'=====================================================================
Function ShowExportForm(doc As Inventor.Document) As ExportSettings
    Dim old As ExportSettings = LoadSettings()
    Dim isAsm As Boolean = (doc.DocumentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject)

    Dim f As New System.Windows.Forms.Form()
    f.Text = "Convert STEP + Xuất Flat Pattern DXF - Inventor 2026 (V14)"
    f.Width = 920
    f.Height = 670
    f.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
    f.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
    f.MaximizeBox = False
    f.MinimizeBox = False

    Dim tabs As New System.Windows.Forms.TabControl()
    tabs.Left = 15
    tabs.Top = 10
    tabs.Width = 880
    tabs.Height = 505
    f.Controls.Add(tabs)

    Dim tpConv As New System.Windows.Forms.TabPage("Convert STEP")
    Dim tpExport As New System.Windows.Forms.TabPage("Xuất DXF")
    Dim tpLayer As New System.Windows.Forms.TabPage("Layer & Đường chấn")
    Dim tpInfo As New System.Windows.Forms.TabPage("Thông tin DXF")
    If isAsm Then tabs.TabPages.Add(tpConv)
    tabs.TabPages.Add(tpExport)
    tabs.TabPages.Add(tpLayer)
    tabs.TabPages.Add(tpInfo)

    '===================== TAB 1: CONVERT STEP =====================
    Dim lblFilter As New System.Windows.Forms.Label()
    lblFilter.Text = "Lọc tên Part (để trống = tất cả):"
    lblFilter.Left = 20
    lblFilter.Top = 18
    lblFilter.Width = 400
    tpConv.Controls.Add(lblFilter)

    Dim txtFilter As New System.Windows.Forms.TextBox()
    txtFilter.Left = 20
    txtFilter.Top = 42
    txtFilter.Width = 820
    txtFilter.Text = old.ConvFilter
    tpConv.Controls.Add(txtFilter)

    Dim grpMode As New System.Windows.Forms.GroupBox()
    grpMode.Text = "Chế độ áp Thickness"
    grpMode.Left = 20
    grpMode.Top = 80
    grpMode.Width = 820
    grpMode.Height = 90
    tpConv.Controls.Add(grpMode)

    Dim radMode1 As New System.Windows.Forms.RadioButton()
    radMode1.Text = "1. Tự phát hiện độ dày, điền thủ công vào Thickness"
    radMode1.Left = 15
    radMode1.Top = 25
    radMode1.Width = 700
    radMode1.Checked = (old.ConvMode = 1)
    grpMode.Controls.Add(radMode1)

    Dim radMode2 As New System.Windows.Forms.RadioButton()
    radMode2.Text = "2. Tự phát hiện độ dày, khớp với Sheet Metal Rule có sẵn (khuyên dùng)"
    radMode2.Left = 15
    radMode2.Top = 55
    radMode2.Width = 700
    radMode2.Checked = (old.ConvMode <> 1)
    grpMode.Controls.Add(radMode2)

    Dim chkSkip As New System.Windows.Forms.CheckBox()
    chkSkip.Text = "Bỏ qua Part đã có Flat Pattern"
    chkSkip.Left = 20
    chkSkip.Top = 185
    chkSkip.Width = 400
    chkSkip.Checked = old.ConvSkipExisting
    tpConv.Controls.Add(chkSkip)

    Dim lblTol As New System.Windows.Forms.Label()
    lblTol.Text = "Dung sai khớp Sheet Metal Rule (mm) - dùng cho chế độ 2:"
    lblTol.Left = 20
    lblTol.Top = 220
    lblTol.Width = 500
    tpConv.Controls.Add(lblTol)

    Dim txtTol As New System.Windows.Forms.TextBox()
    txtTol.Left = 20
    txtTol.Top = 244
    txtTol.Width = 70
    txtTol.Text = old.ConvTolerance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    tpConv.Controls.Add(txtTol)

    Dim lblLog As New System.Windows.Forms.Label()
    lblLog.Text = "Thư mục lưu báo cáo (log) Convert:"
    lblLog.Left = 20
    lblLog.Top = 285
    lblLog.Width = 400
    tpConv.Controls.Add(lblLog)

    Dim txtLog As New System.Windows.Forms.TextBox()
    txtLog.Left = 20
    txtLog.Top = 309
    txtLog.Width = 720
    If Not String.IsNullOrEmpty(old.ConvLogFolder) Then
        txtLog.Text = old.ConvLogFolder
    ElseIf Not String.IsNullOrEmpty(doc.FullFileName) Then
        txtLog.Text = System.IO.Path.GetDirectoryName(doc.FullFileName)
    Else
        txtLog.Text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
    End If
    tpConv.Controls.Add(txtLog)

    Dim btnLogBrowse As New System.Windows.Forms.Button()
    btnLogBrowse.Text = "Chọn..."
    btnLogBrowse.Left = 750
    btnLogBrowse.Top = 307
    btnLogBrowse.Width = 90
    btnLogBrowse.Height = 28
    AddHandler btnLogBrowse.Click, Sub(sender As Object, e As System.EventArgs)
        Dim d As New System.Windows.Forms.FolderBrowserDialog()
        d.Description = "Chọn thư mục lưu log Convert"
        d.SelectedPath = txtLog.Text
        If d.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txtLog.Text = d.SelectedPath
        End If
    End Sub
    tpConv.Controls.Add(btnLogBrowse)

    Dim lblConvNote As New System.Windows.Forms.Label()
    lblConvNote.Text = "Convert chuyển các Part (STEP/import) trong Assembly sang Sheet Metal, gán A-Side, tạo Flat Pattern và lưu file."
    lblConvNote.Left = 20
    lblConvNote.Top = 360
    lblConvNote.Width = 820
    lblConvNote.Height = 36
    tpConv.Controls.Add(lblConvNote)

    '===================== TAB 2: XUẤT DXF =====================
    Dim lRoot As New System.Windows.Forms.Label()
    lRoot.Text = "Thư mục xuất:"
    lRoot.Left = 15
    lRoot.Top = 20
    lRoot.Width = 100
    tpExport.Controls.Add(lRoot)

    Dim tRoot As New System.Windows.Forms.TextBox()
    tRoot.Left = 120
    tRoot.Top = 16
    tRoot.Width = 625
    If Not String.IsNullOrEmpty(old.OutputRoot) Then
        tRoot.Text = old.OutputRoot
    ElseIf Not String.IsNullOrEmpty(doc.FullFileName) Then
        tRoot.Text = System.IO.Path.GetDirectoryName(doc.FullFileName)
    Else
        tRoot.Text = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
    End If
    tpExport.Controls.Add(tRoot)

    Dim bRoot As New System.Windows.Forms.Button()
    bRoot.Text = "Chọn..."
    bRoot.Left = 755
    bRoot.Top = 14
    bRoot.Width = 90
    bRoot.Height = 28
    AddHandler bRoot.Click, Sub(sender As Object, e As System.EventArgs)
        Dim d As New System.Windows.Forms.FolderBrowserDialog()
        d.Description = "Chọn thư mục cha để xuất DXF"
        d.SelectedPath = tRoot.Text
        If d.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            tRoot.Text = d.SelectedPath
        End If
    End Sub
    tpExport.Controls.Add(bRoot)

    Dim gMode As New System.Windows.Forms.GroupBox()
    gMode.Text = "Phạm vi xử lý"
    gMode.Left = 15
    gMode.Top = 55
    gMode.Width = 830
    gMode.Height = 60
    tpExport.Controls.Add(gMode)

    Dim lMode As New System.Windows.Forms.Label()
    lMode.Text = If(isAsm, _
                    "Assembly hiện tại – duyệt cả sub-assembly. Part đang Suppress sẽ bị bỏ qua.", _
                    "Part hiện tại – chỉ xuất chi tiết Sheet Metal.")
    lMode.Left = 15
    lMode.Top = 25
    lMode.Width = 700
    gMode.Controls.Add(lMode)

    Dim gName As New System.Windows.Forms.GroupBox()
    gName.Text = "Tên file DXF"
    gName.Left = 15
    gName.Top = 125
    gName.Width = 830
    gName.Height = 95
    tpExport.Controls.Add(gName)

    Dim lPattern As New System.Windows.Forms.Label()
    lPattern.Text = "Mẫu tên:"
    lPattern.Left = 15
    lPattern.Top = 28
    lPattern.Width = 80
    gName.Controls.Add(lPattern)

    Dim tPattern As New System.Windows.Forms.TextBox()
    tPattern.Left = 100
    tPattern.Top = 24
    tPattern.Width = 520
    tPattern.Text = old.FilePattern
    gName.Controls.Add(tPattern)

    Dim lToken As New System.Windows.Forms.Label()
    lToken.Text = "Biến hỗ trợ:  {DoDay}   {Qty}   {VatLieu}   {TenChiTiet}" & vbCrLf & _
                  "Ví dụ: T2.5_Q3_SS400_CAP1.2.dxf"
    lToken.Left = 100
    lToken.Top = 55
    lToken.Width = 700
    lToken.Height = 32
    gName.Controls.Add(lToken)

    Dim gOver As New System.Windows.Forms.GroupBox()
    gOver.Text = "Tùy chọn xuất"
    gOver.Left = 15
    gOver.Top = 230
    gOver.Width = 830
    gOver.Height = 125
    tpExport.Controls.Add(gOver)

    Dim lOver As New System.Windows.Forms.Label()
    lOver.Text = "File trùng tên:"
    lOver.Left = 15
    lOver.Top = 28
    lOver.Width = 90
    gOver.Controls.Add(lOver)

    Dim cOver As New System.Windows.Forms.ComboBox()
    cOver.Left = 110
    cOver.Top = 24
    cOver.Width = 220
    cOver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
    cOver.Items.AddRange(New String() {"Hỏi từng file", "Ghi đè tự động", "Bỏ qua file đã có"})
    cOver.SelectedItem = old.OverwriteMode
    If cOver.SelectedIndex < 0 Then cOver.SelectedIndex = 0
    gOver.Controls.Add(cOver)

    Dim cFast As New System.Windows.Forms.CheckBox()
    cFast.Text = "Tắt cập nhật giao diện (tăng tốc)"
    cFast.Left = 360
    cFast.Top = 26
    cFast.Width = 250
    cFast.Checked = old.FastMode
    gOver.Controls.Add(cFast)

    Dim cCleanupSuppressed As New System.Windows.Forms.CheckBox()
    cCleanupSuppressed.Text = "Tự động xóa file DXF cũ của part đang Suppress"
    cCleanupSuppressed.Left = 15
    cCleanupSuppressed.Top = 58
    cCleanupSuppressed.Width = 400
    cCleanupSuppressed.Checked = old.CleanupSuppressed
    gOver.Controls.Add(cCleanupSuppressed)

    Dim cUpdateBom As New System.Windows.Forms.CheckBox()
    cUpdateBom.Text = "Cập nhật Description + Thickness và xuất BOM Excel"
    cUpdateBom.Left = 15
    cUpdateBom.Top = 88
    cUpdateBom.Width = 420
    cUpdateBom.Checked = old.UpdateInfoAndBom
    gOver.Controls.Add(cUpdateBom)

    '===================== TAB 3: LAYER & ĐƯỜNG CHẤN =====================
    Dim gLayer As New System.Windows.Forms.GroupBox()
    gLayer.Text = "Layer DXF – chỉ chọn layer muốn xuất"
    gLayer.Left = 15
    gLayer.Top = 8
    gLayer.Width = 830
    gLayer.Height = 320
    tpLayer.Controls.Add(gLayer)

    Dim grid As New System.Windows.Forms.DataGridView()
    grid.Left = 15
    grid.Top = 25
    grid.Width = 600
    grid.Height = 275
    grid.AllowUserToAddRows = False
    grid.AllowUserToDeleteRows = False
    grid.RowHeadersVisible = False
    grid.MultiSelect = False
    grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
    grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter
    grid.ColumnHeadersHeight = 30
    grid.RowTemplate.Height = 24
    grid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle

    Dim colCheck As New System.Windows.Forms.DataGridViewCheckBoxColumn()
    colCheck.HeaderText = "Xuất"
    colCheck.Width = 50
    grid.Columns.Add(colCheck)

    grid.Columns.Add("Layer", "Tên layer")
    grid.Columns(1).Width = 180
    grid.Columns.Add("MoTa", "Mô tả")
    grid.Columns(2).Width = 340
    gLayer.Controls.Add(grid)

    Dim layerNames() As String = {
        "IV_OUTER_PROFILE", "IV_INTERIOR_PROFILES", "IV_BEND_UP", "IV_BEND_DOWN",
        "IV_TOOL_CENTER", "IV_ARC_CENTERS", "IV_TANGENT", "IV_FEATURE_PROFILES",
        "IV_UNCONSUMED_SKETCHES", "IV_ROLL_TANGENT", "IV_ROLL"
    }
    Dim layerArgs() As String = {
        "OuterProfileLayer", "InteriorProfilesLayer", "BendUpLayer", "BendDownLayer",
        "ToolCenterLayer", "ArcCentersLayer", "TangentLayer", "FeatureProfilesLayer",
        "UnconsumedSketchesLayer", "TangentRollLinesLayer", "RollLinesLayer"
    }
    Dim layerDescriptions() As String = {
        "Đường bao ngoài", "Đường bao trong / lỗ", "Đường gấp lên (bend up)", "Đường gấp xuống (bend down)",
        "Tâm dụng cụ (tool center)", "Tâm cung tròn", "Đường tiếp tuyến", "Đường bao chi tiết đặc trưng",
        "Sketch chưa dùng đến", "Tiếp tuyến đường cán (roll)", "Đường cán (roll line)"
    }

    For i As Integer = 0 To layerNames.Length - 1
        Dim row As Integer = grid.Rows.Add()
        grid.Rows(row).Cells(0).Value = old.IsLayerEnabled(layerNames(i), (i = 0 OrElse i = 1))
        grid.Rows(row).Cells(1).Value = layerNames(i)
        grid.Rows(row).Cells(2).Value = layerDescriptions(i)
    Next

    Dim bAll As New System.Windows.Forms.Button()
    bAll.Text = "Chọn tất cả"
    bAll.Left = 635
    bAll.Top = 40
    bAll.Width = 180
    bAll.Height = 32
    AddHandler bAll.Click, Sub(s, e)
        For i As Integer = 0 To grid.Rows.Count - 1
            grid.Rows(i).Cells(0).Value = True
        Next
    End Sub
    gLayer.Controls.Add(bAll)

    Dim bNone As New System.Windows.Forms.Button()
    bNone.Text = "Bỏ chọn tất cả"
    bNone.Left = 635
    bNone.Top = 80
    bNone.Width = 180
    bNone.Height = 32
    AddHandler bNone.Click, Sub(s, e)
        For i As Integer = 0 To grid.Rows.Count - 1
            grid.Rows(i).Cells(0).Value = False
        Next
    End Sub
    gLayer.Controls.Add(bNone)

    Dim bDefault As New System.Windows.Forms.Button()
    bDefault.Text = "Mặc định (Outer + Interior)"
    bDefault.Left = 635
    bDefault.Top = 120
    bDefault.Width = 180
    bDefault.Height = 32
    AddHandler bDefault.Click, Sub(s, e)
        For i As Integer = 0 To grid.Rows.Count - 1
            grid.Rows(i).Cells(0).Value = (i = 0 OrElse i = 1)
        Next
    End Sub
    gLayer.Controls.Add(bDefault)

    Dim gBend As New System.Windows.Forms.GroupBox()
    gBend.Text = "Xử lý đường chấn (Bend)"
    gBend.Left = 15
    gBend.Top = 335
    gBend.Width = 830
    gBend.Height = 90
    tpLayer.Controls.Add(gBend)

    Dim lBendMode As New System.Windows.Forms.Label()
    lBendMode.Text = "Kiểu đường chấn:"
    lBendMode.Left = 15
    lBendMode.Top = 28
    lBendMode.Width = 110
    gBend.Controls.Add(lBendMode)

    Dim cBend As New System.Windows.Forms.ComboBox()
    cBend.Left = 130
    cBend.Top = 24
    cBend.Width = 220
    cBend.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
    cBend.Items.AddRange(New String() {"Giữ nguyên đường dài", "Chỉ đánh dấu 2 đầu"})
    cBend.SelectedItem = old.BendMode
    If cBend.SelectedIndex < 0 Then cBend.SelectedIndex = 0
    gBend.Controls.Add(cBend)

    Dim lBendHelp As New System.Windows.Forms.Label()
    lBendHelp.Text = "Đánh dấu 2 đầu ngoài cùng của đường chấn (đơn vị mm)"
    lBendHelp.Left = 365
    lBendHelp.Top = 28
    lBendHelp.Width = 450
    gBend.Controls.Add(lBendHelp)

    Dim lUp As New System.Windows.Forms.Label()
    lUp.Text = "Bend UP – dài:"
    lUp.Left = 15
    lUp.Top = 60
    lUp.Width = 95
    gBend.Controls.Add(lUp)

    Dim tUpLen As New System.Windows.Forms.TextBox()
    tUpLen.Left = 112
    tUpLen.Top = 56
    tUpLen.Width = 50
    tUpLen.Text = old.BendUpLen.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gBend.Controls.Add(tUpLen)

    Dim lUpInset As New System.Windows.Forms.Label()
    lUpInset.Text = "cách mép:"
    lUpInset.Left = 175
    lUpInset.Top = 60
    lUpInset.Width = 65
    gBend.Controls.Add(lUpInset)

    Dim tUpInset As New System.Windows.Forms.TextBox()
    tUpInset.Left = 243
    tUpInset.Top = 56
    tUpInset.Width = 50
    tUpInset.Text = old.BendUpInset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gBend.Controls.Add(tUpInset)

    Dim lDn As New System.Windows.Forms.Label()
    lDn.Text = "Bend DOWN – dài:"
    lDn.Left = 390
    lDn.Top = 60
    lDn.Width = 110
    gBend.Controls.Add(lDn)

    Dim tDnLen As New System.Windows.Forms.TextBox()
    tDnLen.Left = 503
    tDnLen.Top = 56
    tDnLen.Width = 50
    tDnLen.Text = old.BendDownLen.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gBend.Controls.Add(tDnLen)

    Dim lDnInset As New System.Windows.Forms.Label()
    lDnInset.Text = "cách mép:"
    lDnInset.Left = 566
    lDnInset.Top = 60
    lDnInset.Width = 65
    gBend.Controls.Add(lDnInset)

    Dim tDnInset As New System.Windows.Forms.TextBox()
    tDnInset.Left = 634
    tDnInset.Top = 56
    tDnInset.Width = 50
    tDnInset.Text = old.BendDownInset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gBend.Controls.Add(tDnInset)

    Dim updateBendEnabled As System.Action = Sub()
        Dim bendOn As Boolean = (cBend.SelectedIndex = 1)
        tUpLen.Enabled = bendOn
        tUpInset.Enabled = bendOn
        tDnLen.Enabled = bendOn
        tDnInset.Enabled = bendOn
    End Sub
    AddHandler cBend.SelectedIndexChanged, Sub(s, e)
        updateBendEnabled()
    End Sub
    updateBendEnabled()

    '===================== TAB 4: THÔNG TIN TRÊN DXF =====================
    Dim gInfo As New System.Windows.Forms.GroupBox()
    gInfo.Text = "Ghi thông tin Part vào DXF"
    gInfo.Left = 15
    gInfo.Top = 15
    gInfo.Width = 830
    gInfo.Height = 190
    tpInfo.Controls.Add(gInfo)

    Dim cInfo As New System.Windows.Forms.CheckBox()
    cInfo.Text = "Ghi thông tin Part trực tiếp vào DXF"
    cInfo.Left = 15
    cInfo.Top = 28
    cInfo.Width = 350
    cInfo.Checked = old.AddInfoText
    gInfo.Controls.Add(cInfo)

    Dim lAlign As New System.Windows.Forms.Label()
    lAlign.Text = "Căn text:"
    lAlign.Left = 15
    lAlign.Top = 70
    lAlign.Width = 70
    gInfo.Controls.Add(lAlign)

    Dim cAlign As New System.Windows.Forms.ComboBox()
    cAlign.Left = 90
    cAlign.Top = 66
    cAlign.Width = 150
    cAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
    cAlign.Items.AddRange(New String() {"Căn trái", "Căn giữa"})
    cAlign.SelectedItem = old.TextAlignment
    If cAlign.SelectedIndex < 0 Then cAlign.SelectedIndex = 1
    gInfo.Controls.Add(cAlign)

    Dim lTextHeight As New System.Windows.Forms.Label()
    lTextHeight.Text = "Cỡ chữ:"
    lTextHeight.Left = 260
    lTextHeight.Top = 70
    lTextHeight.Width = 60
    gInfo.Controls.Add(lTextHeight)

    Dim tTextHeight As New System.Windows.Forms.TextBox()
    tTextHeight.Left = 325
    tTextHeight.Top = 66
    tTextHeight.Width = 70
    tTextHeight.Text = old.TextHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gInfo.Controls.Add(tTextHeight)

    Dim lTextGap As New System.Windows.Forms.Label()
    lTextGap.Text = "Khoảng dòng:"
    lTextGap.Left = 420
    lTextGap.Top = 70
    lTextGap.Width = 90
    gInfo.Controls.Add(lTextGap)

    Dim tTextGap As New System.Windows.Forms.TextBox()
    tTextGap.Left = 515
    tTextGap.Top = 66
    tTextGap.Width = 70
    tTextGap.Text = old.TextLineGap.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gInfo.Controls.Add(tTextGap)

    Dim lTextUnit As New System.Windows.Forms.Label()
    lTextUnit.Text = "(đơn vị DXF)"
    lTextUnit.Left = 595
    lTextUnit.Top = 70
    lTextUnit.Width = 100
    gInfo.Controls.Add(lTextUnit)

    Dim lYOffset As New System.Windows.Forms.Label()
    lYOffset.Text = "Khoảng Y:"
    lYOffset.Left = 15
    lYOffset.Top = 112
    lYOffset.Width = 70
    gInfo.Controls.Add(lYOffset)

    Dim tYOffset As New System.Windows.Forms.TextBox()
    tYOffset.Left = 90
    tYOffset.Top = 108
    tYOffset.Width = 100
    tYOffset.Text = old.TextYOffset.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    gInfo.Controls.Add(tYOffset)

    Dim lYOffsetHelp As New System.Windows.Forms.Label()
    lYOffsetHelp.Text = "Âm = đặt dưới hình; mặc định -250"
    lYOffsetHelp.Left = 200
    lYOffsetHelp.Top = 112
    lYOffsetHelp.Width = 260
    gInfo.Controls.Add(lYOffsetHelp)

    Dim lStyleInfo As New System.Windows.Forms.Label()
    lStyleInfo.Text = "TEXT ghi vào layer DXF_INFO / style DXF_INFO_STYLE (R12, font txt.shx); tiếng Việt được tự bỏ dấu."
    lStyleInfo.Left = 15
    lStyleInfo.Top = 150
    lStyleInfo.Width = 650
    gInfo.Controls.Add(lStyleInfo)

    '===================== PHẦN DƯỚI: HÀNH ĐỘNG + NÚT =====================
    Dim lAction As New System.Windows.Forms.Label()
    lAction.Text = "Hành động:"
    lAction.Left = 20
    lAction.Top = 530
    lAction.Width = 80
    f.Controls.Add(lAction)

    Dim cAction As New System.Windows.Forms.ComboBox()
    cAction.Left = 105
    cAction.Top = 526
    cAction.Width = 270
    cAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
    If isAsm Then
        cAction.Items.AddRange(New String() {"Chỉ Convert", "Chỉ Xuất DXF + BOM", "Convert rồi Xuất DXF"})
        Dim ai As Integer = old.ActionIndex
        If ai < 0 OrElse ai > 2 Then ai = 1
        cAction.SelectedIndex = ai
    Else
        cAction.Items.AddRange(New String() {"Chỉ Xuất DXF + BOM"})
        cAction.SelectedIndex = 0
        cAction.Enabled = False
    End If
    f.Controls.Add(cAction)

    AddHandler cAction.SelectedIndexChanged, Sub(s, e)
        If isAsm Then
            If cAction.SelectedIndex = 0 Then tabs.SelectedTab = tpConv
            If cAction.SelectedIndex = 1 Then tabs.SelectedTab = tpExport
        End If
    End Sub

    Dim bOK As New System.Windows.Forms.Button()
    bOK.Text = "CHẠY"
    bOK.Left = 620
    bOK.Top = 570
    bOK.Width = 130
    bOK.Height = 36
    bOK.DialogResult = System.Windows.Forms.DialogResult.OK
    f.Controls.Add(bOK)

    Dim bCancel As New System.Windows.Forms.Button()
    bCancel.Text = "HỦY"
    bCancel.Left = 760
    bCancel.Top = 570
    bCancel.Width = 120
    bCancel.Height = 36
    bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
    f.Controls.Add(bCancel)

    Call Microsoft.VisualBasic.Interaction.CallByName(f, "AcceptButton", Microsoft.VisualBasic.CallType.Let, bOK)
    Call Microsoft.VisualBasic.Interaction.CallByName(f, "CancelButton", Microsoft.VisualBasic.CallType.Let, bCancel)

    Dim lAuthor As New System.Windows.Forms.Label()
    lAuthor.Text = "Tác giả: Phạm Đức Duy"
    lAuthor.Left = 20
    lAuthor.Top = 580
    lAuthor.Width = 400
    f.Controls.Add(lAuthor)

    If f.ShowDialog() <> System.Windows.Forms.DialogResult.OK Then Return Nothing

    Dim result As New ExportSettings()

    ' Hành động
    If isAsm Then
        result.ActionIndex = cAction.SelectedIndex
        If result.ActionIndex < 0 Then result.ActionIndex = 1
    Else
        result.ActionIndex = old.ActionIndex
    End If
    result.DoConvert = isAsm AndAlso (result.ActionIndex = 0 OrElse result.ActionIndex = 2)
    result.DoExport = Not isAsm OrElse (result.ActionIndex = 1 OrElse result.ActionIndex = 2)

    If result.DoExport AndAlso String.IsNullOrWhiteSpace(tRoot.Text) Then
        System.Windows.Forms.MessageBox.Show("Chưa chọn thư mục xuất.", "Xuất DXF")
        Return Nothing
    End If

    ' Convert
    result.ConvFilter = txtFilter.Text.Trim()
    result.ConvMode = If(radMode1.Checked, 1, 2)
    result.ConvSkipExisting = chkSkip.Checked
    ' Ép InvariantCulture để "0.01" không bị đọc sai theo locale Tiếng Việt; chấp nhận cả dấu , và .
    Double.TryParse(txtTol.Text.Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, result.ConvTolerance)
    If result.ConvTolerance <= 0 Then result.ConvTolerance = 0.01
    result.ConvLogFolder = txtLog.Text.Trim()

    ' Xuất DXF
    result.OutputRoot = tRoot.Text.Trim()
    result.FilePattern = tPattern.Text.Trim()
    result.OverwriteMode = CStr(cOver.SelectedItem)
    result.FastMode = cFast.Checked
    result.CleanupSuppressed = cCleanupSuppressed.Checked
    result.UpdateInfoAndBom = cUpdateBom.Checked

    ' Thông tin trên DXF
    result.AddInfoText = cInfo.Checked
    result.TextAlignment = CStr(cAlign.SelectedItem)
    Double.TryParse(tTextHeight.Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.TextHeight)
    Double.TryParse(tTextGap.Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.TextLineGap)
    Double.TryParse(tYOffset.Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.TextYOffset)
    If result.TextHeight <= 0 Then result.TextHeight = 10.0
    If result.TextLineGap <= 0 Then result.TextLineGap = result.TextHeight * 1.5

    ' Đường chấn
    result.BendMode = CStr(cBend.SelectedItem)
    Double.TryParse(tUpLen.Text.Trim().Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.BendUpLen)
    Double.TryParse(tUpInset.Text.Trim().Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.BendUpInset)
    Double.TryParse(tDnLen.Text.Trim().Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.BendDownLen)
    Double.TryParse(tDnInset.Text.Trim().Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, result.BendDownInset)
    If result.BendUpLen <= 0 Then result.BendUpLen = 10.0
    If result.BendUpInset < 0 Then result.BendUpInset = 3.0
    If result.BendDownLen <= 0 Then result.BendDownLen = 3.0
    If result.BendDownInset < 0 Then result.BendDownInset = 3.0

    result.DxfOptions = BuildDxfOptionsFromGrid(grid, layerArgs, layerNames)
    result.LayerRows = ReadLayerRows(grid, layerNames)
    Return result
End Function

Function BuildDxfOptionsFromGrid(grid As System.Windows.Forms.DataGridView, args() As String, names() As String) As String
    Dim hidden As New System.Text.StringBuilder()
    Dim options As New System.Text.StringBuilder()
    options.Append("FLAT PATTERN DXF?AcadVersion=R12")

    For i As Integer = 0 To grid.Rows.Count - 1
        Dim arg As String = args(i)
        Dim layer As String = names(i)
        Dim enabled As Boolean = CBool(grid.Rows(i).Cells(0).Value)
        If Not enabled Then
            If hidden.Length > 0 Then hidden.Append(";")
            hidden.Append(layer)
        End If
        options.Append("&" & arg & "=" & layer)
    Next
    options.Append("&InvisibleLayers=" & hidden.ToString())
    options.Append("&RebaseGeometry=True")
    Return options.ToString()
End Function

Function ReadLayerRows(grid As System.Windows.Forms.DataGridView, names() As String) As System.Collections.Generic.List(Of LayerRow)
    Dim result As New System.Collections.Generic.List(Of LayerRow)
    For i As Integer = 0 To grid.Rows.Count - 1
        Dim r As New LayerRow()
        r.Name = names(i)
        r.Enabled = CBool(grid.Rows(i).Cells(0).Value)
        result.Add(r)
    Next
    Return result
End Function

'=====================================================================
' V12 - XỬ LÝ ĐƯỜNG CHẤN: thay đường chấn dài bằng vạch ở 2 đầu ngoài cùng
' Trả về số đường chấn (sau khi gộp các đoạn thẳng hàng) đã xử lý.
'=====================================================================
' V14: làm việc trên danh sách dòng trong bộ nhớ (đọc/ghi file chỉ 1 lần ở PostProcessDxf).
' Chỉ thay nội dung "lines" ở cuối hàm, nên nếu có lỗi giữa chừng thì "lines" vẫn nguyên vẹn.
Function ConvertBendLinesToEndMarks(lines As System.Collections.Generic.List(Of String), s As ExportSettings) As Integer
    Dim inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture

    ' --- Tìm ranh giới ENTITIES ... ENDSEC ---
    Dim entStart As Integer = -1
    Dim entEnd As Integer = -1
    Dim i As Integer = 0
    Do While i < lines.Count - 1
        Dim c As String = lines(i).Trim()
        Dim v As String = lines(i + 1).Trim().ToUpperInvariant()
        If entStart < 0 Then
            If c = "2" AndAlso v = "ENTITIES" Then entStart = i + 2
        ElseIf c = "0" AndAlso v = "ENDSEC" Then
            entEnd = i
            Exit Do
        End If
        i += 2
    Loop
    If entStart < 0 OrElse entEnd < 0 Then Throw New Exception("Không tìm thấy ranh giới ENTITIES/ENDSEC.")

    ' --- Tách danh sách entity ---
    Dim ents As New System.Collections.Generic.List(Of EntRange)
    Dim cur As EntRange = Nothing
    i = entStart
    Do While i < entEnd
        If lines(i).Trim() = "0" Then
            If cur IsNot Nothing Then cur.EndIdx = i
            cur = New EntRange()
            cur.StartIdx = i
            cur.TypeName = lines(i + 1).Trim().ToUpperInvariant()
            ents.Add(cur)
        End If
        i += 2
    Loop
    If cur IsNot Nothing Then cur.EndIdx = entEnd

    ' --- Gom các LINE thuộc layer chấn thành từng đường chấn (cùng layer, cùng phương, cùng vị trí) ---
    Dim groups As New System.Collections.Generic.List(Of BendGroup)
    For Each ent As EntRange In ents
        If ent.TypeName <> "LINE" Then Continue For

        Dim layer As String = ""
        Dim x1 As Double = 0.0
        Dim y1 As Double = 0.0
        Dim x2 As Double = 0.0
        Dim y2 As Double = 0.0
        Dim ok1 As Boolean = False
        Dim ok2 As Boolean = False
        Dim ok3 As Boolean = False
        Dim ok4 As Boolean = False

        For j As Integer = ent.StartIdx + 2 To ent.EndIdx - 2 Step 2
            Dim code As String = lines(j).Trim()
            Dim val As String = lines(j + 1).Trim()
            If code = "8" Then
                layer = val
            ElseIf code = "10" Then
                ok1 = Double.TryParse(val, System.Globalization.NumberStyles.Float, inv, x1)
            ElseIf code = "20" Then
                ok2 = Double.TryParse(val, System.Globalization.NumberStyles.Float, inv, y1)
            ElseIf code = "11" Then
                ok3 = Double.TryParse(val, System.Globalization.NumberStyles.Float, inv, x2)
            ElseIf code = "21" Then
                ok4 = Double.TryParse(val, System.Globalization.NumberStyles.Float, inv, y2)
            End If
        Next

        Dim layerU As String = layer.ToUpperInvariant()
        If layerU <> "IV_BEND_UP" AndAlso layerU <> "IV_BEND_DOWN" Then Continue For
        If Not (ok1 AndAlso ok2 AndAlso ok3 AndAlso ok4) Then Continue For

        Dim dx As Double = x2 - x1
        Dim dy As Double = y2 - y1
        Dim segLen As Double = Math.Sqrt(dx * dx + dy * dy)
        If segLen < 0.000001 Then Continue For

        Dim ux As Double = dx / segLen
        Dim uy As Double = dy / segLen
        ' Chuẩn hóa chiều để các đoạn cùng phương luôn cùng hướng
        If ux < -0.000000001 OrElse (Math.Abs(ux) <= 0.000000001 AndAlso uy < 0) Then
            ux = -ux
            uy = -uy
        End If
        Dim nx As Double = -uy
        Dim ny As Double = ux
        Dim d As Double = x1 * nx + y1 * ny
        Dim t1 As Double = x1 * ux + y1 * uy
        Dim t2 As Double = x2 * ux + y2 * uy
        Dim lo As Double = Math.Min(t1, t2)
        Dim hi As Double = Math.Max(t1, t2)

        Dim g As BendGroup = Nothing
        For Each cand As BendGroup In groups
            If cand.Layer = layerU AndAlso Math.Abs(cand.Ux * uy - cand.Uy * ux) < 0.0001 AndAlso Math.Abs(cand.D - d) < 0.05 Then
                g = cand
                Exit For
            End If
        Next

        If g Is Nothing Then
            g = New BendGroup()
            g.Layer = layerU
            g.Ux = ux
            g.Uy = uy
            g.D = d
            g.TMin = lo
            g.TMax = hi
            ' Giữ lại các mã nhóm khác (màu, linetype...) để vạch mới giống đường gốc
            For j As Integer = ent.StartIdx + 2 To ent.EndIdx - 2 Step 2
                Dim code As String = lines(j).Trim()
                If code <> "10" AndAlso code <> "11" AndAlso code <> "20" AndAlso code <> "21" _
                   AndAlso code <> "30" AndAlso code <> "31" AndAlso code <> "5" AndAlso code <> "330" Then
                    g.Template.Add(lines(j))
                    g.Template.Add(lines(j + 1))
                End If
            Next
            groups.Add(g)
        Else
            If lo < g.TMin Then g.TMin = lo
            If hi > g.TMax Then g.TMax = hi
        End If

        ent.Removed = True
    Next

    If groups.Count = 0 Then Return 0

    ' --- Dựng lại danh sách dòng ---
    Dim result As New System.Collections.Generic.List(Of String)
    For k As Integer = 0 To entStart - 1
        result.Add(lines(k))
    Next
    For Each ent As EntRange In ents
        If Not ent.Removed Then
            For k As Integer = ent.StartIdx To ent.EndIdx - 1
                result.Add(lines(k))
            Next
        End If
    Next

    For Each g As BendGroup In groups
        Dim isUp As Boolean = (g.Layer = "IV_BEND_UP")
        Dim tickLen As Double = If(isUp, s.BendUpLen, s.BendDownLen)
        Dim inset As Double = If(isUp, s.BendUpInset, s.BendDownInset)
        Dim totalLen As Double = g.TMax - g.TMin

        If totalLen >= 2.0 * (inset + tickLen) + 1.0 Then
            ' Đủ chỗ: 2 vạch ở 2 đầu
            AddBendLine(result, g, g.TMin + inset, g.TMin + inset + tickLen, inv)
            AddBendLine(result, g, g.TMax - inset - tickLen, g.TMax - inset, inv)
        ElseIf totalLen - 2.0 * inset > 0.1 Then
            ' Quá ngắn: chỉ 1 đoạn thẳng nằm giữa 2 mép (cách mỗi mép đúng khoảng "cách mép")
            AddBendLine(result, g, g.TMin + inset, g.TMax - inset, inv)
        Else
            ' Cực ngắn (không đủ chỗ cho khoảng cách mép): giữ nguyên cả đường
            AddBendLine(result, g, g.TMin, g.TMax, inv)
        End If
    Next

    For k As Integer = entEnd To lines.Count - 1
        result.Add(lines(k))
    Next

    If Not ValidateDxfLines(result) Then Throw New Exception("DXF không hợp lệ sau khi xử lý đường chấn.")
    lines.Clear()
    lines.AddRange(result)

    Return groups.Count
End Function

Sub AddBendLine(dest As System.Collections.Generic.List(Of String), g As BendGroup, t1 As Double, t2 As Double, inv As System.Globalization.CultureInfo)
    ' Điểm tại tham số t: P = n*D + u*t, với n = (-Uy, Ux)
    Dim xa As Double = -g.Uy * g.D + g.Ux * t1
    Dim ya As Double = g.Ux * g.D + g.Uy * t1
    Dim xb As Double = -g.Uy * g.D + g.Ux * t2
    Dim yb As Double = g.Ux * g.D + g.Uy * t2

    dest.Add("0")
    dest.Add("LINE")
    dest.AddRange(g.Template)
    dest.Add("10")
    dest.Add(xa.ToString("0.####", inv))
    dest.Add("20")
    dest.Add(ya.ToString("0.####", inv))
    dest.Add("30")
    dest.Add("0")
    dest.Add("11")
    dest.Add(xb.ToString("0.####", inv))
    dest.Add("21")
    dest.Add(yb.ToString("0.####", inv))
    dest.Add("31")
    dest.Add("0")
End Sub

' V14: chèn TEXT vào danh sách dòng trong bộ nhớ (file được đọc/ghi 1 lần ở PostProcessDxf).
Sub AddInfoTextToDxf(lines As System.Collections.Generic.List(Of String), item As ExportItem, textAlignment As String, textHeight As Double, textLineGap As Double, textYOffset As Double)
    Dim entityEnd As Integer = -1
    Dim minX As Double = 0.0
    Dim maxX As Double = 0.0
    Dim minY As Double = 0.0
    Dim maxY As Double = 0.0
    Dim hasX As Boolean = False
    Dim hasY As Boolean = False

    Dim i As Integer = 0
    Dim inEntities As Boolean = False
    Do While i < lines.Count - 1
        Dim code As Integer = -1
        Dim c As String = lines(i).Trim()
        Dim v As String = lines(i + 1).Trim()
        If Integer.TryParse(c, code) Then
            If code = 2 AndAlso v.ToUpperInvariant() = "ENTITIES" Then
                inEntities = True
            ElseIf inEntities AndAlso code = 0 AndAlso v.ToUpperInvariant() = "ENDSEC" Then
                entityEnd = i
                Exit Do
            ElseIf inEntities AndAlso (code = 10 OrElse code = 11 OrElse code = 13 OrElse code = 14) Then
                Dim x As Double = 0.0
                If Double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, x) Then
                    If Not hasX Then
                        minX = x
                        maxX = x
                        hasX = True
                    Else
                        If x < minX Then minX = x
                        If x > maxX Then maxX = x
                    End If
                End If
            ElseIf inEntities AndAlso (code = 20 OrElse code = 21 OrElse code = 23 OrElse code = 24) Then
                Dim y As Double = 0.0
                If Double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, y) Then
                    If Not hasY Then
                        minY = y
                        maxY = y
                        hasY = True
                    Else
                        If y < minY Then minY = y
                        If y > maxY Then maxY = y
                    End If
                End If
            End If
        End If
        i += 2
    Loop

    If entityEnd < 0 Then Throw New Exception("TEXT - Không tìm thấy ranh giới ENTITIES/ENDSEC.")
    If Not hasX Then
        minX = 0.0
        maxX = 0.0
    End If
    If Not hasY Then
        minY = 0.0
        maxY = 0.0
    End If

    Dim cx As Double = (minX + maxX) / 2.0
    Dim h As Double = Math.Abs(textHeight)
    If h < 0.1 Then h = 10.0
    Dim gap As Double = Math.Abs(textLineGap)
    If gap < h * 1.2 Then gap = h * 1.2
    Dim baseY As Double = minY + textYOffset

    Dim entityCountBefore As Integer = CountDxfEntities(lines)

    If Not EnsureInfoLayerAndStyle(lines) Then
        Throw New Exception("TEXT - Không tìm thấy TABLES/LAYER/STYLE phù hợp để tạo layer/style thử nghiệm.")
    End If

    ' TABLES đã tăng số dòng; bắt buộc quét lại ENTITIES và bounding box.
    If Not RefreshEntitiesMetrics(lines, entityEnd, minX, maxX, minY, maxY, hasX, hasY) Then
        Throw New Exception("TEXT - Không quét lại được ENTITIES sau khi cập nhật TABLES.")
    End If

    Dim info() As String = { _
        "DO DAY: " & item.Thickness.Replace("T", ""), _
        "SO LUONG: " & item.Quantity.ToString(), _
        "VAT LIEU: " & CleanTextForDxf(item.Material), _
        "TEN CHI TIET: " & CleanTextForDxf(item.DetailName) _
    }
    Dim startY As Double = baseY + (info.Length - 1) * gap
    Dim insertLines As New System.Collections.Generic.List(Of String)
    For n As Integer = 0 To info.Length - 1
        Dim lineText As String = info(n)
        Dim textWidth As Double = h * 0.6 * lineText.Length
        Dim x As Double
        If textAlignment = "Căn giữa" AndAlso textWidth <= (maxX - minX) Then
            x = cx - textWidth / 2.0
            If x < minX Then x = minX
            If x + textWidth > maxX Then x = maxX - textWidth
        Else
            x = minX
        End If
        ' Tất cả các dòng dùng đúng khoảng cách TextLineGap; không cộng thêm riêng cho tên chi tiết.
        Dim y As Double = startY - (n * gap)

        ' TEXT dùng layer DXF_INFO / style DXF_INFO_STYLE đã được thêm vào TABLES.
        insertLines.Add("0")
        insertLines.Add("TEXT")
        insertLines.Add("8")
        insertLines.Add("DXF_INFO")
        insertLines.Add("10")
        insertLines.Add(x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
        insertLines.Add("20")
        insertLines.Add(y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
        insertLines.Add("30")
        insertLines.Add("0")
        insertLines.Add("40")
        insertLines.Add(h.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
        insertLines.Add("1")
        insertLines.Add(lineText)
        insertLines.Add("7")
        insertLines.Add("DXF_INFO_STYLE")
    Next

    lines.InsertRange(entityEnd, insertLines)
    Dim entityCountAfter As Integer = CountDxfEntities(lines)
    If entityCountAfter <> entityCountBefore + info.Length Then
        Throw New Exception("TEXT - Số entity thay đổi bất thường trước/sau khi chèn: " & entityCountBefore.ToString() & " -> " & entityCountAfter.ToString())
    End If

    If Not ValidateDxfLines(lines) Then Throw New Exception("TEXT - DXF không hợp lệ sau khi chèn.")
End Sub

' V14: đọc DXF 1 lần -> xử lý đường chấn + chèn TEXT trong bộ nhớ -> kiểm tra -> ghi 1 lần.
' Trả về số đường chấn đã đánh dấu. Lỗi đường chấn được ghi vào errors và không chặn bước chèn TEXT.
' Lỗi chèn TEXT / ghi file được ném ra ngoài (giống V13).
Function PostProcessDxf(dxfPath As String, item As ExportItem, settings As ExportSettings, errors As System.Collections.Generic.List(Of String)) As Integer
    Dim doBend As Boolean = (settings.BendMode = "Chỉ đánh dấu 2 đầu")
    If Not doBend AndAlso Not settings.AddInfoText Then Return 0
    If Not System.IO.File.Exists(dxfPath) Then Throw New Exception("Không tìm thấy DXF sau khi xuất: " & dxfPath)

    Dim lines As New System.Collections.Generic.List(Of String)(System.IO.File.ReadAllLines(dxfPath, DxfEncoding()))
    Dim bendN As Integer = 0

    If doBend Then
        Try
            bendN = ConvertBendLinesToEndMarks(lines, settings)
        Catch exBend As Exception
            errors.Add(item.DetailName & " (đường chấn): " & exBend.Message)
        End Try
    End If

    If settings.AddInfoText Then
        AddInfoTextToDxf(lines, item, settings.TextAlignment, settings.TextHeight, settings.TextLineGap, settings.TextYOffset)
    End If

    Dim tempPath As String = dxfPath & ".tmp_" & System.Guid.NewGuid().ToString("N")
    Try
        System.IO.File.WriteAllLines(tempPath, lines.ToArray(), DxfEncoding())
        System.IO.File.Copy(tempPath, dxfPath, True)
    Catch ex As Exception
        Throw New Exception("Không ghi được DXF sau khi xử lý: " & ex.Message)
    Finally
        If System.IO.File.Exists(tempPath) Then System.IO.File.Delete(tempPath)
    End Try
    Return bendN
End Function

' V14: Inventor 2026 chạy .NET 8, Encoding.Default = UTF-8 (khác .NET Framework = ANSI).
' DXF R12 do Inventor ghi là 1 byte/ký tự, nên dùng ISO-8859-1 (Latin1): đọc rồi ghi lại giữ nguyên từng byte.
Function DxfEncoding() As System.Text.Encoding
    Return System.Text.Encoding.GetEncoding(28591)
End Function

Function EnsureInfoLayerAndStyle(lines As System.Collections.Generic.List(Of String)) As Boolean
    Dim layerTable As Integer = -1
    Dim styleTable As Integer = -1
    Dim layerEnd As Integer = -1
    Dim styleEnd As Integer = -1
    Dim i As Integer = 0

    Do While i < lines.Count - 3
        If lines(i).Trim() = "0" AndAlso lines(i + 1).Trim().ToUpperInvariant() = "TABLE" Then
            If lines(i + 3).Trim().ToUpperInvariant() = "LAYER" Then layerTable = i
            If lines(i + 3).Trim().ToUpperInvariant() = "STYLE" Then styleTable = i
        End If
        i += 2
    Loop
    If layerTable < 0 OrElse styleTable < 0 Then Return False

    i = layerTable
    Do While i < lines.Count - 1
        If lines(i).Trim() = "0" AndAlso lines(i + 1).Trim().ToUpperInvariant() = "ENDTAB" Then
            layerEnd = i
            Exit Do
        End If
        i += 2
    Loop
    i = styleTable
    Do While i < lines.Count - 1
        If lines(i).Trim() = "0" AndAlso lines(i + 1).Trim().ToUpperInvariant() = "ENDTAB" Then
            styleEnd = i
            Exit Do
        End If
        i += 2
    Loop
    If layerEnd < 0 OrElse styleEnd < 0 Then Return False

    Dim hasLayer As Boolean = False
    Dim hasStyle As Boolean = False
    For j As Integer = layerTable To layerEnd - 1
        If lines(j).Trim() = "2" AndAlso j + 1 < lines.Count AndAlso lines(j + 1).Trim().ToUpperInvariant() = "DXF_INFO" Then hasLayer = True
    Next
    For j As Integer = styleTable To styleEnd - 1
        If lines(j).Trim() = "2" AndAlso j + 1 < lines.Count AndAlso lines(j + 1).Trim().ToUpperInvariant() = "DXF_INFO_STYLE" Then hasStyle = True
    Next

    ' Insert STYLE first because its table normally appears after LAYER.
    If Not hasStyle Then
        Dim styleEntry As New System.Collections.Generic.List(Of String)
        styleEntry.Add("0")
        styleEntry.Add("STYLE")
        styleEntry.Add("2")
        styleEntry.Add("DXF_INFO_STYLE")
        styleEntry.Add("70")
        styleEntry.Add("0")
        styleEntry.Add("40")
        styleEntry.Add("0")
        styleEntry.Add("41")
        styleEntry.Add("1")
        styleEntry.Add("50")
        styleEntry.Add("0")
        styleEntry.Add("71")
        styleEntry.Add("0")
        styleEntry.Add("42")
        styleEntry.Add("10")
        styleEntry.Add("3")
        styleEntry.Add("txt.shx")
        styleEntry.Add("4")
        styleEntry.Add("")
        lines.InsertRange(styleEnd, styleEntry)
    End If

    If Not hasLayer Then
        Dim layerEntry As New System.Collections.Generic.List(Of String)
        layerEntry.Add("0")
        layerEntry.Add("LAYER")
        layerEntry.Add("2")
        layerEntry.Add("DXF_INFO")
        layerEntry.Add("70")
        layerEntry.Add("0")
        layerEntry.Add("62")
        layerEntry.Add("7")
        layerEntry.Add("6")
        layerEntry.Add("CONTINUOUS")
        lines.InsertRange(layerEnd, layerEntry)
    End If
    Return True
End Function

Function RefreshEntitiesMetrics(lines As System.Collections.Generic.List(Of String), ByRef entityEnd As Integer, ByRef minX As Double, ByRef maxX As Double, ByRef minY As Double, ByRef maxY As Double, ByRef hasX As Boolean, ByRef hasY As Boolean) As Boolean
    entityEnd = -1
    minX = 0.0
    maxX = 0.0
    minY = 0.0
    maxY = 0.0
    hasX = False
    hasY = False
    Dim inEntities As Boolean = False
    Dim i As Integer = 0
    Do While i < lines.Count - 1
        Dim code As Integer = -1
        Dim c As String = lines(i).Trim()
        Dim v As String = lines(i + 1).Trim()
        If Integer.TryParse(c, code) Then
            If code = 2 AndAlso v.ToUpperInvariant() = "ENTITIES" Then
                inEntities = True
            ElseIf inEntities AndAlso code = 0 AndAlso v.ToUpperInvariant() = "ENDSEC" Then
                entityEnd = i
                Exit Do
            ElseIf inEntities AndAlso code = 10 Then
                Dim x As Double = 0.0
                If Double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, x) Then
                    If Not hasX Then
                        minX = x
                        maxX = x
                        hasX = True
                    Else
                        If x < minX Then minX = x
                        If x > maxX Then maxX = x
                    End If
                End If
            ElseIf inEntities AndAlso code = 20 Then
                Dim y As Double = 0.0
                If Double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, y) Then
                    If Not hasY Then
                        minY = y
                        maxY = y
                        hasY = True
                    Else
                        If y < minY Then minY = y
                        If y > maxY Then maxY = y
                    End If
                End If
            End If
        End If
        i += 2
    Loop
    Return entityEnd >= 0
End Function

Function CountDxfEntities(lines As System.Collections.Generic.List(Of String)) As Integer
    Dim inEntities As Boolean = False
    Dim count As Integer = 0
    Dim i As Integer = 0
    Do While i < lines.Count - 1
        Dim code As String = lines(i).Trim()
        Dim value As String = lines(i + 1).Trim().ToUpperInvariant()
        If code = "2" AndAlso value = "ENTITIES" Then
            inEntities = True
        ElseIf inEntities AndAlso code = "0" AndAlso value = "ENDSEC" Then
            Exit Do
        ElseIf inEntities AndAlso code = "0" AndAlso value <> "ENDSEC" Then
            count += 1
        End If
        i += 2
    Loop
    Return count
End Function

' V14: kiểm tra DXF ngay trong bộ nhớ (không đọc lại file)
Function ValidateDxfLines(a As System.Collections.Generic.IList(Of String)) As Boolean
    Dim sec As Boolean = False
    Dim ent As Boolean = False
    Dim endsec As Boolean = False
    Dim eof As Boolean = False
    For i As Integer = 0 To a.Count - 2
        Dim c As String = a(i).Trim().ToUpperInvariant()
        Dim v As String = a(i + 1).Trim().ToUpperInvariant()
        If c = "0" AndAlso v = "SECTION" Then sec = True
        If c = "2" AndAlso v = "ENTITIES" Then ent = True
        If c = "0" AndAlso v = "ENDSEC" Then endsec = True
        If c = "0" AndAlso v = "EOF" Then eof = True
    Next
    Return sec AndAlso ent AndAlso endsec AndAlso eof
End Function

Function ValidateDxfBasic(filePath As String) As Boolean
    If Not System.IO.File.Exists(filePath) Then Return False
    Return ValidateDxfLines(System.IO.File.ReadAllLines(filePath, DxfEncoding()))
End Function

' V14: bỏ dấu tiếng Việt (DXF R12 / font SHX không hiển thị được Unicode) + làm sạch ký tự điều khiển
Function CleanTextForDxf(value As String) As String
    If String.IsNullOrEmpty(value) Then Return ""
    Dim t As String = value.Replace(vbCr, " ").Replace(vbLf, " ").Replace("|", " ")
    t = t.Replace("đ"c, "d"c).Replace("Đ"c, "D"c)
    Dim decomposed As String = t.Normalize(System.Text.NormalizationForm.FormD)
    Dim sb As New System.Text.StringBuilder()
    For Each ch As Char In decomposed
        If System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) <> System.Globalization.UnicodeCategory.NonSpacingMark Then
            sb.Append(ch)
        End If
    Next
    Dim outText As String = sb.ToString().Normalize(System.Text.NormalizationForm.FormC)
    ' Ký tự còn lại ngoài ASCII in được -> "?" để file DXF luôn sạch
    Dim sb2 As New System.Text.StringBuilder()
    For Each ch As Char In outText
        If AscW(ch) >= 32 AndAlso AscW(ch) <= 126 Then
            sb2.Append(ch)
        Else
            sb2.Append("?"c)
        End If
    Next
    Return sb2.ToString()
End Function

Function BuildFileName(item As ExportItem, pattern As String) As String
    Dim p As String = pattern
    If String.IsNullOrWhiteSpace(p) Then p = "{DoDay}_{Qty}_{VatLieu}_{TenChiTiet}"

    Dim result As String = p
    result = result.Replace("{DoDay}", CleanName(item.Thickness))
    result = result.Replace("{Qty}", "Q" & item.Quantity.ToString())
    result = result.Replace("{VatLieu}", CleanName(item.Material))
    result = result.Replace("{TenChiTiet}", CleanName(item.DetailName))

    If Not result.ToLowerInvariant().EndsWith(".dxf") Then result &= ".dxf"

    For Each invalidChar As Char In System.IO.Path.GetInvalidFileNameChars()
        result = result.Replace(invalidChar, "_"c)
    Next

    If String.IsNullOrWhiteSpace(result) Then result = "FlatPattern.dxf"
    Return result
End Function

Sub ScanOccurrences(occurrences As Inventor.ComponentOccurrences, items As System.Collections.Generic.Dictionary(Of String, ExportItem), skipped As System.Collections.Generic.List(Of String), errors As System.Collections.Generic.List(Of String), suppressedParts As System.Collections.Generic.List(Of ExportItem))
    For Each occ As Inventor.ComponentOccurrence In occurrences
        Try
            If occ.Suppressed Then
                skipped.Add("Suppressed: " & occ.Name)
                TryCollectSuppressedPartInfo(occ, suppressedParts)
                Continue For
            End If
            If occ.DefinitionDocumentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject Then
                ScanOccurrences(occ.SubOccurrences, items, skipped, errors, suppressedParts)
            ElseIf occ.DefinitionDocumentType = Inventor.DocumentTypeEnum.kPartDocumentObject Then
                AddPartItem(TryCast(occ.Definition.Document, Inventor.PartDocument), 1, items, skipped, errors)
            End If
        Catch ex As Exception
            errors.Add(occ.Name & ": " & ex.Message)
        End Try
    Next
End Sub

Sub TryCollectSuppressedPartInfo(occ As Inventor.ComponentOccurrence, suppressedParts As System.Collections.Generic.List(Of ExportItem))
    Try
        If occ.DefinitionDocumentType = Inventor.DocumentTypeEnum.kPartDocumentObject Then
            Dim partDoc As Inventor.PartDocument = TryCast(occ.Definition.Document, Inventor.PartDocument)
            If partDoc IsNot Nothing AndAlso Not String.IsNullOrEmpty(partDoc.FullFileName) Then
                Dim sm As Inventor.SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)
                If sm IsNot Nothing Then
                    Dim x As New ExportItem()
                    x.Document = partDoc
                    x.DetailName = System.IO.Path.GetFileNameWithoutExtension(partDoc.DisplayName)
                    x.Material = GetMaterial(partDoc)
                    x.Thickness = GetThickness(sm)
                    x.Quantity = 1
                    suppressedParts.Add(x)
                End If
            End If
        End If
    Catch
    End Try
End Sub

Sub AddPartItem(partDoc As Inventor.PartDocument, qty As Integer, items As System.Collections.Generic.Dictionary(Of String, ExportItem), skipped As System.Collections.Generic.List(Of String), errors As System.Collections.Generic.List(Of String))
    If partDoc Is Nothing Then
        skipped.Add("Part không thể mở")
        Return
    End If
    Try
        Dim sm As Inventor.SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)
        If sm Is Nothing Then
            skipped.Add(partDoc.DisplayName & " - không phải Sheet Metal")
            Return
        End If
        If String.IsNullOrEmpty(partDoc.FullFileName) Then
            skipped.Add(partDoc.DisplayName & " - chưa lưu")
            Return
        End If
        Dim key As String = partDoc.FullFileName
        If items.ContainsKey(key) Then
            items(key).Quantity += qty
        Else
            Dim x As New ExportItem()
            x.Document = partDoc
            x.DetailName = System.IO.Path.GetFileNameWithoutExtension(partDoc.DisplayName)
            x.Material = GetMaterial(partDoc)
            x.Thickness = GetThickness(sm)
            x.Quantity = qty
            items.Add(key, x)
        End If
    Catch ex As Exception
        errors.Add(partDoc.DisplayName & ": " & ex.Message)
    End Try
End Sub

Function GetMaterial(partDoc As Inventor.PartDocument) As String
    Try
        If partDoc.ActiveMaterial IsNot Nothing Then Return partDoc.ActiveMaterial.DisplayName
    Catch
    End Try
    Return "Khong_Xac_Dinh"
End Function

Function GetThickness(sm As Inventor.SheetMetalComponentDefinition) As String
    Try
        Dim mm As Double = Math.Round(sm.Thickness.Value * 10.0, 3)
        Return "T" & mm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
    Catch
        Return "T_Khong_Xac_Dinh"
    End Try
End Function

Function FormatNumberVN(dValue As Double) As String
    Dim s As String = dValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
    Return s.Replace(".", ",")
End Function

Sub UpdateFlatPatternInfo(partDoc As Inventor.PartDocument, errors As System.Collections.Generic.List(Of String))
    Try
        Dim sm As Inventor.SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)
        If sm Is Nothing Then Return

        Dim oParams As Inventor.Parameters = sm.Parameters
        Dim oThickness As Inventor.Parameter = oParams.Item("Thickness")
        Dim dThicknessMM As Double = ThisApplication.UnitsOfMeasure.ConvertUnits(oThickness.Value, Inventor.UnitsTypeEnum.kDatabaseLengthUnits, Inventor.UnitsTypeEnum.kMillimeterLengthUnits)
        dThicknessMM = Math.Round(dThicknessMM, 2)

        If Not sm.HasFlatPattern Then sm.Unfold()
        Dim oFlatPattern As Inventor.FlatPattern = sm.FlatPattern
        Dim dFlatWidthMM As Double = ThisApplication.UnitsOfMeasure.ConvertUnits(oFlatPattern.Width, Inventor.UnitsTypeEnum.kDatabaseLengthUnits, Inventor.UnitsTypeEnum.kMillimeterLengthUnits)
        Dim dFlatLengthMM As Double = ThisApplication.UnitsOfMeasure.ConvertUnits(oFlatPattern.Length, Inventor.UnitsTypeEnum.kDatabaseLengthUnits, Inventor.UnitsTypeEnum.kMillimeterLengthUnits)
        dFlatWidthMM = Math.Round(dFlatWidthMM, 1)
        dFlatLengthMM = Math.Round(dFlatLengthMM, 1)

        Dim sDesc As String = FormatNumberVN(dFlatLengthMM) & " x " & FormatNumberVN(dFlatWidthMM) & " x " & FormatNumberVN(dThicknessMM)

        Dim oDesignProps As Inventor.PropertySet = partDoc.PropertySets.Item("Design Tracking Properties")
        oDesignProps.Item("Description").Value = sDesc

        Dim oCustomProps As Inventor.PropertySet = partDoc.PropertySets.Item("Inventor User Defined Properties")
        Dim sThicknessValue As String = dThicknessMM.ToString(System.Globalization.CultureInfo.InvariantCulture) & " mm"
        Dim oProp As Inventor.Property
        Try
            oProp = oCustomProps.Item("Thickness")
            oProp.Value = sThicknessValue
        Catch
            oProp = oCustomProps.Add(sThicknessValue, "Thickness")
        End Try

        ' V14: chỉ lưu khi file thực sự thay đổi (tránh Save thừa sau bước Convert đã lưu)
        If partDoc.Dirty Then partDoc.Save()
    Catch ex As Exception
        errors.Add(partDoc.DisplayName & " (cập nhật Description/Thickness): " & ex.Message)
    End Try
End Sub

Function GetPartNumber(partDoc As Inventor.PartDocument) As String
    Try
        Dim oDesignProps As Inventor.PropertySet = partDoc.PropertySets.Item("Design Tracking Properties")
        Dim pn As String = CStr(oDesignProps.Item("Part Number").Value)
        If String.IsNullOrWhiteSpace(pn) Then Return System.IO.Path.GetFileNameWithoutExtension(partDoc.DisplayName)
        Return pn
    Catch
        Return System.IO.Path.GetFileNameWithoutExtension(partDoc.DisplayName)
    End Try
End Function

Function GetThicknessCustomProp(partDoc As Inventor.PartDocument) As String
    Try
        Dim oCustomProps As Inventor.PropertySet = partDoc.PropertySets.Item("Inventor User Defined Properties")
        Return CStr(oCustomProps.Item("Thickness").Value)
    Catch
        Return ""
    End Try
End Function

Function GetDescriptionProp(partDoc As Inventor.PartDocument) As String
    Try
        Dim oDesignProps As Inventor.PropertySet = partDoc.PropertySets.Item("Design Tracking Properties")
        Return CStr(oDesignProps.Item("Description").Value)
    Catch
        Return ""
    End Try
End Function

Function ExportBomToExcel(items As System.Collections.Generic.Dictionary(Of String, ExportItem), outputRoot As String, errors As System.Collections.Generic.List(Of String)) As String
    Dim xlApp As Object = Nothing
    Dim xlBook As Object = Nothing
    Dim bomPath As String = ""
    Try
        xlApp = CreateObject("Excel.Application")
        xlApp.Visible = False
        xlApp.DisplayAlerts = False
        xlBook = xlApp.Workbooks.Add()
        Dim xlSheet As Object = xlBook.Sheets(1)
        xlSheet.Name = "BOM"

        xlSheet.Cells(1, 1).Value = "STT"
        xlSheet.Cells(1, 2).Value = "Part number"
        xlSheet.Cells(1, 3).Value = "Số lượng"
        xlSheet.Cells(1, 4).Value = "Thickness"
        xlSheet.Cells(1, 5).Value = "Description"
        xlSheet.Range("A1:E1").Font.Bold = True

        ' V14: gom dữ liệu vào mảng 2 chiều rồi ghi vào Excel 1 lần (thay vì 5 lệnh COM cho mỗi dòng)
        Dim rowCount As Integer = items.Count
        If rowCount > 0 Then
            Dim data(rowCount - 1, 4) As Object
            Dim r As Integer = 0
            For Each pair As System.Collections.Generic.KeyValuePair(Of String, ExportItem) In items
                Dim it As ExportItem = pair.Value
                data(r, 0) = r + 1
                Try
                    data(r, 1) = GetPartNumber(it.Document)
                    data(r, 2) = it.Quantity
                    data(r, 3) = GetThicknessCustomProp(it.Document)
                    data(r, 4) = GetDescriptionProp(it.Document)
                Catch exRow As Exception
                    errors.Add(it.DetailName & " (BOM): " & exRow.Message)
                End Try
                r += 1
            Next
            xlSheet.Range("A2:E" & (rowCount + 1).ToString()).Value = data
        End If

        xlSheet.Columns.AutoFit()
        bomPath = System.IO.Path.Combine(outputRoot, "BOM_" & System.DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".xlsx")
        xlBook.SaveAs(bomPath, 51)
        xlBook.Close(False)
        xlApp.Quit()
    Catch ex As Exception
        errors.Add("Xuất BOM Excel: " & ex.Message)
        bomPath = ""
        Try
            If xlBook IsNot Nothing Then xlBook.Close(False)
        Catch
        End Try
        Try
            If xlApp IsNot Nothing Then xlApp.Quit()
        Catch
        End Try
    Finally
        Try
            If xlBook IsNot Nothing Then System.Runtime.InteropServices.Marshal.ReleaseComObject(xlBook)
        Catch
        End Try
        Try
            If xlApp IsNot Nothing Then System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp)
        Catch
        End Try
        xlBook = Nothing
        xlApp = Nothing
        System.GC.Collect()
        System.GC.WaitForPendingFinalizers()
    End Try
    Return bomPath
End Function

Sub ExportOnePart(partDoc As Inventor.PartDocument, path As String, options As String)
    If partDoc Is Nothing Then Throw New Exception("PartDocument rỗng.")

    Dim sm As Inventor.SheetMetalComponentDefinition = TryCast(partDoc.ComponentDefinition, Inventor.SheetMetalComponentDefinition)
    If sm Is Nothing Then Throw New Exception("Không phải Sheet Metal.")

    If Not sm.HasFlatPattern Then
        Try
            sm.Unfold()
        Catch exUnfold As Exception
            Throw New Exception("UNFOLD - " & exUnfold.Message)
        End Try
    End If

    If sm.FlatPattern Is Nothing Then Throw New Exception("Không tạo được Flat Pattern.")

    Dim dataIO As Inventor.DataIO = Nothing
    Try
        dataIO = sm.FlatPattern.DataIO
    Catch
        dataIO = sm.DataIO
    End Try

    Try
        Dim dxfAddIn As Inventor.TranslatorAddIn = TryCast(ThisApplication.ApplicationAddIns.ItemById("{C24E3AC4-122E-11D5-8E91-0010B541CD80}"), Inventor.TranslatorAddIn)
        If dxfAddIn IsNot Nothing AndAlso Not dxfAddIn.Activated Then dxfAddIn.Activate()
    Catch
    End Try

    Try
        dataIO.WriteDataToFile(options, path)
        Return
    Catch
    End Try

    Dim fallback1 As String = "FLAT PATTERN DXF?AcadVersion=2004&OuterProfileLayer=IV_OUTER_PROFILE&InteriorProfilesLayer=IV_INTERIOR_PROFILES&RebaseGeometry=True"
    Try
        dataIO.WriteDataToFile(fallback1, path)
        Return
    Catch
    End Try

    Try
        sm.FlatPattern.Edit()
        dataIO = sm.DataIO
        dataIO.WriteDataToFile(fallback1, path)
        sm.FlatPattern.ExitEdit()
        Return
    Catch
        Try
            sm.FlatPattern.ExitEdit()
        Catch
        End Try
    End Try

    Try
        sm.DataIO.WriteDataToFile(fallback1, path)
        Return
    Catch exFinal As Exception
        Throw New Exception("DATAIO lỗi. Chi tiết: " & exFinal.Message)
    End Try
End Sub

Function CleanName(value As String) As String
    If String.IsNullOrEmpty(value) Then Return "Khong_Xac_Dinh"
    Dim s As String = value.Trim()
    For Each c As Char In System.IO.Path.GetInvalidFileNameChars()
        s = s.Replace(c, "_"c)
    Next
    Return s.Replace(" ", "_")
End Function

Function SettingsPath() As String
    Return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Inventor_DXF_Export_Form.ini")
End Function

Function LoadSettings() As ExportSettings
    Dim s As New ExportSettings()
    s.OutputRoot = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
    s.FilePattern = "{DoDay}_{Qty}_{VatLieu}_{TenChiTiet}"
    s.OverwriteMode = "Hỏi từng file"
    s.FastMode = True
    s.CleanupSuppressed = False
    s.UpdateInfoAndBom = True
    s.AddInfoText = False
    s.TextAlignment = "Căn giữa"
    s.TextHeight = 10.0
    s.TextLineGap = 15.0
    s.TextYOffset = -250.0
    s.BendMode = "Giữ nguyên đường dài"
    s.BendUpLen = 10.0
    s.BendUpInset = 3.0
    s.BendDownLen = 3.0
    s.BendDownInset = 3.0
    Try
        If System.IO.File.Exists(SettingsPath()) Then
            For Each line As String In System.IO.File.ReadAllLines(SettingsPath())
                Dim k As Integer = Line.IndexOf("=")
                If k <= 0 Then Continue For
                Dim key As String = Line.Substring(0, k)
                Dim val As String = Line.Substring(k + 1)
                If key = "OutputRoot" Then s.OutputRoot = val
                If key = "FilePattern" Then s.FilePattern = val
                If key = "OverwriteMode" Then s.OverwriteMode = val
                If key = "FastMode" Then s.FastMode = (val.ToLower() = "true")
                If key = "CleanupSuppressed" Then s.CleanupSuppressed = (val.ToLower() = "true")
                If key = "UpdateInfoAndBom" Then s.UpdateInfoAndBom = (val.ToLower() = "true")
                If key = "AddInfoText" Then s.AddInfoText = (val.ToLower() = "true")
                If key = "TextAlignment" Then s.TextAlignment = val
                If key = "TextHeight" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.TextHeight)
                If key = "TextLineGap" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.TextLineGap)
                If key = "TextYOffset" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.TextYOffset)
                If key = "BendMode" Then s.BendMode = val
                If key = "ActionIndex" Then Integer.TryParse(val, s.ActionIndex)
                If key = "ConvFilter" Then s.ConvFilter = val
                If key = "ConvMode" Then Integer.TryParse(val, s.ConvMode)
                If key = "ConvSkipExisting" Then s.ConvSkipExisting = (val.ToLower() = "true")
                If key = "ConvTolerance" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.ConvTolerance)
                If key = "ConvLogFolder" Then s.ConvLogFolder = val
                If key = "BendUpLen" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.BendUpLen)
                If key = "BendUpInset" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.BendUpInset)
                If key = "BendDownLen" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.BendDownLen)
                If key = "BendDownInset" Then Double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, s.BendDownInset)
                If key.StartsWith("Layer.") Then s.LayerSettings(key.Substring(6)) = val
            Next
        End If
    Catch
    End Try
    Return s
End Function

Sub SaveSettings(s As ExportSettings)
    Try
        Dim lines As New System.Collections.Generic.List(Of String)
        lines.Add("OutputRoot=" & s.OutputRoot)
        lines.Add("FilePattern=" & s.FilePattern)
        lines.Add("OverwriteMode=" & s.OverwriteMode)
        lines.Add("FastMode=" & s.FastMode.ToString())
        lines.Add("CleanupSuppressed=" & s.CleanupSuppressed.ToString())
        lines.Add("UpdateInfoAndBom=" & s.UpdateInfoAndBom.ToString())
        lines.Add("AddInfoText=" & s.AddInfoText.ToString())
        lines.Add("TextAlignment=" & s.TextAlignment)
        lines.Add("TextHeight=" & s.TextHeight.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("TextLineGap=" & s.TextLineGap.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("TextYOffset=" & s.TextYOffset.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("BendMode=" & s.BendMode)
        lines.Add("ActionIndex=" & s.ActionIndex.ToString())
        lines.Add("ConvFilter=" & s.ConvFilter)
        lines.Add("ConvMode=" & s.ConvMode.ToString())
        lines.Add("ConvSkipExisting=" & s.ConvSkipExisting.ToString())
        lines.Add("ConvTolerance=" & s.ConvTolerance.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("ConvLogFolder=" & s.ConvLogFolder)
        lines.Add("BendUpLen=" & s.BendUpLen.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("BendUpInset=" & s.BendUpInset.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("BendDownLen=" & s.BendDownLen.ToString(System.Globalization.CultureInfo.InvariantCulture))
        lines.Add("BendDownInset=" & s.BendDownInset.ToString(System.Globalization.CultureInfo.InvariantCulture))
        If s.LayerRows IsNot Nothing Then
            For Each r As LayerRow In s.LayerRows
                lines.Add("Layer." & r.Name & "=" & r.Enabled.ToString())
            Next
        End If
        System.IO.File.WriteAllLines(SettingsPath(), lines.ToArray(), System.Text.Encoding.UTF8)
    Catch
    End Try
End Sub

Class ExportSettings
    Public OutputRoot As String = ""
    Public FilePattern As String = "{DoDay}_{Qty}_{VatLieu}_{TenChiTiet}"
    Public OverwriteMode As String = "Hỏi từng file"
    Public FastMode As Boolean = True
    Public CleanupSuppressed As Boolean = False
    Public UpdateInfoAndBom As Boolean = True
    Public AddInfoText As Boolean = False
    Public TextAlignment As String = "Căn giữa"
    Public TextHeight As Double = 10.0
    Public TextLineGap As Double = 15.0
    Public TextYOffset As Double = -250.0
    Public BendMode As String = "Giữ nguyên đường dài"
    Public BendUpLen As Double = 10.0
    Public BendUpInset As Double = 3.0
    Public BendDownLen As Double = 3.0
    Public BendDownInset As Double = 3.0
    Public ActionIndex As Integer = 1
    Public DoConvert As Boolean = False
    Public DoExport As Boolean = True
    Public ConvFilter As String = ""
    Public ConvMode As Integer = 2
    Public ConvSkipExisting As Boolean = True
    Public ConvTolerance As Double = 0.01
    Public ConvLogFolder As String = ""
    Public DxfOptions As String = ""
    Public LayerRows As System.Collections.Generic.List(Of LayerRow) = New System.Collections.Generic.List(Of LayerRow)()
    Public LayerSettings As New System.Collections.Generic.Dictionary(Of String, String)(System.StringComparer.OrdinalIgnoreCase)

    Public Function IsLayerEnabled(name As String, defaultValue As Boolean) As Boolean
        If Not LayerSettings.ContainsKey(name) Then Return defaultValue
        Return LayerSettings(name).Split("|"c)(0).ToLower() = "true"
    End Function
End Class

Class LayerRow
    Public Name As String
    Public Enabled As Boolean
End Class

Class ExportItem
    Public Document As Inventor.PartDocument
    Public DetailName As String
    Public Material As String
    Public Thickness As String
    Public Quantity As Integer
End Class

' V12: phạm vi 1 entity trong section ENTITIES
Class EntRange
    Public StartIdx As Integer
    Public EndIdx As Integer
    Public TypeName As String = ""
    Public Removed As Boolean = False
End Class

' V12: một đường chấn (các đoạn LINE cùng layer, cùng phương, cùng vị trí được gộp lại)
Class BendGroup
    Public Layer As String = ""
    Public Ux As Double
    Public Uy As Double
    Public D As Double
    Public TMin As Double
    Public TMax As Double
    Public Template As New System.Collections.Generic.List(Of String)
End Class

' V13: kết quả Convert STEP
Class ConvertResult
    Public Success As Integer = 0
    Public Skip As Integer = 0
    Public Err As Integer = 0
    Public LogFile As String = ""
End Class
