using QCValidator.Application.Interfaces;
using QCValidator.Domain.Models;
using System.Collections.Generic;

namespace QCValidator.Infrastructure.Providers;

public class MockDrawingProvider : IDrawingDataProvider
{
    public List<Layer> GetLayers()
    {
        return new List<Layer>
        {
            new Layer("RRU_L", 7),
            new Layer("ANT_L", 3)
        };
    }

    public List<TextStyle> GetTextStyles()
    {
        return new List<TextStyle>
        {
            new TextStyle("ROMANS", "romans.shx", 0.0)
        };
    }
}
