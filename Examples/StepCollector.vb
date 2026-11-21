' =====================================================================================
'  XUẤT STEP TỪNG CHI TIẾT  -  File 2/5: Lấy danh sách chi tiết (Assembly hoặc Part)
' =====================================================================================
Option Strict Off
Option Explicit On
Option Infer On

Imports System
Imports System.Collections.Generic

Namespace StepExport

    Public Class StepCollectResult
        Public Items As New List(Of StepPartInfo)
        Public SkippedLibrary As Integer
        Public SkippedOther As Integer          ' iPart / iAssembly member
        Public Warnings As New List(Of String)
    End Class

    Public Class StepCollector
        Private _app As Inventor.Application
        Private _libPaths As List(Of String)
        Private _map As Dictionary(Of String, StepPartInfo)
        Private _libKeys As HashSet(Of String)
        Private _otherKeys As HashSet(Of String)
        Private _res As StepCollectResult

        Public Sub New(app As Inventor.Application)
            _app = app
        End Sub

        ' Assembly: duyệt đệ quy toàn bộ cây (theo thứ tự Model browser), gộp theo file, đếm số lượng.
        ' Part: một dòng, số lượng 1.
        Public Function Collect(doc As Inventor.Document) As StepCollectResult
            _res = New StepCollectResult
            _map = New Dictionary(Of String, StepPartInfo)(StringComparer.OrdinalIgnoreCase)
            _libKeys = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            _otherKeys = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            _libPaths = LoadLibraryPaths()

            Select Case doc.DocumentType
                Case Inventor.DocumentTypeEnum.kPartDocumentObject
                    AddPart(doc, Nothing, False)
                Case Inventor.DocumentTypeEnum.kAssemblyDocumentObject
                    Dim asm As Inventor.AssemblyDocument = CType(doc, Inventor.AssemblyDocument)
                    Walk(asm.ComponentDefinition.Occurrences)
            End Select

            _res.SkippedLibrary = _libKeys.Count
            Dim n As Integer = 0
            For Each k As String In _otherKeys
                If Not _map.ContainsKey(k) Then n += 1
            Next
            _res.SkippedOther = n
            Return _res
        End Function

        Private Sub Walk(occs As System.Collections.IEnumerable)
            For Each o As Object In occs
                Dim occ As Inventor.ComponentOccurrence = CType(o, Inventor.ComponentOccurrence)
                Try
                    If occ.Suppressed Then Continue For
                    If TypeOf occ.Definition Is Inventor.VirtualComponentDefinition Then Continue For

                    If occ.DefinitionDocumentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject Then
                        Walk(occ.SubOccurrences)
                    ElseIf occ.DefinitionDocumentType = Inventor.DocumentTypeEnum.kPartDocumentObject Then
                        AddPart(CType(occ.Definition.Document, Inventor.Document), occ, True)
                    End If
                Catch ex As Exception
                    Dim nm As String = "?"
                    Try
                        nm = occ.Name
                    Catch
                    End Try
                    _res.Warnings.Add(nm & ": " & ex.Message.Replace(vbCr, " ").Replace(vbLf, " "))
                End Try
            Next
        End Sub

        Private Sub AddPart(d As Inventor.Document, occ As Inventor.ComponentOccurrence, applyFilter As Boolean)
            Dim key As String = d.FullDocumentName
            Dim existing As StepPartInfo = Nothing
            If _map.TryGetValue(key, existing) Then
                existing.Quantity += 1
                Return
            End If

            If applyFilter Then
                If _libKeys.Contains(key) OrElse _otherKeys.Contains(key) Then Return
                Dim def As Object = occ.Definition
                If IsiMember(def) Then
                    _otherKeys.Add(key)
                    Return
                End If
                If IsLibraryDoc(d, def) Then
                    _libKeys.Add(key)
                    Return
                End If
            End If

            Dim info As New StepPartInfo
            info.Doc = d
            info.DocKey = key
            info.PartNumber = GetPartNumber(d)
            info.Material = GetMaterial(d)
            info.Quantity = 1
            _map(key) = info
            _res.Items.Add(info)
        End Sub

        ' ---------- Part Number / Vật liệu ----------
        Private Function GetPartNumber(d As Inventor.Document) As String
            Dim pn As String = ""
            Try
                pn = CStr(d.PropertySets.Item("Design Tracking Properties").Item("Part Number").Value).Trim()
            Catch
            End Try
            If pn = "" Then
                Try
                    pn = System.IO.Path.GetFileNameWithoutExtension(d.FullFileName)
                Catch
                End Try
            End If
            If pn = "" Then pn = d.DisplayName
            Return pn
        End Function

        Private Function GetMaterial(d As Inventor.Document) As String
            Try
                Dim s As String = CStr(d.PropertySets.Item("Design Tracking Properties").Item("Material").Value).Trim()
                If s <> "" Then Return s
            Catch
            End Try
            Try
                Dim pd As Inventor.PartDocument = TryCast(d, Inventor.PartDocument)
                If pd IsNot Nothing Then Return pd.ActiveMaterial.Name
            Catch
            End Try
            Return ""
        End Function

        ' ---------- Thư viện / iPart ----------
        Private Function LoadLibraryPaths() As List(Of String)
            Dim res As New List(Of String)
            Try
                Dim prj As Object = _app.DesignProjectManager.ActiveDesignProject
                Dim prjDir As String = System.IO.Path.GetDirectoryName(CStr(prj.FullFileName))
                Try
                    Dim paths As Object = prj.LibraryPaths
                    For i As Integer = 1 To CInt(paths.Count)
                        Try
                            AddLibPath(res, CStr(paths.Item(i).Path), prjDir)
                        Catch
                        End Try
                    Next
                Catch
                End Try
                Try
                    AddLibPath(res, CStr(prj.ContentCenterPath), prjDir)
                Catch
                End Try
            Catch
            End Try
            Return res
        End Function

        Private Sub AddLibPath(res As List(Of String), p As String, prjDir As String)
            If String.IsNullOrWhiteSpace(p) Then Return
            Try
                If Not System.IO.Path.IsPathRooted(p) Then p = System.IO.Path.Combine(prjDir, p)
                p = System.IO.Path.GetFullPath(p)
                If Not p.EndsWith("\") Then p &= "\"
                res.Add(p)
            Catch
            End Try
        End Sub

        Private Function IsLibraryDoc(d As Inventor.Document, def As Object) As Boolean
            Try
                If CBool(CallByName(def, "IsContentMember", CallType.Get)) Then Return True
            Catch
            End Try
            Dim f As String = d.FullFileName
            For Each lp As String In _libPaths
                If f.StartsWith(lp, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
            Return False
        End Function

        Private Function IsiMember(def As Object) As Boolean
            For Each pn As String In New String() {"IsiPartMember", "IsiPartFactory", "IsiAssemblyMember", "IsiAssemblyFactory"}
                Try
                    If CBool(CallByName(def, pn, CallType.Get)) Then Return True
                Catch
                End Try
            Next
            Return False
        End Function
    End Class

End Namespace
