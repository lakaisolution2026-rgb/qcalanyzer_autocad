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
            var consolidatedErrors = new Dictionary<string, QCError>();

            var allowedTextStyles = _templateProvider.GetTemplateTextStyles();
            var allowedStyleNames = new HashSet<string>(
                allowedTextStyles.Select(s => s.Name.ToUpperInvariant())
            );

            // ─────────────────────────────────────────────────────────
            // Rule 1: Layer 0 – scan entities in model/paper space
            // ─────────────────────────────────────────────────────────
            var entities = _drawingProvider.GetEntities();
            foreach (var entity in entities)
            {
                string entityTypeUpper = entity.EntityType?.ToUpperInvariant() ?? "";
                bool isTextOrAttr = entityTypeUpper.Contains("TEXT") || entityTypeUpper.Contains("ATTRIBUTE");
                bool hasContent = !string.IsNullOrWhiteSpace(entity.Value);

                // For text/attributes: only flag if they have visible content.
                // For graphics (lines, circles, etc.): always flag.
                if (entity.LayerName == "0" && (!isTextOrAttr || hasContent))
                {
                    const string key = "LAYER_0_ERROR";
                    if (!consolidatedErrors.ContainsKey(key))
                    {
                        consolidatedErrors[key] = new QCError(
                            "Layer 0 Rule",
                            "Entities found on Layer 0. No entities should exist on Layer 0. Use the LAS command to validate layer state.",
                            "Warning"
                        );
                    }
                    string displayContent = hasContent ? entity.Value : $"[{entity.EntityType}]";
                    consolidatedErrors[key].AddLocation(entity.X, entity.Y, entity.Space, displayContent, "");
                }
            }

            // ─────────────────────────────────────────────────────────
            // Rule 2: Text Style – check the DRAWING'S STYLE TABLE.
            // This is reliable and catches styles defined anywhere in
            // the DWG (including inside block definitions).
            // ─────────────────────────────────────────────────────────
            var drawingTextStyles = _drawingProvider.GetTextStyles();
            foreach (var style in drawingTextStyles)
            {
                if (string.IsNullOrEmpty(style.Name)) continue;
                if (allowedStyleNames.Contains(style.Name.ToUpperInvariant())) continue;

                string key = $"STYLE_ERROR_{style.Name.ToUpperInvariant()}";
                if (consolidatedErrors.ContainsKey(key)) continue;

                // Build a meaningful list of where this style is actually used
                // by scanning entities. If not found, still report the style error.
                var styleUsages = entities
                    .Where(e =>
                    {
                        string t = e.EntityType?.ToUpperInvariant() ?? "";
                        bool isTxt = t.Contains("TEXT") || t.Contains("ATTRIBUTE");
                        return isTxt && string.Equals(e.StyleName, style.Name, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                var err = new QCError(
                    "Text Style Rule",
                    $"Non-compliant text style '{style.Name}' is used in this drawing. Only 'Arial', 'Arial Narrow', or 'Standard' are permitted.",
                    "Error"
                );

                if (styleUsages.Any())
                {
                    foreach (var e in styleUsages)
                    {
                        bool hasContent = !string.IsNullOrWhiteSpace(e.Value);
                        string displayVal = hasContent ? e.Value : $"[{e.EntityType} – empty]";
                        err.AddLocation(e.X, e.Y, e.Space, displayVal, e.StyleName);
                    }
                }
                else
                {
                    // Style is in the table but no located entity – report without coordinates
                    err.Locations.Add(new QCLocation
                    {
                        X = 0, Y = 0,
                        Space = "Unknown (style defined in Style Table)",
                        TextContent = "[No specific location found – style exists in drawing style table]",
                        FoundStyle = style.Name
                    });
                }

                consolidatedErrors[key] = err;
            }

            // ─────────────────────────────────────────────────────────
            // Summary
            // ─────────────────────────────────────────────────────────
            int layer0Count = consolidatedErrors.Values
                .FirstOrDefault(e => e.Category == "Layer 0 Rule")?.Locations.Count ?? 0;
            int styleErrorCount = consolidatedErrors.Values
                .Where(e => e.Category == "Text Style Rule").Sum(e => e.Locations.Count);

            string summary = (layer0Count == 0 && styleErrorCount == 0)
                ? "✅ Drawing is fully compliant. No Layer 0 or Text Style issues found."
                : $"⚠️ Found {layer0Count} entity(s) on Layer 0 and {styleErrorCount} text style violation(s).";

            var report = new QCReport(fileName, DateTime.Now, summary, consolidatedErrors.Values.ToList());
            return _reportGenerator.Generate(report);
        }
    }
}
