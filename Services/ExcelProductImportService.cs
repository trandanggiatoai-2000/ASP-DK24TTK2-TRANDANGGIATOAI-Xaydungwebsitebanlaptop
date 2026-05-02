using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using websitebanlaptop.Models;
using Microsoft.AspNetCore.Http;

namespace websitebanlaptop.Services;

public static class ExcelProductImportService
{
    public static async Task<List<InventoryImportRow>> ReadRowsAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => await ReadCsvAsync(file),
            ".xlsx" => await ReadXlsxAsync(file),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx hoặc .csv")
        };
    }

    private static async Task<List<InventoryImportRow>> ReadCsvAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var rows = new List<InventoryImportRow>();
        bool isHeader = true;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader) { isHeader = false; continue; }
            var cells = line.Split(',');
            rows.Add(Map(cells));
        }
        return rows;
    }

    private static async Task<List<InventoryImportRow>> ReadXlsxAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read, true);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var sharedStrings = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry != null)
        {
            using var s = sharedEntry.Open();
            var doc = XDocument.Load(s);
            sharedStrings = doc.Descendants(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                .ToList();
        }

        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet"));
        if (sheetEntry == null) return new List<InventoryImportRow>();

        using var ws = sheetEntry.Open();
        var sheet = XDocument.Load(ws);
        var result = new List<InventoryImportRow>();
        bool isHeader = true;

        foreach (var row in sheet.Descendants(ns + "row"))
        {
            var values = new string[17];
            foreach (var cell in row.Elements(ns + "c"))
            {
                var r = cell.Attribute("r")?.Value ?? string.Empty;
                var col = GetColumnIndex(r);
                if (col < 0 || col >= values.Length) continue;
                values[col] = GetCellValue(cell, ns, sharedStrings);
            }

            if (isHeader) { isHeader = false; continue; }
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            result.Add(Map(values));
        }

        return result;
    }

    private static string GetCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? string.Empty;

        var value = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(value, out var idx) && idx >= 0 && idx < sharedStrings.Count)
            return sharedStrings[idx];
        return value;
    }

    private static int GetColumnIndex(string cellReference)
    {
        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        if (string.IsNullOrWhiteSpace(letters)) return -1;
        int sum = 0;
        foreach (var c in letters)
            sum = sum * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
        return sum - 1;
    }

    private static InventoryImportRow Map(string[] cells)
    {
        string Get(int i) => i < cells.Length ? cells[i]?.Trim() ?? string.Empty : string.Empty;

        return new InventoryImportRow
        {
            Brand = Get(0),
            CategoryName = Get(1),
            ProductName = Get(2),
            Cpu = Get(3),
            Ram = Get(4),
            Ssd = Get(5),
            Price = ParseDecimal(Get(6)),
            OldPrice = ParseDecimal(Get(7)),
            StockQty = ParseInt(Get(8)),
            IsFeatured = ParseBool(Get(9)),
            BadgeText = string.IsNullOrWhiteSpace(Get(10)) ? "Giảm sốc" : Get(10),
            DescriptionText = Get(11),
            DiscountPercent = ParseInt(Get(12)),
            SortOrder = ParseInt(Get(13)),
            Image1 = Get(14),
            Image2 = Get(15),
            Image3 = Get(16)
        };
    }

    private static decimal ParseDecimal(string value)
    {
        value = value.Replace(".", "").Replace(",", "");
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
        return result;
    }

    private static int ParseInt(string value)
    {
        int.TryParse(value, out var result);
        return result;
    }

    private static bool ParseBool(string value)
    {
        return value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}

