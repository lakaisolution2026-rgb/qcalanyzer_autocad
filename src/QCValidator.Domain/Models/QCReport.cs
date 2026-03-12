using System;
using System.Collections.Generic;

namespace QCValidator.Domain.Models
{
    public class QCReport
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime RunDate { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<QCError> Errors { get; set; } = new List<QCError>();
        public List<Layer> Layers { get; set; } = new List<Layer>();

        public QCReport() { }

        public QCReport(string fileName, DateTime runDate)
        {
            FileName = fileName;
            RunDate = runDate;
        }

        public QCReport(string fileName, DateTime runDate, string summary, List<QCError> errors, List<Layer> layers = null)
        {
            FileName = fileName;
            RunDate = runDate;
            Summary = summary;
            Errors = errors;
            Layers = layers ?? new List<Layer>();
        }
    }
}
