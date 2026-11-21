' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  File 3/5: Xuất file STEP (AP214, dạng gấp 3D, từ file Part)
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace StepExport

    Public Class StepExportResult
        Public Ok As Integer
        Public Skipped As Integer
        Public Failed As Integer
        Public Cancelled As Boolean
        Public Errors As New List(Of String)
    End Class

    Public Class StepExporter
        Private Const STEP_TRANSLATOR_ID As String = "{90AF7F40-0C01-11D5-8E83-0010B541CD80}"
        Private Const AP214_AUTOMOTIVE As Integer = 3          ' ApplicationProtocolType: 2 = AP203, 3 = AP214
        Public Const STEP_EXT As String = ".stp"

        Public Const ST_EXPORTING As String = "Đang xuất..."
        Public Const ST_DONE As String = "Đã xuất"
        Public Const ST_SKIPPED As String = "Bỏ qua (đã tồn tại)"
        Public Const ST_ERR As String = "Lỗi"

        Private _app As Inventor.Application
        Private _translator As Inventor.TranslatorAddIn

        Public Sub New(app As Inventor.Application)
            _app = app
        End Sub

        Private Sub EnsureTranslator()
            If _translator IsNot Nothing Then Return
            Dim ai As Inventor.ApplicationAddIn = _app.ApplicationAddIns.ItemById(STEP_TRANSLATOR_ID)
            If ai Is Nothing Then Throw New Exception("Không tìm thấy STEP Translator của Inventor.")
            If Not ai.Activated Then ai.Activate()
            _translator = CType(CObj(ai), Inventor.TranslatorAddIn)
        End Sub

        Public Shared Function SanitizeName(name As String) As String
            Dim s As String = If(name, "").Trim()
            For Each c As Char In Path.GetInvalidFileNameChars()
                s = s.Replace(c, "_"c)
            Next
            s = s.TrimEnd("."c, " "c)
            If s = "" Then s = "NoName"
            Return s
        End Function

        Private Sub Report(progress As Action(Of Integer, Integer, StepPartInfo), done As Integer, total As Integer, it As StepPartInfo)
            If progress IsNot Nothing Then progress.Invoke(done, total, it)
        End Sub

        ' progress(done, total, item): gọi trước và sau mỗi chi tiết. isCancelled: trả True để dừng.
        Public Function Export(items As List(Of StepPartInfo), folder As String, dupMode As StepDuplicateMode,
                               askDup As Func(Of String, StepDuplicateAnswer),
                               progress As Action(Of Integer, Integer, StepPartInfo),
                               isCancelled As Func(Of Boolean)) As StepExportResult
            Dim res As New StepExportResult
            EnsureTranslator()
            Directory.CreateDirectory(folder)

            Dim used As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim mode As StepDuplicateMode = dupMode

            For i As Integer = 0 To items.Count - 1
                If isCancelled IsNot Nothing AndAlso isCancelled.Invoke() Then
                    res.Cancelled = True
                    Exit For
                End If

                Dim it As StepPartInfo = items(i)
                it.Status = ST_EXPORTING
                Report(progress, i, items.Count, it)

                ' Tên file = Part Number. Hai file khác nhau trùng Part Number trong cùng đợt -> thêm _2, _3...
                Dim baseName As String = SanitizeName(it.PartNumber)
                Dim fileName As String = baseName & STEP_EXT
                Dim n As Integer = 1
                While used.Contains(fileName)
                    n += 1
                    fileName = baseName & "_" & n.ToString() & STEP_EXT
                End While
                used.Add(fileName)
                Dim fullPath As String = Path.Combine(folder, fileName)

                ' File đã tồn tại trên đĩa
                Dim doSkip As Boolean = False
                If File.Exists(fullPath) Then
                    If mode = StepDuplicateMode.Ask Then
                        Dim ans As StepDuplicateAnswer = StepDuplicateAnswer.Cancel
                        If askDup IsNot Nothing Then ans = askDup.Invoke(fullPath)
                        Select Case ans
                            Case StepDuplicateAnswer.Overwrite
                                doSkip = False
                            Case StepDuplicateAnswer.OverwriteAll
                                mode = StepDuplicateMode.Overwrite
                            Case StepDuplicateAnswer.Skip
                                doSkip = True
                            Case StepDuplicateAnswer.SkipAll
                                mode = StepDuplicateMode.Skip
                                doSkip = True
                            Case Else
                                res.Cancelled = True
                        End Select
                        If res.Cancelled Then
                            it.Status = ""
                            Report(progress, i, items.Count, it)
                            Exit For
                        End If
                    ElseIf mode = StepDuplicateMode.Skip Then
                        doSkip = True
                    End If
                End If

                If doSkip Then
                    it.Status = ST_SKIPPED
                    res.Skipped += 1
                Else
                    Try
                        ExportOne(it.Doc, fullPath)
                        If File.Exists(fullPath) Then
                            it.Status = ST_DONE
                            res.Ok += 1
                        Else
                            Throw New Exception("không thấy file sau khi xuất")
                        End If
                    Catch ex As Exception
                        it.Status = ST_ERR
                        res.Failed += 1
                        res.Errors.Add(it.PartNumber & ": " & ex.Message.Replace(vbCr, " ").Replace(vbLf, " "))
                    End Try
                End If
                Report(progress, i + 1, items.Count, it)
            Next
            Return res
        End Function

        ' Xuất trực tiếp từ file Part -> gốc tọa độ là của chi tiết (không phải của assembly).
        Private Sub ExportOne(doc As Inventor.Document, fullPath As String)
            Dim ctx As Inventor.TranslationContext = _app.TransientObjects.CreateTranslationContext()
            ctx.Type = Inventor.IOMechanismEnum.kFileBrowseIOMechanism

            Dim opts As Inventor.NameValueMap = _app.TransientObjects.CreateNameValueMap()
            _translator.HasSaveCopyAsOptions(doc, ctx, opts)
            opts.Value("ApplicationProtocolType") = AP214_AUTOMOTIVE

            Dim dm As Inventor.DataMedium = _app.TransientObjects.CreateDataMedium()
            dm.FileName = fullPath

            _translator.SaveCopyAs(doc, ctx, opts, dm)
        End Sub
    End Class

End Namespace
