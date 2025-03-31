using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ECFramework;

public static class ExcelExtensions
{
    /// <summary>
    /// Generates an excel file in byte[].
    /// This can either use a 
    /// </summary>
    public static byte[] ToExcel<T>(this List<T> data, string sheetTitle, List<(string Field, string Alias)>? schema)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetTitle);

        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<ExcelOmit>() == null)
            .ToList();

        var nestedProperties = properties
            .Where(p => p.GetCustomAttribute<ExcelNested>() != null)
            .ToList();

        if (schema == null || !schema.Any())
        {
            schema = properties
                .Where(p => !nestedProperties.Contains(p))
                .Select(p => (p.Name, p.GetCustomAttribute<ExcelAlias>()?.Alias ?? p.Name))
                .ToList();

            // Process nested properties
            foreach (var nestedProp in nestedProperties)
            {
                var nestedAttr = nestedProp.GetCustomAttribute<ExcelNested>();
                var prefix = nestedProp.Name;
                var nestedType = nestedProp.PropertyType;

                var nestedSchemaEntries = nestedAttr!.Mappings
                    .Select(mapping =>
                    {
                        var parts = mapping.Split('=', 2);
                        var propertyName = parts[0].Trim();
                        var alias = parts.Length > 1 ? parts[1].Trim() : propertyName;
                        return ($"{nestedProp.Name}.{propertyName}", $"{prefix} {alias}");
                    });

                schema.AddRange(nestedSchemaEntries);
            }
        }

        var flatProperties = properties.Where(p => !nestedProperties.Contains(p)).ToArray();

        // Add headers
        for (int i = 0; i < schema.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = schema[i].Alias.ToUpper();
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Add data rows
        for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
        {
            var rowData = data[rowIndex];

            for (int colIndex = 0; colIndex < schema.Count; colIndex++)
            {
                var fieldPath = schema[colIndex].Field.Split('.');
                object? value = rowData;

                foreach (var field in fieldPath)
                {
                    if (value == null) break;
                    var prop = value.GetType().GetProperty(field, BindingFlags.Instance | BindingFlags.Public);
                    value = prop?.GetValue(value);
                }

                var cell = worksheet.Cells[rowIndex + 2, colIndex + 1];
                cell.Value = value;

                if (value is DateTime)
                    cell.Style.Numberformat.Format = "dd-mm-yyyy";
            }
        }

        var stream = new MemoryStream();
        package.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Generates an excel file in byte[].
    /// This is the dynamic version that uses type names as colums for the table.
    /// </summary>
    public static byte[] ToExcel(this List<dynamic> data, string sheetTitle)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetTitle);
        // Determine the properties to include based on the schema or use all properties
        var properties = ((IDictionary<string, object>)data[0]).Keys.ToArray();

        // Add headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = properties[i].ToUpper().Replace("_", " ");
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }
        // Add data rows
        for (int rowIndex = 0; rowIndex < data.Count; rowIndex++)
        {
            var rowData = data[rowIndex];
            for (int colIndex = 0; colIndex < properties.Length; colIndex++)
            {
                var property = properties[colIndex];
                var value = ((IDictionary<string, object>)rowData)[property];
                var cell = worksheet.Cells[rowIndex + 2, colIndex + 1];
                // Set the cell value
                cell.Value = value;

                // Format as short date if the property is a DateTime or Nullable<DateTime>
                if (value is DateTime || value is DateTime?)
                    cell.Style.Numberformat.Format = "dd-mm-yyyy"; // Adjust format as needed
            }
        }
        // Save the file into a MemoryStream
        var stream = new MemoryStream();
        package.SaveAs(stream);
        return stream.ToArray();
    }
}
