' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  Điểm vào cho add-in
'
'  Trong add-in, ở sự kiện bấm nút (ButtonDefinition.OnExecute) chỉ cần gọi:
'        StepExport.StepExportTool.Show(m_inventorApplication)
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports WF = System.Windows.Forms

Namespace StepExport

    Public Class StepExportTool

        Private Const CAPTION As String = "Xuất STEP"

        Public Shared Sub Show(app As Inventor.Application)
            Dim doc As Inventor.Document = app.ActiveDocument
            If doc Is Nothing OrElse
               (doc.DocumentType <> Inventor.DocumentTypeEnum.kAssemblyDocumentObject AndAlso
                doc.DocumentType <> Inventor.DocumentTypeEnum.kPartDocumentObject) Then
                WF.MessageBox.Show("Hãy mở file Assembly (.iam) hoặc Part (.ipt) rồi chạy lại.", CAPTION,
                                   WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                Return
            End If

            Dim f As StepListForm = Nothing
            Try
                WF.Cursor.Current = WF.Cursors.WaitCursor
                f = New StepListForm(app, doc)
            Catch ex As Exception
                WF.MessageBox.Show("Không đọc được danh sách chi tiết:" & vbCrLf & ex.Message, CAPTION,
                                   WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Error)
                Return
            Finally
                WF.Cursor.Current = WF.Cursors.Default
            End Try

            Using f
                If f.ItemCount = 0 Then
                    WF.MessageBox.Show("Không tìm thấy chi tiết nào để xuất (đã bỏ qua chi tiết thư viện).", CAPTION,
                                       WF.MessageBoxButtons.OK, WF.MessageBoxIcon.Information)
                    Return
                End If
                Dim owner As New HwndWrapper(New IntPtr(CLng(app.MainFrameHWND)))
                f.ShowDialog(owner)
            End Using
        End Sub
    End Class

    ' Cho form con của cửa sổ Inventor
    Public Class HwndWrapper
        Implements WF.IWin32Window
        Private _h As IntPtr
        Public Sub New(h As IntPtr)
            _h = h
        End Sub
        Public ReadOnly Property Handle As IntPtr Implements WF.IWin32Window.Handle
            Get
                Return _h
            End Get
        End Property
    End Class

End Namespace
