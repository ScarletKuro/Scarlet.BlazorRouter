using System.Reflection;

namespace Scarlet.BlazorRouter;

public static class BlazorRouterEndpointConventionBuilderExtensions
{
    public static TBuilder AddBlazorRouterRoutes<TBuilder>(this TBuilder builder, IReadOnlyList<BlazorRouteDefinition> routes)
        where TBuilder : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(routes);

        var builderType = builder.GetType();
        var applicationBuilderProperty = builderType.GetProperty(
            "ApplicationBuilder",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (applicationBuilderProperty?.GetValue(builder) is { } applicationBuilder)
        {
            ApplyRoutesToApplicationBuilder(applicationBuilder, builderType.Assembly, routes);
            return builder;
        }

        var componentApplicationBuilderActionsProperty = builderType.GetProperty(
            "ComponentApplicationBuilderActions",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (componentApplicationBuilderActionsProperty?.GetValue(builder) is System.Collections.IList actions)
        {
            var applicationBuilderType = builderType.Assembly.GetType("Microsoft.AspNetCore.Components.Discovery.ComponentApplicationBuilder")
                ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core component application builder type.");

            var delegateFactory = new ComponentApplicationBuilderActionFactory(builderType.Assembly, routes);
            var delegateType = typeof(Action<>).MakeGenericType(applicationBuilderType);
            var method = typeof(ComponentApplicationBuilderActionFactory)
                .GetMethod(nameof(ComponentApplicationBuilderActionFactory.Apply), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(applicationBuilderType);

            actions.Add(Delegate.CreateDelegate(delegateType, delegateFactory, method));
            return builder;
        }

        throw new InvalidOperationException(
            $"The provided builder type '{builderType.FullName}' does not expose the Razor component application builder.");
    }

    private static void ApplyRoutesToApplicationBuilder(object applicationBuilder, Assembly endpointsAssembly, IReadOnlyList<BlazorRouteDefinition> routes)
    {
        var applicationBuilderType = applicationBuilder.GetType();
        var pagesProperty = applicationBuilderType.GetProperty(
            "Pages",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (pagesProperty?.GetValue(applicationBuilder) is not { } pagesBuilder)
        {
            throw new InvalidOperationException(
                $"The Razor component application builder type '{applicationBuilderType.FullName}' does not expose page collection support.");
        }

        var pagesBuilderType = pagesBuilder.GetType();
        var pageComponentBuilderType = endpointsAssembly.GetType("Microsoft.AspNetCore.Components.Discovery.PageComponentBuilder")
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page component builder type.");

        var removeFromAssemblyMethod = pagesBuilderType.GetMethod(
            "RemoveFromAssembly",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(string)],
            modifiers: null)
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page removal method.");

        var addFromLibraryInfoMethod = pagesBuilderType.GetMethod(
            "AddFromLibraryInfo",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(IReadOnlyList<>).MakeGenericType(pageComponentBuilderType)],
            modifiers: null)
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page registration method.");

        var assemblyNameProperty = pageComponentBuilderType.GetProperty(
            "AssemblyName",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page builder assembly name property.");

        var routeTemplatesProperty = pageComponentBuilderType.GetProperty(
            "RouteTemplates",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page builder route templates property.");

        var pageTypeProperty = pageComponentBuilderType.GetProperty(
            "PageType",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the ASP.NET Core page builder page type property.");

        var explicitPagesByAssembly = routes
            .GroupBy(route => route.PageType.Assembly.GetName().Name!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(route => route.PageType)
                    .Select(pageGroup =>
                    {
                        var pageBuilder = Activator.CreateInstance(pageComponentBuilderType)
                            ?? throw new InvalidOperationException("Unable to create the ASP.NET Core page component builder.");

                        assemblyNameProperty.SetValue(pageBuilder, group.Key);
                        routeTemplatesProperty.SetValue(pageBuilder, pageGroup.Select(route => route.Template).ToArray());
                        pageTypeProperty.SetValue(pageBuilder, pageGroup.Key);
                        return pageBuilder;
                    })
                    .ToArray(),
                StringComparer.Ordinal);

        foreach (var entry in explicitPagesByAssembly)
        {
            removeFromAssemblyMethod.Invoke(pagesBuilder, [entry.Key]);

            var listType = typeof(List<>).MakeGenericType(pageComponentBuilderType);
            var list = Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("Unable to create the ASP.NET Core page component builder list.");

            var addMethod = listType.GetMethod("Add")
                ?? throw new InvalidOperationException("Unable to add explicit page components to the ASP.NET Core page collection.");

            foreach (var pageBuilder in entry.Value)
            {
                addMethod.Invoke(list, [pageBuilder]);
            }

            addFromLibraryInfoMethod.Invoke(pagesBuilder, [entry.Key, list]);
        }

    }

    private sealed class ComponentApplicationBuilderActionFactory(Assembly endpointsAssembly, IReadOnlyList<BlazorRouteDefinition> routes)
    {
        private readonly Assembly _endpointsAssembly = endpointsAssembly;
        private readonly IReadOnlyList<BlazorRouteDefinition> _routes = routes;

        internal void Apply<TApplicationBuilder>(TApplicationBuilder applicationBuilder)
        {
            ArgumentNullException.ThrowIfNull(applicationBuilder);
            ApplyRoutesToApplicationBuilder(applicationBuilder, _endpointsAssembly, _routes);
        }
    }
}
