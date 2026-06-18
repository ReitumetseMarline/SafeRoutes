using SafeRouteMvc.Models;

namespace SafeRouteMvc.Services;

public interface ISafeRouteService
{
    SafeRouteViewModel BuildRoute(SafeRouteRequest request);
}
