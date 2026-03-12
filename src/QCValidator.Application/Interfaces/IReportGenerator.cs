using QCValidator.Domain.Models;

namespace QCValidator.Application.Interfaces
{
    public interface IReportGenerator
    {
        string Generate(QCReport report);
    }
}
