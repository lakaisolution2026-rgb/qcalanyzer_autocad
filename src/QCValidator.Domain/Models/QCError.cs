namespace QCValidator.Domain.Models
{
    public class QCError
    {
        public string Category { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error";

        public QCError() { }

        public QCError(string category, string item, string message, string severity = "Error")
        {
            Category = category;
            Item = item;
            Message = message;
            Severity = severity;
        }
    }
}
