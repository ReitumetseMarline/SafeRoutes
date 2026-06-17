namespace SafeRouteMvc.Models;

public sealed class SafeRouteRequest
{
    public string Start { get; set; } = "Orlando West";
    public string Destination { get; set; } = "Bara Mall";
    public string TravelTime { get; set; } = "After 19:00";
}

public sealed class SafeRouteViewModel
{
    public SafeRouteRequest Request { get; set; } = new();
    public IReadOnlyList<RouteOption> Routes { get; set; } = [];
    public RouteOption? RecommendedRoute { get; set; }
    public IReadOnlyList<CrimeArea> CrimeAreas { get; set; } = [];
    public IReadOnlyList<string> DataSources { get; set; } = [];
    public AiRouteExplanation AiExplanation { get; set; } = new();
    public IReadOnlyList<string> EthicalSafeguards { get; set; } = [];
    public string AnalysisSummary { get; set; } = "";
    public string EvidenceNote { get; set; } = "";
    public string? InputWarning { get; set; }
}

public sealed class AiRouteExplanation
{
    public bool UsedLiveGpt4o { get; set; }
    public string ModelName { get; set; } = "GPT-4o demo reasoning";
    public string Summary { get; set; } = "";
    public IReadOnlyList<string> Reasons { get; set; } = [];
    public string SafetyAdvisory { get; set; } = "";
    public string DataValidationNote { get; set; } = "";
}

public sealed class RouteOption
{
    public string Name { get; set; } = "";
    public int SafetyScore { get; set; }
    public string Summary { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public IReadOnlyList<string> AreasCrossed { get; set; } = [];
    public IReadOnlyList<MapPoint> Coordinates { get; set; } = [];
    public string Color { get; set; } = "#16865d";
}

public sealed class CrimeArea
{
    public string Name { get; set; } = "";
    public int CrimeScore { get; set; }
    public string PolicePrecinct { get; set; } = "";
    public string RiskSignal { get; set; } = "";
    public MapPoint Coordinates { get; set; } = new();
}

public sealed class MapPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
