using QCValidator.Domain.Models;
using System.Collections.Generic;

namespace QCValidator.Application.Interfaces;

public interface IDrawingDataProvider
{
    List<Layer> GetLayers();
    List<TextStyle> GetTextStyles();
}
