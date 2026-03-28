using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QCValidator.Domain.Models
{
    public class QCLocation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Space { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty; // Actual text found there
        public string FoundStyle { get; set; } = string.Empty; // Style name found there

        public QCLocation() { }
        public QCLocation(double x, double y, string space, string textContent = "", string foundStyle = "")
        {
            X = x;
            Y = y;
            Space = space;
            TextContent = textContent;
            FoundStyle = foundStyle;
        }
    }

    public class QCError
    {
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error";
        public List<QCLocation> Locations { get; set; } = new List<QCLocation>();
        [JsonIgnore]
        public int Count => Locations.Count;

        public QCError() { }

        public QCError(string category, string message, string severity = "Error")
        {
            Category = category;
            Message = message;
            Severity = severity;
        }

        public void AddLocation(double? x, double? y, string space, string textContent = "", string foundStyle = "")
        {
            if (x.HasValue && y.HasValue)
            {
                Locations.Add(new QCLocation(x.Value, y.Value, space, textContent, foundStyle));
            }
        }
    }
}
