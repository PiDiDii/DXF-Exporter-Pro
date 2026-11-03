# Inventor 2027 API Source Index

Use the strongest available, version-specific evidence for each claim. A link being listed here does not mean that every API member on the site has been verified.

## Source precedence

| Priority | Source | Use | Authority and limitations |
|---|---|---|---|
| 1 | Installed Inventor 2027 API Help (`admapi_31_0.chm`) | Exact API topic descriptions, signatures, parameter/return information, and release notes | Autodesk-shipped, version-specific reference. Search for the exact object/member. |
| 2 | Installed Inventor 2027 API interop assembly and XML documentation | Confirm the C#-visible COM interface/type/member metadata and signatures | Autodesk-shipped Inventor 2027 artifacts. Pair the XML with its assembly; metadata alone does not prove runtime behavior. |
| 3 | Inventor 2027 SDK DeveloperTools samples and Object Model Chart | Verify demonstrated usage, object navigation, event patterns, and tooling | Autodesk SDK material. A sample is evidence of demonstrated usage, not proof of every edge case. |
| 4 | Autodesk official online Programming Interface help | Corroboration and a shareable web entry point to Inventor 2027 programming documentation | Autodesk-hosted. The web application is dynamic; a landing page is not itself proof of a member signature. |
| 5 | Autodesk Platform Services Inventor API overview | SDK locations, API scope, and broad development resources | Official Autodesk overview, not a member-level reference. |
| 6 | Autodesk Inventor DevBlog | Release announcements and implementation guidance | Autodesk DevBlog. Verify API signatures against the installed 2027 reference/SDK. |
| 7 | `inventor.autodeskapi.dev` | Search/navigation when available | Independent, third-party reference. Cross-check important claims against Autodesk sources. |
| 8 | GitHub/community material | Discover examples and possible approaches | Check repository ownership, release target, license, and 2027 compatibility before relying on or adapting code. |

## Sources and links

### Installed Inventor 2027 sources

- **Inventor 2027 API Help**, Autodesk-shipped CHM:  
  `C:\Users\Public\Documents\Autodesk\Inventor 2027\Local Help\admapi_31_0.chm`  
  The file was present on the inspected development machine. Search the help for the exact API topic; do not assume another machine has the same installation.
- **Inventor 2027 API interop assembly**:  
  `C:\Program Files\Autodesk\Inventor 2027\Bin\Autodesk.Inventor.Interop.dll`
- **Inventor 2027 API XML documentation**:  
  `C:\Program Files\Autodesk\Inventor 2027\Bin\Autodesk.Inventor.Interop.xml`
- **Inventor 2027 SDK installers**:  
  `C:\Users\Public\Documents\Autodesk\Inventor 2027\SDK\developertools.msi` and `usertools.msi`  
  The SDK readme says DeveloperTools installs samples, tools (including EventWatcher), Object Model Chart material, and Visual Studio wizards. These MSI files were present, but DeveloperTools content was not installed or inspected in this verification pass.

### Autodesk-hosted references

1. **Inventor API — Autodesk Platform Services**  
   https://aps.autodesk.com/developer/overview/inventor-api  
   Official overview; documents the SDK location, samples/tools, Apprentice, and Object Model Chart.
2. **Inventor 2027 Programming Interface — Autodesk Help**  
   https://help.autodesk.com/view/INVNTOR/2027/ENU/?guid=GUID-6FD7AA08-1E43-43FC-971B-5F20E56C8846  
   Official Inventor 2027 programming-interface help entry point. The page uses a dynamic help application; use the local CHM for exact topic/member lookup when needed.
3. **Inventor 2027 API update — Autodesk DevBlog**  
   https://blog.autodesk.io/inventor-2027-api-update/  
   Release guidance covering .NET, assembly loading, add-in manifests, and SDK/help.
4. **What's New in the Autodesk Inventor 2027 API**  
   https://blog.autodesk.io/whats-new-in-the-autodesk-inventor-2027-api-feature-highlights-and-enhancements/  
   Release highlights. Treat as a discovery index; confirm each named member in the installed reference before use.
5. **Creating your first Inventor Add-in**  
   https://blog.autodesk.io/lesson-1-creating-your-first-inventor-add-in/  
   Autodesk DevBlog lifecycle walkthrough and example. Check its runtime, project, manifest, and installation details against the installed 2027 SDK before copying; examples on the page may need validation.

### Independent searchable reference

- **Inventor 2027 API Reference**: https://inventor.autodeskapi.dev/docs/2027  
  This is not Autodesk-hosted. A fetch attempt during this pass returned HTTP 402, so it was unavailable for verification; do not treat it as inspected or use it as the sole evidence for an API claim.

## Framework and add-in architecture notes

- The inspected installation has Inventor 2027 binaries and `Autodesk.Inventor.Interop.dll` version `31.10.26700.0` (ProductVersion: Autodesk Inventor 2027). It also has .NET SDK `10.0.402` and `10.0.403`; this only establishes that those SDKs are installed, not that a repository project targets them.
- Autodesk DevBlog's 2027 API update describes .NET 10 and the `UseInventorAssemblyContext` manifest setting. Another Autodesk DevBlog article states that existing .NET 8 add-ins are binary compatible. Distinguish the host's supported compatibility from the recommended target for a new project; verify migration/hosting requirements in the installed help before changing a project.
- Do not infer framework, COM registration, manifest location, or loading-context behavior from the framework installed on the developer machine or from an unverified blog sample.

## Citation format for API claims

For a nontrivial API claim, give the source title and direct URL when available, the Inventor 2027 topic/member, source class (Autodesk Help / installed Autodesk SDK / Autodesk DevBlog / third party), and the verification status from [API_VERIFICATION.md](./API_VERIFICATION.md). For local artifacts, identify the installed file and topic/member because the local path is not a portable web URL.
