using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QCValidator.Application.Services;

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

    public void Run(string fileName)
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

        // 3. Create Report
        var report = new QCReport(fileName, DateTime.Now, errors);

        // 4. Generate Output
        _reportGenerator.Generate(report);
    }
}
