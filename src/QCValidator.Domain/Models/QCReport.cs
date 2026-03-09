using System;
using System.Collections.Generic;

namespace QCValidator.Domain.Models;

public class QCReport
{
    public string FileName { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public List<QCError> Errors { get; set; } = new List<QCError>();

    public QCReport() { }

    public QCReport(string fileName, DateTime runDate)
    {
        FileName = fileName;
        RunDate = runDate;
    }

    public QCReport(string fileName, DateTime runDate, List<QCError> errors)
    {
        FileName = fileName;
        RunDate = runDate;
        Errors = errors;
    }
}
