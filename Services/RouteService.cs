using OpenAI;
using OpenAI.Chat;
using SafeRoute.Models;
using System.Text.Json;

namespace SafeRoute.Services;

public class RouteService
{
    private readonly ChatClient _chatClient;
    private readonly CrimeDataService _crimeData;
    private readonly HttpClient _http;

    public RouteService(IConfiguration config, CrimeDataService crimeData, IHttpClientFactory httpFactory)
    {
        var openAiKey = config["OpenAI:Key"]!;
        var openAiModel = config["OpenAI:Model"] ?? "gpt-4o";
        _chatClient = new OpenAIClient(openAiKey).GetChatClient(openAiModel);
        _crimeData = crimeData;
        _http = httpFactory.CreateClient("maps");
    }

    public async Task<RouteResult> GetSafeRoutesAsync(RouteRequest request)
    {
        var origin = await GeocodeAsync(request.Origin);
        var destination = await GeocodeAsync(request.Destination);

        var routeOptions = await GetRouteOptionsAsync(origin, destination);

        foreach (var option in routeOptions)
        {
            option.SafetyScore = ComputeSafetyScore(option);
            option.SafetyNotes = BuildSafetyNotes(option, request.TimeOfDay);
        }

        var bestRoute = routeOptions.OrderByDescending(r => r.SafetyScore).First();
        var nearbyIncidents = _crimeData.GetIncidentsNear(origin.Lat, origin.Lon, radiusKm: 3);
        var assessment = await GetAiSafetyAssessmentAsync(request, routeOptions, nearbyIncidents);

        return new RouteResult
        {
            Request = request,
            Options = routeOptions,
            AiSafetyAssessment = assessment,
            RecommendedOptionId = bestRoute.Id,
            NearbyIncidents = nearbyIncidents.Take(5).ToList(),
            DataDisclaimer = true
        };
    }

    // Nominatim (OpenStreetMap) — free, no key needed
    private async Task<(double Lat, double Lon)> GeocodeAsync(string address)
    {
        var encoded = Uri.EscapeDataString(address);
        var url = $"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1&countrycodes=za";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.EnumerateArray().ToList();
        if (!arr.Any()) throw new Exception($"Could not find location: {address}");
        return (
            double.Parse(arr[0].GetProperty("lat").GetString()!),
            double.Parse(arr[0].GetProperty("lon").GetString()!)
        );
    }

    // OSRM — free, open-source routing, no key needed
    private async Task<List<RouteOption>> GetRouteOptionsAsync(
        (double Lat, double Lon) origin, (double Lat, double Lon) destination)
    {
        var url = $"https://router.project-osrm.org/route/v1/foot/" +
                  $"{origin.Lon},{origin.Lat};{destination.Lon},{destination.Lat}" +
                  $"?alternatives=true&geometries=geojson&overview=full";

        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var routes = doc.RootElement.GetProperty("routes").EnumerateArray().ToList();

        var options = new List<RouteOption>();
        for (int i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            var coords = route
                .GetProperty("geometry")
                .GetProperty("coordinates")
                .EnumerateArray()
                .Select(c => new double[] {
                    c[1].GetDouble(), // lat
                    c[0].GetDouble()  // lon
                })
                .ToList();

            options.Add(new RouteOption
            {
                Id = $"route_{i + 1}",
                Label = i == 0 ? "Primary Route" : $"Alternative {i}",
                DistanceKm = Math.Round(route.GetProperty("distance").GetDouble() / 1000.0, 1),
                DurationMinutes = (int)(route.GetProperty("duration").GetDouble() / 60),
                Polyline = coords
            });
        }

        return options;
    }

    private int ComputeSafetyScore(RouteOption option)
    {
        var samplePoints = option.Polyline
            .Where((_, i) => i % Math.Max(1, option.Polyline.Count / 10) == 0)
            .ToList();

        if (!samplePoints.Any()) return 5;

        double total = samplePoints.Sum(p => _crimeData.GetCrimeScoreNear(p[0], p[1], radiusKm: 0.5));
        double avg = total / samplePoints.Count;

        return avg switch
        {
            < 50 => 9,
            < 200 => 7,
            < 500 => 5,
            < 1000 => 3,
            _ => 1
        };
    }

    private List<string> BuildSafetyNotes(RouteOption option, TimeOfDay time)
    {
        var notes = new List<string>();
        if (time is TimeOfDay.Night or TimeOfDay.Evening)
            notes.Add("Night travel: stick to well-lit main roads where possible.");
        if (option.SafetyScore <= 3)
            notes.Add("High crime density detected along this route. Consider alternatives.");
        if (option.SafetyScore >= 8)
            notes.Add("Relatively lower crime density compared to other options.");
        notes.Add("Share your live location with a trusted contact before travelling.");
        return notes;
    }

    private async Task<string> GetAiSafetyAssessmentAsync(
        RouteRequest request, List<RouteOption> routes, List<Models.CrimeIncident> incidents)
    {
        var crimeContext = incidents.Any()
            ? string.Join("\n", incidents.Select(i =>
                $"- {i.Station} station area: {i.Count} {i.Category} incidents (2023)"))
            : "No recent incident data found near origin.";

        var routeSummary = string.Join("\n", routes.Select(r =>
            $"- {r.Label}: {r.DistanceKm}km, {r.DurationMinutes} min, safety score {r.SafetyScore}/10"));

        var prompt = $"""
            You are SafeRoute's safety advisor helping a woman navigate safely in South Africa.

            Journey: {request.Origin} → {request.Destination}
            Time of travel: {request.TimeOfDay}

            Available routes:
            {routeSummary}

            Nearby crime data (SAPS 2023):
            {crimeContext}

            Provide a concise safety assessment (3-4 sentences) that:
            1. Recommends the safest route and explains why
            2. Highlights specific risks based on crime data
            3. Gives one practical safety tip for this specific journey
            4. Uses empathetic, non-alarmist language

            Important: Be factual. Acknowledge data limitations — SAPS data may not capture all incidents.
            """;

        var completion = await _chatClient.CompleteChatAsync(new UserChatMessage(prompt));
        return completion.Value.Content[0].Text;
    }
}
