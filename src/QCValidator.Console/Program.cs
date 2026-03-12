using System;
using System.IO;
using QCValidator.Application.Interfaces;
using QCValidator.Application.Services;
using QCValidator.Infrastructure.Generators;
using QCValidator.Infrastructure.Providers;

Console.WriteLine("=== QC Validator Starting ===");

// 1. Get target file from arguments or use a placeholder
string targetFile = args.Length > 0 ? args[0] : "SampleDrawing.dwg";

if (!File.Exists(targetFile))
{
    Console.WriteLine($"Error: AutoCAD file '{targetFile}' not found.");
    Console.WriteLine("Usage: dotnet run --project QCValidator.Console.csproj <path-to-dwg-file>");
    return;
}

Console.WriteLine($"Reading layers from: {targetFile}...");

try
{
    // 2. Setup Infrastructure (Wiring Dependencies)
    // We are now using the REAL AutoCadDrawingProvider
    IDrawingDataProvider drawingProvider = new AutoCadDrawingProvider(targetFile);
    ITemplateProvider templateProvider = new ExcelTemplateProvider();
    IReportGenerator reportGenerator = new JsonReportGenerator();

    // 3. Setup Application Service
    var validationService = new QCValidationService(
        drawingProvider, 
        templateProvider, 
        reportGenerator);

    // 4. Run Validation
    validationService.Run(targetFile);
    
    Console.WriteLine("Processing Completed.");
    Console.WriteLine("Report generated: qc_report.json");
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

Console.WriteLine("=== QC Validator Terminated ===");
