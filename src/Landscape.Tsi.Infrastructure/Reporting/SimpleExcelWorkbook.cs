using System.IO.Compression;
using System.Security;
using System.Text;

namespace Landscape.Tsi.Infrastructure.Reporting;

public sealed class SimpleExcelWorkbook
{
    private readonly List<SimpleExcelSheet> _sheets = [];

    public SimpleExcelSheet CreateSheet(string name)
    {
        // Limpieza de caracteres inválidos en nombre de pestaña Excel
        var safeName = string.Join("", name.Where(c => c != ':' && c != '\\' && c != '/' && c != '?' && c != '*' && c != '[' && c != ']'));
        if (safeName.Length > 31) safeName = safeName[..31];
        if (string.IsNullOrWhiteSpace(safeName)) safeName = $"Sheet{_sheets.Count + 1}";

        var sheet = new SimpleExcelSheet(safeName, _sheets.Count + 1);
        _sheets.Add(sheet);
        return sheet;
    }

    public byte[] Build()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // 1. [Content_Types].xml
            var contentTypesEntry = zip.CreateEntry("[Content_Types].xml");
            using (var writer = new StreamWriter(contentTypesEntry.Open(), Encoding.UTF8))
            {
                writer.Write(BuildContentTypesXml());
            }

            // 2. _rels/.rels
            var relsEntry = zip.CreateEntry("_rels/.rels");
            using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
            {
                writer.Write(BuildGlobalRelsXml());
            }

            // 3. xl/_rels/workbook.xml.rels
            var wbRelsEntry = zip.CreateEntry("xl/_rels/workbook.xml.rels");
            using (var writer = new StreamWriter(wbRelsEntry.Open(), Encoding.UTF8))
            {
                writer.Write(BuildWorkbookRelsXml());
            }

            // 4. xl/workbook.xml
            var wbEntry = zip.CreateEntry("xl/workbook.xml");
            using (var writer = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
            {
                writer.Write(BuildWorkbookXml());
            }

            // 5. xl/styles.xml
            var stylesEntry = zip.CreateEntry("xl/styles.xml");
            using (var writer = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
            {
                writer.Write(BuildStylesXml());
            }

            // 6. xl/worksheets/sheet{i}.xml
            for (int i = 0; i < _sheets.Count; i++)
            {
                var sheetEntry = zip.CreateEntry($"xl/worksheets/sheet{i + 1}.xml");
                using var writer = new StreamWriter(sheetEntry.Open(), Encoding.UTF8);
                writer.Write(_sheets[i].BuildXml());
            }
        }

        return ms.ToArray();
    }

    private string BuildContentTypesXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\n");
        sb.Append("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>\n");
        sb.Append("  <Default Extension=\"xml\" ContentType=\"application/xml\"/>\n");
        sb.Append("  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>\n");
        sb.Append("  <Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>\n");
        for (int i = 0; i < _sheets.Count; i++)
        {
            sb.Append($"  <Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\n");
        }
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string BuildGlobalRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n" +
        "  <Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>\n" +
        "</Relationships>";

    private string BuildWorkbookRelsXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">\n");
        sb.Append("  <Relationship Id=\"rIdStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\n");
        for (int i = 0; i < _sheets.Count; i++)
        {
            sb.Append($"  <Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>\n");
        }
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private string BuildWorkbookXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">\n");
        sb.Append("  <sheets>\n");
        for (int i = 0; i < _sheets.Count; i++)
        {
            var sheet = _sheets[i];
            sb.Append($"    <sheet name=\"{SecurityElement.Escape(sheet.Name)}\" sheetId=\"{sheet.SheetId}\" r:id=\"rId{i + 1}\"/>\n");
        }
        sb.Append("  </sheets>\n");
        sb.Append("</workbook>");
        return sb.ToString();
    }

    private static string BuildStylesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n" +
        "  <fonts count=\"2\">\n" +
        "    <font><sz val=\"10\"/><name val=\"Segoe UI\"/></font>\n" +
        "    <font><b/><sz val=\"10\"/><color rgb=\"FFFFFFFF\"/><name val=\"Segoe UI\"/></font>\n" +
        "  </fonts>\n" +
        "  <fills count=\"3\">\n" +
        "    <fill><patternFill patternType=\"none\"/></fill>\n" +
        "    <fill><patternFill patternType=\"gray125\"/></fill>\n" +
        "    <fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF003366\"/></patternFill></fill>\n" +
        "  </fills>\n" +
        "  <borders count=\"2\">\n" +
        "    <border><left/><right/><top/><bottom/></border>\n" +
        "    <border><left style=\"thin\"><color rgb=\"FFD0D5DD\"/></left><right style=\"thin\"><color rgb=\"FFD0D5DD\"/></right><top style=\"thin\"><color rgb=\"FFD0D5DD\"/></top><bottom style=\"thin\"><color rgb=\"FFD0D5DD\"/></bottom></border>\n" +
        "  </borders>\n" +
        "  <cellStyleXfs count=\"1\">\n" +
        "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>\n" +
        "  </cellStyleXfs>\n" +
        "  <cellXfs count=\"3\">\n" +
        "    <xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>\n" +
        "    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>\n" +
        "    <xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>\n" +
        "  </cellXfs>\n" +
        "</styleSheet>";
}

public sealed class SimpleExcelSheet(string name, int sheetId)
{
    public string Name { get; } = name;
    public int SheetId { get; } = sheetId;

    private readonly List<IReadOnlyList<string?>> _headers = [];
    private readonly List<IReadOnlyList<object?>> _rows = [];

    public void AddHeader(params string[] headers) => _headers.Add(headers);
    public void AddHeader(IEnumerable<string?> headers) => _headers.Add(headers.ToArray());
    public void AddRow(params object?[] cells) => _rows.Add(cells);
    public void AddRow(IEnumerable<object?> cells) => _rows.Add(cells.ToArray());

    public string BuildXml()
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">\n");
        sb.Append("  <sheetData>\n");

        int rowIndex = 1;

        // Render headers
        foreach (var header in _headers)
        {
            sb.Append($"    <row r=\"{rowIndex}\">\n");
            for (int col = 0; col < header.Count; col++)
            {
                var cellRef = GetCellReference(col + 1, rowIndex);
                var val = SecurityElement.Escape(header[col] ?? string.Empty);
                sb.Append($"      <c r=\"{cellRef}\" s=\"1\" t=\"inlineStr\"><is><t>{val}</t></is></c>\n");
            }
            sb.Append("    </row>\n");
            rowIndex++;
        }

        // Render data rows
        foreach (var row in _rows)
        {
            sb.Append($"    <row r=\"{rowIndex}\">\n");
            for (int col = 0; col < row.Count; col++)
            {
                var cellRef = GetCellReference(col + 1, rowIndex);
                var val = row[col];

                if (val is null)
                {
                    sb.Append($"      <c r=\"{cellRef}\" s=\"0\"/>\n");
                }
                else if (val is int or long or short or byte)
                {
                    sb.Append($"      <c r=\"{cellRef}\" s=\"0\" t=\"n\"><v>{val}</v></c>\n");
                }
                else if (val is decimal or double or float)
                {
                    var formatted = Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture);
                    sb.Append($"      <c r=\"{cellRef}\" s=\"2\" t=\"n\"><v>{formatted}</v></c>\n");
                }
                else if (val is bool b)
                {
                    sb.Append($"      <c r=\"{cellRef}\" s=\"0\" t=\"b\"><v>{(b ? 1 : 0)}</v></c>\n");
                }
                else if (val is DateTime dt)
                {
                    var str = SecurityElement.Escape(dt.ToString("yyyy-MM-dd"));
                    sb.Append($"      <c r=\"{cellRef}\" s=\"0\" t=\"inlineStr\"><is><t>{str}</t></is></c>\n");
                }
                else
                {
                    var str = SecurityElement.Escape(val.ToString() ?? string.Empty);
                    sb.Append($"      <c r=\"{cellRef}\" s=\"0\" t=\"inlineStr\"><is><t>{str}</t></is></c>\n");
                }
            }
            sb.Append("    </row>\n");
            rowIndex++;
        }

        sb.Append("  </sheetData>\n");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string GetCellReference(int col, int row)
    {
        var colName = string.Empty;
        while (col > 0)
        {
            var rem = (col - 1) % 26;
            colName = (char)('A' + rem) + colName;
            col = (col - 1) / 26;
        }
        return $"{colName}{row}";
    }
}