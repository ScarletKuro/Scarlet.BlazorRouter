using System.Diagnostics.CodeAnalysis;

namespace Scarlet.BlazorRouter;

public sealed class BlazorRouteDefinition
{
    public BlazorRouteDefinition()
    {
    }

    [SetsRequiredMembers]
    public BlazorRouteDefinition(string template, Type pageType)
    {
        Template = template;
        PageType = pageType;
    }

    public required string Template { get; init; }

    public required Type PageType { get; init; }
}
