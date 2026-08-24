# Scarlet.BlazorRouter
[![NuGet](https://img.shields.io/nuget/v/Scarlet.BlazorRouter?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Scarlet.BlazorRouter/)
[![NuGet](https://img.shields.io/nuget/dt/Scarlet.BlazorRouter?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Scarlet.BlazorRouter/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Scarlet.BlazorRouter?color=594ae2&logo=github)](https://github.com/ScarletKuro/Scarlet.BlazorRouter/blob/main/LICENSE)

`Scarlet.BlazorRouter` is an explicit-route alternative to Blazor's built-in `Router`.

Instead of scanning assemblies for `@page` / `[Route]` attributes, you provide the allowed routes yourself.

This is useful when:

- the available pages depend on runtime conditions
- you want one boot mode to expose route set `A` and another to expose route set `B`
- you want routing to be explicit instead of assembly-discovered

## What It Keeps

The library is designed to keep normal Blazor page rendering behavior after route selection:

- native route-template semantics
- native `RouteData`
- `RouteView`
- `AuthorizeRouteView`
- `FocusOnNavigate`
- page-level `@layout`
- `OnNavigateAsync`
- `Navigating`

So the main thing that changes is route discovery, not route rendering.

## Quick Start

Replace the built-in router with `BlazorRouter` and pass an explicit route list:

```razor
<BlazorRouter Routes="@Routes" NotFoundPage="@typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
</BlazorRouter>

@code {
    private static readonly IReadOnlyList<BlazorRouteDefinition> Routes =
    [
        new("/", typeof(Pages.Home)),
        new("/counter", typeof(Pages.Counter)),
        new("/products/{id:int}", typeof(Pages.Product)),
        new("/docs/{*path}", typeof(Pages.Docs)),
    ];
}
```

## Public API

### `BlazorRouter`

- `Routes : IReadOnlyList<BlazorRouteDefinition>` required
- `Found : RenderFragment<RouteData>` required
- `NotFoundPage : Type?` optional
- `Navigating : RenderFragment?` optional
- `OnNavigateAsync : EventCallback<BlazorNavigationContext>` optional

### `BlazorRouteDefinition`

- `Template : string` required
- `PageType : Type` required

### `BlazorNavigationContext`

- `Path : string`
- `CancellationToken : CancellationToken`

## Route Definitions

Routes support the same template style you would normally use in Blazor page directives:

```csharp
new("/", typeof(Pages.Home))
new("/products/{id}", typeof(Pages.Product))
new("/products/{id:int}", typeof(Pages.Product))
new("/products/{id?}", typeof(Pages.Product))
new("/docs/{*path}", typeof(Pages.Docs))
```

Supported constraint conversions:

- `bool`
- `datetime`
- `decimal`
- `double`
- `float`
- `guid`
- `int`
- `long`

Query string and hash are ignored for matching, just like the native router.

## Layouts

You do not need to add layout information to `BlazorRouteDefinition`.

If you render matched pages through `RouteView`, page-level `@layout` continues to work automatically:

```razor
@layout DoctorWhoLayout
```

`RouteView` still resolves layout from `RouteData.PageType`, so this behaves the same as normal Blazor routing.

## `OnNavigateAsync`

The library uses its own navigation context type:

```csharp
private async Task OnNavigateAsync(BlazorNavigationContext context)
{
    if (context.Path == "products/42")
    {
        await LoadSomethingAsync(context.CancellationToken);
    }
}
```

This avoids depending on the framework's internal `NavigationContext` constructor.

## Conditional Boot Example

This is the main scenario the library is built for:

```csharp
private IReadOnlyList<BlazorRouteDefinition> Routes =>
    IsAdminMode
        ? new[]
        {
            new BlazorRouteDefinition("/", typeof(Pages.Home)),
            new BlazorRouteDefinition("/admin", typeof(Pages.Admin)),
        }
        : new[]
        {
            new BlazorRouteDefinition("/", typeof(Pages.Home)),
        };
```

If a route isn't in the current list, it doesn't exist for that boot mode.

## Hosting Models

### Standalone WebAssembly

No extra server-side route mapping is needed.

Use `BlazorRouter` in `App.razor` and supply the explicit route list.

Sample:
- [samples/Scarlet.BlazorRouter.WasmStandaloneSample/App.razor](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.WasmStandaloneSample/App.razor)
- [samples/Scarlet.BlazorRouter.WasmStandaloneSample/Program.cs](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.WasmStandaloneSample/Program.cs)

### Interactive Server / Blazor Web App

For server-hosted routing, ASP.NET Core also needs endpoint mappings for the allowed pages.

Use the endpoint extension together with `MapRazorComponents(...)`:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddBlazorRouterRoutes(RouteCatalog.Definitions);
```

Then use the same route list inside `BlazorRouter`.

This removes the need for the host component to declare:

```razor
@page "/"
@page "/{*path:nonfile}"
```

Sample:
- [samples/Scarlet.BlazorRouter.InteractiveServerSample/Program.cs](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.InteractiveServerSample/Program.cs)
- [samples/Scarlet.BlazorRouter.InteractiveServerSample/Components/RouteCatalog.cs](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.InteractiveServerSample/Components/RouteCatalog.cs)
- [samples/Scarlet.BlazorRouter.InteractiveServerSample/Components/Routes.razor](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.InteractiveServerSample/Components/Routes.razor)

## Do Pages Still Need `@page`?

For the client-side router behavior: no.

For interactive server endpoint mapping with `AddBlazorRouterRoutes(...)`: also no.

That means your pages can be plain components referenced only from the explicit route list.

## Compatibility Notes

- `RouteView` works because the router returns normal `RouteData`.
- `AuthorizeRouteView` works for the same reason.
- `FocusOnNavigate` works unchanged.
- `NotFoundPage` can be any component type. It does not need `@page`.

## Dependencies

The package has exactly one dependency: `Microsoft.AspNetCore.Components.Web`.

Route matching normally means referencing `Microsoft.AspNetCore.Routing`, whose last standalone release is **2.3.0** —
everything after it lives only in the `Microsoft.AspNetCore.App` shared framework. Taking that reference would push
eight ASP.NET Core 2.x packages (`Microsoft.AspNetCore.Http`, `.Http.Features`, `.WebUtilities`,
`Microsoft.Net.Http.Headers`, …) onto Blazor WebAssembly, MAUI, WPF and WinForms apps that cannot use any of them.

Instead this library does what `Microsoft.AspNetCore.Components.dll` itself does: it compiles the ASP.NET Core routing
sources directly into its own assembly as `internal` types, under the same `COMPONENTS` compilation symbol Microsoft
uses. Matching, precedence, inline constraints and parameter conversion are therefore the exact code the built-in
`Router` runs, not a reimplementation. See [`src/Scarlet.BlazorRouter/Routing/README.md`](src/Scarlet.BlazorRouter/Routing/README.md)
for provenance and how to refresh those files.

## Validation Rules

The library throws if:

- `Routes` is missing
- `Found` is missing
- a route template is empty
- `PageType` does not implement `IComponent`
- route definitions are ambiguous under native-style matching rules

## Samples

- Interactive server sample: [samples/Scarlet.BlazorRouter.InteractiveServerSample](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.InteractiveServerSample)
- Standalone WASM sample: [samples/Scarlet.BlazorRouter.WasmStandaloneSample](C:/Users/Kuro/source/repos/Scarlet.BlazorRouter/samples/Scarlet.BlazorRouter.WasmStandaloneSample)

## Current Target

- `net10.0`

## Important Note

The router itself is explicit and does not depend on `[Route]` discovery.

For interactive server endpoint registration, `AddBlazorRouterRoutes(...)` integrates with ASP.NET Core's Razor component endpoint pipeline so the same explicit route list can drive both:

- initial server request routing
- interactive client-side routing after boot
