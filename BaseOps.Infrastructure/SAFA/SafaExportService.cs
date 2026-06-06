using BaseOps.Application.SAFA;
using BaseOps.Application.SAFA.DTOs;
using BaseOps.Domain.Entities;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace BaseOps.Infrastructure.SAFA;

public class SafaExportService : ISafaExportService
{
    public async Task<byte[]> ExportInspectionsToPdfAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("SAFA Inspection Report").Bold().FontSize(16);
                            column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Content().Element(content =>
                {
                    content.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("ID").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Aircraft").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Type").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Inspector").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Status").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Defects").Bold();
                        });

                        foreach (var inspection in inspections)
                        {
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(inspection.Id.ToString().Substring(0, 8));
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(inspection.AircraftRegistration);
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(inspection.InspectionType.ToString());
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(inspection.InspectorName);
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(inspection.Status.ToString());
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text($"{inspection.ActiveDefects}/{inspection.TotalDefects}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return await Task.FromResult(document.GeneratePdf());
    }

    public async Task<byte[]> ExportInspectionsToExcelAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SAFA Inspections");

        worksheet.Cell("A1").Value = "ID";
        worksheet.Cell("B1").Value = "Aircraft Registration";
        worksheet.Cell("C1").Value = "Fleet Type";
        worksheet.Cell("D1").Value = "Inspection Type";
        worksheet.Cell("E1").Value = "Inspector";
        worksheet.Cell("F1").Value = "Status";
        worksheet.Cell("G1").Value = "Total Defects";
        worksheet.Cell("H1").Value = "Active Defects";
        worksheet.Cell("I1").Value = "Inspection Date";
        worksheet.Cell("J1").Value = "Created At";

        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Font.Bold = true;

        int row = 2;
        foreach (var inspection in inspections)
        {
            worksheet.Cell(row, 1).Value = inspection.Id.ToString();
            worksheet.Cell(row, 2).Value = inspection.AircraftRegistration;
            worksheet.Cell(row, 3).Value = inspection.FleetType;
            worksheet.Cell(row, 4).Value = inspection.InspectionType.ToString();
            worksheet.Cell(row, 5).Value = inspection.InspectorName;
            worksheet.Cell(row, 6).Value = inspection.Status.ToString();
            worksheet.Cell(row, 7).Value = inspection.TotalDefects;
            worksheet.Cell(row, 8).Value = inspection.ActiveDefects;
            worksheet.Cell(row, 9).Value = inspection.InspectionDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 10).Value = inspection.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    public async Task<byte[]> ExportInspectionsToCsvAsync(List<SafaInspectionListDto> inspections, CancellationToken cancellationToken = default)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.WriteRecords(inspections);
        return await Task.FromResult(Encoding.UTF8.GetBytes(writer.ToString()));
    }

    public async Task<byte[]> ExportDefectsToPdfAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("SAFA Defect Report").Bold().FontSize(16);
                            column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Content().Element(content =>
                {
                    content.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("ID").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Category").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Finding").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Status").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Action").Bold();
                            header.Cell().Element(c => c.Background("#E0E0E0").Padding(5)).Text("Action Date").Bold();
                        });

                        foreach (var defect in defects)
                        {
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.Id.ToString().Substring(0, 8));
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.Category);
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.ObservationFinding.Length > 50 ? defect.ObservationFinding.Substring(0, 50) + "..." : defect.ObservationFinding);
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.Status.ToString());
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.CorrectiveAction?.Length > 50 ? defect.CorrectiveAction.Substring(0, 50) + "..." : defect.CorrectiveAction ?? "-");
                            table.Cell().Element(c => c.BorderBottom(1).Padding(5)).Text(defect.ActionTakenAt?.ToString("yyyy-MM-dd") ?? "-");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return await Task.FromResult(document.GeneratePdf());
    }

    public async Task<byte[]> ExportDefectsToExcelAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SAFA Defects");

        worksheet.Cell("A1").Value = "ID";
        worksheet.Cell("B1").Value = "Inspection ID";
        worksheet.Cell("C1").Value = "Category";
        worksheet.Cell("D1").Value = "SubCategory";
        worksheet.Cell("E1").Value = "Standard Description";
        worksheet.Cell("F1").Value = "Observation Finding";
        worksheet.Cell("G1").Value = "Need To Fix";
        worksheet.Cell("H1").Value = "Status";
        worksheet.Cell("I1").Value = "Corrective Action";
        worksheet.Cell("J1").Value = "Task Card Code";
        worksheet.Cell("K1").Value = "Part Request ID";
        worksheet.Cell("L1").Value = "Action Taken By";
        worksheet.Cell("M1").Value = "Action Taken At";
        worksheet.Cell("N1").Value = "Created At";

        var headerRange = worksheet.Range(1, 1, 1, 14);
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Font.Bold = true;

        int row = 2;
        foreach (var defect in defects)
        {
            worksheet.Cell(row, 1).Value = defect.Id.ToString();
            worksheet.Cell(row, 2).Value = defect.InspectionId.ToString();
            worksheet.Cell(row, 3).Value = defect.Category;
            worksheet.Cell(row, 4).Value = defect.SubCategory ?? "";
            worksheet.Cell(row, 5).Value = defect.StandardDescription;
            worksheet.Cell(row, 6).Value = defect.ObservationFinding;
            worksheet.Cell(row, 7).Value = defect.NeedToFix;
            worksheet.Cell(row, 8).Value = defect.Status.ToString();
            worksheet.Cell(row, 9).Value = defect.CorrectiveAction ?? "";
            worksheet.Cell(row, 10).Value = defect.TaskCardCode ?? "";
            worksheet.Cell(row, 11).Value = defect.PartRequestId ?? "";
            worksheet.Cell(row, 12).Value = defect.ActionTakenByName ?? "";
            worksheet.Cell(row, 13).Value = defect.ActionTakenAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            worksheet.Cell(row, 14).Value = defect.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    public async Task<byte[]> ExportDefectsToCsvAsync(List<SafaDefectDto> defects, CancellationToken cancellationToken = default)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.WriteRecords(defects);
        return await Task.FromResult(Encoding.UTF8.GetBytes(writer.ToString()));
    }
}
