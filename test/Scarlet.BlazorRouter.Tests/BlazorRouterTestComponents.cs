using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;

namespace Scarlet.BlazorRouter.Tests;

public sealed class HomePage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h1");
        builder.AddContent(1, "Home");
        builder.CloseElement();
    }
}

public sealed class ProductPage : ComponentBase
{
    [Parameter] public int Id { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, $"Product:{Id}:{Id.GetType().Name}");
        builder.CloseElement();
    }
}

public sealed class OptionalProductPage : ComponentBase
{
    [Parameter] public string? Id { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, Id is null ? "Optional:null" : $"Optional:{Id}");
        builder.CloseElement();
    }
}

public sealed class SlugPage : ComponentBase
{
    [Parameter] public string? Slug { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, $"Slug:{Slug}");
        builder.CloseElement();
    }
}

public sealed class CatchAllPage : ComponentBase
{
    [Parameter] public string? Path { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, $"CatchAll:{Path}");
        builder.CloseElement();
    }
}

public sealed class MultiTemplatePage : ComponentBase
{
    [Parameter] public string? Id { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, Id is null ? "Multi:null" : $"Multi:{Id}");
        builder.CloseElement();
    }
}

public sealed class LayoutAwarePage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "section");
        builder.AddContent(1, "Inside layout");
        builder.CloseElement();
    }
}

[Authorize]
public sealed class AuthorizedPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Authorized content");
        builder.CloseElement();
    }
}

public sealed class NoRouteNotFoundPage : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "No route not found");
        builder.CloseElement();
    }
}

public sealed class TestLayout : LayoutComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "layout");
        builder.AddContent(2, Body);
        builder.CloseElement();
    }
}

public sealed class TestRoutingStateProvider(RouteData? routeData) : IRoutingStateProvider
{
    public RouteData? RouteData { get; } = routeData;
}
