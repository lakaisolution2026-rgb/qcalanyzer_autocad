namespace QCValidator.Domain.Models;

public class Layer
{
    public string Name { get; set; } = string.Empty;
    public short Color { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsLocked { get; set; }

    public Layer() { }

    public Layer(string name, short color, bool isFrozen = false, bool isLocked = false)
    {
        Name = name;
        Color = color;
        IsFrozen = isFrozen;
        IsLocked = isLocked;
    }
}
