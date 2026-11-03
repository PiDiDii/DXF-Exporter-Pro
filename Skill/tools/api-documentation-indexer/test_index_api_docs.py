import tempfile
import unittest
from pathlib import Path

from index_api_docs import load_symbols, parse_member_id, write_index


class ParseMemberIdTests(unittest.TestCase):
    def test_parses_method_parameter_types_without_splitting_generic_arguments(self):
        symbol = parse_member_id(
            "M:Inventor.Sample.Transform("
            "System.Collections.Generic.IList{System.String},System.Int32)"
        )

        self.assertIsNotNone(symbol)
        assert symbol is not None
        self.assertEqual(symbol.owner, "Inventor.Sample")
        self.assertEqual(symbol.name, "Transform")
        self.assertEqual(
            symbol.parameter_types,
            ("System.Collections.Generic.IList{System.String}", "System.Int32"),
        )

    def test_rejects_unknown_member_identifier_kinds(self):
        self.assertIsNone(parse_member_id("X:Inventor.Sample.Unknown"))


class IndexGenerationTests(unittest.TestCase):
    def test_filters_by_owner_and_marks_every_generated_entry_unverified(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            xml_path = root / "interop.xml"
            xml_path.write_text(
                """<doc><members>
<member name="T:Inventor.Sample" />
<member name="M:Inventor.Sample.Run(System.Int32)" />
<member name="T:Inventor.Other" />
</members></doc>""",
                encoding="utf-8",
            )
            output_path = root / "symbols.md"

            symbols = load_symbols(xml_path, ("Inventor.Sample",))
            search_path = write_index(
                symbols,
                xml_path,
                output_path,
                "2027",
                "https://help.autodesk.com/example",
            )

            markdown = output_path.read_text(encoding="utf-8")
            search_index = search_path.read_text(encoding="utf-8")
            self.assertIn("Symbols indexed: 2", markdown)
            self.assertIn("Unverified", markdown)
            self.assertIn("System.Int32", markdown)
            self.assertNotIn("Inventor.Other", markdown)
            self.assertIn("Inventor.Sample.Run(System.Int32)", search_index)


if __name__ == "__main__":
    unittest.main()
