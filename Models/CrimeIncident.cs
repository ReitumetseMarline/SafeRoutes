namespace SafeRoute.Models;

public class CrimeIncident
{
    public string Province { get; set; } = "";
    public string Station { get; set; } = "";
    public string Category { get; set; } = "";
    public int Count { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Year { get; set; }
}
