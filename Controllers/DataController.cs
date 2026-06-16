using Microsoft.AspNetCore.Mvc;
using SafeRoute.Services;

namespace SafeRoute.Controllers;

public class DataController : Controller
{
    private readonly CrimeDataService _crimeData;

    public DataController(CrimeDataService crimeData)
    {
        _crimeData = crimeData;
    }

    // Ethical safeguard: transparent data endpoint so users can inspect the source data
    [HttpGet]
    public IActionResult Transparency()
    {
        var incidents = _crimeData.GetIncidentsNear(-26.2041, 28.0473, radiusKm: 500);
        return Json(incidents);
    }

    // API endpoint for the frontend to get nearby incidents as JSON
    [HttpGet]
    public IActionResult NearbyIncidents(double lat, double lon, double radiusKm = 3)
    {
        var incidents = _crimeData.GetIncidentsNear(lat, lon, radiusKm);
        return Json(incidents);
    }
}
