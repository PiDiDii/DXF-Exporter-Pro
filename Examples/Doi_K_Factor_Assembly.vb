AddReference "System.Windows.Forms"
AddReference "System.Drawing"
Imports WF = System.Windows.Forms
Imports DR = System.Drawing
' ============================================================
' Doi he so K theo SHEET METAL RULE dang dung trong Assembly
' Inventor 2027 - iLogic (chay trong file .iam)
' Buoc 1: quet cac chi tiet kim loai tam (bo qua file khac)
' Buoc 2: tick Rule -> xem truoc danh sach chi tiet se doi (co the bo tick tung file)
' Buoc 3: nhap K moi, thuc hien
' ============================================================

Sub Main()
    Dim SM_GUID As String = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}"
    Dim inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture

    Dim asmDoc As Document = ThisDoc.Document
    If asmDoc.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        WF.MessageBox.Show("Hãy chạy rule này trong file Assembly (.iam).", "Lỗi")
        Exit Sub
    End If

    ' ---- Duong dan thu vien cua project ----
    Dim libPaths As New System.Collections.Generic.List(Of String)
    Try
        For Each pp As ProjectPath In ThisApplication.DesignProjectManager.ActiveDesignProject.LibraryPaths
            libPaths.Add(pp.Path.ToLower())
        Next
    Catch
    End Try

    ' ---- BUOC 1: quet - chi lay Part Sheet Metal, gom theo ten Rule dang dung ----
    Dim nhom As New System.Collections.Generic.Dictionary(Of String, System.Collections.Generic.List(Of Document))
    Dim docTheoPath As New System.Collections.Generic.Dictionary(Of String, Document)
    Dim kTheoPath As New System.Collections.Generic.Dictionary(Of String, String)
    Dim kTheoRule As New System.Collections.Generic.Dictionary(Of String, System.Collections.Generic.List(Of String))
    Dim seen As New System.Collections.Generic.HashSet(Of String)
    Dim tongSM As Integer = 0, khongPhaiSM As Integer = 0

    ThisApplication.StatusBarText = "Đang quét các chi tiết kim loại tấm..."
    For Each d As Document In asmDoc.AllReferencedDocuments
        If d.DocumentType <> DocumentTypeEnum.kPartDocumentObject Then Continue For
        If d.SubType <> SM_GUID Then
            khongPhaiSM += 1
            Continue For
        End If
        If Not seen.Add(d.FullFileName) Then Continue For

        Try
            Dim smd As SheetMetalComponentDefinition = CType(d, PartDocument).ComponentDefinition
            Dim tenRule As String = smd.ActiveSheetMetalStyle.Name
            Dim kStr As String = Math.Round(CDbl(smd.UnfoldMethod.kFactor), 4).ToString(inv)

            If Not nhom.ContainsKey(tenRule) Then
                nhom.Add(tenRule, New System.Collections.Generic.List(Of Document))
                kTheoRule.Add(tenRule, New System.Collections.Generic.List(Of String))
            End If
            nhom(tenRule).Add(d)
            docTheoPath(d.FullFileName) = d
            kTheoPath(d.FullFileName) = kStr
            If Not kTheoRule(tenRule).Contains(kStr) Then kTheoRule(tenRule).Add(kStr)
            tongSM += 1
        Catch
        End Try
    Next
    ThisApplication.StatusBarText = ""

    If nhom.Count = 0 Then
        WF.MessageBox.Show("Không tìm thấy chi tiết Sheet Metal nào trong assembly.", "Thông báo")
        Exit Sub
    End If

    Dim tenList As New System.Collections.Generic.List(Of String)(nhom.Keys)
    tenList.Sort()

    ' ---- BUOC 2: Bang GUI ----
    Dim f As New WF.Form()
    f.Text = "Đổi hệ số K theo Sheet Metal Rule"
    f.ClientSize = New DR.Size(820, 650)
    f.StartPosition = WF.FormStartPosition.CenterScreen
    f.FormBorderStyle = WF.FormBorderStyle.FixedDialog
    f.MaximizeBox = False
    f.MinimizeBox = False

    Dim lbInfo As New WF.Label()
    lbInfo.Text = "Tìm thấy " & tongSM & " chi tiết Sheet Metal dùng " & nhom.Count & " Rule. " & _
                  "Đã bỏ qua " & khongPhaiSM & " file không phải kim loại tấm."
    lbInfo.Location = New DR.Point(15, 12)
    lbInfo.Size = New DR.Size(790, 22)

    Dim lbRule As New WF.Label()
    lbRule.Text = "1. Tick Rule cần đổi hệ số K:"
    lbRule.Location = New DR.Point(15, 44)
    lbRule.Size = New DR.Size(300, 22)

    Dim chkAll As New WF.CheckBox()
    chkAll.Text = "Chọn tất cả Rule"
    chkAll.Location = New DR.Point(650, 42)
    chkAll.Size = New DR.Size(155, 24)

    Dim clb As New WF.CheckedListBox()
    clb.Location = New DR.Point(15, 70)
    clb.Size = New DR.Size(790, 125)
    clb.CheckOnClick = True
    For Each ten As String In tenList
        clb.Items.Add(ten & "      (K hiện tại: " & String.Join(" / ", kTheoRule(ten)) & _
                      "  -  " & nhom(ten).Count & " chi tiết)", False)
    Next

    Dim lbList As New WF.Label()
    lbList.Text = "2. Xem trước danh sách chi tiết sẽ đổi (bỏ tick chi tiết nào không muốn đổi):"
    lbList.Location = New DR.Point(15, 208)
    lbList.Size = New DR.Size(790, 22)

    Dim lv As New WF.ListView()
    lv.Location = New DR.Point(15, 234)
    lv.Size = New DR.Size(790, 270)
    lv.View = WF.View.Details
    lv.CheckBoxes = True
    lv.FullRowSelect = True
    lv.GridLines = True
    lv.Columns.Add("Tên chi tiết", 270)
    lv.Columns.Add("Rule đang dùng", 130)
    lv.Columns.Add("K hiện tại", 75)
    lv.Columns.Add("Thư mục", 290)
    For Each ten As String In tenList
        Dim dsNhom As System.Collections.Generic.List(Of Document) = nhom(ten)
        dsNhom.Sort(Function(a As Document, b As Document) _
                    String.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase))
        For Each d As Document In dsNhom
            Dim it As New WF.ListViewItem(System.IO.Path.GetFileName(d.FullFileName))
            it.SubItems.Add(ten)
            it.SubItems.Add(kTheoPath(d.FullFileName))
            it.SubItems.Add(System.IO.Path.GetDirectoryName(d.FullFileName))
            it.Tag = d.FullFileName
            it.Checked = False
            lv.Items.Add(it)
        Next
    Next

    Dim lbCount As New WF.Label()
    lbCount.Text = "Đã chọn: 0 / " & tongSM & " chi tiết"
    lbCount.Location = New DR.Point(15, 510)
    lbCount.Size = New DR.Size(400, 22)

    Dim lbK As New WF.Label()
    lbK.Text = "3. Hệ số K mới (0 - 1):"
    lbK.Location = New DR.Point(15, 546)
    lbK.Size = New DR.Size(160, 22)

    Dim tbK As New WF.TextBox()
    tbK.Text = "0.44"
    tbK.Location = New DR.Point(180, 543)
    tbK.Size = New DR.Size(80, 24)

    Dim chkSave As New WF.CheckBox()
    chkSave.Text = "Tự động lưu từng chi tiết sau khi đổi"
    chkSave.Checked = True
    chkSave.Location = New DR.Point(15, 578)
    chkSave.Size = New DR.Size(360, 24)

    Dim chkLib As New WF.CheckBox()
    chkLib.Text = "Bỏ qua chi tiết trong thư viện / Content Center"
    chkLib.Checked = True
    chkLib.Location = New DR.Point(15, 606)
    chkLib.Size = New DR.Size(360, 24)

    Dim btnOK As New WF.Button()
    btnOK.Text = "Thực hiện"
    btnOK.DialogResult = WF.DialogResult.OK
    btnOK.Location = New DR.Point(600, 604)
    btnOK.Size = New DR.Size(110, 32)

    Dim btnCancel As New WF.Button()
    btnCancel.Text = "Hủy"
    btnCancel.DialogResult = WF.DialogResult.Cancel
    btnCancel.Location = New DR.Point(720, 604)
    btnCancel.Size = New DR.Size(85, 32)

    ' Tick Rule -> tu dong tick/bo tick cac chi tiet cua Rule do trong danh sach xem truoc
    AddHandler clb.ItemCheck, Sub(s As Object, e As WF.ItemCheckEventArgs)
                                  Dim ten As String = tenList(e.Index)
                                  Dim bat As Boolean = (e.NewValue = WF.CheckState.Checked)
                                  For Each it As WF.ListViewItem In lv.Items
                                      If it.SubItems(1).Text = ten Then it.Checked = bat
                                  Next
                              End Sub

    AddHandler chkAll.CheckedChanged, Sub(s As Object, e As System.EventArgs)
                                          For i As Integer = 0 To clb.Items.Count - 1
                                              clb.SetItemChecked(i, chkAll.Checked)
                                          Next
                                      End Sub

    AddHandler lv.ItemChecked, Sub(s As Object, e As WF.ItemCheckedEventArgs)
                                   lbCount.Text = "Đã chọn: " & lv.CheckedItems.Count & " / " & tongSM & " chi tiết"
                               End Sub

    f.Controls.Add(lbInfo)
    f.Controls.Add(lbRule)
    f.Controls.Add(chkAll)
    f.Controls.Add(clb)
    f.Controls.Add(lbList)
    f.Controls.Add(lv)
    f.Controls.Add(lbCount)
    f.Controls.Add(lbK)
    f.Controls.Add(tbK)
    f.Controls.Add(chkSave)
    f.Controls.Add(chkLib)
    f.Controls.Add(btnOK)
    f.Controls.Add(btnCancel)

    Dim kNew As Double = 0
    Do
        If f.ShowDialog() <> WF.DialogResult.OK Then Exit Sub
        Dim s As String = tbK.Text.Trim().Replace(",", ".")
        If lv.CheckedItems.Count = 0 Then
            WF.MessageBox.Show("Hãy tick ít nhất một Rule (hoặc một chi tiết) cần đổi.", "Lỗi")
        ElseIf Not (Double.TryParse(s, System.Globalization.NumberStyles.Float, inv, kNew) _
                    AndAlso kNew > 0 AndAlso kNew <= 1) Then
            WF.MessageBox.Show("Hệ số K không hợp lệ. Nhập số trong khoảng 0 < K ≤ 1 (ví dụ 0.44).", "Lỗi")
        Else
            Exit Do
        End If
    Loop
    Dim luuFile As Boolean = chkSave.Checked
    Dim boThuVien As Boolean = chkLib.Checked

    ' ---- Lay danh sach file da tick ----
    Dim chon As New System.Collections.Generic.List(Of String)
    Dim ruleCuaFile As New System.Collections.Generic.Dictionary(Of String, String)
    For Each it As WF.ListViewItem In lv.CheckedItems
        Dim p As String = CStr(it.Tag)
        chon.Add(p)
        ruleCuaFile(p) = it.SubItems(1).Text
    Next

    ' ---- BUOC 3: Doi K ----
    Dim ok As Integer = 0, bo As Integer = 0, khongLuu As Integer = 0
    Dim loi As String = ""
    Dim demTheoRule As New System.Collections.Generic.Dictionary(Of String, Integer)

    ThisApplication.ScreenUpdating = False
    Try
        For Each path As String In chon
            Dim d As Document = docTheoPath(Path)
            Dim laThuVien As Boolean = Path.ToLower().Contains("content center")
            For Each lp As String In libPaths
                If Path.ToLower().StartsWith(lp) Then laThuVien = True
            Next
            If boThuVien AndAlso laThuVien Then
                bo += 1
                Continue For
            End If

            Try
                ThisApplication.StatusBarText = "Đang đổi K: " & System.IO.Path.GetFileName(Path)
                Dim pd As PartDocument = CType(d, PartDocument)
                Dim smd As SheetMetalComponentDefinition = pd.ComponentDefinition
                smd.UnfoldMethod.kFactor = kNew
                pd.Update()

                If luuFile Then
                    Dim choGhi As Boolean = True
                    Try
                        If Not pd.IsModifiable Then choGhi = False
                        Dim attr As System.IO.FileAttributes = System.IO.File.GetAttributes(pd.FullFileName)
                        If (attr And System.IO.FileAttributes.ReadOnly) = System.IO.FileAttributes.ReadOnly Then choGhi = False
                    Catch
                    End Try
                    If choGhi Then
                        pd.Save2(False)
                    Else
                        khongLuu += 1
                    End If
                End If
                ok += 1
                Dim r As String = ruleCuaFile(Path)
                If demTheoRule.ContainsKey(r) Then
                    demTheoRule(r) += 1
                Else
                    demTheoRule(r) = 1
                End If
            Catch ex As Exception
                loi &= System.IO.Path.GetFileName(Path) & " : " & ex.Message & vbCrLf
            End Try
        Next
    Finally
        ThisApplication.ScreenUpdating = True
        ThisApplication.StatusBarText = ""
    End Try

    asmDoc.Update()

    Dim chiTiet As String = ""
    For Each kv As System.Collections.Generic.KeyValuePair(Of String, Integer) In demTheoRule
        chiTiet &= "  - " & kv.Key & " : " & kv.Value & " chi tiết" & vbCrLf
    Next
    Dim msg As String = "Đã đổi K = " & kNew.ToString(inv) & " cho " & ok & " chi tiết:" & vbCrLf & chiTiet & vbCrLf & _
                        "Bỏ qua (thư viện): " & bo
    If khongLuu > 0 Then msg &= vbCrLf & "Không lưu được (file chỉ đọc): " & khongLuu
    If loi <> "" Then msg &= vbCrLf & vbCrLf & "LỖI:" & vbCrLf & loi
    WF.MessageBox.Show(msg, "Kết quả")
End Sub