using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QCValidator.Application.Services
{
    public class QCValidationService
    {
        private readonly IDrawingDataProvider _drawingProvider;
        private readonly ITemplateProvider _templateProvider;
        private readonly IReportGenerator _reportGenerator;

        public QCValidationService(
            IDrawingDataProvider drawingProvider,
            ITemplateProvider templateProvider,
            IReportGenerator reportGenerator)
        {
            _drawingProvider = drawingProvider;
            _templateProvider = templateProvider;
            _reportGenerator = reportGenerator;
        }

        public string Run(string fileName)
        {
            var errors = new List<QCError>();

            // 1. Get data from providers
            var currentLayers = _drawingProvider.GetLayers();
            var templateLayers = _templateProvider.GetTemplateLayers();

            // 2. Compare Layers
            foreach (var templateLayer in templateLayers)
            {
                var existingLayer = currentLayers.FirstOrDefault(l => 
                    l.Name.Equals(templateLayer.Name, StringComparison.OrdinalIgnoreCase));

                if (existingLayer == null)
                {
                    errors.Add(new QCError(
                        "Layer",
                        templateLayer.Name,
                        $"Layer '{templateLayer.Name}' is missing from the drawing."
                    ));
                }
                else if (existingLayer.Color != templateLayer.Color)
                {
                    errors.Add(new QCError(
                        "Layer Color",
                        existingLayer.Name,
                        $"Layer '{existingLayer.Name}' has color {existingLayer.Color}, but template requires {templateLayer.Color}."
                    ));
                }
            }

            // 2.2 Validate Text Styles (Only allowed styles)
            var currentTextStyles = _drawingProvider.GetTextStyles();
            var allowedTextStyles = _templateProvider.GetTemplateTextStyles();
            bool textStylesClean = true;

            foreach (var currentStyle in currentTextStyles)
            {
                bool isAllowed = allowedTextStyles.Any(a => 
                    a.Name.Equals(currentStyle.Name, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    textStylesClean = false;
                    errors.Add(new QCError(
                        "Text Style Rule",
                        currentStyle.Name,
                        $"Text style '{currentStyle.Name}' (Font: {currentStyle.Font}) is not allowed. Only Arial, Arial Narrow, and Standard are permitted.",
                        "Error"
                    ));
                }
            }

            // 3. Check for entities on Layer 0
            var entities = _drawingProvider.GetEntities();
            bool layer0Found = false;
            foreach (var entity in entities)
            {
                if (entity.LayerName == "0")
                {
                    layer0Found = true;
                    errors.Add(new QCError(
                        "Layer Rule",
                        "Layer 0",
                        $"Entity of type '{entity.EntityType}' exists on Layer 0.",
                        "Warning"
                    ));
                }
            }

            // Generate Summary
            string summary = "";
            summary += layer0Found 
                ? "Warning: Layer 0 entities detected. " 
                : "No Layer 0 entities found. ";
            
            summary += textStylesClean 
                ? "All text styles are compliant." 
                : "Non-compliant text styles found.";

            // 4. Create Report
            var report = new QCReport(fileName, DateTime.Now, summary, errors);

            // 4. Generate Output
            return _reportGenerator.Generate(report);
        }
    }
}
