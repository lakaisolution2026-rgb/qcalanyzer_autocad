using ACadSharp;
using ACadSharp.IO;
using ACadSharp.Tables;
using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QCValidator.Infrastructure.Providers
{
    public class AutoCadDrawingProvider : IDrawingDataProvider
    {
        private readonly string _filePath;
        private CadDocument? _document;

        public AutoCadDrawingProvider(string filePath)
        {
            _filePath = filePath;
            LoadDocument();
        }

        private void LoadDocument()
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("AutoCAD file not found.", _filePath);
            }

            // Using ACadSharp to read the DWG file
            using (DwgReader reader = new DwgReader(_filePath))
            {
                _document = reader.Read();
            }
        }

        public List<Domain.Models.Layer> GetLayers()
        {
            if (_document == null) return new List<Domain.Models.Layer>();

            return _document.Layers.Select(l => new Domain.Models.Layer
            {
                Name = l.Name,
                Color = (short)l.Color.Index,
                IsFrozen = l.Flags.HasFlag(LayerFlags.Frozen),
                IsLocked = l.Flags.HasFlag(LayerFlags.Locked)
            }).ToList();
        }

        public List<Domain.Models.TextStyle> GetTextStyles()
        {
            if (_document == null) return new List<Domain.Models.TextStyle>();

            return _document.TextStyles.Select(ts => new Domain.Models.TextStyle
            {
                Name = ts.Name,
                Font = ts.Filename,
                Height = ts.Height
            }).ToList();
        }

        public List<Domain.Models.DrawingEntity> GetEntities()
        {
            if (_document == null) return new List<Domain.Models.DrawingEntity>();

            var drawingEntities = new List<Domain.Models.DrawingEntity>();

            foreach (var e in _document.Entities)
            {
                var drawingEntity = new Domain.Models.DrawingEntity
                {
                    LayerName = e.Layer.Name,
                    EntityType = e.ObjectName,
                    Space = (e.Owner as ACadSharp.Tables.BlockRecord)?.Name ?? "Unknown"
                };

                // Extract location based on entity type
                if (e is ACadSharp.Entities.TextEntity text)
                {
                    drawingEntity.X = text.InsertPoint.X;
                    drawingEntity.Y = text.InsertPoint.Y;
                    drawingEntity.StyleName = text.Style?.Name ?? "Standard";
                    drawingEntity.Value = text.Value;
                }
                else if (e is ACadSharp.Entities.MText mtext)
                {
                    drawingEntity.X = mtext.InsertPoint.X;
                    drawingEntity.Y = mtext.InsertPoint.Y;
                    drawingEntity.StyleName = mtext.Style?.Name ?? "Standard";
                    drawingEntity.Value = mtext.Value;
                }
                else if (e is ACadSharp.Entities.Line line)
                {
                    drawingEntity.X = line.StartPoint.X;
                    drawingEntity.Y = line.StartPoint.Y;
                }
                else if (e is ACadSharp.Entities.Circle circle)
                {
                    drawingEntity.X = circle.Center.X;
                    drawingEntity.Y = circle.Center.Y;
                }
                else if (e is ACadSharp.Entities.Point point)
                {
                    drawingEntity.X = point.Location.X;
                    drawingEntity.Y = point.Location.Y;
                }
                else if (e is ACadSharp.Entities.Arc arc)
                {
                    drawingEntity.X = arc.Center.X;
                    drawingEntity.Y = arc.Center.Y;
                }
                else if (e is ACadSharp.Entities.LwPolyline polyline && polyline.Vertices.Any())
                {
                    drawingEntity.X = polyline.Vertices.First().Location.X;
                    drawingEntity.Y = polyline.Vertices.First().Location.Y;

                    // Calculate bounding box for box/rectangle detection
                    var verts = polyline.Vertices.ToList();
                    if (verts.Count >= 3)
                    {
                        var xs = verts.Select(v => v.Location.X);
                        var ys = verts.Select(v => v.Location.Y);
                        drawingEntity.BoundsMinX = xs.Min();
                        drawingEntity.BoundsMinY = ys.Min();
                        drawingEntity.BoundsMaxX = xs.Max();
                        drawingEntity.BoundsMaxY = ys.Max();

                        // Detect closed polyline via flag or matching first/last vertex
                        bool flagClosed = polyline.IsClosed;
                        double dx = verts.First().Location.X - verts.Last().Location.X;
                        double dy = verts.First().Location.Y - verts.Last().Location.Y;
                        bool vertexClosed = System.Math.Sqrt(dx * dx + dy * dy) < 0.1;
                        drawingEntity.IsClosed = flagClosed || vertexClosed;
                    }
                }
                else if (e is ACadSharp.Entities.Insert insert)
                {
                    drawingEntity.X = insert.InsertPoint.X;
                    drawingEntity.Y = insert.InsertPoint.Y;
                    
                    // Check attributes inside the block for text styles
                    foreach(var attr in insert.Attributes)
                    {
                        // Skip empty attribute placeholders
                        if (string.IsNullOrWhiteSpace(attr.Value)) continue;

                        drawingEntities.Add(new Domain.Models.DrawingEntity
                        {
                            LayerName = attr.Layer.Name,
                            EntityType = "ATTRIBUTE",
                            StyleName = attr.Style?.Name ?? "Standard",
                            Value = attr.Value,
                            X = attr.InsertPoint.X,
                            Y = attr.InsertPoint.Y,
                            Space = drawingEntity.Space
                        });
                    }
                }

                drawingEntities.Add(drawingEntity);
            }

            // ─────────────────────────────────────────────────────────
            // Paper Space scanning – find all *Paper_Space block records
            // and extract entities exactly the same way as Model Space.
            // ─────────────────────────────────────────────────────────
            foreach (var blockRecord in _document.BlockRecords)
            {
                if (!blockRecord.Name.StartsWith("*Paper_Space", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // Get the actual layout/sheet name (the tab name visible in AutoCAD)
                string? layoutName = blockRecord.Layout?.Name;
                string spaceName = !string.IsNullOrWhiteSpace(layoutName)
                    ? $"PaperSpace – {layoutName}"
                    : "PaperSpace";

                foreach (var e in blockRecord.Entities)
                {
                    // Extract Viewport entities with their bounds (for boxed-callout zone detection)
                    // but mark them so they don't trigger Layer 0 or other rules.
                    if (e is ACadSharp.Entities.Viewport vp)
                    {
                        // Skip the system default viewport (viewport #1, or zero-size)
                        if (vp.Width <= 0 || vp.Height <= 0)
                            continue;

                        drawingEntities.Add(new Domain.Models.DrawingEntity
                        {
                            LayerName = vp.Layer?.Name ?? "VIEWPORTS",
                            EntityType = "VIEWPORT",
                            Space = spaceName,
                            X = vp.Center.X,
                            Y = vp.Center.Y,
                            BoundsMinX = vp.Center.X - (vp.Width / 2.0),
                            BoundsMinY = vp.Center.Y - (vp.Height / 2.0),
                            BoundsMaxX = vp.Center.X + (vp.Width / 2.0),
                            BoundsMaxY = vp.Center.Y + (vp.Height / 2.0),
                            IsClosed = true
                        });
                        continue;
                    }

                    var drawingEntity = new Domain.Models.DrawingEntity
                    {
                        LayerName = e.Layer?.Name ?? "0",
                        EntityType = e.ObjectName,
                        Space = spaceName
                    };

                    if (e is ACadSharp.Entities.TextEntity psText)
                    {
                        drawingEntity.X = psText.InsertPoint.X;
                        drawingEntity.Y = psText.InsertPoint.Y;
                        drawingEntity.StyleName = psText.Style?.Name ?? "Standard";
                        drawingEntity.Value = psText.Value;
                    }
                    else if (e is ACadSharp.Entities.MText psMtext)
                    {
                        drawingEntity.X = psMtext.InsertPoint.X;
                        drawingEntity.Y = psMtext.InsertPoint.Y;
                        drawingEntity.StyleName = psMtext.Style?.Name ?? "Standard";
                        drawingEntity.Value = psMtext.Value;
                    }
                    else if (e is ACadSharp.Entities.Line psLine)
                    {
                        drawingEntity.X = psLine.StartPoint.X;
                        drawingEntity.Y = psLine.StartPoint.Y;
                    }
                    else if (e is ACadSharp.Entities.Circle psCircle)
                    {
                        drawingEntity.X = psCircle.Center.X;
                        drawingEntity.Y = psCircle.Center.Y;
                    }
                    else if (e is ACadSharp.Entities.Point psPoint)
                    {
                        drawingEntity.X = psPoint.Location.X;
                        drawingEntity.Y = psPoint.Location.Y;
                    }
                    else if (e is ACadSharp.Entities.Arc psArc)
                    {
                        drawingEntity.X = psArc.Center.X;
                        drawingEntity.Y = psArc.Center.Y;
                    }
                    else if (e is ACadSharp.Entities.LwPolyline psPoly && psPoly.Vertices.Any())
                    {
                        drawingEntity.X = psPoly.Vertices.First().Location.X;
                        drawingEntity.Y = psPoly.Vertices.First().Location.Y;

                        // Calculate bounding box for box/rectangle detection
                        var psVerts = psPoly.Vertices.ToList();
                        if (psVerts.Count >= 3)
                        {
                            var psXs = psVerts.Select(v => v.Location.X);
                            var psYs = psVerts.Select(v => v.Location.Y);
                            drawingEntity.BoundsMinX = psXs.Min();
                            drawingEntity.BoundsMinY = psYs.Min();
                            drawingEntity.BoundsMaxX = psXs.Max();
                            drawingEntity.BoundsMaxY = psYs.Max();

                            bool psFlagClosed = psPoly.IsClosed;
                            double psDx = psVerts.First().Location.X - psVerts.Last().Location.X;
                            double psDy = psVerts.First().Location.Y - psVerts.Last().Location.Y;
                            bool psVertexClosed = System.Math.Sqrt(psDx * psDx + psDy * psDy) < 0.1;
                            drawingEntity.IsClosed = psFlagClosed || psVertexClosed;
                        }
                    }
                    else if (e is ACadSharp.Entities.Insert psInsert)
                    {
                        drawingEntity.X = psInsert.InsertPoint.X;
                        drawingEntity.Y = psInsert.InsertPoint.Y;
                        drawingEntity.IsInsideBlock = true; // The INSERT itself is a block

                        // Check attributes inside the block for text styles
                        foreach (var attr in psInsert.Attributes)
                        {
                            // Skip empty attribute placeholders
                            if (string.IsNullOrWhiteSpace(attr.Value)) continue;

                            drawingEntities.Add(new Domain.Models.DrawingEntity
                            {
                                LayerName = attr.Layer?.Name ?? "0",
                                EntityType = "ATTRIBUTE",
                                StyleName = attr.Style?.Name ?? "Standard",
                                Value = attr.Value,
                                X = attr.InsertPoint.X,
                                Y = attr.InsertPoint.Y,
                                Space = spaceName,
                                IsInsideBlock = true // Mark as from a block (e.g., title block)
                            });
                        }
                    }

                    drawingEntities.Add(drawingEntity);
                }
            }

            // Also scan all block DEFINITIONS (not instances) for text entities.
            // This finds text with wrong styles that live inside symbol blocks.
            // Coordinates are local to the block; Space is labeled "Block: <name>".
            foreach (var blockRecord in _document.BlockRecords)
            {
                // Skip the auto-generated model/paper space blocks (already covered above)
                if (blockRecord.Name.StartsWith("*")) continue;

                foreach (var e in blockRecord.Entities)
                {
                    ExtractTextFromEntity(e, $"Block: {blockRecord.Name}", drawingEntities);

                    // Also check attributes defined inside block definitions
                    if (e is ACadSharp.Entities.Insert nestedInsert)
                    {
                        foreach (var attr in nestedInsert.Attributes)
                        {
                            if (!string.IsNullOrWhiteSpace(attr.Value))
                            {
                                drawingEntities.Add(new Domain.Models.DrawingEntity
                                {
                                    LayerName = attr.Layer?.Name ?? "0",
                                    EntityType = "ATTRIBUTE",
                                    StyleName = attr.Style?.Name ?? "Standard",
                                    Value = attr.Value,
                                    X = attr.InsertPoint.X,
                                    Y = attr.InsertPoint.Y,
                                    Space = $"Block: {blockRecord.Name}"
                                });
                            }
                        }
                    }
                }
            }

            return drawingEntities;
        }

        private void ExtractTextFromEntity(
            ACadSharp.Entities.Entity e,
            string space,
            List<Domain.Models.DrawingEntity> drawingEntities)
        {
            if (e is ACadSharp.Entities.TextEntity text && !string.IsNullOrWhiteSpace(text.Value))
            {
                drawingEntities.Add(new Domain.Models.DrawingEntity
                {
                    LayerName = e.Layer?.Name ?? "0",
                    EntityType = "TEXT",
                    StyleName = text.Style?.Name ?? "Standard",
                    Value = text.Value,
                    X = text.InsertPoint.X,
                    Y = text.InsertPoint.Y,
                    Space = space
                });
            }
            else if (e is ACadSharp.Entities.MText mtext && !string.IsNullOrWhiteSpace(mtext.Value))
            {
                drawingEntities.Add(new Domain.Models.DrawingEntity
                {
                    LayerName = e.Layer?.Name ?? "0",
                    EntityType = "MTEXT",
                    StyleName = mtext.Style?.Name ?? "Standard",
                    Value = mtext.Value,
                    X = mtext.InsertPoint.X,
                    Y = mtext.InsertPoint.Y,
                    Space = space
                });
            }
        }
    }
}
