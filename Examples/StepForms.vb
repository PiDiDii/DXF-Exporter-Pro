' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  File 5/5: Giao diện
'     StepListForm      : "Danh sách chi tiết xuất STEP"
'     StepSettingsForm  : "Cài đặt xuất STEP"
'     StepPreviewForm   : cửa sổ ảnh xem trước (bấm vào Part Number)
'     StepDuplicateForm : hỏi khi file STEP đã tồn tại
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports WF = System.Windows.Forms
Imports DR = System.Drawing

Namespace StepExport

    ' -------------------------------------------------------------------------------
    '  Form danh sách
    ' -------------------------------------------------------------------------------
    Public Class StepListForm
        Inherits WF.Form

        Private _app As Inventor.Application
        Private _doc As Inventor.Document
        Private _items As List(Of StepPartInfo)
        Private _settings As StepSettings
        Private _previewSvc As StepPreviewService
        Private _preview As StepPreviewForm
        Private _exporting As Boolean = False
        Private _cancel As Boolean = False
        Private _busy As Boolean = False

        Private grid As WF.DataGridView
        Private lblCount As WF.Label
        Private lblInfo As WF.Label
        Private bar As WF.ProgressBar
        Private btnAll As WF.Button
        Private btnSettings As WF.Button
        Private btnExport As WF.Button
        Private btnClose As WF.Button

        Public ReadOnly Property ItemCount As Integer
            Get
                Return _items.Count
            End Get
        End Property

        Public Sub New(app As Inventor.Application, doc As Inventor.Document)
            _app = app
            _doc = doc
            _settings = StepSettings.Load()
            _previewSvc = New StepPreviewService(app)

            Dim res As StepCollectResult = New StepCollector(app).Collect(doc)
            _items = res.Items

            BuildUI()

            Dim parts As New List(Of String)
            If res.SkippedLibrary > 0 Then parts.Add(res.SkippedLibrary.ToString() & " chi tiết thư viện")
            If res.SkippedOther > 0 Then parts.Add(res.SkippedOther.ToString() & " iPart/iAssembly")
            Dim info As String = ""
            If parts.Count > 0 Then info = "Đã bỏ qua: " & String.Join(", ", parts) & "."
            If res.Warnings.Count > 0 Then
                info &= "  Có " & res.Warnings.Count.ToString() & " cảnh báo khi quét (rê chuột vào đây để xem)."
                Dim tip As New WF.ToolTip
                tip.SetToolTip(lblInfo, String.Join(vbCrLf, res.Warnings))
            End If
            lblInfo.Text = info

            FillGrid()
        End Sub

        ' ---------- Dựng giao diện ----------
        Private Sub BuildUI()
            Me.Text = "Danh sách chi tiết xuất STEP"
            Me.Font = New DR.Font("Segoe UI", 9.0F)
            Me.ClientSize = New DR.Size(910, 700)
            Me.MinimumSize = New DR.Size(760, 460)
            Me.StartPosition = WF.FormStartPosition.CenterScreen
            Me.BackColor = DR.Color.White

            grid = New WF.DataGridView
            grid.Location = New DR.Point(18, 18)
            grid.Size = New DR.Size(874, 584)
            grid.Anchor = WF.AnchorStyles.Top Or WF.AnchorStyles.Bottom Or WF.AnchorStyles.Left Or WF.AnchorStyles.Right
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.AllowUserToResizeRows = False
            grid.RowHeadersVisible = False
            grid.MultiSelect = False
            grid.SelectionMode = WF.DataGridViewSelectionMode.FullRowSelect
            grid.BackgroundColor = DR.Color.White
            grid.RowTemplate.Height = 24
            grid.DefaultCellStyle.SelectionBackColor = DR.Color.FromArgb(0, 120, 215)
            grid.DefaultCellStyle.SelectionForeColor = DR.Color.White

            ' 0: STT
            Dim cStt As New WF.DataGridViewTextBoxColumn
            cStt.HeaderText = "STT"
            cStt.ReadOnly = True
            cStt.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.None
            cStt.Width = 50
            cStt.DefaultCellStyle.Alignment = WF.DataGridViewContentAlignment.MiddleCenter
            grid.Columns.Add(cStt)

            ' 1: Xuất
            Dim cChk As New WF.DataGridViewCheckBoxColumn
            cChk.HeaderText = "Xuất"
            cChk.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.None
            cChk.Width = 52
            cChk.SortMode = WF.DataGridViewColumnSortMode.Automatic
            grid.Columns.Add(cChk)

            ' 2: Tên chi tiết (bấm để xem ảnh)
            Dim cName As New WF.DataGridViewButtonColumn
            cName.HeaderText = "Tên chi tiết"
            cName.ReadOnly = True
            cName.FlatStyle = WF.FlatStyle.Flat
            cName.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.Fill
            cName.FillWeight = 36
            cName.DefaultCellStyle.BackColor = DR.Color.White
            cName.DefaultCellStyle.ForeColor = DR.Color.Black
            cName.DefaultCellStyle.Alignment = WF.DataGridViewContentAlignment.MiddleCenter
            cName.SortMode = WF.DataGridViewColumnSortMode.Automatic
            grid.Columns.Add(cName)

            ' 3: Vật liệu
            Dim cMat As New WF.DataGridViewTextBoxColumn
            cMat.HeaderText = "Vật liệu"
            cMat.ReadOnly = True
            cMat.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.Fill
            cMat.FillWeight = 22
            grid.Columns.Add(cMat)

            ' 4: Số lượng
            Dim cQty As New WF.DataGridViewTextBoxColumn
            cQty.HeaderText = "Số lượng"
            cQty.ReadOnly = True
            cQty.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.Fill
            cQty.FillWeight = 20
            cQty.DefaultCellStyle.Alignment = WF.DataGridViewContentAlignment.MiddleCenter
            grid.Columns.Add(cQty)

            ' 5: Trạng thái
            Dim cSt As New WF.DataGridViewTextBoxColumn
            cSt.HeaderText = "Trạng thái"
            cSt.ReadOnly = True
            cSt.AutoSizeMode = WF.DataGridViewAutoSizeColumnMode.Fill
            cSt.FillWeight = 22
            cSt.DefaultCellStyle.Alignment = WF.DataGridViewContentAlignment.MiddleCenter
            grid.Columns.Add(cSt)

            Me.Controls.Add(grid)

            ' Thanh tiến trình + thông tin
            bar = New WF.ProgressBar
            bar.Location = New DR.Point(18, 608)
            bar.Size = New DR.Size(874, 8)
            bar.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Left Or WF.AnchorStyles.Right
            bar.Visible = False
            Me.Controls.Add(bar)

            lblInfo = New WF.Label
            lblInfo.Location = New DR.Point(20, 622)
            lblInfo.Size = New DR.Size(870, 18)
            lblInfo.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Left Or WF.AnchorStyles.Right
            lblInfo.ForeColor = DR.Color.DimGray
            lblInfo.Font = New DR.Font("Segoe UI", 8.5F)
            Me.Controls.Add(lblInfo)

            ' Chân form
            lblCount = New WF.Label
            lblCount.AutoSize = True
            lblCount.Location = New DR.Point(20, 662)
            lblCount.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Left
            Me.Controls.Add(lblCount)

            btnAll = MakeButton("Chọn/Bỏ chọn tất cả", 262, 160)
            btnSettings = MakeButton("Cài đặt", 440, 115)
            btnExport = MakeButton("Xuất STEP", 658, 115)
            btnClose = MakeButton("Đóng", 790, 112)
            btnExport.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Right
            btnClose.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Right

            AddHandler btnAll.Click, AddressOf OnToggleAll
            AddHandler btnSettings.Click, AddressOf OnSettings
            AddHandler btnExport.Click, AddressOf OnExport
            AddHandler btnClose.Click, AddressOf OnCloseClick
            AddHandler grid.CellClick, AddressOf OnGridCellClick
            AddHandler grid.CellValueChanged, AddressOf OnGridCellValueChanged
            AddHandler grid.CurrentCellDirtyStateChanged, AddressOf OnGridDirty
            AddHandler Me.FormClosing, AddressOf OnFormClosing
        End Sub

        Private Function MakeButton(text As String, x As Integer, w As Integer) As WF.Button
            Dim b As New WF.Button
            b.Text = text
            b.Location = New DR.Point(x, 654)
            b.Size = New DR.Size(w, 32)
            b.Anchor = WF.AnchorStyles.Bottom Or WF.AnchorStyles.Left
            Me.Controls.Add(b)
            Return b
        End Function

        ' ---------- Dữ liệu lên bảng ----------
        Private Sub FillGrid()
            _busy = True
            grid.Rows.Clear()
            For i As Integer = 0 To _items.Count - 1
                Dim it As StepPartInfo = _items(i)
                Dim idx As Integer = grid.Rows.Add()
                Dim row As WF.DataGridViewRow = grid.Rows(idx)
                row.Tag = it
                row.Cells(0).Value = i + 1
                row.Cells(1).Value = it.Export
                row.Cells(2).Value = it.PartNumber
                row.Cells(3).Value = it.Material
                row.Cells(4).Value = it.Quantity
                row.Cells(5).Value = it.Status
            Next
            _busy = False
            UpdateCount()
        End Sub

        Private Sub UpdateRow(row As WF.DataGridViewRow)
            Dim it As StepPartInfo = TryCast(row.Tag, StepPartInfo)
            If it Is Nothing Then Return
            _busy = True
            row.Cells(5).Value = it.Status
            Dim bg As DR.Color = DR.Color.White
            If it.Status = StepExporter.ST_DONE Then
                bg = DR.Color.LightGreen
            ElseIf it.Status = StepExporter.ST_ERR Then
                bg = DR.Color.FromArgb(255, 150, 150)
            ElseIf it.Status = StepExporter.ST_SKIPPED Then
                bg = DR.Color.FromArgb(255, 255, 190)
            End If
            For Each i As Integer In New Integer() {0, 1, 3, 4, 5}
                row.Cells(i).Style.BackColor = bg
            Next
            _busy = False
        End Sub

        Private Function FindRow(it As StepPartInfo) As WF.DataGridViewRow
            For Each r As WF.DataGridViewRow In grid.Rows
                If Object.ReferenceEquals(r.Tag, it) Then Return r
            Next
            Return Nothing
        End Function

        Private Sub UpdateCount()
            Dim n As Integer = 0
            For Each it As StepPartInfo In _items
                If it.Export Then n += 1
            Next
            lblCount.Text = "Đã chọn: " & n.ToString() & " / " & _items.Count.ToString()
        End Sub

        Private Function SelectedItems() As List(Of StepPartInfo)
            Dim l As New List(Of StepPartInfo)
            For Each it As StepPartInfo In _items
                If it.Export Then l.Add(it)
            Next
            Return l
        End Function

        ' ---------- Sự kiện bảng ----------
        Private Sub OnGridDirty(sender As Object, e As EventArgs)
            If grid.IsCurrentCellDirty AndAlso grid.CurrentCell IsNot Nothing AndAlso grid.CurrentCell.ColumnIndex = 1 Then
                grid.CommitEdit(WF.DataGridViewDataErrorContexts.Commit)
            End If
        End Sub

        Private Sub OnGridCellValueChanged(sender As Object, e As WF.DataGridViewCellEventArgs)
            If _busy OrElse e.RowIndex < 0 OrElse e.ColumnIndex <> 1 Then Return
            Dim row As WF.DataGridViewRow = grid.Rows(e.RowIndex)
            Dim it As StepPartInfo = TryCast(row.Tag, StepPartInfo)
            If it Is Nothing Then Return
            it.Export = (row.Cells(1).Value IsNot Nothing AndAlso CBool(row.Cells(1).Value))
            UpdateCount()
        End Sub

        Private Sub OnGridCellClick(sender As Object, e As WF.DataGridViewCellEventArgs)
            If e.RowIndex < 0 OrElse e.ColumnIndex <> 2 Then Return
            Dim it As StepPartInfo = TryCast(grid.Rows(e.RowIndex).Tag, StepPartInfo)
            If it Is Nothing Then Return
            ShowPreview(it)
        End Sub

        ' ---------- Xem ảnh chi tiết ----------
        Private Sub ShowPreview(it As StepPartInfo)
            If _exporting Then Return
            If _preview Is Nothing OrElse _preview.IsDisposed Then
                _preview = New StepPreviewForm
                _preview.Location = New DR.Point(Me.Left + (Me.Width - _preview.Width) \ 2, Me.Top + 90)
                _preview.Show(Me)
            End If
            _preview.ShowLoading(it.PartNumber)
            Me.Cursor = WF.Cursors.WaitCursor
            Try
                Dim png As String = _previewSvc.GetPreview(it)
                _preview.ShowImageFile(it.PartNumber, png)
            Catch ex As Exception
                _preview.ShowError(it.PartNumber, "Không tạo được ảnh xem trước:" & vbCrLf & ex.Message)
            Finally
                Me.Cursor = WF.Cursors.Default
                Me.Activate()
            End Try
        End Sub

        ' ---------- Nút ----------
        Private Sub OnToggleAll(sender As Object, e As EventArgs)
            Dim allOn As Boolean = True
            For Each it As StepPartInfo In _items
                If Not it.Export Then
                    allOn = False
                    Exit For
                End If
            Next
            _busy = True
            For Each r As WF.DataGridViewRow In grid.Rows
                Dim it As StepPartInfo = TryCast(r.Tag, StepPartInfo)
                If it Is Nothing Then Continue For
                it.Export = Not allOn
                r.Cells(1).Value = it.Export
            Next
            _busy = False
            UpdateCount()
        End Sub

        Private Sub OnSettings(sender As Object, e As EventArgs)
            Using f As New StepSettingsForm(_settings)
                f.ShowDialog(Me)
            End Using
        End Sub

        Private Sub OnCloseClick(sender As Object, e As EventArgs)
            Me.Close()
        End Sub

        Private Sub OnFormClosing(sender As Object, e As WF.FormClosingEventArgs)
            If _exporting Then
                e.Cancel = True
                Return
            End If
            If _preview IsNot Nothing AndAlso Not _preview.IsDisposed Then _preview.Close()
        End Sub

        ' ---------- Chọn thư mục lưu theo cài đặt ----------
        Private Function ResolveFolder() As String
            Select Case _settings.SaveMode
                Case StepSaveMode.FixedFolder
                    Dim p As String = _settings.FixedFolder
                    If String.IsNullOrWhiteSpace(p) Then
                        WF.MessageBox.Show(Me, "Chưa chọn thư mục cố định. Hãy vào Cài đặt để chọn.", Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                        Return ""
                    End If
                    Try
                        Directory.CreateDirectory(p)
                        Return p
                    Catch ex As Exception
                        WF.MessageBox.Show(Me, "Không dùng được thư mục cố định:" & vbCrLf & ex.Message, Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Warning)
                        Return ""
                    End Try

                Case StepSaveMode.NextToDocument
                    If Not File.Exists(_doc.FullFileName) Then
                        WF.MessageBox.Show(Me, "File đang mở chưa được lưu nên chưa có thư mục. Hãy lưu file hoặc đổi cách lưu trong Cài đặt.", Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                        Return ""
                    End If
                    Dim p2 As String = Path.Combine(Path.GetDirectoryName(_doc.FullFileName), "STEP")
                    Try
                        Directory.CreateDirectory(p2)
                        Return p2
                    Catch ex As Exception
                        WF.MessageBox.Show(Me, "Không tạo được thư mục STEP:" & vbCrLf & ex.Message, Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Warning)
                        Return ""
                    End Try

                Case Else
                    Using dlg As New WF.FolderBrowserDialog
                        dlg.Description = "Chọn thư mục lưu file STEP"
                        If Not String.IsNullOrWhiteSpace(_settings.LastFolder) AndAlso Directory.Exists(_settings.LastFolder) Then
                            dlg.SelectedPath = _settings.LastFolder
                        ElseIf File.Exists(_doc.FullFileName) Then
                            dlg.SelectedPath = Path.GetDirectoryName(_doc.FullFileName)
                        End If
                        If dlg.ShowDialog(Me) = WF.DialogResult.OK Then Return dlg.SelectedPath
                    End Using
                    Return ""
            End Select
        End Function

        ' ---------- Xuất STEP ----------
        Private Sub SetBusy(b As Boolean)
            _exporting = b
            grid.Enabled = Not b
            btnAll.Enabled = Not b
            btnSettings.Enabled = Not b
            btnClose.Enabled = Not b
            bar.Visible = b
            btnExport.Enabled = True
            btnExport.Text = If(b, "Dừng", "Xuất STEP")
        End Sub

        Private Function IsCancelled() As Boolean
            Return _cancel
        End Function

        Private Function AskDuplicate(path As String) As StepDuplicateAnswer
            Using f As New StepDuplicateForm(path)
                f.ShowDialog(Me)
                Return f.Answer
            End Using
        End Function

        Private Sub OnProgress(done As Integer, total As Integer, it As StepPartInfo)
            bar.Maximum = Math.Max(1, total)
            bar.Value = Math.Min(done, bar.Maximum)
            lblInfo.Text = "Đang xuất " & Math.Min(done + 1, total).ToString() & " / " & total.ToString() & ": " & it.PartNumber
            Dim r As WF.DataGridViewRow = FindRow(it)
            If r IsNot Nothing Then
                UpdateRow(r)
                Try
                    grid.FirstDisplayedScrollingRowIndex = Math.Max(0, r.Index - 3)
                Catch
                End Try
            End If
            WF.Application.DoEvents()
        End Sub

        Private Sub OnExport(sender As Object, e As EventArgs)
            If _exporting Then
                _cancel = True
                btnExport.Enabled = False
                lblInfo.Text = "Đang dừng sau chi tiết hiện tại..."
                Return
            End If

            Dim sel As List(Of StepPartInfo) = SelectedItems()
            If sel.Count = 0 Then
                WF.MessageBox.Show(Me, "Chưa chọn chi tiết nào để xuất.", Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                Return
            End If

            Dim folder As String = ResolveFolder()
            If folder = "" Then Return

            For Each it As StepPartInfo In sel
                it.Status = ""
                Dim r As WF.DataGridViewRow = FindRow(it)
                If r IsNot Nothing Then UpdateRow(r)
            Next

            _cancel = False
            SetBusy(True)
            Dim res As StepExportResult = Nothing
            Dim err As String = ""
            Try
                Dim exporter As New StepExporter(_app)
                res = exporter.Export(sel, folder, _settings.DuplicateMode,
                                      New Func(Of String, StepDuplicateAnswer)(AddressOf AskDuplicate),
                                      New Action(Of Integer, Integer, StepPartInfo)(AddressOf OnProgress),
                                      New Func(Of Boolean)(AddressOf IsCancelled))
            Catch ex As Exception
                err = ex.Message
            Finally
                SetBusy(False)
                _cancel = False
                lblInfo.Text = ""
            End Try

            If err <> "" Then
                WF.MessageBox.Show(Me, "Lỗi khi xuất STEP:" & vbCrLf & err, Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Error)
                Return
            End If

            _settings.LastFolder = folder
            _settings.Save()

            Dim msg As String = If(res.Cancelled, "Đã dừng.", "Hoàn tất.")
            msg &= vbCrLf & "Đã xuất: " & res.Ok.ToString() & vbCrLf & "Bỏ qua: " & res.Skipped.ToString() & vbCrLf & "Lỗi: " & res.Failed.ToString()
            If res.Errors.Count > 0 Then
                msg &= vbCrLf & vbCrLf & "Chi tiết lỗi:"
                For i As Integer = 0 To Math.Min(9, res.Errors.Count - 1)
                    msg &= vbCrLf & " - " & res.Errors(i)
                Next
                If res.Errors.Count > 10 Then msg &= vbCrLf & " ..."
            End If
            msg &= vbCrLf & vbCrLf & "Thư mục: " & folder & vbCrLf & "Mở thư mục chứa file?"

            If WF.MessageBox.Show(Me, msg, Me.Text, WF.MessageBoxButtons.YesNo, WF.MessageBoxIcon.Information) = WF.DialogResult.Yes Then
                Try
                    System.Diagnostics.Process.Start(New System.Diagnostics.ProcessStartInfo("explorer.exe", """" & folder & """"))
                Catch
                End Try
            End If
        End Sub
    End Class

    ' -------------------------------------------------------------------------------
    '  Form cài đặt
    ' -------------------------------------------------------------------------------
    Public Class StepSettingsForm
        Inherits WF.Form

        Private _s As StepSettings
        Private cboSave As WF.ComboBox
        Private cboDup As WF.ComboBox
        Private txtFolder As WF.TextBox
        Private btnBrowse As WF.Button

        Public Sub New(s As StepSettings)
            _s = s
            Me.Text = "Cài đặt xuất STEP"
            Me.Font = New DR.Font("Segoe UI", 9.0F)
            Me.FormBorderStyle = WF.FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ShowInTaskbar = False
            Me.StartPosition = WF.FormStartPosition.CenterParent
            Me.ClientSize = New DR.Size(660, 260)

            AddLabel("Lựa chọn lưu file xuất STEP", 30, 44)
            cboSave = New WF.ComboBox
            cboSave.DropDownStyle = WF.ComboBoxStyle.DropDownList
            cboSave.Items.AddRange(New Object() {"Hỏi vị trí lưu", "Thư mục cố định", "Thư mục STEP cạnh file đang mở"})
            cboSave.Location = New DR.Point(250, 40)
            cboSave.Size = New DR.Size(380, 25)
            Me.Controls.Add(cboSave)

            AddLabel("Thư mục cố định", 30, 88)
            txtFolder = New WF.TextBox
            txtFolder.Location = New DR.Point(250, 84)
            txtFolder.Size = New DR.Size(310, 25)
            Me.Controls.Add(txtFolder)
            btnBrowse = New WF.Button
            btnBrowse.Text = "..."
            btnBrowse.Location = New DR.Point(566, 83)
            btnBrowse.Size = New DR.Size(64, 27)
            Me.Controls.Add(btnBrowse)

            AddLabel("Khi file STEP đã tồn tại", 30, 132)
            cboDup = New WF.ComboBox
            cboDup.DropDownStyle = WF.ComboBoxStyle.DropDownList
            cboDup.Items.AddRange(New Object() {"Hỏi", "Ghi đè", "Bỏ qua"})
            cboDup.Location = New DR.Point(250, 128)
            cboDup.Size = New DR.Size(380, 25)
            Me.Controls.Add(cboDup)

            Dim btnSave As New WF.Button
            btnSave.Text = "Lưu cài đặt"
            btnSave.Location = New DR.Point(400, 205)
            btnSave.Size = New DR.Size(120, 32)
            Me.Controls.Add(btnSave)
            Dim btnClose As New WF.Button
            btnClose.Text = "Đóng"
            btnClose.Location = New DR.Point(530, 205)
            btnClose.Size = New DR.Size(100, 32)
            Me.Controls.Add(btnClose)

            cboSave.SelectedIndex = CInt(_s.SaveMode)
            cboDup.SelectedIndex = CInt(_s.DuplicateMode)
            txtFolder.Text = _s.FixedFolder
            UpdateFolderEnabled()

            AddHandler cboSave.SelectedIndexChanged, AddressOf OnSaveModeChanged
            AddHandler btnBrowse.Click, AddressOf OnBrowse
            AddHandler btnSave.Click, AddressOf OnSave
            AddHandler btnClose.Click, AddressOf OnCloseClick
            Me.CancelButton = btnClose
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim l As New WF.Label
            l.Text = text
            l.AutoSize = True
            l.Location = New DR.Point(x, y)
            Me.Controls.Add(l)
        End Sub

        Private Sub UpdateFolderEnabled()
            Dim on1 As Boolean = (cboSave.SelectedIndex = CInt(StepSaveMode.FixedFolder))
            txtFolder.Enabled = on1
            btnBrowse.Enabled = on1
        End Sub

        Private Sub OnSaveModeChanged(sender As Object, e As EventArgs)
            UpdateFolderEnabled()
        End Sub

        Private Sub OnBrowse(sender As Object, e As EventArgs)
            Using dlg As New WF.FolderBrowserDialog
                dlg.Description = "Chọn thư mục cố định để lưu file STEP"
                If txtFolder.Text.Trim() <> "" AndAlso Directory.Exists(txtFolder.Text.Trim()) Then dlg.SelectedPath = txtFolder.Text.Trim()
                If dlg.ShowDialog(Me) = WF.DialogResult.OK Then txtFolder.Text = dlg.SelectedPath
            End Using
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            If cboSave.SelectedIndex = CInt(StepSaveMode.FixedFolder) AndAlso txtFolder.Text.Trim() = "" Then
                WF.MessageBox.Show(Me, "Hãy chọn thư mục cố định.", Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                Return
            End If
            _s.SaveMode = CType(cboSave.SelectedIndex, StepSaveMode)
            _s.FixedFolder = txtFolder.Text.Trim()
            _s.DuplicateMode = CType(cboDup.SelectedIndex, StepDuplicateMode)
            If Not _s.Save() Then
                WF.MessageBox.Show(Me, "Không ghi được file cài đặt (đã áp dụng cho lần chạy này).", Me.Text, WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Warning)
            End If
            Me.DialogResult = WF.DialogResult.OK
        End Sub

        Private Sub OnCloseClick(sender As Object, e As EventArgs)
            Me.DialogResult = WF.DialogResult.Cancel
        End Sub
    End Class

    ' -------------------------------------------------------------------------------
    '  Cửa sổ ảnh xem trước
    ' -------------------------------------------------------------------------------
    Public Class StepPreviewForm
        Inherits WF.Form

        Private pic As WF.PictureBox
        Private lbl As WF.Label

        Public Sub New()
            Me.Font = New DR.Font("Segoe UI", 9.0F)
            Me.FormBorderStyle = WF.FormBorderStyle.Sizable
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ShowInTaskbar = False
            Me.StartPosition = WF.FormStartPosition.Manual
            Me.ClientSize = New DR.Size(560, 420)
            Me.BackColor = DR.Color.White
            Me.KeyPreview = True

            pic = New WF.PictureBox
            pic.Dock = WF.DockStyle.Fill
            pic.SizeMode = WF.PictureBoxSizeMode.Zoom
            pic.BackColor = DR.Color.White
            Me.Controls.Add(pic)

            lbl = New WF.Label
            lbl.Dock = WF.DockStyle.Fill
            lbl.TextAlign = DR.ContentAlignment.MiddleCenter
            lbl.BackColor = DR.Color.White
            lbl.ForeColor = DR.Color.DimGray
            lbl.Visible = False
            Me.Controls.Add(lbl)

            AddHandler Me.KeyDown, AddressOf OnKey
        End Sub

        Private Sub OnKey(sender As Object, e As WF.KeyEventArgs)
            If e.KeyCode = WF.Keys.Escape Then Me.Close()
        End Sub

        Public Sub ShowLoading(title As String)
            Me.Text = title
            lbl.Text = "Đang tạo ảnh xem trước..."
            lbl.Visible = True
            lbl.BringToFront()
            Me.Refresh()
        End Sub

        Public Sub ShowError(title As String, msg As String)
            Me.Text = title
            lbl.Text = msg
            lbl.Visible = True
            lbl.BringToFront()
        End Sub

        Public Sub ShowImageFile(title As String, filePath As String)
            Me.Text = title
            Dim bytes As Byte() = File.ReadAllBytes(filePath)
            Using ms As New MemoryStream(bytes)
                Using img As DR.Image = DR.Image.FromStream(ms)
                    Dim bmp As New DR.Bitmap(img)
                    If pic.Image IsNot Nothing Then pic.Image.Dispose()
                    pic.Image = bmp
                End Using
            End Using
            lbl.Visible = False
        End Sub
    End Class

    ' -------------------------------------------------------------------------------
    '  Hỏi khi file STEP đã tồn tại
    ' -------------------------------------------------------------------------------
    Public Class StepDuplicateForm
        Inherits WF.Form

        Public Answer As StepDuplicateAnswer = StepDuplicateAnswer.Cancel

        Public Sub New(existingPath As String)
            Me.Text = "File đã tồn tại"
            Me.Font = New DR.Font("Segoe UI", 9.0F)
            Me.FormBorderStyle = WF.FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.ShowInTaskbar = False
            Me.StartPosition = WF.FormStartPosition.CenterParent
            Me.ClientSize = New DR.Size(640, 170)

            Dim l As New WF.Label
            l.AutoSize = False
            l.Location = New DR.Point(16, 14)
            l.Size = New DR.Size(608, 80)
            l.Text = "File đã tồn tại:" & vbCrLf & existingPath & vbCrLf & vbCrLf & "Chọn cách xử lý:"
            Me.Controls.Add(l)

            AddAnswerButton("Ghi đè", StepDuplicateAnswer.Overwrite, 16, 110)
            AddAnswerButton("Ghi đè tất cả", StepDuplicateAnswer.OverwriteAll, 134, 130)
            AddAnswerButton("Bỏ qua", StepDuplicateAnswer.Skip, 272, 110)
            AddAnswerButton("Bỏ qua tất cả", StepDuplicateAnswer.SkipAll, 390, 120)
            AddAnswerButton("Dừng", StepDuplicateAnswer.Cancel, 518, 106)
        End Sub

        Private Sub AddAnswerButton(text As String, ans As StepDuplicateAnswer, x As Integer, w As Integer)
            Dim b As New WF.Button
            b.Text = text
            b.Location = New DR.Point(x, 112)
            b.Size = New DR.Size(w, 32)
            AddHandler b.Click, Sub(s As Object, e As EventArgs)
                                    Answer = ans
                                    Me.DialogResult = WF.DialogResult.OK
                                End Sub
            Me.Controls.Add(b)
        End Sub
    End Class

End Namespace
