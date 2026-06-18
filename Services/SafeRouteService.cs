using SafeRouteMvc.Models;

namespace SafeRouteMvc.Services;

public sealed class SafeRouteService : ISafeRouteService
{
    private static readonly IReadOnlyList<CrimeArea> JohannesburgCrimeAreas =
    [
        new()
        {
            Name = "Orlando West",
            PolicePrecinct = "Orlando SAPS",
            CrimeScore = 20,
            RiskSignal = "Lower comparative route risk in the prototype dataset.",
            Coordinates = Point(-26.2353, 27.9226)
        },
        new()
        {
            Name = "Diepkloof",
            PolicePrecinct = "Diepkloof SAPS",
            CrimeScore = 50,
            RiskSignal = "Medium-high weighting for robbery and contact-crime caution.",
            Coordinates = Point(-26.2507, 27.9492)
        },
        new()
        {
            Name = "Meadowlands",
            PolicePrecinct = "Meadowlands SAPS",
            CrimeScore = 35,
            RiskSignal = "Moderate risk, used as a safer corridor in the demo.",
            Coordinates = Point(-26.2186, 27.8887)
        },
        new()
        {
            Name = "Pimville",
            PolicePrecinct = "Pimville SAPS",
            CrimeScore = 42,
            RiskSignal = "Moderate evening travel caution.",
            Coordinates = Point(-26.2678, 27.9077)
        },
        new()
        {
            Name = "Johannesburg CBD",
            PolicePrecinct = "Johannesburg Central SAPS",
            CrimeScore = 68,
            RiskSignal = "Higher density and stronger theft/robbery caution.",
            Coordinates = Point(-26.2041, 28.0473)
        },
        new()
        {
            Name = "Braamfontein",
            PolicePrecinct = "Hillbrow SAPS",
            CrimeScore = 56,
            RiskSignal = "Student commuter area with moderate-high caution.",
            Coordinates = Point(-26.1929, 28.0367)
        },
        new()
        {
            Name = "Alexandra",
            PolicePrecinct = "Alexandra SAPS",
            CrimeScore = 62,
            RiskSignal = "Higher caution weighting for selected route segments.",
            Coordinates = Point(-26.1047, 28.0989)
        },
        new()
        {
            Name = "Sandton",
            PolicePrecinct = "Sandton SAPS",
            CrimeScore = 30,
            RiskSignal = "Lower route risk in the prototype comparison.",
            Coordinates = Point(-26.1076, 28.0567)
        },
        new()
        {
            Name = "Tembisa",
            PolicePrecinct = "Tembisa SAPS",
            CrimeScore = 58,
            RiskSignal = "Gauteng township corridor with elevated commuter and evening caution.",
            Coordinates = Point(-26.0063, 28.2105)
        },
        new()
        {
            Name = "Kempton Park",
            PolicePrecinct = "Kempton Park SAPS",
            CrimeScore = 44,
            RiskSignal = "Moderate corridor risk near transport and commercial activity.",
            Coordinates = Point(-26.1004, 28.2314)
        },
        new()
        {
            Name = "Midrand",
            PolicePrecinct = "Midrand SAPS",
            CrimeScore = 36,
            RiskSignal = "Moderate-low route risk in the prototype comparison.",
            Coordinates = Point(-25.9992, 28.1263)
        },
        new()
        {
            Name = "Bara Mall",
            PolicePrecinct = "Diepkloof SAPS",
            CrimeScore = 28,
            RiskSignal = "Destination corridor caution near major transport activity.",
            Coordinates = Point(-26.2601, 27.9409)
        },
        new()
        {
            Name = "Maponya Mall",
            PolicePrecinct = "Pimville SAPS",
            CrimeScore = 32,
            RiskSignal = "Retail destination with moderate transport-node caution.",
            Coordinates = Point(-26.2578, 27.9012)
        },
        new()
        {
            Name = "Jabulani Mall",
            PolicePrecinct = "Jabulani SAPS",
            CrimeScore = 38,
            RiskSignal = "Moderate destination-area caution.",
            Coordinates = Point(-26.2504, 27.8619)
        }
    ];

    private static readonly IReadOnlyList<string> DataSources =
    [
        "SAPS Crime Statistics",
        "SAPS Annual Crime Reports",
        "Open government datasets",
        "City of Johannesburg and municipal safety reports",
        "Community safety reports"
    ];

    public SafeRouteViewModel BuildRoute(SafeRouteRequest request)
    {
        var start = MatchPlace(request.Start);
        var destination = MatchPlace(request.Destination);
        var inputWarning = BuildInputWarning(request, start, destination);

        var routes = BuildRoutes(start, destination);
        var recommended = routes.OrderByDescending(route => route.SafetyScore).First();

        return new SafeRouteViewModel
        {
            Request = new SafeRouteRequest
            {
                Start = request.Start,
                Destination = request.Destination,
                TravelTime = request.TravelTime
            },
            Routes = routes,
            RecommendedRoute = recommended,
            CrimeAreas = JohannesburgCrimeAreas,
            DataSources = DataSources,
            EthicalSafeguards =
            [
                "SafeRoute uses aggregated area-level crime indicators, not exact victim incident locations.",
                "Safety scores are decision support, not a guarantee that a route is safe.",
                "The tool warns users to contact emergency services if they are in immediate danger."
            ],
            EvidenceNote = "For the prototype, Johannesburg crime indicators are structured from publicly available SAPS-style reporting categories for selected Gauteng communities.",
            AnalysisSummary = $"SafeRoute compared route segments against Johannesburg crime-score data and recommends {recommended.Name} with a safety score of {recommended.SafetyScore}.",
            InputWarning = inputWarning
        };
    }

    private static IReadOnlyList<RouteOption> BuildRoutes(CrimeArea start, CrimeArea destination)
    {
        var saferMidpoint = PickSaferMidpoint(start, destination);
        var cautionMidpoint = PickCautionMidpoint(start, destination);

        var routeA = BuildRouteOption(
            name: "Route A",
            areas: [start, saferMidpoint, destination],
            color: "#16865d",
            summary: $"{start.Name} -> {saferMidpoint.Name} corridor -> {destination.Name}",
            recommendation: "Recommended because it avoids the highest-risk segment and keeps to a lower crime-score corridor.",
            safetyBoost: 20);

        var routeB = BuildRouteOption(
            name: "Route B",
            areas: [start, cautionMidpoint, destination],
            color: "#c4483e",
            summary: $"{start.Name} -> {cautionMidpoint.Name} corridor -> {destination.Name}",
            recommendation: "Shorter-looking option, but it crosses a higher-risk Johannesburg segment in the prototype data.",
            safetyBoost: -6);

        return routeA.SafetyScore >= routeB.SafetyScore
            ? [routeA, routeB]
            : [routeB, routeA];
    }

    private static RouteOption BuildRouteOption(
        string name,
        IReadOnlyList<CrimeArea> areas,
        string color,
        string summary,
        string recommendation,
        int safetyBoost)
    {
        var averageRisk = areas.Average(area => area.CrimeScore);
        var safetyScore = Math.Clamp((int)Math.Round(100 - averageRisk + safetyBoost), 1, 99);

        return new RouteOption
        {
            Name = name,
            SafetyScore = safetyScore,
            Summary = summary,
            Recommendation = recommendation,
            AreasCrossed = areas.Select(area => area.Name).ToArray(),
            Coordinates = areas.Select(area => area.Coordinates).ToArray(),
            Color = color
        };
    }

    private static CrimeArea PickSaferMidpoint(CrimeArea start, CrimeArea destination)
    {
        if (IsSoweto(start) || IsSoweto(destination))
        {
            return FirstAvailable(start, destination, "Meadowlands", "Pimville", "Orlando West");
        }

        if (IsEastGauteng(start) || IsEastGauteng(destination))
        {
            return FirstAvailable(start, destination, "Midrand", "Kempton Park", "Sandton");
        }

        return FirstAvailable(start, destination, "Sandton", "Midrand", "Braamfontein", "Alexandra");
    }

    private static CrimeArea PickCautionMidpoint(CrimeArea start, CrimeArea destination)
    {
        if (IsSoweto(start) || IsSoweto(destination))
        {
            return FirstAvailable(start, destination, "Diepkloof", "Pimville", "Johannesburg CBD");
        }

        if (IsEastGauteng(start) || IsEastGauteng(destination))
        {
            return FirstAvailable(start, destination, "Alexandra", "Johannesburg CBD", "Kempton Park");
        }

        return FirstAvailable(start, destination, "Johannesburg CBD", "Alexandra", "Braamfontein");
    }

    private static CrimeArea FirstAvailable(CrimeArea start, CrimeArea destination, params string[] names)
    {
        return names
            .Select(Find)
            .First(area => area.Name != start.Name && area.Name != destination.Name);
    }

    private static bool IsSoweto(CrimeArea area)
    {
        var sowetoNames = new[] { "Orlando West", "Diepkloof", "Meadowlands", "Pimville", "Bara Mall", "Maponya Mall", "Jabulani Mall" };
        return sowetoNames.Contains(area.Name);
    }

    private static bool IsEastGauteng(CrimeArea area)
    {
        var eastGautengNames = new[] { "Tembisa", "Kempton Park", "Midrand", "Alexandra" };
        return eastGautengNames.Contains(area.Name);
    }

    private static CrimeArea MatchPlace(string? input)
    {
        var normalized = Normalize(input);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Find("Orlando West");
        }

        var aliases = new Dictionary<string, string>
        {
            ["orlando"] = "Orlando West",
            ["orlando west"] = "Orlando West",
            ["bara"] = "Bara Mall",
            ["bara mall"] = "Bara Mall",
            ["baragwanath"] = "Bara Mall",
            ["chris hani baragwanath"] = "Bara Mall",
            ["diepkloof"] = "Diepkloof",
            ["meadowlands"] = "Meadowlands",
            ["pimville"] = "Pimville",
            ["maponya"] = "Maponya Mall",
            ["maponya mall"] = "Maponya Mall",
            ["jabulani"] = "Jabulani Mall",
            ["jabulani mall"] = "Jabulani Mall",
            ["joburg cbd"] = "Johannesburg CBD",
            ["johannesburg cbd"] = "Johannesburg CBD",
            ["johannesburg central"] = "Johannesburg CBD",
            ["braamfontein"] = "Braamfontein",
            ["alex"] = "Alexandra",
            ["alexandra"] = "Alexandra",
            ["sandton"] = "Sandton",
            ["tembisa"] = "Tembisa",
            ["themba"] = "Tembisa",
            ["kempton"] = "Kempton Park",
            ["kempton park"] = "Kempton Park",
            ["midrand"] = "Midrand"
        };

        if (aliases.TryGetValue(normalized, out var exactMatch))
        {
            return Find(exactMatch);
        }

        var partialMatch = JohannesburgCrimeAreas.FirstOrDefault(area =>
            normalized.Contains(Normalize(area.Name)) || Normalize(area.Name).Contains(normalized));

        return partialMatch ?? Find("Orlando West");
    }

    private static string? BuildInputWarning(SafeRouteRequest request, CrimeArea start, CrimeArea destination)
    {
        var startMatched = Normalize(request.Start).Contains(Normalize(start.Name)) || Normalize(start.Name).Contains(Normalize(request.Start));
        var destinationMatched = Normalize(request.Destination).Contains(Normalize(destination.Name)) || Normalize(destination.Name).Contains(Normalize(request.Destination));

        if (startMatched && destinationMatched)
        {
            return null;
        }

        return "Some typed locations were matched to the closest supported Gauteng demo area. Try Orlando West, Bara Mall, Diepkloof, Meadowlands, Pimville, Maponya Mall, Jabulani Mall, Johannesburg CBD, Braamfontein, Alexandra, Sandton, Midrand, Kempton Park, or Tembisa.";
    }

    private static CrimeArea Find(string name)
    {
        return JohannesburgCrimeAreas.First(area => area.Name == name);
    }

    private static string Normalize(string? value)
    {
        return (value ?? "").Trim().ToLowerInvariant();
    }

    private static MapPoint Point(double latitude, double longitude)
    {
        return new MapPoint { Latitude = latitude, Longitude = longitude };
    }
}
