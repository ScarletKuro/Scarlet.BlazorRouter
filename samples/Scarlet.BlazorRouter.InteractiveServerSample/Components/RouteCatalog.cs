using Scarlet.BlazorRouter.InteractiveServerSample.Components.Pages;

namespace Scarlet.BlazorRouter.InteractiveServerSample.Components;

internal static class RouteCatalog
{
    public static IReadOnlyList<BlazorRouteDefinition> Definitions { get; } =
    [
        new("/", typeof(Home)),
        new("/counter", typeof(Counter)),
        new("/weather", typeof(Weather)),
    ];
}
