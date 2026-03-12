using QCValidator.Domain.Models;
using System.Collections.Generic;

namespace QCValidator.Application.Interfaces
{
    public interface ITemplateProvider
    {
        List<Layer> GetTemplateLayers();
        List<TextStyle> GetTemplateTextStyles();
    }
}
