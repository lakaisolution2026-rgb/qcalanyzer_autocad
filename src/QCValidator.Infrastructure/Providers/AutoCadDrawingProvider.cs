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
                }
                else if (e is ACadSharp.Entities.MText mtext)
                {
                    drawingEntity.X = mtext.InsertPoint.X;
                    drawingEntity.Y = mtext.InsertPoint.Y;
                    drawingEntity.StyleName = mtext.Style?.Name ?? "Standard";
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
                }

                drawingEntities.Add(drawingEntity);
            }

            return drawingEntities;
        }
    }
}
