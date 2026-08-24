using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Scarlet.BlazorRouter.Tests;

public sealed class BlazorRouterEndpointMappingTests
{
    [Fact]
    public async Task ExplicitRoutesBecomeServerEndpointsWithoutPageDirectives()
    {
        await using var app = await BuildAppAsync(
            routes:
            [
                new("/", typeof(HomePage)),
                new("/products/{id:int}", typeof(ProductPage)),
            ]);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/products/42");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Product:42:Int32", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RoutesExcludedFromExplicitMapReturnNotFound()
    {
        await using var app = await BuildAppAsync(
            routes:
            [
                new("/", typeof(HomePage)),
            ]);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/products/42");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> BuildAppAsync(IReadOnlyList<BlazorRouteDefinition> routes)
    {
        EndpointTestApp.Routes = routes;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRazorComponents();

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorComponents<EndpointTestApp>()
            .AddBlazorRouterRoutes(routes);

        await app.StartAsync();
        return app;
    }

    private sealed class EndpointTestApp : ComponentBase
    {
        public static IReadOnlyList<BlazorRouteDefinition> Routes { get; set; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BlazorRouter>(0);
            builder.AddAttribute(1, nameof(BlazorRouter.Routes), Routes);
            builder.AddAttribute(2, nameof(BlazorRouter.Found), Found);
            builder.AddAttribute(3, nameof(BlazorRouter.NotFoundPage), typeof(NoRouteNotFoundPage));
            builder.CloseComponent();
        }

        private static RenderFragment<RouteData> Found => routeData => builder =>
        {
            builder.OpenComponent<RouteView>(0);
            builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
            builder.CloseComponent();
        };
    }
}
