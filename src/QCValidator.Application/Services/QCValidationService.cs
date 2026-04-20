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

                // Skip VIEWPORT entities – they are infrastructure, not user-placed
                if (entityTypeUpper == "VIEWPORT") continue;

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

                // Only flag styles that are actually used by entities.
                // Skip orphaned styles that exist in the style table but aren't applied anywhere.
                if (!styleUsages.Any()) continue;

                foreach (var e in styleUsages)
                {
                    bool hasContent = !string.IsNullOrWhiteSpace(e.Value);
                    string displayVal = hasContent ? e.Value : $"[{e.EntityType} – empty]";
                    err.AddLocation(e.X, e.Y, e.Space, displayVal, e.StyleName);
                }

                consolidatedErrors[key] = err;
            }

            // ─────────────────────────────────────────────────────────
            // Rule 3: Paper Space Callout Text – check for forbidden
            // words "PROPOSED" and "TO BE INSTALLED" in Paper Space
            // TEXT/MTEXT entities only. Model Space is ignored.
            // ─────────────────────────────────────────────────────────
            var forbiddenPhrases = new[] { "PROPOSED", "TO BE INSTALLED" };
            const string calloutKey = "PAPERSPACE_CALLOUT_TEXT_ERROR";

            // Collect all Paper Space text/mtext entities with visible content
            var paperSpaceTextEntities = entities.Where(entity =>
            {
                if (string.IsNullOrEmpty(entity.Space) ||
                    !entity.Space.StartsWith("PaperSpace", StringComparison.OrdinalIgnoreCase))
                    return false;

                string et = entity.EntityType?.ToUpperInvariant() ?? "";
                if (!et.Contains("TEXT") && !et.Contains("MTEXT"))
                    return false;

                return !string.IsNullOrWhiteSpace(entity.Value);
            }).ToList();

            int psTextScannedCount = paperSpaceTextEntities.Count;

            // Collect unique Paper Space layout names for display
            var paperSpaceNames = paperSpaceTextEntities
                .Select(e => e.Space)
                .Distinct()
                .ToList();

            foreach (var entity in paperSpaceTextEntities)
            {
                string valueUpper = entity.Value.ToUpperInvariant();
                foreach (var phrase in forbiddenPhrases)
                {
                    if (valueUpper.Contains(phrase))
                    {
                        if (!consolidatedErrors.ContainsKey(calloutKey))
                        {
                            consolidatedErrors[calloutKey] = new QCError(
                                "PaperSpace Callout Text Rule",
                                $"Callout text in Paper Space contains forbidden words ('PROPOSED' or 'TO BE INSTALLED'). Scanned {psTextScannedCount} text entit(ies) across {paperSpaceNames.Count} layout(s): {string.Join(", ", paperSpaceNames)}.",
                                "Error"
                            );
                        }
                        consolidatedErrors[calloutKey].AddLocation(
                            entity.X, entity.Y, entity.Space, entity.Value, entity.StyleName);
                        break; // avoid double-flagging the same entity for both phrases
                    }
                }
            }

            // Always emit this rule (even with 0 violations) so the UI can show it
            if (!consolidatedErrors.ContainsKey(calloutKey))
            {
                consolidatedErrors[calloutKey] = new QCError(
                    "PaperSpace Callout Text Rule",
                    $"Scanned {psTextScannedCount} text entit(ies) across {paperSpaceNames.Count} Paper Space layout(s): {(paperSpaceNames.Any() ? string.Join(", ", paperSpaceNames) : "none found")}. No forbidden words ('PROPOSED' or 'TO BE INSTALLED') detected.",
                    "Info"
                );
            }

            // ─────────────────────────────────────────────────────────
            // Rule 4: Boxed Callout Text – detect TEXT/MTEXT in Paper
            // Space that falls inside a closed rectangle/polyline
            // (i.e., text covered with a box line).
            //
            // FILTERS:
            // 1. Skip text from block inserts (title block text)
            // 2. Only check text over the viewport area
            // 3. Rectangle must be within 3mm of the text
            // ─────────────────────────────────────────────────────────
            const string boxedKey = "PAPERSPACE_BOXED_CALLOUT_ERROR";

            // ── Filter 1: Only direct Paper Space TEXT/MTEXT (NOT from block inserts) ──
            var directPsText = paperSpaceTextEntities.Where(t => !t.IsInsideBlock).ToList();
            int directPsTextCount = directPsText.Count;

            // ── Filter 2: Build viewport zone from user viewports ──
            var psViewports = entities.Where(e =>
                !string.IsNullOrEmpty(e.Space) &&
                e.Space.StartsWith("PaperSpace", StringComparison.OrdinalIgnoreCase) &&
                e.EntityType?.ToUpperInvariant() == "VIEWPORT" &&
                e.BoundsMinX.HasValue && e.BoundsMinY.HasValue &&
                e.BoundsMaxX.HasValue && e.BoundsMaxY.HasValue
            ).ToList();

            // Exclude the system default viewport (largest by area per layout)
            var userViewports = new List<Domain.Models.DrawingEntity>();
            foreach (var vpGroup in psViewports.GroupBy(v => v.Space, StringComparer.OrdinalIgnoreCase))
            {
                var vpList = vpGroup.OrderByDescending(v =>
                    (v.BoundsMaxX.GetValueOrDefault() - v.BoundsMinX.GetValueOrDefault()) *
                    (v.BoundsMaxY.GetValueOrDefault() - v.BoundsMinY.GetValueOrDefault())).ToList();

                // Skip the first (largest = system viewport), keep the rest
                if (vpList.Count > 1)
                    userViewports.AddRange(vpList.Skip(1));
            }

            int psViewportCount = userViewports.Count;

            // Build combined viewport zone per layout
            var layoutViewportZones = new Dictionary<string, (double MinX, double MinY, double MaxX, double MaxY)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var vpGroup in userViewports.GroupBy(v => v.Space, StringComparer.OrdinalIgnoreCase))
            {
                layoutViewportZones[vpGroup.Key] = (
                    vpGroup.Min(v => v.BoundsMinX.GetValueOrDefault()),
                    vpGroup.Min(v => v.BoundsMinY.GetValueOrDefault()),
                    vpGroup.Max(v => v.BoundsMaxX.GetValueOrDefault()),
                    vpGroup.Max(v => v.BoundsMaxY.GetValueOrDefault())
                );
            }

            // ── Gather closed rectangles (NOT from blocks, NOT viewports) ──
            var psClosedRects = entities.Where(e =>
                !string.IsNullOrEmpty(e.Space) &&
                e.Space.StartsWith("PaperSpace", StringComparison.OrdinalIgnoreCase) &&
                e.EntityType?.ToUpperInvariant() != "VIEWPORT" &&
                !e.IsInsideBlock &&
                e.IsClosed &&
                e.BoundsMinX.HasValue && e.BoundsMinY.HasValue &&
                e.BoundsMaxX.HasValue && e.BoundsMaxY.HasValue
            ).ToList();

            int psRectsScannedCount = psClosedRects.Count;

            // ── Filter 3: 3mm proximity check ──
            double proximity = 3.0; // 3mm — rectangle must be within 3mm of the text

            foreach (var textEntity in directPsText)
            {
                // Check text is over the viewport area
                if (layoutViewportZones.TryGetValue(textEntity.Space, out var vpZone))
                {
                    bool overViewport =
                        textEntity.X >= vpZone.MinX && textEntity.X <= vpZone.MaxX &&
                        textEntity.Y >= vpZone.MinY && textEntity.Y <= vpZone.MaxY;

                    if (!overViewport)
                        continue; // Text is NOT over the viewport — skip
                }
                else
                {
                    continue; // No user viewports in this layout — skip entirely
                }

                // Check if covered by a small, close rectangle
                foreach (var rect in psClosedRects)
                {
                    if (!string.Equals(textEntity.Space, rect.Space, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Skip large rectangles (sheet borders, title block frames)
                    double rectW = rect.BoundsMaxX.GetValueOrDefault() - rect.BoundsMinX.GetValueOrDefault();
                    double rectH = rect.BoundsMaxY.GetValueOrDefault() - rect.BoundsMinY.GetValueOrDefault();
                    if (rectW > 500 || rectH > 200)
                        continue; // Too large to be a callout box

                    // Text must be inside the rectangle, within 3mm proximity
                    if (textEntity.X >= rect.BoundsMinX.GetValueOrDefault() - proximity &&
                        textEntity.X <= rect.BoundsMaxX.GetValueOrDefault() + proximity &&
                        textEntity.Y >= rect.BoundsMinY.GetValueOrDefault() - proximity &&
                        textEntity.Y <= rect.BoundsMaxY.GetValueOrDefault() + proximity)
                    {
                        if (!consolidatedErrors.ContainsKey(boxedKey))
                        {
                            consolidatedErrors[boxedKey] = new QCError(
                                "PaperSpace Boxed Callout Rule",
                                $"Callout text over the viewport area is covered by a rectangle/box (within 3mm proximity). " +
                                $"Checked {directPsTextCount} direct callout(s) against {psRectsScannedCount} rectangle(s) in {psViewportCount} viewport zone(s). " +
                                $"Block/template text is excluded.",
                                "Error"
                            );
                        }
                        consolidatedErrors[boxedKey].AddLocation(
                            textEntity.X, textEntity.Y, textEntity.Space, textEntity.Value, textEntity.StyleName);
                        break;
                    }
                }
            }

            // Always emit this rule (even with 0 violations) so the UI can show it
            if (!consolidatedErrors.ContainsKey(boxedKey))
            {
                consolidatedErrors[boxedKey] = new QCError(
                    "PaperSpace Boxed Callout Rule",
                    $"Scanned {directPsTextCount} direct callout(s) against {psRectsScannedCount} rectangle(s) in {psViewportCount} viewport zone(s). " +
                    $"Block/template text excluded. No boxed callout text detected (3mm proximity).",
                    "Info"
                );
            }

            // ─────────────────────────────────────────────────────────
            // Summary
            // ─────────────────────────────────────────────────────────
            int layer0Count = consolidatedErrors.Values
                .FirstOrDefault(e => e.Category == "Layer 0 Rule")?.Locations.Count ?? 0;
            int styleErrorCount = consolidatedErrors.Values
                .Where(e => e.Category == "Text Style Rule").Sum(e => e.Locations.Count);
            int calloutErrorCount = consolidatedErrors.Values
                .FirstOrDefault(e => e.Category == "PaperSpace Callout Text Rule")?.Locations.Count ?? 0;
            int boxedErrorCount = consolidatedErrors.Values
                .FirstOrDefault(e => e.Category == "PaperSpace Boxed Callout Rule")?.Locations.Count ?? 0;

            bool isClean = layer0Count == 0 && styleErrorCount == 0 && calloutErrorCount == 0 && boxedErrorCount == 0;
            string summary = isClean
                ? "✅ Drawing is fully compliant. No Layer 0, Text Style, Callout Text, or Boxed Callout issues found."
                : $"⚠️ Found {layer0Count} entity(s) on Layer 0, {styleErrorCount} text style violation(s), {calloutErrorCount} callout text violation(s), and {boxedErrorCount} boxed callout violation(s).";

            var report = new QCReport(fileName, DateTime.Now, summary, consolidatedErrors.Values.ToList());
            return _reportGenerator.Generate(report);
        }
    }
}
