# Minimal Add-in Server

`MinimalAddinServer.cs` demonstrates only the Inventor 2027 `ApplicationAddInServer` contract and retaining/clearing the application reference. It does not contain a manifest, registration script, command UI, or deployment package; use a unique GUID and the 2027 manifest guidance when adapting it.

The shared [`Inventor2027.Examples.csproj`](../Inventor2027.Examples.csproj) compiles this source with all other samples against the installed 2027 interop assembly:

```powershell
dotnet build .\examples\Inventor2027.Examples.csproj
```

Status: signature **Verified from Inventor 2027 Reference**; project **Compile Verified** only after the recorded build; **Inventor Runtime Tested: no**.

Sources: Autodesk Inventor 2027 [ApplicationAddInServer](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInServer.htm) and [ApplicationAddInSite.Application](https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/ApplicationAddInSite_Application.htm).
