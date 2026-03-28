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
    }
}
