# Vendored ASP.NET Core routing sources

**Do not edit anything in this directory.** Every `.cs` file here is copied verbatim from
[dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) at tag **v10.0.6**. `tools/sync-aspnetcore-routing.ps1 -Verify`
fails the build if any of them differs from `tools/aspnetcore-routing.sha256`.

## Why

`Scarlet.BlazorRouter` needs a route template parser, a matcher, precedence ordering and inline constraints. The only
standalone NuGet shipping of those is `Microsoft.AspNetCore.Routing` **2.3.0** — from 3.0 onward routing exists solely
inside the `Microsoft.AspNetCore.App` shared framework. Referencing 2.3.0 forces eight unusable ASP.NET Core 2.x
packages (`Microsoft.AspNetCore.Http`, `.Http.Features`, `.WebUtilities`, `Microsoft.Net.Http.Headers`, …) onto every
Blazor WebAssembly, MAUI, WPF and WinForms consumer, none of which can load them.

Microsoft hit the same wall building the framework's own `Router`, and solved it by compiling the routing sources
straight into `Microsoft.AspNetCore.Components.dll` as `internal` types. The file list lives upstream at
`src/Components/Components/src/Microsoft.AspNetCore.Components.Routing.targets`; `tools/aspnetcore-routing.files.txt`
mirrors it. The switch is the `COMPONENTS` compilation symbol, set in `Scarlet.BlazorRouter.csproj`: it flips every type
to `internal` and strips the `HttpContext`, `IRouter`, `Routing.Matching`, `ObjectPool` and `PropertyHelper` code paths
that only make sense inside a web host.

Because this is the same code the built-in `Router` runs, matching, precedence and parameter conversion behave
identically — no reimplementation to keep in sync with Blazor's behaviour.

## Layout

| Directory | Contents |
| --- | --- |
| *(root)* | `RouteValueDictionary`, constraint resolution, `RouteOptions`, `PathTokenizer` |
| `Tree/` | The tree router used for matching (link-generation halves excluded) |
| `Patterns/` | `RoutePattern` parsing plus `RoutePrecedence` |
| `Constraints/` | Inline route constraints (`int`, `guid`, `regex`, …) |
| `Shared/` | Small helpers the files above reference (`LinkerFlags`, `UrlDecoder`, debug views) |
| `Components/` | The Blazor-side types the `COMPONENTS` variants bind to (`RouteContext`, `PathString`, …) |

One piece is *not* vendored: `Microsoft.AspNetCore.Components.Routing.Resources`. Upstream generates it from a `.resx`
with an Arcade MSBuild task that does not exist outside the dotnet/aspnetcore build, so it is hand-written at
`../Internal/RoutingResources.cs` with the message text copied verbatim.

## Updating

```pwsh
./tools/sync-aspnetcore-routing.ps1 -Tag v10.0.7   # then bump the script's default -Tag
```

Re-check `RoutingResources.cs` against the upstream `Resources.resx` if the new tag changes any message.

## Licence

These files are © .NET Foundation and contributors, licensed under the MIT Licence — the same licence as this
repository. See the header on each file.
