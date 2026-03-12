using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System.Collections.Generic;

namespace QCValidator.Infrastructure.Providers
{
    public class ExcelTemplateProvider : ITemplateProvider
    {
        public List<Layer> GetTemplateLayers()
        {
            return new List<Layer>
            {
                new Layer("RRU_L", 7),
             
                new Layer("CABLE_L", 4) // This will trigger a missing layer error in our mock setup
            };
        }

        public List<TextStyle> GetTemplateTextStyles()
        {
            return new List<TextStyle>
            {
                new TextStyle("Arial", "", 0.0),
                new TextStyle("Arial Narrow", "", 0.0),
                new TextStyle("Standard", "", 0.0)
            };
        }
    }
}
