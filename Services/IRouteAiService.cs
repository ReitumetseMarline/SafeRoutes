using SafeRouteMvc.Models;

namespace SafeRouteMvc.Services;

public interface IRouteAiService
{
    Task<AiRouteExplanation> ExplainRouteAsync(SafeRouteViewModel route, CancellationToken cancellationToken);
}
