using QCValidator.Domain.Models;

namespace QCValidator.Application.Interfaces;

public interface IReportGenerator
{
    void Generate(QCReport report);
}
