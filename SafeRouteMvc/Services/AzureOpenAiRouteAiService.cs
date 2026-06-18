using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SafeRouteMvc.Models;

namespace SafeRouteMvc.Services;

public sealed class AzureOpenAiRouteAiService : IRouteAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAiRouteAiService> _logger;

    public AzureOpenAiRouteAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureOpenAiRouteAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiRouteExplanation> ExplainRouteAsync(SafeRouteViewModel route, CancellationToken cancellationToken)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var deployment = _configuration["AzureOpenAI:DeploymentName"];
        var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2024-10-21";

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(deployment))
        {
            return BuildDemoExplanation(route);
        }

        try
        {
            var requestUri = $"{endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(apiVersion)}";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var prompt = BuildPrompt(route);
            var body = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are SafeRoute's safety analyst. Use only the supplied route and crime-score data. Do not claim certainty. Give practical, ethical guidance for Johannesburg/Gauteng commuters."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.2,
                max_tokens = 420
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                return BuildDemoExplanation(route);
            }

            return new AiRouteExplanation
            {
                UsedLiveGpt4o = true,
                ModelName = "Azure OpenAI GPT-4o",
                Summary = content.Trim(),
                Reasons = BuildReasonBullets(route),
                SafetyAdvisory = BuildSafetyAdvisory(route),
                DataValidationNote = "GPT-4o received only structured route options and Johannesburg/Gauteng crime-score records from the server. Production data should be refreshed from SAPS releases and cross-checked against municipal and community safety reports."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI route explanation failed. Falling back to demo reasoning.");
            return BuildDemoExplanation(route);
        }
    }

    private static string BuildPrompt(SafeRouteViewModel route)
    {
        var routes = string.Join(Environment.NewLine, route.Routes.Select(option =>
            $"- {option.Name}: score {option.SafetyScore}, path {option.Summary}, areas {string.Join(", ", option.AreasCrossed)}"));

        var crimeRows = string.Join(Environment.NewLine, route.CrimeAreas.Select(area =>
            $"- {area.Name}: precinct {area.PolicePrecinct}, crime score {area.CrimeScore}, signal {area.RiskSignal}"));

        return $"""
        User trip:
        Start: {route.Request.Start}
        Destination: {route.Request.Destination}
        Travel time: {route.Request.TravelTime}

        Route options:
        {routes}

        Crime data:
        {crimeRows}

        Recommended route from scoring engine:
        {route.RecommendedRoute?.Name} with safety score {route.RecommendedRoute?.SafetyScore}

        Return a concise explanation for the demo with:
        1. why the recommended route is safer,
        2. what warning area to be aware of,
        3. one ethical note that this is decision support, not a safety guarantee.
        """;
    }

    private static AiRouteExplanation BuildDemoExplanation(SafeRouteViewModel route)
    {
        return new AiRouteExplanation
        {
            UsedLiveGpt4o = false,
            ModelName = "GPT-4o-ready demo reasoning",
            Summary = $"{route.RecommendedRoute?.Name} is recommended because its route corridor has a stronger safety score than the alternative when compared against the structured Johannesburg/Gauteng crime data.",
            Reasons = BuildReasonBullets(route),
            SafetyAdvisory = BuildSafetyAdvisory(route),
            DataValidationNote = "Prototype data is structured from SAPS-style public crime categories for selected Johannesburg/Gauteng areas. For production, SafeRoute should validate each release against SAPS annual reports, municipal safety reports, and community safety inputs before updating scores."
        };
    }

    private static IReadOnlyList<string> BuildReasonBullets(SafeRouteViewModel route)
    {
        var recommended = route.RecommendedRoute;
        var alternative = route.Routes.FirstOrDefault(item => item.Name != recommended?.Name);
        var warningArea = alternative?.AreasCrossed
            .Select(name => route.CrimeAreas.FirstOrDefault(area => area.Name == name))
            .Where(area => area is not null)
            .OrderByDescending(area => area!.CrimeScore)
            .FirstOrDefault();

        return
        [
            $"{recommended?.Name} scores {recommended?.SafetyScore}, compared with {alternative?.SafetyScore} for {alternative?.Name}.",
            warningArea is null
                ? "No higher-risk warning area was found in the alternative route."
                : $"The main caution area is {warningArea.Name}, linked to {warningArea.PolicePrecinct} with a crime score of {warningArea.CrimeScore}.",
            $"The user selected {route.Request.TravelTime}, so SafeRoute highlights isolated or higher-risk segments more strongly."
        ];
    }

    private static string BuildSafetyAdvisory(SafeRouteViewModel route)
    {
        return $"Use {route.RecommendedRoute?.Name}, avoid isolated segments where possible, and treat the score as decision support rather than a guarantee of safety.";
    }
}
