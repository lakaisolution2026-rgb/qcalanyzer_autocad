using System;
using QCValidator.Application.Interfaces;
using QCValidator.Application.Services;
using QCValidator.Infrastructure.Generators;
using QCValidator.Infrastructure.Providers;

Console.WriteLine("=== QC Validator Starting ===");

// 1. Setup Infrastructure (Wiring Dependencies)
IDrawingDataProvider drawingProvider = new MockDrawingProvider();
ITemplateProvider templateProvider = new ExcelTemplateProvider();
IReportGenerator reportGenerator = new JsonReportGenerator();

// 2. Setup Application Service
var validationService = new QCValidationService(
    drawingProvider, 
    templateProvider, 
    reportGenerator);

// 3. Run Validation
string targetFile = "SampleDrawing.dwg";
Console.WriteLine($"Running QC for: {targetFile}...");

try
{
    validationService.Run(targetFile);
    Console.WriteLine("QC Processing Completed.");
    Console.WriteLine("Report generated: qc_report.json");
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

Console.WriteLine("=== QC Validator Terminated ===");
