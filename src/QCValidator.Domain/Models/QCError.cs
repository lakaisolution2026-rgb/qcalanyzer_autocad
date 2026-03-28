namespace QCValidator.Domain.Models
{
    public class QCError
    {
        public string Category { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error";
        public double? X { get; set; }
        public double? Y { get; set; }
        public string Space { get; set; } = string.Empty;

        public QCError() { }

        public QCError(string category, string item, string message, string severity = "Error", double? x = null, double? y = null, string space = "")
        {
            Category = category;
            Item = item;
            Message = message;
            Severity = severity;
            X = x;
            Y = y;
            Space = space;
        }
    }
}
