namespace QCValidator.Domain.Models
{
    public class DrawingEntity
    {
        public string LayerName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public string Space { get; set; } = string.Empty; // e.g., "ModelSpace" or "PaperSpace"
        public string StyleName { get; set; } = string.Empty; // For text/mtext
        public string Value { get; set; } = string.Empty; // Actual text content or layer/entity value

        // Bounding box for polyline/rectangle entities
        public double? BoundsMinX { get; set; }
        public double? BoundsMinY { get; set; }
        public double? BoundsMaxX { get; set; }
        public double? BoundsMaxY { get; set; }
        public bool IsClosed { get; set; }

        // True if this entity was extracted from inside a block insert (e.g., title block)
        public bool IsInsideBlock { get; set; }
    }
}
