using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SafeRouteMvc.Models;
using SafeRouteMvc.Services;

namespace SafeRouteMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ISafeRouteService _safeRouteService;
    private readonly IRouteAiService _routeAiService;

    public HomeController(
        ILogger<HomeController> logger,
        ISafeRouteService safeRouteService,
        IRouteAiService routeAiService)
    {
        _logger = logger;
        _safeRouteService = safeRouteService;
        _routeAiService = routeAiService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = _safeRouteService.BuildRoute(new SafeRouteRequest());
        model.AiExplanation = await _routeAiService.ExplainRouteAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FindSafeRoute(SafeRouteRequest request, CancellationToken cancellationToken)
    {
        var model = _safeRouteService.BuildRoute(request);
        model.AiExplanation = await _routeAiService.ExplainRouteAsync(model, cancellationToken);
        return View("Index", model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
