namespace SafeRoute.Models;

public class RouteResult
{
    public RouteRequest Request { get; set; } = new();
    public List<RouteOption> Options { get; set; } = new();
    public string AiSafetyAssessment { get; set; } = "";
    public string RecommendedOptionId { get; set; } = "";
    public List<CrimeIncident> NearbyIncidents { get; set; } = new();
    public bool DataDisclaimer { get; set; } = true;
}

public class RouteOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public int SafetyScore { get; set; } // 1-10, 10 = safest
    public List<double[]> Polyline { get; set; } = new(); // [lat, lng] pairs
    public List<string> SafetyNotes { get; set; } = new();
}
