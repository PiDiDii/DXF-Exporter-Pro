#!/usr/bin/env python3
"""Build a local, searchable symbol index from an Inventor interop XML file."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class ApiSymbol:
    member_id: str
    kind: str
    owner: str
    name: str
    parameter_types: tuple[str, ...]


def split_parameters(value: str) -> tuple[str, ...]:
    if not value:
        return ()

    parts: list[str] = []
    start = 0
    nesting = 0
    for index, character in enumerate(value):
        if character in "{[(":
            nesting += 1
        elif character in "}])":
            nesting -= 1
        elif character == "," and nesting == 0:
            parts.append(value[start:index].strip())
            start = index + 1

    parts.append(value[start:].strip())
    return tuple(part for part in parts if part)


def parse_member_id(member_id: str) -> ApiSymbol | None:
    if len(member_id) < 3 or member_id[1] != ":":
        return None

    kind = {
        "T": "Type",
        "M": "Method",
        "P": "Property",
        "E": "Event",
        "F": "FieldOrEnumValue",
    }.get(member_id[0])
    if kind is None:
        return None

    qualified_name = member_id[2:]
    parameter_types: tuple[str, ...] = ()
    open_paren = qualified_name.find("(")
    if open_paren >= 0:
        if not qualified_name.endswith(")"):
            return None
        parameter_types = split_parameters(qualified_name[open_paren + 1 : -1])
        qualified_name = qualified_name[:open_paren]

    owner, separator, name = qualified_name.rpartition(".")
    if not separator:
        owner = ""
        name = qualified_name

    return ApiSymbol(
        member_id=member_id,
        kind=kind,
        owner=owner,
        name=name,
        parameter_types=parameter_types,
    )


def load_symbols(xml_path: Path, prefixes: Iterable[str]) -> list[ApiSymbol]:
    normalized_prefixes = tuple(prefix.rstrip(".") for prefix in prefixes)
    symbols: list[ApiSymbol] = []

    with xml_path.open("rb") as xml_file:
        for _, element in ET.iterparse(xml_file, events=("end",)):
            if element.tag != "member":
                continue
            member_id = element.get("name", "")
            symbol = parse_member_id(member_id)
            if symbol and (
                not normalized_prefixes
                or any(
                    symbol.owner == prefix
                    or symbol.owner.startswith(prefix + ".")
                    or member_id[2:].startswith(prefix)
                    for prefix in normalized_prefixes
                )
            ):
                symbols.append(symbol)
            element.clear()

    symbols.sort(key=lambda item: (item.kind, item.owner.casefold(), item.name.casefold(), item.member_id))
    return symbols


def escape_cell(value: str) -> str:
    return value.replace("|", r"\|").replace("\r", " ").replace("\n", " ")


def search_terms(symbol: ApiSymbol) -> str:
    text = " ".join((symbol.owner, symbol.name, *symbol.parameter_types))
    words = re.findall(r"[A-Za-z_][A-Za-z0-9_]*", text)
    return " ".join(dict.fromkeys(words))


def write_index(
    symbols: list[ApiSymbol],
    xml_path: Path,
    output_path: Path,
    inventor_version: str,
    source_url: str,
) -> Path:
    source_hash = hashlib.sha256(xml_path.read_bytes()).hexdigest()
    lines = [
        "# Generated Inventor API Symbol Index",
        "",
        "> Generated locally from interop XML structure. No Autodesk prose, samples, or full help pages are copied.",
        "> Every entry is **Unverified** until its exact API topic and source version are checked by a developer.",
        "",
        f"- Caller-declared Inventor version: `{escape_cell(inventor_version)}` (not independently validated by this script)",
        f"- Input XML: `{escape_cell(str(xml_path.resolve()))}`",
        f"- Input SHA-256: `{source_hash}`",
        f"- Reference entry point: {source_url}",
        f"- Symbols indexed: {len(symbols)}",
        "- Return types and parameter names are not inferred from XML member IDs.",
        "",
    ]

    for kind in ("Type", "Method", "Property", "Event", "FieldOrEnumValue"):
        group = [symbol for symbol in symbols if symbol.kind == kind]
        if not group:
            continue
        lines.extend(
            [
                f"## {kind}",
                "",
                "| Owner | Member | Parameter types from XML ID | Return type | Reference entry point | Status |",
                "|---|---|---|---|---|---|",
            ]
        )
        for symbol in group:
            parameters = ", ".join(symbol.parameter_types) or "Not encoded"
            return_type = "Not encoded; inspect 2027 reference" if kind == "Method" else "Not encoded here"
            lines.append(
                "| `{}` | `{}` | `{}` | {} | [{}]({}) | Unverified |".format(
                    escape_cell(symbol.owner or "(global)"),
                    escape_cell(symbol.name),
                    escape_cell(parameters),
                    return_type,
                    "Reference entry point",
                    source_url,
                )
            )
        lines.append("")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    search_path = output_path.with_name(output_path.stem + ".search.tsv")
    with search_path.open("w", encoding="utf-8", newline="\n") as search_file:
        search_file.write("terms\tmember_id\towner\tkind\tparameter_types\n")
        for symbol in symbols:
            search_file.write(
                "\t".join(
                    (
                        search_terms(symbol),
                        symbol.member_id,
                        symbol.owner,
                        symbol.kind,
                        ", ".join(symbol.parameter_types),
                    )
                )
                + "\n"
            )

    return search_path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Create an offline Markdown and TSV symbol index from Autodesk interop XML."
    )
    parser.add_argument("--xml", required=True, type=Path, help="Local Autodesk.Inventor.Interop.xml file")
    parser.add_argument("--inventor-version", required=True, help="Caller-declared release, e.g. 2027")
    parser.add_argument(
        "--source-url",
        required=True,
        help="Versioned official reference entry-point URL recorded with the local XML",
    )
    parser.add_argument("--output", required=True, type=Path, help="Markdown output file")
    parser.add_argument(
        "--prefix",
        action="append",
        default=[],
        help="Optional owner/member prefix filter; may be supplied more than once",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    if not args.xml.is_file():
        parser.error(f"XML input does not exist: {args.xml}")
    if not args.source_url.startswith(("https://", "http://")):
        parser.error("--source-url must be an explicit HTTP(S) URL")

    try:
        symbols = load_symbols(args.xml, args.prefix)
    except ET.ParseError as error:
        print(f"Invalid XML in {args.xml}: {error}", file=sys.stderr)
        return 2

    if not symbols:
        print("No matching API member IDs found.", file=sys.stderr)
        return 1

    search_path = write_index(
        symbols,
        args.xml,
        args.output,
        args.inventor_version,
        args.source_url,
    )
    print(f"Indexed {len(symbols)} symbols to {args.output}")
    print(f"Search terms written to {search_path}")
    print("All generated entries remain Unverified; confirm the release and exact API topics.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
