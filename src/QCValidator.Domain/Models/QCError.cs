namespace QCValidator.Domain.Models;

public class QCError
{
    public string Category { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public QCError() { }

    public QCError(string category, string item, string message)
    {
        Category = category;
        Item = item;
        Message = message;
    }
}
