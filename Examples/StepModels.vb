' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  File 1/5: Mô hình dữ liệu + Cài đặt
'  Tích hợp vào add-in: copy 5 file StepModels / StepCollector / StepExporter /
'  StepPreview / StepForms / StepExportTool vào project, tham chiếu Autodesk.Inventor.Interop.
'  Điểm vào duy nhất:  StepExport.StepExportTool.Show(inventorApplication)
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports System.IO

Namespace StepExport

    ' Cách chọn nơi lưu file STEP
    Public Enum StepSaveMode
        AskEachTime = 0        ' Hỏi vị trí lưu
        FixedFolder = 1        ' Thư mục cố định
        NextToDocument = 2     ' Thư mục "STEP" cạnh file đang mở
    End Enum

    ' Khi file STEP đã tồn tại
    Public Enum StepDuplicateMode
        Ask = 0
        Overwrite = 1
        Skip = 2
    End Enum

    Public Enum StepDuplicateAnswer
        Overwrite
        OverwriteAll
        [Skip]
        SkipAll
        Cancel
    End Enum

    ' Một dòng trong danh sách = một file Part
    Public Class StepPartInfo
        Public Doc As Inventor.Document
        Public DocKey As String
        Public PartNumber As String
        Public Material As String
        Public Quantity As Integer
        Public [Export] As Boolean = True
        Public Status As String = ""
    End Class

    ' Cài đặt, lưu ở %AppData%\StepExport\settings.ini (không cần thư viện ngoài).
    ' Muốn lưu chung với cài đặt của add-in: sửa 2 hàm Load/Save.
    Public Class StepSettings
        Public Shared StorePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StepExport", "settings.ini")

        Public SaveMode As StepSaveMode = StepSaveMode.AskEachTime
        Public FixedFolder As String = ""
        Public DuplicateMode As StepDuplicateMode = StepDuplicateMode.Ask
        Public LastFolder As String = ""

        Public Shared Function Load() As StepSettings
            Dim s As New StepSettings
            Try
                If File.Exists(StorePath) Then
                    For Each line As String In File.ReadAllLines(StorePath)
                        Dim p As Integer = line.IndexOf("="c)
                        If p <= 0 Then Continue For
                        Dim k As String = line.Substring(0, p).Trim()
                        Dim v As String = line.Substring(p + 1).Trim()
                        Dim n As Integer
                        Select Case k
                            Case "SaveMode"
                                If Integer.TryParse(v, n) AndAlso [Enum].IsDefined(GetType(StepSaveMode), n) Then
                                    s.SaveMode = CType(n, StepSaveMode)
                                End If
                            Case "FixedFolder"
                                s.FixedFolder = v
                            Case "DuplicateMode"
                                If Integer.TryParse(v, n) AndAlso [Enum].IsDefined(GetType(StepDuplicateMode), n) Then
                                    s.DuplicateMode = CType(n, StepDuplicateMode)
                                End If
                            Case "LastFolder"
                                s.LastFolder = v
                        End Select
                    Next
                End If
            Catch
            End Try
            Return s
        End Function

        Public Function Save() As Boolean
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(StorePath))
                File.WriteAllLines(StorePath, New String() {
                    "SaveMode=" & CInt(SaveMode).ToString(),
                    "FixedFolder=" & FixedFolder,
                    "DuplicateMode=" & CInt(DuplicateMode).ToString(),
                    "LastFolder=" & LastFolder})
                Return True
            Catch
                Return False
            End Try
        End Function
    End Class

End Namespace
