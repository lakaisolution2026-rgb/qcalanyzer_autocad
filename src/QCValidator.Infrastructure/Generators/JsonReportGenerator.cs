using System;
using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System.IO;
using System.Text.Json;

namespace QCValidator.Infrastructure.Generators
{
    public class JsonReportGenerator : IReportGenerator
    {
        public string Generate(QCReport report)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(report, options);
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"qc_report_{timestamp}.json";
            
            File.WriteAllText(fileName, jsonContent);
            return fileName;
        }
    }
}
