using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System.IO;
using System.Text.Json;

namespace QCValidator.Infrastructure.Generators;

public class JsonReportGenerator : IReportGenerator
{
    private const string ReportFileName = "qc_report.json";

    public void Generate(QCReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string jsonContent = JsonSerializer.Serialize(report, options);
        
        // Save file in the same directory as the application
        File.WriteAllText(ReportFileName, jsonContent);
    }
}
