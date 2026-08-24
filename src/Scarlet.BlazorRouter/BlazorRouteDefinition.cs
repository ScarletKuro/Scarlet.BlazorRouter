using System.Diagnostics.CodeAnalysis;

namespace Scarlet.BlazorRouter;

public sealed class BlazorRouteDefinition
{
    public BlazorRouteDefinition()
    {
    }

    [SetsRequiredMembers]
    public BlazorRouteDefinition(
        string template,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type pageType)
    {
        Template = template;
        PageType = pageType;
    }

    public required string Template { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public required Type PageType { get; init; }
}
