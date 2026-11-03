# Offline API Documentation Indexer

Builds a compact, searchable symbol inventory from a locally installed `Autodesk.Inventor.Interop.xml`. It uses Python 3.10+ and the standard library only, and makes no network requests. It does not crawl Autodesk sites, download documentation, or copy Autodesk summary prose/samples into generated files.

## Usage

```powershell
python .\tools\api-documentation-indexer\index_api_docs.py `
  --xml "C:\Program Files\Autodesk\Inventor 2027\Bin\Autodesk.Inventor.Interop.xml" `
  --inventor-version 2027 `
  --source-url "https://help.autodesk.com/cloudhelp/2027/ENU/Inventor-API/files/WhatsNew.htm" `
  --output .\api\generated\INVENTOR_2027_SYMBOLS.md
```

Optional `--prefix Inventor.SheetMetalComponentDefinition` filters the output. Repeat `--prefix` for multiple owners. Each run writes Markdown and a sibling `.search.tsv` text index. The curated [`api/API_INDEX.md`](../../api/API_INDEX.md) is not overwritten.

## Data and verification limitations

- The caller supplies the version label and source URL; the script cannot validate the Autodesk release/version of the input file.
- It extracts XML member IDs and the parameter type strings encoded in those IDs. XML member IDs do not reliably include parameter names or method return types; those fields are explicitly left unknown.
- Every generated record is **Unverified**. Confirm the local interop assembly version and the exact Inventor 2027 Help topic before promoting an entry into the curated index.
- The tool indexes structural metadata only. It does not scrape online documentation, bundle SDK Help, or redistribute Autodesk samples. Review the applicable Autodesk SDK license before sharing any locally generated material.
- To update for another Inventor release, point to that release's installed XML and provide its matching official versioned reference URL; do not relabel an older XML file as a newer release.

## Validation

The indexer can be syntax-checked with `python -m py_compile`. Run it against a small local XML fixture first, then the intended Inventor interop XML.
