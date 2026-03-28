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
                }
                else if (e is ACadSharp.Entities.Insert insert)
                {
                    drawingEntity.X = insert.InsertPoint.X;
                    drawingEntity.Y = insert.InsertPoint.Y;
                    
                    // Check attributes inside the block for text styles
                    foreach(var attr in insert.Attributes)
                    {
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
