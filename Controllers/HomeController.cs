using Microsoft.AspNetCore.Mvc;
using SafeRoute.Models;
using SafeRoute.Services;

namespace SafeRoute.Controllers;

public class HomeController : Controller
{
    private readonly RouteService _routeService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(RouteService routeService, ILogger<HomeController> logger)
    {
        _routeService = routeService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new RouteRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FindRoute(RouteRequest request)
    {
        if (!ModelState.IsValid)
            return View("Index", request);

        try
        {
            var result = await _routeService.GetSafeRoutesAsync(request);
            return View("Results", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route calculation failed for {Origin} -> {Destination}",
                request.Origin, request.Destination);
            ModelState.AddModelError("", "Could not calculate routes. Please check your addresses and try again.");
            return View("Index", request);
        }
    }

    // JSON API endpoint — frontend team can call this instead of the form POST
    [HttpPost]
    public async Task<IActionResult> Api([FromBody] RouteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _routeService.GetSafeRoutesAsync(request);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API route calculation failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public IActionResult About() => View();
}
