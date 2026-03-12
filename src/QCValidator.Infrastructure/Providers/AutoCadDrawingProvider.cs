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

            // ACadSharp Entities collection contains all entities in the drawing
            return _document.Entities.Select(e => new Domain.Models.DrawingEntity
            {
                LayerName = e.Layer.Name,
                EntityType = e.ObjectName // e.g. "AcDbLine", "AcDbCircle"
            }).ToList();
        }
    }
}
