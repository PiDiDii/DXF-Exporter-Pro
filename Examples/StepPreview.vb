' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  File 4/5: Tạo ảnh xem trước của chi tiết (khi bấm vào Part Number)
'
'  Cách làm: mở cửa sổ của file Part, đặt góc nhìn Iso, Fit, lưu ảnh PNG bằng View.SaveAsBitmap,
'  rồi đóng cửa sổ và trả lại tài liệu đang làm việc. Ảnh được cache ở %TEMP%\StepExportPreview
'  (tạo lại nếu file Part mới hơn ảnh hoặc đang có thay đổi chưa lưu).
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports System.IO
Imports System.Text

Namespace StepExport

    Public Class StepPreviewService
        Private _app As Inventor.Application

        Public Sub New(app As Inventor.Application)
            _app = app
        End Sub

        Private Shared Function HashKey(key As String) As String
            Using sha As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                Dim b As Byte() = sha.ComputeHash(Encoding.UTF8.GetBytes(key.ToLowerInvariant()))
                Return BitConverter.ToString(b, 0, 12).Replace("-", "")
            End Using
        End Function

        ' Trả về đường dẫn file PNG xem trước (ném lỗi nếu không tạo được)
        Public Function GetPreview(info As StepPartInfo) As String
            Dim dir As String = Path.Combine(Path.GetTempPath(), "StepExportPreview")
            Directory.CreateDirectory(dir)
            Dim png As String = Path.Combine(dir, HashKey(info.DocKey) & ".png")

            Dim dirty As Boolean = False
            Try
                dirty = info.Doc.Dirty
            Catch
            End Try

            If File.Exists(png) AndAlso Not dirty Then
                Dim srcTime As DateTime = DateTime.MinValue
                Try
                    If File.Exists(info.Doc.FullFileName) Then srcTime = File.GetLastWriteTime(info.Doc.FullFileName)
                Catch
                End Try
                If File.GetLastWriteTime(png) >= srcTime Then Return png
            End If

            Render(info.Doc, png)
            If Not File.Exists(png) Then Throw New Exception("Inventor không tạo được ảnh.")
            Return png
        End Function

        Private Sub Render(d As Inventor.Document, png As String)
            Dim home As Inventor.Document = _app.ActiveDocument
            Dim view As Inventor.View = Nothing
            Dim win As Object = Nothing
            Dim created As Boolean = False
            Dim hadView As Boolean = False
            Dim eye0 As Inventor.Point = Nothing
            Dim tgt0 As Inventor.Point = Nothing
            Dim up0 As Inventor.UnitVector = Nothing
            Dim w0 As Double = 0
            Dim h0 As Double = 0

            Try
                If d.Views.Count > 0 Then
                    ' Part đang có cửa sổ riêng: dùng luôn, nhớ camera để trả lại sau
                    hadView = True
                    d.Activate()
                    view = _app.ActiveView
                    Dim c0 As Inventor.Camera = view.Camera
                    eye0 = c0.Eye
                    tgt0 = c0.Target
                    up0 = c0.UpVector
                    Try
                        c0.GetExtents(w0, h0)
                    Catch
                        w0 = 0
                    End Try
                Else
                    _app.Documents.Open(d.FullFileName, True)
                    created = True
                    view = _app.ActiveView
                    win = view.Parent
                End If

                Dim cam As Inventor.Camera = view.Camera
                cam.ViewOrientationType = Inventor.ViewOrientationTypeEnum.kIsoTopRightViewOrientation
                cam.Apply()
                view.Fit()

                Dim white As Inventor.Color = _app.TransientObjects.CreateColor(255, 255, 255)
                view.SaveAsBitmap(png, 900, 640, white, white)
            Finally
                Try
                    If created AndAlso win IsNot Nothing Then
                        win.Close()
                    ElseIf hadView AndAlso view IsNot Nothing AndAlso eye0 IsNot Nothing Then
                        Dim c1 As Inventor.Camera = view.Camera
                        c1.Eye = eye0
                        c1.Target = tgt0
                        c1.UpVector = up0
                        If w0 > 0 Then
                            Try
                                c1.SetExtents(w0, h0)
                            Catch
                            End Try
                        End If
                        c1.Apply()
                    End If
                Catch
                End Try
                Try
                    If home IsNot Nothing Then home.Activate()
                Catch
                End Try
            End Try
        End Sub
    End Class

End Namespace
