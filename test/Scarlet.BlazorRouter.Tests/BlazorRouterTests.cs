using System.Security.Claims;
using System.Reflection;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Scarlet.BlazorRouter;

namespace Scarlet.BlazorRouter.Tests;

public sealed class BlazorRouterTests : BunitContext
{
    [Fact]
    public void RendersExactLiteralMatch()
    {
        NavigateTo("/");

        var cut = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
            ]);

        cut.Find("h1").MarkupMatches("<h1>Home</h1>");
    }

    [Fact]
    public void ConvertsTypedConstraintParameters()
    {
        NavigateTo("/products/42");

        var cut = RenderRouter(
            routes:
            [
                new("/products/{id:int}", typeof(ProductPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Product:42:Int32</p>");
    }

    [Fact]
    public void SupportsOptionalParameters()
    {
        NavigateTo("/products");

        var cut = RenderRouter(
            routes:
            [
                new("/products/{id?}", typeof(OptionalProductPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Optional:null</p>");
    }

    [Fact]
    public void SupportsCatchAllRoutes()
    {
        NavigateTo("/files/docs/2026/report");

        var cut = RenderRouter(
            routes:
            [
                new("/files/{*path}", typeof(CatchAllPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>CatchAll:docs/2026/report</p>");
    }

    [Fact]
    public void IgnoresQueryStringAndHashWhenMatching()
    {
        NavigateTo("/products/42?tab=details#summary");

        var cut = RenderRouter(
            routes:
            [
                new("/products/{id:int}", typeof(ProductPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Product:42:Int32</p>");
    }

    [Fact]
    public void DecodesEncodedSlashValuesForSimpleParameters()
    {
        NavigateTo("/docs/guide%2Fintro");

        var cut = RenderRouter(
            routes:
            [
                new("/docs/{slug}", typeof(SlugPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Slug:guide/intro</p>");
    }

    [Fact]
    public void DecodesEncodedSlashValuesForCatchAllAfterMatch()
    {
        NavigateTo("/docs/guide%2Fintro");

        var cut = RenderRouter(
            routes:
            [
                new("/docs/{*path}", typeof(CatchAllPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>CatchAll:guide/intro</p>");
    }

    [Fact]
    public void DecodesPercentEncodedPathBeforeMatching()
    {
        NavigateTo("/docs/hello%20world");

        var cut = RenderRouter(
            routes:
            [
                new("/docs/{slug}", typeof(SlugPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Slug:hello world</p>");
    }

    [Fact]
    public void DecodesPercentEncodedLiteralSegments()
    {
        NavigateTo("/user%20guide/intro");

        var cut = RenderRouter(
            routes:
            [
                new("/user guide/{slug}", typeof(SlugPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Slug:intro</p>");
    }

    [Fact]
    public void AppliesRegexConstraints()
    {
        NavigateTo("/codes/abc");

        var cut = RenderRouter(
            routes:
            [
                new("/codes/{slug:regex(^[a-z]+$)}", typeof(SlugPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Slug:abc</p>");
    }

    [Fact]
    public void DoesNotMatchWhenRegexConstraintRejectsTheValue()
    {
        NavigateTo("/codes/123");

        var cut = RenderRouter(
            routes:
            [
                new("/codes/{slug:regex(^[a-z]+$)}", typeof(SlugPage)),
            ],
            notFoundPage: typeof(NoRouteNotFoundPage));

        cut.Find("p").MarkupMatches("<p>No route not found</p>");
    }

    [Fact]
    public void AddsNullForUnusedRouteParametersAcrossTemplates()
    {
        NavigateTo("/dashboard");

        var cut = RenderRouter(
            routes:
            [
                new("/dashboard/{id}", typeof(MultiTemplatePage)),
                new("/dashboard", typeof(MultiTemplatePage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Multi:null</p>");
    }

    [Fact]
    public void ThrowsForAmbiguousRoutes()
    {
        NavigateTo("/anything/value");

        var exception = Assert.Throws<InvalidOperationException>(() => RenderRouter(
            routes:
            [
                new("/items/{id}", typeof(HomePage)),
                new("/items/{value}", typeof(ProductPage)),
            ]));

        Assert.Contains("ambiguous", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("items/{id}", exception.Message, StringComparison.Ordinal);
        Assert.Contains("items/{value}", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(HomePage).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ProductPage).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersNotFoundPageWithoutRouteAttribute()
    {
        NavigateTo("/missing");

        var cut = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
            ],
            notFoundPage: typeof(NoRouteNotFoundPage));

        cut.Find("p").MarkupMatches("<p>No route not found</p>");
    }

    [Fact]
    public void UsesRoutingStateProviderWhenItMatchesAnExplicitRoute()
    {
        Services.AddSingleton<IRoutingStateProvider>(new TestRoutingStateProvider(new RouteData(
            typeof(ProductPage),
            new Dictionary<string, object?> { ["Id"] = "42" })
        {
            Template = "/products/{id:int}",
        }));

        NavigateTo("/products/42");

        var cut = RenderRouter(
            routes:
            [
                new("/products/{id:int}", typeof(ProductPage)),
            ]);

        cut.Find("p").MarkupMatches("<p>Product:42:Int32</p>");
    }

    [Fact]
    public void RespectsConditionalBootRouteSet()
    {
        NavigateTo("/admin");

        var bootA = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
            ],
            notFoundPage: typeof(NoRouteNotFoundPage));

        bootA.Find("p").MarkupMatches("<p>No route not found</p>");

        bootA.Dispose();
        NavigateTo("/admin");

        var bootB = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
                new("/admin", typeof(LayoutAwarePage)),
            ],
            notFoundPage: typeof(NoRouteNotFoundPage));

        bootB.Find("section").MarkupMatches("<section>Inside layout</section>");
    }

    [Fact]
    public async Task ShowsNavigatingAndCancelsPreviousNavigation()
    {
        NavigateTo("/");

        var firstNavigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondNavigation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedTokens = new List<CancellationToken>();

        var cut = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
                new("/products/{id:int}", typeof(ProductPage)),
            ],
            navigating: builder => builder.AddContent(0, "Loading..."),
            onNavigateAsync: async context =>
            {
                if (string.IsNullOrEmpty(context.Path))
                {
                    return;
                }

                observedTokens.Add(context.CancellationToken);
                var pending = string.Equals(context.Path, "products/1", StringComparison.Ordinal)
                    ? firstNavigation.Task
                    : secondNavigation.Task;

                await pending.WaitAsync(context.CancellationToken);
            });

        cut.Find("h1").MarkupMatches("<h1>Home</h1>");

        NavigateTo("/products/1");
        cut.WaitForAssertion(() => cut.MarkupMatches("Loading..."));

        NavigateTo("/products/2");
        cut.WaitForAssertion(() => Assert.Equal(2, observedTokens.Count));
        Assert.True(observedTokens[0].IsCancellationRequested);

        secondNavigation.SetResult();

        cut.WaitForAssertion(() => cut.Find("p").MarkupMatches("<p>Product:2:Int32</p>"));

        await Assert.ThrowsAsync<TaskCanceledException>(() => firstNavigation.Task.WaitAsync(observedTokens[0]));
    }

    [Fact]
    public void WorksWithRouteViewAndDefaultLayout()
    {
        NavigateTo("/layout");

        var cut = RenderRouter(
            routes:
            [
                new("/layout", typeof(LayoutAwarePage)),
            ],
            found: RouteViewFound(defaultLayout: typeof(TestLayout)));

        cut.Find(".layout").MarkupMatches("<div class=\"layout\"><section>Inside layout</section></div>");
    }

    [Fact]
    public void WorksWithAuthorizeRouteView()
    {
        AddAuthorization().SetAuthorized("Kuro");
        NavigateTo("/secure");

        var cut = RenderRouter(
            routes:
            [
                new("/secure", typeof(AuthorizedPage)),
            ],
            found: AuthorizeRouteViewFound());

        cut.Find("p").MarkupMatches("<p>Authorized content</p>");
    }

    [Fact]
    public void WorksWithAuthorizeRouteViewUnauthorizedContent()
    {
        AddAuthorization().SetNotAuthorized();
        NavigateTo("/secure");

        var cut = RenderRouter(
            routes:
            [
                new("/secure", typeof(AuthorizedPage)),
            ],
            found: AuthorizeRouteViewFound());

        cut.MarkupMatches("Not authorized");
    }

    [Fact]
    public void ForceLoadsInterceptedNavigationWhenNoExplicitRouteMatches()
    {
        NavigateTo("/");

        var cut = RenderRouter(
            routes:
            [
                new("/", typeof(HomePage)),
            ],
            notFoundPage: typeof(NoRouteNotFoundPage));

        cut.Find("h1").MarkupMatches("<h1>Home</h1>");

        InvokeLocationChanged(cut, "http://localhost/missing", isNavigationIntercepted: true);

        var navigationManager = Services.GetRequiredService<BunitNavigationManager>();
        cut.WaitForAssertion(() =>
        {
            var latest = navigationManager.History.First();
            Assert.Equal("http://localhost/missing", latest.Uri);
            Assert.True(latest.Options.ForceLoad);
        });

        cut.Find("h1").MarkupMatches("<h1>Home</h1>");
    }

    private static RenderFragment<RouteData> RouteViewFound(Type? defaultLayout = null) => routeData => builder =>
    {
        builder.OpenComponent<RouteView>(0);
        builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
        if (defaultLayout is not null)
        {
            builder.AddAttribute(2, nameof(RouteView.DefaultLayout), defaultLayout);
        }

        builder.CloseComponent();
    };

    private static RenderFragment<RouteData> AuthorizeRouteViewFound() => routeData => builder =>
    {
        builder.OpenComponent<AuthorizeRouteView>(0);
        builder.AddAttribute(1, nameof(AuthorizeRouteView.RouteData), routeData);
        builder.AddAttribute(2, nameof(AuthorizeRouteView.NotAuthorized), (RenderFragment<AuthenticationState>)(_ => notAuthorizedBuilder =>
        {
            notAuthorizedBuilder.AddContent(0, "Not authorized");
        }));
        builder.CloseComponent();
    };

    private IRenderedComponent<BlazorRouter> RenderRouter(
        IReadOnlyList<BlazorRouteDefinition> routes,
        Type? notFoundPage = null,
        RenderFragment<RouteData>? found = null,
        RenderFragment? navigating = null,
        Func<BlazorNavigationContext, Task>? onNavigateAsync = null)
    {
        return Render<BlazorRouter>(parameters =>
        {
            parameters.Add(component => component.Routes, routes);
            parameters.Add(component => component.Found, found ?? RouteViewFound());

            if (notFoundPage is not null)
            {
                parameters.Add(component => component.NotFoundPage, notFoundPage);
            }

            if (navigating is not null)
            {
                parameters.Add(component => component.Navigating, navigating);
            }

            if (onNavigateAsync is not null)
            {
                parameters.Add(component => component.OnNavigateAsync, EventCallback.Factory.Create<BlazorNavigationContext>(this, onNavigateAsync));
            }
        });
    }

    private void NavigateTo(string uri)
    {
        Services.GetRequiredService<BunitNavigationManager>().NavigateTo(uri);
    }

    private static void InvokeLocationChanged(IRenderedComponent<BlazorRouter> cut, string location, bool isNavigationIntercepted)
    {
        typeof(BlazorRouter)
            .GetMethod("OnLocationChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(cut.Instance, [null, new LocationChangedEventArgs(location, isNavigationIntercepted)]);
    }
}
