namespace QCValidator.Domain.Models;

public class TextStyle
{
    public string Name { get; set; } = string.Empty;
    public string Font { get; set; } = string.Empty;
    public double Height { get; set; }
    public bool IsAnnotative { get; set; }

    public TextStyle() { }

    public TextStyle(string name, string font, double height, bool isAnnotative = false)
    {
        Name = name;
        Font = font;
        Height = height;
        IsAnnotative = isAnnotative;
    }
}
